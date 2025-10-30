using System;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events.Safety {

    /// <summary>
    /// 安全隔离状态变化事件参数。
    /// </summary>
    public sealed record class SafetyStateChangedEventArgs {
        public required SafetyIsolationState Previous { get; init; }

        public required SafetyIsolationState Current { get; init; }

        public required SafetyTriggerKind ReasonKind { get; init; }

        public string? ReasonText { get; init; }

        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }
}
