using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZakYip.Singulation.Infrastructure.Cabinet {

    /// <summary>
    /// 帧保护器，负责检测重复帧、监控心跳超时，并在降级模式下调整速度。
    /// </summary>
    public sealed class FrameGuard : IFrameGuard {
        private readonly ILogger<FrameGuard> _log;
        private readonly ICabinetPipeline _safety;
        private readonly FrameGuardOptions _options;
        private readonly IUpstreamFrameHub _hub;
        private readonly IUpstreamOptionsStore _upstreamOptionsStore;

        // 序列窗口：用于检测重复序列号
        private readonly Queue<int> _window = new();
        private readonly HashSet<int> _seen = new();
        private readonly object _gate = new();

        private DateTime _lastHeartbeatUtc = DateTime.UtcNow;
        private IDisposable? _heartbeatSubscription;
        private Task? _heartbeatTask;
        private Task? _watchdogTask;
        private CancellationTokenSource? _internalCts;
        private volatile bool _heartbeatDegraded;

        /// <summary>
        /// 初始化 <see cref="FrameGuard"/> 类的新实例。
        /// </summary>
        /// <param name="log">日志记录器。</param>
        /// <param name="safety">安全管道。</param>
        /// <param name="options">帧保护选项。</param>
        /// <param name="hub">上游帧中心。</param>
        /// <param name="upstreamOptionsStore">上游选项存储。</param>
        public FrameGuard(
            ILogger<FrameGuard> log,
            ICabinetPipeline safety,
            IOptions<FrameGuardOptions> options,
            IUpstreamFrameHub hub,
            IUpstreamOptionsStore upstreamOptionsStore) {
            _log = log;
            _safety = safety;
            _options = options.Value;
            _hub = hub;
            _upstreamOptionsStore = upstreamOptionsStore;
            _safety.StateChanged += OnSafetyStateChanged;
        }

        /// <inheritdoc />
        public async ValueTask<bool> InitializeAsync(CancellationToken ct) {
            if (_internalCts is not null) return false;

            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            
            // Check if heartbeat port is 0, if so skip heartbeat monitoring
            var upstreamOptions = await _upstreamOptionsStore.GetAsync(ct).ConfigureAwait(false);
            if (upstreamOptions.HeartbeatPort == 0) {
                _log.LogInformation("Heartbeat port is 0, skipping heartbeat timeout monitoring.");
                return true;
            }
            
            var (reader, disposer) = _hub.SubscribeHeartbeat(128);
            _heartbeatSubscription = disposer;
            _heartbeatTask = Task.Run(() => RunHeartbeatAsync(reader, _internalCts.Token), ct);
            _watchdogTask = Task.Run(() => RunHeartbeatWatchdogAsync(_internalCts.Token), ct);
            return true;
        }

        /// <inheritdoc />
        public FrameGuardDecision Evaluate(SpeedSet set) {
            var state = _safety.State;
            // 如果系统处于隔离状态，直接拒绝所有帧
            if (state == CabinetIsolationState.Isolated) {
                SingulationMetrics.Instance.FrameDroppedCounter.Add(1,
                    new KeyValuePair<string, object?>("reason", "isolated"));
                return new FrameGuardDecision(false, set, false, "isolated");
            }

            // 检查序列号，拒绝重复的帧
            var accepted = AcceptSequence(set.Sequence);
            if (!accepted) {
                SingulationMetrics.Instance.FrameDroppedCounter.Add(1,
                    new KeyValuePair<string, object?>("reason", "sequence"));
                return new FrameGuardDecision(false, set, false, "duplicate");
            }

            // 【关键修复】移除速度降级逻辑
            // 无论系统处于何种状态（包括降级状态），速度都不应该被缩放
            // 速度必须始终保持与预期速度一致，不能偏离
            return new FrameGuardDecision(true, set, false, null);
        }

        /// <inheritdoc />
        public void ReportHeartbeat() => _lastHeartbeatUtc = DateTime.UtcNow;

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            try {
                _internalCts?.Cancel();
                if (_heartbeatTask is not null) await _heartbeatTask.ConfigureAwait(false);
                if (_watchdogTask is not null) await _watchdogTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally {
                _heartbeatSubscription?.Dispose();
                _internalCts?.Dispose();
                _safety.StateChanged -= OnSafetyStateChanged;
            }
        }

        /// <summary>
        /// 检查并接受序列号，防止重复帧。使用滑动窗口维护最近的序列号。
        /// </summary>
        /// <param name="sequence">帧序列号。</param>
        /// <returns>如果序列号可接受则返回 true，否则返回 false。</returns>
        private bool AcceptSequence(int sequence) {
            if (sequence <= 0) return true;
            lock (_gate) {
                if (_seen.Contains(sequence)) return false;
                _seen.Add(sequence);
                _window.Enqueue(sequence);
                while (_window.Count > Math.Max(1, _options.SequenceWindow)) {
                    var removed = _window.Dequeue();
                    _seen.Remove(removed);
                }
            }
            SingulationMetrics.Instance.FrameProcessedCounter.Add(1);
            return true;
        }

        /// <summary>
        /// 按指定系数缩放速度集合（用于降级模式）。
        /// </summary>
        /// <param name="set">原始速度集合。</param>
        /// <param name="factor">缩放系数（0-1之间）。</param>
        /// <param name="delta">输出平均速度差值。</param>
        /// <returns>缩放后的速度集合。</returns>
        private SpeedSet Scale(SpeedSet set, decimal factor, out double delta) {
            if (factor <= 0m) factor = 0.1m;
            var main = set.MainMmps ?? Array.Empty<int>();
            var eject = set.EjectMmps ?? Array.Empty<int>();
            var scaledMain = new int[main.Count];
            var scaledEject = new int[eject.Count];
            decimal diffSum = 0m;
            for (var i = 0; i < main.Count; i++) {
                var scaled = (int)Math.Round(main[i] * factor, MidpointRounding.AwayFromZero);
                diffSum += Math.Abs(main[i] - scaled);
                scaledMain[i] = scaled;
            }
            for (var i = 0; i < eject.Count; i++) {
                var scaled = (int)Math.Round(eject[i] * factor, MidpointRounding.AwayFromZero);
                diffSum += Math.Abs(eject[i] - scaled);
                scaledEject[i] = scaled;
            }
            var count = main.Count + eject.Count;
            delta = count > 0 ? (double)(diffSum / count) : 0d;
            return new SpeedSet(set.TimestampUtc, set.Sequence, scaledMain, scaledEject);
        }

        /// <summary>
        /// 运行心跳接收任务，持续监听心跳消息并更新心跳时间。
        /// </summary>
        private async Task RunHeartbeatAsync(ChannelReader<ReadOnlyMemory<byte>> reader, CancellationToken ct) {
            await foreach (var _ in reader.ReadAllAsync(ct)) {
                ReportHeartbeat();
                if (_heartbeatDegraded && _safety.TryRecoverFromDegraded("heartbeat")) {
                    _heartbeatDegraded = false;
                    _log.LogInformation("Heartbeat recovered, exiting degraded state.");
                }
            }
        }

        /// <summary>
        /// 运行心跳看门狗任务，定期检查心跳超时并触发降级。
        /// </summary>
        private async Task RunHeartbeatWatchdogAsync(CancellationToken ct) {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
                var elapsed = DateTime.UtcNow - _lastHeartbeatUtc;
                if (elapsed > _options.HeartbeatTimeout && !_heartbeatDegraded) {
                    _heartbeatDegraded = _safety.TryEnterDegraded(CabinetTriggerKind.HeartbeatTimeout, $"heartbeat timeout {elapsed.TotalMilliseconds:F0}ms");
                    if (_heartbeatDegraded) {
                        _log.LogWarning("Heartbeat timeout detected ({Elapsed} ms).", elapsed.TotalMilliseconds);
                        SingulationMetrics.Instance.HeartbeatTimeoutCounter.Add(1);
                    }
                }
            }
        }

        private void OnSafetyStateChanged(object? sender, CabinetStateChangedEventArgs e) {
            if (e.Current == CabinetIsolationState.Normal) {
                _heartbeatDegraded = false;
            }
        }
    }
}
