using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Contracts.Events {

    /// <summary>
    /// 轴断线事件参数。
    /// </summary>
    public sealed class AxisDisconnectedEventArgs : EventArgs {

        public AxisDisconnectedEventArgs(AxisId axis, string reason) {
            Axis = axis;
            Reason = reason;
        }

        /// <summary>断线的轴标识。</summary>
        public AxisId Axis { get; }

        /// <summary>断线原因描述（如 "Ping failed"、"Timeout"）。</summary>
        public string Reason { get; }
    }
}