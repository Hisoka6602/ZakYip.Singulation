using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>
    /// 连接状态变化事件参数。
    /// </summary>
    /// <remarks>
    /// - 触发时机：连接拨号、连上、断开、进入重试窗口、彻底停止等。<br/>
    /// - 订阅方回调请勿阻塞内部读循环；实现侧应使用 <c>Task.Run</c> 或通道转发触发事件。
    /// </remarks>
    public sealed record class TransportStateChangedEventArgs {
        /// <summary>新的连接状态。</summary>
        public required TransportConnectionState State { get; init; }

        /// <summary>端点字符串，如 "192.168.5.11:5001" 或 "0.0.0.0:5001"。</summary>
        public string? Endpoint { get; init; }

        /// <summary>若处于重连阶段：当前尝试序号（从 1 开始或实现定义）。</summary>
        public int? Attempt { get; init; }

        /// <summary>若处于重连阶段：下一次尝试的延时。</summary>
        public TimeSpan? NextDelay { get; init; }

        /// <summary>简要原因说明（例如 "Remote closed"、"Connect timeout"）。</summary>
        public string? Reason { get; init; }

        /// <summary>关联异常（若有）。用于诊断，不建议直接向上抛出。</summary>
        public Exception? Exception { get; init; }

        /// <summary>是否为被动断开（远端关闭/网络中断 等导致）。</summary>
        public bool PassiveClose { get; init; }
    }
}