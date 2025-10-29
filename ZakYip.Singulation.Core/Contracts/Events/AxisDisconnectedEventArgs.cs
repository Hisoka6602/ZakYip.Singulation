using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Contracts.Events {

    /// <summary>
    /// 轴断线事件参数。
    /// </summary>
    public sealed record class AxisDisconnectedEventArgs {
        /// <summary>断线的轴标识。</summary>
        public required AxisId Axis { get; init; }

        /// <summary>断线原因描述（如 "Ping failed"、"Timeout"）。</summary>
        public required string Reason { get; init; }
    }
}