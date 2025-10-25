using System.ComponentModel;

namespace ZakYip.Singulation.Host.Workers {
    /// <summary>联机流程内部命令类型。</summary>
    internal enum CommissioningCommandKind {
        /// <summary>开始执行流程。</summary>
        [Description("开始执行")]
        Start,

        /// <summary>停止流程。</summary>
        [Description("停止流程")]
        Stop,

        /// <summary>重置流程。</summary>
        [Description("重置流程")]
        Reset,

        /// <summary>安全状态变更事件。</summary>
        [Description("安全状态变更")]
        SafetyStateChanged
    }
}
