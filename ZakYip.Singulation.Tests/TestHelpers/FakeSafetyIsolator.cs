using System;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的简单 SafetyIsolator 模拟实现。
    /// </summary>
    internal sealed class FakeSafetyIsolator : ISafetyIsolator {
        public SafetyIsolationState State => SafetyIsolationState.Normal;
        public bool IsDegraded => false;
        public bool IsIsolated => false;
        public SafetyTriggerKind LastTriggerKind { get; private set; }
        public string? LastTriggerReason { get; private set; }

        public event EventHandler<SafetyStateChangedEventArgs>? StateChanged;

        public bool TryTrip(SafetyTriggerKind kind, string reason) {
            LastTriggerKind = kind;
            LastTriggerReason = reason;
            return true;
        }

        public bool TryEnterDegraded(SafetyTriggerKind kind, string reason) {
            LastTriggerKind = kind;
            LastTriggerReason = reason;
            return true;
        }

        public bool TryRecover() => true;

        public bool TryRecoverFromDegraded(string reason) => true;

        public bool TryResetIsolation(string reason, CancellationToken ct = default) => true;
    }
}
