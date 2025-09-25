using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>
    /// 传输错误/告警事件参数。
    /// </summary>
    /// <remarks>
    /// - 长连接的很多异常属于可恢复的“瞬态错误”（如断线、超时）；可通过 <see cref="IsTransient"/> 标记。<br/>
    /// - 建议实现侧统一通过事件上报错误，避免把异常外抛阻塞业务线程。
    /// </remarks>
    public sealed record class TransportErrorEventArgs {
        /// <summary>错误/告警的简要描述。</summary>
        public required string Message { get; init; }

        /// <summary>关联异常（若有）。</summary>
        public Exception? Exception { get; init; }

        /// <summary>是否为可恢复的瞬态错误（true 表示通常会自动重试）。</summary>
        public bool IsTransient { get; init; } = true;

        /// <summary>可选：相关端点（如 "192.168.5.11:5001"）。</summary>
        public string? Endpoint { get; init; }

        /// <summary>可选：相关端口（speed/position/heartbeat 的具体端口号）。</summary>
        public int? Port { get; init; }

        /// <summary>事件时间（UTC）。</summary>
        public DateTime TimestampUtc { get; init; } = DateTime.Now;
    }
}