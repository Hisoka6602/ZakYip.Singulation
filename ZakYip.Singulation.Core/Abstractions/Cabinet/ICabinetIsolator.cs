using System;
using System.Threading;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;

namespace ZakYip.Singulation.Core.Abstractions.Cabinet {

    /// <summary>
    /// 安全隔离器：负责集中管理降级/隔离状态，并对外广播状态变化。
    /// </summary>
    public interface ICabinetIsolator {
        /// <summary>当前隔离状态。</summary>
        CabinetIsolationState State { get; }

        /// <summary>当前是否处于降级。</summary>
        bool IsDegraded { get; }

        /// <summary>当前是否处于隔离。</summary>
        bool IsIsolated { get; }

        /// <summary>最近一次触发的来源。</summary>
        CabinetTriggerKind LastTriggerKind { get; }

        /// <summary>最近一次触发的描述。</summary>
        string? LastTriggerReason { get; }

        /// <summary>状态变化事件。</summary>
        event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

        /// <summary>触发隔离。</summary>
        bool TryTrip(CabinetTriggerKind kind, string reason);

        /// <summary>进入降级运行。</summary>
        bool TryEnterDegraded(CabinetTriggerKind kind, string reason);

        /// <summary>从降级恢复。</summary>
        bool TryRecoverFromDegraded(string reason);

        /// <summary>从隔离状态复位。</summary>
        bool TryResetIsolation(string reason, CancellationToken ct = default);
    }
}
