using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZakYip.Singulation.Host.Safety {

    public sealed class FrameGuard : IFrameGuard {
        private readonly ILogger<FrameGuard> _log;
        private readonly ISafetyPipeline _safety;
        private readonly FrameGuardOptions _options;
        private readonly IUpstreamFrameHub _hub;

        private readonly Queue<int> _window = new();
        private readonly HashSet<int> _seen = new();
        private readonly object _gate = new();

        private DateTime _lastHeartbeatUtc = DateTime.UtcNow;
        private IDisposable? _heartbeatSubscription;
        private Task? _heartbeatTask;
        private Task? _watchdogTask;
        private CancellationTokenSource? _internalCts;
        private volatile bool _heartbeatDegraded;

        public FrameGuard(
            ILogger<FrameGuard> log,
            ISafetyPipeline safety,
            IOptions<FrameGuardOptions> options,
            IUpstreamFrameHub hub) {
            _log = log;
            _safety = safety;
            _options = options.Value;
            _hub = hub;
            _safety.StateChanged += OnSafetyStateChanged;
        }

        public ValueTask<bool> InitializeAsync(CancellationToken ct) {
            if (_internalCts is not null) return ValueTask.FromResult(false);

            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var (reader, disposer) = _hub.SubscribeHeartbeat(128);
            _heartbeatSubscription = disposer;
            _heartbeatTask = Task.Run(() => RunHeartbeatAsync(reader, _internalCts.Token), ct);
            _watchdogTask = Task.Run(() => RunHeartbeatWatchdogAsync(_internalCts.Token), ct);
            return ValueTask.FromResult(true);
        }

        public FrameGuardDecision Evaluate(SpeedSet set) {
            var state = _safety.State;
            if (state == SafetyIsolationState.Isolated) {
                SingulationMetrics.Instance.FrameDroppedCounter.Add(1,
                    new KeyValuePair<string, object?>("reason", "isolated"));
                return new FrameGuardDecision(false, set, false, "isolated");
            }

            var accepted = AcceptSequence(set.Sequence);
            if (!accepted) {
                SingulationMetrics.Instance.FrameDroppedCounter.Add(1,
                    new KeyValuePair<string, object?>("reason", "sequence"));
                return new FrameGuardDecision(false, set, false, "duplicate");
            }

            if (state == SafetyIsolationState.Degraded) {
                var scaled = Scale(set, _options.DegradeScale, out var delta);
                if (delta > 0)
                    SingulationMetrics.Instance.SpeedDelta.Record(delta);
                return new FrameGuardDecision(true, scaled, true, "degraded");
            }

            return new FrameGuardDecision(true, set, false, null);
        }

        public void ReportHeartbeat() => _lastHeartbeatUtc = DateTime.UtcNow;

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

        private async Task RunHeartbeatAsync(ChannelReader<ReadOnlyMemory<byte>> reader, CancellationToken ct) {
            await foreach (var _ in reader.ReadAllAsync(ct)) {
                ReportHeartbeat();
                if (_heartbeatDegraded && _safety.TryRecoverFromDegraded("heartbeat")) {
                    _heartbeatDegraded = false;
                    _log.LogInformation("Heartbeat recovered, exiting degraded state.");
                }
            }
        }

        private async Task RunHeartbeatWatchdogAsync(CancellationToken ct) {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
                var elapsed = DateTime.UtcNow - _lastHeartbeatUtc;
                if (elapsed > _options.HeartbeatTimeout && !_heartbeatDegraded) {
                    _heartbeatDegraded = _safety.TryEnterDegraded(SafetyTriggerKind.HeartbeatTimeout, $"heartbeat timeout {elapsed.TotalMilliseconds:F0}ms");
                    if (_heartbeatDegraded) {
                        _log.LogWarning("Heartbeat timeout detected ({Elapsed} ms).", elapsed.TotalMilliseconds);
                        SingulationMetrics.Instance.HeartbeatTimeoutCounter.Add(1);
                    }
                }
            }
        }

        private void OnSafetyStateChanged(object? sender, SafetyStateChangedEventArgs e) {
            if (e.Current == SafetyIsolationState.Normal) {
                _heartbeatDegraded = false;
            }
        }
    }
}
