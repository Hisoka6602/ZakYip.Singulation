using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Host.Safety {
    /// <summary>
    /// 安全管线内部使用的操作描述对象。
    /// </summary>
    internal sealed class SafetyOperation {
        /// <summary>操作类型。</summary>
        public SafetyOperationKind Kind { get; }

        /// <summary>安全状态变化事件参数。</summary>
        public SafetyStateChangedEventArgs? StateArgs { get; init; }

        /// <summary>触发的安全命令。</summary>
        public SafetyCommand Command { get; init; } = SafetyCommand.None;

        /// <summary>命令来源触发类型。</summary>
        public SafetyTriggerKind CommandKind { get; init; }

        /// <summary>命令附带原因。</summary>
        public string? CommandReason { get; init; }

        /// <summary>是否来自外部 IO 模块。</summary>
        public bool TriggeredByIo { get; init; }

        /// <summary>轴健康触发类型。</summary>
        public SafetyTriggerKind AxisKind { get; init; }

        /// <summary>轴名称或标识。</summary>
        public string? AxisName { get; init; }

        /// <summary>轴相关原因。</summary>
        public string? AxisReason { get; init; }

        private SafetyOperation(SafetyOperationKind kind) {
            Kind = kind;
        }

        /// <summary>创建状态变更操作。</summary>
        public static SafetyOperation StateChanged(SafetyStateChangedEventArgs ev)
            => new(SafetyOperationKind.StateChanged) { StateArgs = ev };

        /// <summary>创建命令操作。</summary>
        public static SafetyOperation Trigger(SafetyCommand command, SafetyTriggerKind kind, string? reason, bool triggeredByIo = false)
            => new(SafetyOperationKind.Command) {
                Command = command,
                CommandKind = kind,
                CommandReason = reason,
                TriggeredByIo = triggeredByIo
            };

        /// <summary>创建轴健康操作。</summary>
        public static SafetyOperation AxisHealth(SafetyTriggerKind kind, string? name, string? reason)
            => new(SafetyOperationKind.AxisHealth) {
                AxisKind = kind,
                AxisName = name,
                AxisReason = reason
            };
    }
}
