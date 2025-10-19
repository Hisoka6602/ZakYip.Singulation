using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Safety;

namespace ZakYip.Singulation.Core.Abstractions.Safety {

    /// <summary>
    /// 表示安全 IO 模块，能够发出启动/停止/复位/急停等命令。
    /// </summary>
    public interface ISafetyIoModule {
        string Name { get; }

        event EventHandler<SafetyTriggerEventArgs>? EmergencyStop;
        event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        event EventHandler<SafetyTriggerEventArgs>? ResetRequested;

        Task StartAsync(CancellationToken ct);
    }
}
