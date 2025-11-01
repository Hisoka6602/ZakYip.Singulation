using System.ComponentModel;

namespace ZakYip.Singulation.Infrastructure.Cabinet {
    /// <summary>安全操作的种类。</summary>
    internal enum CabinetOperationKind {
        /// <summary>安全状态变化。</summary>
        [Description("安全状态变化")]
        StateChanged,

        /// <summary>来自安全命令（Start/Stop/Reset）。</summary>
        [Description("安全命令")]
        Command,

        /// <summary>轴健康状态更新。</summary>
        [Description("轴健康状态更新")]
        AxisHealth
    }
}
