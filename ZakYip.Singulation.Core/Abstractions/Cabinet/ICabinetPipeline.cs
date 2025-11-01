using System;
using System.Threading;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;

namespace ZakYip.Singulation.Core.Abstractions.Cabinet {

    /// <summary>
    /// 安全联动管线：连接 IO、健康事件与隔离器，并对外广播命令。
    /// </summary>
    public interface ICabinetPipeline {
        CabinetIsolationState State { get; }

        event EventHandler<CabinetStateChangedEventArgs>? StateChanged;
        event EventHandler<CabinetTriggerEventArgs>? StartRequested;
        event EventHandler<CabinetTriggerEventArgs>? StopRequested;
        event EventHandler<CabinetTriggerEventArgs>? ResetRequested;

        bool TryTrip(CabinetTriggerKind kind, string reason);
        bool TryEnterDegraded(CabinetTriggerKind kind, string reason);
        bool TryRecoverFromDegraded(string reason);
        bool TryResetIsolation(string reason, CancellationToken ct = default);

        /// <summary>
        /// 外部触发启动命令（可来自 IO 或 API）。
        /// </summary>
        /// <param name="kind">触发来源。</param>
        /// <param name="reason">原因描述。</param>
        /// <param name="triggeredByIo">是否来自 IO。</param>
        void RequestStart(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false);

        /// <summary>
        /// 外部触发停止命令（可来自 IO 或 API）。
        /// </summary>
        void RequestStop(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false);

        /// <summary>
        /// 外部触发复位命令（可来自 IO 或 API）。
        /// </summary>
        void RequestReset(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false);
    }
}
