using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Host.Runtime {

    /// <summary>
    /// 多订阅者广播 Hub：
    /// - 每个订阅者拥有独立的有界通道（DropOldest，SingleReader/SingleWriter）。
    /// - 发布时在同一次回调内将数据 TryWrite 到所有订阅者，避免额外排队造成“感知时间差”。
    /// - 不做任何 Task.Run；发布 O(N_subs) 尝试写入，失败则由各自通道策略丢旧。
    /// </summary>
    public sealed class UpstreamFrameHub : IUpstreamFrameHub {
        private readonly object _gateSpeed = new();
        private readonly List<Channel<ReadOnlyMemory<byte>>> _speedSubscribers = new();

        private readonly object _gateHeartbeat = new();
        private readonly List<Channel<ReadOnlyMemory<byte>>> _heartbeatSubscribers = new();

        public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe)
            SubscribeSpeed(int capacity = 256) {
            var ch = CreateBounded(capacity);
            lock (_gateSpeed) _speedSubscribers.Add(ch);
            lock (_gateSpeed) {
                return (ch.Reader, new Unsub(_speedSubscribers, ch, _gateSpeed));
            }
        }

        public void PublishSpeed(ReadOnlyMemory<byte> payload) {
            // 同步广播：在同一线程回调里 TryWrite 给所有订阅者
            lock (_gateSpeed) {
                var subs = _speedSubscribers;
                foreach (var t in subs) {
                    _ = t.Writer.TryWrite(payload);
                }
            }
        }

        public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe)
            SubscribeHeartbeat(int capacity = 64) {
            var ch = CreateBounded(capacity);
            lock (_gateHeartbeat) _heartbeatSubscribers.Add(ch);
            lock (_gateHeartbeat) {
                return (ch.Reader, new Unsub(_heartbeatSubscribers, ch, _gateHeartbeat));
            }
        }

        public void PublishHeartbeat(ReadOnlyMemory<byte> payload) {
            lock (_gateHeartbeat) {
                var subs = _heartbeatSubscribers;
                foreach (var t in subs) {
                    _ = t.Writer.TryWrite(payload);
                }
            }
        }

        private static Channel<ReadOnlyMemory<byte>> CreateBounded(int capacity) {
            var opt = new BoundedChannelOptions(capacity) {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
                AllowSynchronousContinuations = true
            };
            return Channel.CreateBounded<ReadOnlyMemory<byte>>(opt);
        }

        private sealed class Unsub : IDisposable {
            private readonly List<Channel<ReadOnlyMemory<byte>>> _list;
            private readonly Channel<ReadOnlyMemory<byte>> _ch;
            private readonly object _gate;
            private int _disposed;

            public Unsub(List<Channel<ReadOnlyMemory<byte>>> list,
                         Channel<ReadOnlyMemory<byte>> ch,
                         object gate) {
                _list = list; _ch = ch; _gate = gate;
            }

            public void Dispose() {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                lock (_gate) _list.Remove(_ch);
                _ch.Writer.TryComplete(); // 结束该订阅者通道
            }
        }
    }
}