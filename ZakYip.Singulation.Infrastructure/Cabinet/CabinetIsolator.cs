using System;
using System.Collections.Generic;
using System.Threading;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Infrastructure.Cabinet {

    /// <summary>
    /// 默认安全隔离器实现：集中管理隔离/降级状态，并对外广播。
    /// </summary>
    public sealed class CabinetIsolator : ICabinetIsolator {
        private readonly ILogger<CabinetIsolator> _log;
        private readonly IRealtimeNotifier _realtime;
        private readonly object _gate = new();

        private int _state = (int)CabinetIsolationState.Normal;
        private int _lastTriggerKind = (int)CabinetTriggerKind.Unknown;
        private string? _lastTriggerReason;

        public CabinetIsolator(ILogger<CabinetIsolator> log, IRealtimeNotifier realtime) {
            _log = log;
            _realtime = realtime;
        }

        public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

        public CabinetIsolationState State => (CabinetIsolationState)Volatile.Read(ref _state);

        public bool IsDegraded => State == CabinetIsolationState.Degraded;

        public bool IsIsolated => State == CabinetIsolationState.Isolated;

        public CabinetTriggerKind LastTriggerKind => (CabinetTriggerKind)Volatile.Read(ref _lastTriggerKind);

        public string? LastTriggerReason => Volatile.Read(ref _lastTriggerReason);

        public bool TryTrip(CabinetTriggerKind kind, string reason) {
            if (string.IsNullOrWhiteSpace(reason)) reason = "unknown";
            return Transition(kind, reason, CabinetIsolationState.Isolated);
        }

        public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) {
            if (string.IsNullOrWhiteSpace(reason)) reason = "degraded";
            return Transition(kind, reason, CabinetIsolationState.Degraded);
        }

        public bool TryRecoverFromDegraded(string reason) {
            if (State != CabinetIsolationState.Degraded) return false;
            return Transition(CabinetTriggerKind.HealthRecovered, string.IsNullOrWhiteSpace(reason) ? "recovered" : reason, CabinetIsolationState.Normal);
        }

        public bool TryResetIsolation(string reason, CancellationToken ct = default) {
            if (State != CabinetIsolationState.Isolated) return false;
            ct.ThrowIfCancellationRequested();
            return Transition(CabinetTriggerKind.ResetButton, string.IsNullOrWhiteSpace(reason) ? "reset" : reason, CabinetIsolationState.Normal);
        }

        private bool Transition(CabinetTriggerKind kind, string reason, CabinetIsolationState target) {
            CabinetStateChangedEventArgs? ev = null;
            lock (_gate) {
                var current = (CabinetIsolationState)_state;
                if (current == target) {
                    if (target == CabinetIsolationState.Degraded) {
                        _lastTriggerKind = (int)kind;
                        _lastTriggerReason = reason;
                    }
                    return false;
                }

                if (target == CabinetIsolationState.Degraded && current == CabinetIsolationState.Isolated)
                    return false;

                if (target == CabinetIsolationState.Isolated && current == CabinetIsolationState.Isolated)
                    return false;

                _state = (int)target;
                _lastTriggerKind = (int)kind;
                _lastTriggerReason = reason;
                ev = new CabinetStateChangedEventArgs {
                    Previous = current,
                    Current = target,
                    ReasonKind = kind,
                    ReasonText = reason
                };
            }

            if (ev is not null) {
                try {
                    LogStateChange(ev);
                    StateChanged?.Invoke(this, ev);
                }
                catch (Exception ex) {
                    _log.LogError(ex, "Safety isolator state change notification failed.");
                }
            }
            return ev is not null;
        }

        private void LogStateChange(CabinetStateChangedEventArgs ev) {
            var payload = new {
                kind = ev.ReasonKind.ToString(),
                state = ev.Current.ToString(),
                prev = ev.Previous.ToString(),
                reason = ev.ReasonText
            };

            switch (ev.Current) {
                case CabinetIsolationState.Isolated:
                    _log.LogWarning("Safety isolated due to {Kind}: {Reason}", ev.ReasonKind, ev.ReasonText);
                    SingulationMetrics.Instance.DegradeCounter.Add(1,
                        new KeyValuePair<string, object?>("state", "isolated"));
                    break;
                case CabinetIsolationState.Degraded:
                    _log.LogWarning("Safety degraded due to {Kind}: {Reason}", ev.ReasonKind, ev.ReasonText);
                    SingulationMetrics.Instance.DegradeCounter.Add(1,
                        new KeyValuePair<string, object?>("state", "degraded"));
                    break;
                default:
                    _log.LogInformation("Safety state recovered: {Reason}", ev.ReasonText);
                    break;
            }

            _ = _realtime.PublishDeviceAsync(new {
                kind = "safety.state",
                triggerKind = payload.kind,
                payload.state,
                payload.prev,
                payload.reason
            });
        }
    }
}
