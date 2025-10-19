namespace ZakYip.Singulation.Host.Workers {
    /// <summary>联机流程内部命令类型。</summary>
    internal enum CommissioningCommandKind {
        /// <summary>开始执行流程。</summary>
        Start,

        /// <summary>停止流程。</summary>
        Stop,

        /// <summary>重置流程。</summary>
        Reset,

        /// <summary>安全状态变更事件。</summary>
        SafetyStateChanged
    }
}
