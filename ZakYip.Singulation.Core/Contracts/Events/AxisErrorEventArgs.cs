using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Contracts.Events {

    /// <summary>
    /// 轴运行异常事件参数。
    /// </summary>
    public sealed record class AxisErrorEventArgs {
        /// <summary>发生异常的轴标识。</summary>
        public required AxisId Axis { get; init; }

        /// <summary>具体异常对象。</summary>
        public required Exception Exception { get; init; }
    }
}