using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Infrastructure.Safety {

    /// <summary>
    /// 默认安全隔离器实现：集中管理隔离/降级状态，并对外广播。
    /// </summary>
    public sealed class SafetyIsolator : ISafetyIsolator {
        private readonly ILogger<SafetyIsolator> _log;
        private readonly IRealtimeNotifier _realtime;
        private readonly object _gate = new();

        private SafetyIsolationState _state = SafetyIsolationState.Normal;
        private SafetyTriggerKind _lastTriggerKind = SafetyTriggerKind.Unknown;
        private string? _lastTriggerReason;

        public SafetyIsolator(ILogger<SafetyIsolator> log, IRealtimeNotifier realtime) {
            _log = log;
            _realtime = realtime;
        }

        public event EventHandler<SafetyStateChangedEventArgs>? StateChanged;

        public SafetyIsolationState State => Volatile.Read(ref _state);

        public bool IsDegraded => State == SafetyIsolationState.Degraded;

        public bool IsIsolated => State == SafetyIsolationState.Isolated;

        public SafetyTriggerKind LastTriggerKind
            => (SafetyTriggerKind)Volatile.Read(ref Unsafe.As<SafetyTriggerKind, int>(ref _lastTriggerKind));

        public string? LastTriggerReason => Volatile.Read(ref _lastTriggerReason);

        public bool TryTrip(SafetyTriggerKind kind, string reason) {
            if (string.IsNullOrWhiteSpace(reason)) reason = "unknown";
            return Transition(kind, reason, SafetyIsolationState.Isolated);
        }

        public bool TryEnterDegraded(SafetyTriggerKind kind, string reason) {
            if (string.IsNullOrWhiteSpace(reason)) reason = "degraded";
            return Transition(kind, reason, SafetyIsolationState.Degraded);
        }

        public bool TryRecoverFromDegraded(string reason) {
            if (State != SafetyIsolationState.Degraded) return false;
            return Transition(SafetyTriggerKind.HealthRecovered, string.IsNullOrWhiteSpace(reason) ? "recovered" : reason, SafetyIsolationState.Normal);
        }

        public bool TryResetIsolation(string reason, CancellationToken ct = default) {
            if (State != SafetyIsolationState.Isolated) return false;
            ct.ThrowIfCancellationRequested();
            return Transition(SafetyTriggerKind.ResetButton, string.IsNullOrWhiteSpace(reason) ? "reset" : reason, SafetyIsolationState.Normal);
        }

        private bool Transition(SafetyTriggerKind kind, string reason, SafetyIsolationState target) {
            SafetyStateChangedEventArgs? ev = null;
            lock (_gate) {
                var current = _state;
                if (current == target) {
                    if (target == SafetyIsolationState.Degraded) {
                        _lastTriggerKind = kind;
                        _lastTriggerReason = reason;
                    }
                    return false;
                }

                if (target == SafetyIsolationState.Degraded && current == SafetyIsolationState.Isolated)
                    return false;

                if (target == SafetyIsolationState.Isolated && current == SafetyIsolationState.Isolated)
                    return false;

                _state = target;
                _lastTriggerKind = kind;
                _lastTriggerReason = reason;
                ev = new SafetyStateChangedEventArgs(current, target, kind, reason);
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

        private void LogStateChange(SafetyStateChangedEventArgs ev) {
            var payload = new {
                kind = ev.ReasonKind.ToString(),
                state = ev.Current.ToString(),
                prev = ev.Previous.ToString(),
                reason = ev.ReasonText
            };

            switch (ev.Current) {
                case SafetyIsolationState.Isolated:
                    _log.LogWarning("Safety isolated due to {Kind}: {Reason}", ev.ReasonKind, ev.ReasonText);
                    SingulationMetrics.Instance.DegradeCounter.Add(1,
                        new KeyValuePair<string, object?>("state", "isolated"));
                    break;
                case SafetyIsolationState.Degraded:
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
