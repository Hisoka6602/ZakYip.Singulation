using System;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 单个传输通道的状态快照。
    /// </summary>
    public sealed record class TransportStatusItem {
        /// <summary>通道名称，例如 "upstream.tcp"。</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>通道角色（Server/Client）。</summary>
        public string Role { get; init; } = "Server";

        /// <summary>当前状态（Stopped/Starting/Running）。</summary>
        public string Status { get; init; } = "Stopped";

        /// <summary>对端地址，例如 "192.168.1.23:5100"。</summary>
        public string? Remote { get; init; }

        /// <summary>最近一次状态变更时间（UTC）。</summary>
        public DateTime? LastStateChangedUtc { get; init; }

        /// <summary>累计接收字节数。</summary>
        public long ReceivedBytes { get; init; }
    }
}
