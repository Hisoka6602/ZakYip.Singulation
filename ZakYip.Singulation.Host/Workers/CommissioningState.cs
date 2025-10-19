namespace ZakYip.Singulation.Host.Workers {
    /// <summary>调试联机流程的状态机枚举。</summary>
    internal enum CommissioningState {
        /// <summary>空闲状态。</summary>
        Idle,

        /// <summary>上电执行中。</summary>
        PowerOn,

        /// <summary>回零执行中。</summary>
        Homing,

        /// <summary>对位执行中。</summary>
        Aligning,

        /// <summary>流程已准备完成。</summary>
        Ready,

        /// <summary>流程故障。</summary>
        Faulted
    }
}
