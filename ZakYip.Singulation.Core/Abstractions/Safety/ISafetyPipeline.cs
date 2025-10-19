using System;
using System.Threading;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Safety;

namespace ZakYip.Singulation.Core.Abstractions.Safety {

    /// <summary>
    /// 安全联动管线：连接 IO、健康事件与隔离器，并对外广播命令。
    /// </summary>
    public interface ISafetyPipeline {
        SafetyIsolationState State { get; }

        event EventHandler<SafetyStateChangedEventArgs>? StateChanged;
        event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        event EventHandler<SafetyTriggerEventArgs>? ResetRequested;

        bool TryTrip(SafetyTriggerKind kind, string reason);
        bool TryEnterDegraded(SafetyTriggerKind kind, string reason);
        bool TryRecoverFromDegraded(string reason);
        bool TryResetIsolation(string reason, CancellationToken ct = default);
    }
}
