using System.ComponentModel;

namespace ZakYip.Singulation.Host.Workers {
    /// <summary>调试联机流程的状态机枚举。</summary>
    internal enum CommissioningState {
        /// <summary>空闲状态。</summary>
        [Description("空闲")]
        Idle,

        /// <summary>上电执行中。</summary>
        [Description("上电执行中")]
        PowerOn,

        /// <summary>回零执行中。</summary>
        [Description("回零执行中")]
        Homing,

        /// <summary>对位执行中。</summary>
        [Description("对位执行中")]
        Aligning,

        /// <summary>流程已准备完成。</summary>
        [Description("已准备完成")]
        Ready,

        /// <summary>流程故障。</summary>
        [Description("流程故障")]
        Faulted
    }
}
