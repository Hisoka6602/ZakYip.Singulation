using System;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events.Safety {

    /// <summary>
    /// 安全触发事件参数。
    /// </summary>
    public sealed record class SafetyTriggerEventArgs {
        public required SafetyTriggerKind Kind { get; init; }

        public string? Description { get; init; }

        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }
}
