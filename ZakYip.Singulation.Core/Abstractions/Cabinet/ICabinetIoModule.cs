using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;

namespace ZakYip.Singulation.Core.Abstractions.Cabinet {

    /// <summary>
    /// 表示安全 IO 模块，能够发出启动/停止/复位/急停等命令。
    /// </summary>
    public interface ICabinetIoModule {
        /// <summary>
        /// 获取模块名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 当检测到急停按键按下时触发的事件。
        /// </summary>
        event EventHandler<CabinetTriggerEventArgs>? EmergencyStop;
        
        /// <summary>
        /// 当检测到停止按键按下时触发的事件。
        /// </summary>
        event EventHandler<CabinetTriggerEventArgs>? StopRequested;
        
        /// <summary>
        /// 当检测到启动按键按下时触发的事件。
        /// </summary>
        event EventHandler<CabinetTriggerEventArgs>? StartRequested;
        
        /// <summary>
        /// 当检测到复位按键按下时触发的事件。
        /// </summary>
        event EventHandler<CabinetTriggerEventArgs>? ResetRequested;
        
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
