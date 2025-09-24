using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions.Events;

namespace ZakYip.Singulation.Drivers.Common {

    /// <summary>
    /// IAxisEventAggregator 的默认实现：
    /// <list type="bullet">
    /// <item>对每个 <see cref="IAxisDrive"/> 建立事件转发通道；</item>
    /// <item>使用逐订阅者非阻塞广播，订阅方异常不影响主流程；</item>
    /// <item>Attach/Detach 幂等，避免重复订阅与内存泄漏。</item>
    /// </list>
    /// </summary>
    public sealed class AxisEventAggregator : IAxisEventAggregator {

        // 记录每个 drive 的“订阅句柄”（事件委托集合），用于 Detach 时精确解绑
        private readonly ConcurrentDictionary<IAxisDrive, Subscriptions> _subs = new();

        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        /// <inheritdoc />
        public void Attach(IAxisDrive drive) {
            if (drive is null) throw new ArgumentNullException(nameof(drive));
            // 幂等：已存在则直接返回
            if (_subs.ContainsKey(drive)) return;

            // 为该 drive 创建一组订阅，并尝试加入字典（并发安全）
            var sub = new Subscriptions(this, drive);
            if (!_subs.TryAdd(drive, sub)) {
                // 已被其它线程添加，确保释放本地创建的订阅资源
                sub.Unsubscribe();
                return;
            }

            sub.Subscribe();
        }

        /// <inheritdoc />
        public void Detach(IAxisDrive drive) {
            if (_subs.TryRemove(drive, out var sub)) {
                sub.Unsubscribe();
            }
        }

        // ===== 统一的非阻塞广播 =====

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FireEachNonBlocking<T>(EventHandler<T>? multicast, object sender, T args) {
            if (multicast is null) return;

            foreach (var d in multicast.GetInvocationList()) {
                var h = (EventHandler<T>)d;
                var state = new EvState<T>(sender, h, args);
                ThreadPool.UnsafeQueueUserWorkItem(static s => {
                    var st = (EvState<T>)s!;
                    try { st.Handler(st.Sender, st.Args); }
                    catch { /* 订阅方异常被隔离，不反噬主流程 */ }
                }, state, preferLocal: true);
            }
        }

        private readonly struct EvState<T>(object sender, EventHandler<T> handler, T args) {
            public readonly object Sender = sender;
            public readonly EventHandler<T> Handler = handler;
            public readonly T Args = args;
        }

        // ===== 内部：每个 drive 的订阅集合 =====
        private sealed class Subscriptions {
            private readonly AxisEventAggregator _agg;
            private readonly IAxisDrive _drive;

            // 缓存委托，确保 Detach 能精确 -=
            private readonly EventHandler<AxisSpeedFeedbackEventArgs> _onSpeed;

            private readonly EventHandler<AxisCommandIssuedEventArgs> _onCmd;
            private readonly EventHandler<AxisErrorEventArgs> _onErr;
            private readonly EventHandler<AxisDisconnectedEventArgs> _onDisc;
            private readonly EventHandler<DriverNotLoadedEventArgs> _onDrvNotLoaded;

            public Subscriptions(AxisEventAggregator agg, IAxisDrive drive) {
                _agg = agg;
                _drive = drive;

                _onSpeed = (s, e) => FireEachNonBlocking(_agg.SpeedFeedback, _agg, e);
                _onCmd = (s, e) => FireEachNonBlocking(_agg.CommandIssued, _agg, e);
                _onErr = (s, e) => FireEachNonBlocking(_agg.AxisFaulted, _agg, e);
                _onDisc = (s, e) => FireEachNonBlocking(_agg.AxisDisconnected, _agg, e);
                _onDrvNotLoaded = (s, e) => FireEachNonBlocking(_agg.DriverNotLoaded, _agg, e);
            }

            public void Subscribe() {
                _drive.SpeedFeedback += _onSpeed;
                _drive.CommandIssued += _onCmd;
                _drive.AxisFaulted += _onErr;
                _drive.AxisDisconnected += _onDisc;
                _drive.DriverNotLoaded += _onDrvNotLoaded;
            }

            public void Unsubscribe() {
                _drive.SpeedFeedback -= _onSpeed;
                _drive.CommandIssued -= _onCmd;
                _drive.AxisFaulted -= _onErr;
                _drive.AxisDisconnected -= _onDisc;
                _drive.DriverNotLoaded -= _onDrvNotLoaded;
            }
        }
    }
}