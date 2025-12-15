using System;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events.Cabinet {

    /// <summary>
    /// 安全隔离状态变化事件参数。
    /// </summary>
    public sealed record class CabinetStateChangedEventArgs {
        public required CabinetIsolationState Previous { get; init; }

        public required CabinetIsolationState Current { get; init; }

        public required CabinetTriggerKind ReasonKind { get; init; }

        public string? ReasonText { get; init; }

        public DateTime TimestampUtc { get; init; }
    }
}
