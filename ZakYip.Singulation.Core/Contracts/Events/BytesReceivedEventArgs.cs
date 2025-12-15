using System;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>
    /// 收到上游字节数据时的事件参数。
    /// </summary>
    /// <remarks>
    /// - <see cref="Buffer"/> 为只读内存；如果实现侧使用了缓冲池，请在事件触发前确保内存生存期安全。
    /// - <see cref="TimestampUtc"/> 使用 UTC，便于日志与多机排障。
    /// </remarks>
    public sealed record class BytesReceivedEventArgs {
        /// <summary>原始字节缓冲（请尽快消费，不要长时持有）。</summary>
        public required ReadOnlyMemory<byte> Buffer { get; init; }

        /// <summary>来源端口号（便于区分 speed/position/heartbeat 三路）。</summary>
        public int Port { get; init; }

        /// <summary>接收时间（UTC）。</summary>
        public DateTime TimestampUtc { get; init; }
    }
}
