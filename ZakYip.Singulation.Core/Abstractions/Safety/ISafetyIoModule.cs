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
        /// <summary>
        /// 获取模块名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 当检测到急停按键按下时触发的事件。
        /// </summary>
        event EventHandler<SafetyTriggerEventArgs>? EmergencyStop;
        
        /// <summary>
        /// 当检测到停止按键按下时触发的事件。
        /// </summary>
        event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        
        /// <summary>
        /// 当检测到启动按键按下时触发的事件。
        /// </summary>
        event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        
        /// <summary>
        /// 当检测到复位按键按下时触发的事件。
        /// </summary>
        event EventHandler<SafetyTriggerEventArgs>? ResetRequested;
        
        /// <summary>
        /// 当检测到远程/本地模式切换时触发的事件。
        /// </summary>
        event EventHandler<RemoteLocalModeChangedEventArgs>? RemoteLocalModeChanged;

        /// <summary>
        /// 启动安全 IO 模块的轮询循环。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        Task StartAsync(CancellationToken ct);
    }
}
