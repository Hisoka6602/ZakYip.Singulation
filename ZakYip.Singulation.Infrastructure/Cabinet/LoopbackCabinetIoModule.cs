using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;

namespace ZakYip.Singulation.Infrastructure.Cabinet {

    /// <summary>
    /// 纯内存安全 IO 模块：用于回归测试与 Console Demo。
    /// </summary>
    public sealed class LoopbackCabinetIoModule : ICabinetIoModule {
        public string Name { get; }

        public event EventHandler<CabinetTriggerEventArgs>? EmergencyStop;
        public event EventHandler<CabinetTriggerEventArgs>? StopRequested;
        public event EventHandler<CabinetTriggerEventArgs>? StartRequested;
        public event EventHandler<CabinetTriggerEventArgs>? ResetRequested;
        public event EventHandler<RemoteLocalModeChangedEventArgs>? RemoteLocalModeChanged;

        public LoopbackCabinetIoModule(string name = "loopback") => Name = name;

        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

        public void TriggerEmergencyStop(string? reason = null)
            => EmergencyStop?.Invoke(this, new CabinetTriggerEventArgs { Kind = Core.Enums.CabinetTriggerKind.EmergencyStop, Description = reason });

        public void TriggerStop(string? reason = null)
            => StopRequested?.Invoke(this, new CabinetTriggerEventArgs { Kind = Core.Enums.CabinetTriggerKind.StopButton, Description = reason });

        public void TriggerStart(string? reason = null)
            => StartRequested?.Invoke(this, new CabinetTriggerEventArgs { Kind = Core.Enums.CabinetTriggerKind.StartButton, Description = reason });

        public void TriggerReset(string? reason = null)
            => ResetRequested?.Invoke(this, new CabinetTriggerEventArgs { Kind = Core.Enums.CabinetTriggerKind.ResetButton, Description = reason });

        public void TriggerRemoteLocalModeChange(bool isRemoteMode, string? reason = null, bool isInitialDetection = false)
            => RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { 
                IsRemoteMode = isRemoteMode, 
                Description = reason, 
                IsInitialDetection = isInitialDetection 
            });
    }
}
