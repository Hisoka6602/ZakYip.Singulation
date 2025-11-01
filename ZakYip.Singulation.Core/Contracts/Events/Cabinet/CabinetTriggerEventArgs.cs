using System;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events.Cabinet {

    /// <summary>
    /// 安全触发事件参数。
    /// </summary>
    public sealed record class CabinetTriggerEventArgs {
        public required CabinetTriggerKind Kind { get; init; }

        public string? Description { get; init; }

        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }
}
