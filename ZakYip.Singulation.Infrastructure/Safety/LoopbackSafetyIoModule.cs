using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Events.Safety;

namespace ZakYip.Singulation.Infrastructure.Safety {

    /// <summary>
    /// 纯内存安全 IO 模块：用于回归测试与 Console Demo。
    /// </summary>
    public sealed class LoopbackSafetyIoModule : ISafetyIoModule {
        public string Name { get; }

        public event EventHandler<SafetyTriggerEventArgs>? EmergencyStop;
        public event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        public event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        public event EventHandler<SafetyTriggerEventArgs>? ResetRequested;
        public event EventHandler<RemoteLocalModeChangedEventArgs>? RemoteLocalModeChanged;

        public LoopbackSafetyIoModule(string name = "loopback") => Name = name;

        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

        public void TriggerEmergencyStop(string? reason = null)
            => EmergencyStop?.Invoke(this, new SafetyTriggerEventArgs { Kind = Core.Enums.SafetyTriggerKind.EmergencyStop, Description = reason });

        public void TriggerStop(string? reason = null)
            => StopRequested?.Invoke(this, new SafetyTriggerEventArgs { Kind = Core.Enums.SafetyTriggerKind.StopButton, Description = reason });

        public void TriggerStart(string? reason = null)
            => StartRequested?.Invoke(this, new SafetyTriggerEventArgs { Kind = Core.Enums.SafetyTriggerKind.StartButton, Description = reason });

        public void TriggerReset(string? reason = null)
            => ResetRequested?.Invoke(this, new SafetyTriggerEventArgs { Kind = Core.Enums.SafetyTriggerKind.ResetButton, Description = reason });

        public void TriggerRemoteLocalModeChange(bool isRemoteMode, string? reason = null)
            => RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = isRemoteMode, Description = reason });
    }
}
