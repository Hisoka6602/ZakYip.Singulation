using System;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>
    /// 传输层事件：封装来源、类型和载荷。
    /// </summary>
    public readonly record struct TransportEvent {
        /// <summary>事件来源（名称或端口）。</summary>
        public string Source { get; init; }

        /// <summary>事件类型。</summary>
        public TransportEventType Type { get; init; }

        /// <summary>事件载荷（Type 为 Data 时有效）。</summary>
        public ReadOnlyMemory<byte> Payload { get; init; }

        /// <summary>收到的字节数量（Type 为 BytesReceived 时有效）。</summary>
        public int Count { get; init; }

        /// <summary>连接状态（Type 为 StateChanged 时有效）。</summary>
        public TransportConnectionState Conn { get; init; }

        /// <summary>关联异常（Type 为 Error 时有效）。</summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// 通过构造函数创建传输事件。
        /// </summary>
        public TransportEvent(
            string source,
            TransportEventType type,
            ReadOnlyMemory<byte> payload,
            int count,
            TransportConnectionState conn,
            Exception? exception
        ) {
            Source = source;
            Type = type;
            Payload = payload;
            Count = count;
            Conn = conn;
            Exception = exception;
        }
    }
}
