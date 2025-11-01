using System;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的简单 CabinetIsolator 模拟实现。
    /// </summary>
    internal sealed class FakeSafetyIsolator : ICabinetIsolator {
        public CabinetIsolationState State => CabinetIsolationState.Normal;
        public bool IsDegraded => false;
        public bool IsIsolated => false;
        public CabinetTriggerKind LastTriggerKind { get; private set; }
        public string? LastTriggerReason { get; private set; }

        public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

        public bool TryTrip(CabinetTriggerKind kind, string reason) {
            LastTriggerKind = kind;
            LastTriggerReason = reason;
            return true;
        }

        public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) {
            LastTriggerKind = kind;
            LastTriggerReason = reason;
            return true;
        }


        public bool TryRecoverFromDegraded(string reason) => true;

        public bool TryResetIsolation(string reason, CancellationToken ct = default) => true;
    }
}
