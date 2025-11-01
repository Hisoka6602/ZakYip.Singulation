using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Infrastructure.Cabinet {
    /// <summary>
    /// 安全管线内部使用的操作描述对象。
    /// </summary>
    internal sealed class CabinetOperation {
        /// <summary>操作类型。</summary>
        public CabinetOperationKind Kind { get; }

        /// <summary>安全状态变化事件参数。</summary>
        public CabinetStateChangedEventArgs? StateArgs { get; init; }

        /// <summary>触发的安全命令。</summary>
        public CabinetCommand Command { get; init; } = CabinetCommand.None;

        /// <summary>命令来源触发类型。</summary>
        public CabinetTriggerKind CommandKind { get; init; }

        /// <summary>命令附带原因。</summary>
        public string? CommandReason { get; init; }

        /// <summary>是否来自外部 IO 模块。</summary>
        public bool TriggeredByIo { get; init; }

        /// <summary>轴健康触发类型。</summary>
        public CabinetTriggerKind AxisKind { get; init; }

        /// <summary>轴名称或标识。</summary>
        public string? AxisName { get; init; }

        /// <summary>轴相关原因。</summary>
        public string? AxisReason { get; init; }

        private CabinetOperation(CabinetOperationKind kind) {
            Kind = kind;
        }

        /// <summary>创建状态变更操作。</summary>
        public static CabinetOperation StateChanged(CabinetStateChangedEventArgs ev)
            => new(CabinetOperationKind.StateChanged) { StateArgs = ev };

        /// <summary>创建命令操作。</summary>
        public static CabinetOperation Trigger(CabinetCommand command, CabinetTriggerKind kind, string? reason, bool triggeredByIo = false)
            => new(CabinetOperationKind.Command) {
                Command = command,
                CommandKind = kind,
                CommandReason = reason,
                TriggeredByIo = triggeredByIo
            };

        /// <summary>创建轴健康操作。</summary>
        public static CabinetOperation AxisHealth(CabinetTriggerKind kind, string? name, string? reason)
            => new(CabinetOperationKind.AxisHealth) {
                AxisKind = kind,
                AxisName = name,
                AxisReason = reason
            };
    }
}
