namespace ZakYip.Singulation.Host.Safety {
    /// <summary>安全操作的种类。</summary>
    internal enum SafetyOperationKind {
        /// <summary>安全状态变化。</summary>
        StateChanged,

        /// <summary>来自安全命令（Start/Stop/Reset）。</summary>
        Command,

        /// <summary>轴健康状态更新。</summary>
        AxisHealth
    }
}
