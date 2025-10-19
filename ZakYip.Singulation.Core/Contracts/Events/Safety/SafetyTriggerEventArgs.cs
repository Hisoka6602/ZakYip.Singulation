using System;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events.Safety {

    /// <summary>
    /// 安全触发事件参数。
    /// </summary>
    public sealed class SafetyTriggerEventArgs : EventArgs {
        public SafetyTriggerEventArgs(SafetyTriggerKind kind, string? description) {
            Kind = kind;
            Description = description;
            TimestampUtc = DateTime.UtcNow;
        }

        public SafetyTriggerKind Kind { get; }

        public string? Description { get; }

        public DateTime TimestampUtc { get; }
    }
}
