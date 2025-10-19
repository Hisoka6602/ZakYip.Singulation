using System;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events.Safety {

    /// <summary>
    /// 安全隔离状态变化事件参数。
    /// </summary>
    public sealed class SafetyStateChangedEventArgs : EventArgs {
        public SafetyStateChangedEventArgs(SafetyIsolationState previous, SafetyIsolationState current, SafetyTriggerKind reasonKind, string? reasonText) {
            Previous = previous;
            Current = current;
            ReasonKind = reasonKind;
            ReasonText = reasonText;
            TimestampUtc = DateTime.UtcNow;
        }

        public SafetyIsolationState Previous { get; }

        public SafetyIsolationState Current { get; }

        public SafetyTriggerKind ReasonKind { get; }

        public string? ReasonText { get; }

        public DateTime TimestampUtc { get; }
    }
}
