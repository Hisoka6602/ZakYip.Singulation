using ZakYip.Singulation.Core.Contracts.Events.Safety;

namespace ZakYip.Singulation.Host.Workers {
    /// <summary>
    /// 联机流程内部的命令载荷。
    /// </summary>
    internal sealed class CommissioningCommand {
        /// <summary>命令类型。</summary>
        public CommissioningCommandKind Kind { get; }

        /// <summary>命令附带原因文本。</summary>
        public string? Reason { get; }

        /// <summary>安全状态变更事件参数。</summary>
        public SafetyStateChangedEventArgs? StateArgs { get; }

        /// <summary>使用类型与原因构造命令。</summary>
        public CommissioningCommand(CommissioningCommandKind kind, string? reason) {
            Kind = kind;
            Reason = reason;
        }

        /// <summary>使用安全状态事件构造命令。</summary>
        public CommissioningCommand(CommissioningCommandKind kind, SafetyStateChangedEventArgs args) {
            Kind = kind;
            Reason = args.ReasonText;
            StateArgs = args;
        }
    }
}
