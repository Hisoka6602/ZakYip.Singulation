using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>上游 TCP 连接项快照（单个 transport）。</summary>
    public sealed record class UpstreamConnectionDto {
        public int Index { get; init; }
        /// <summary>对端 IP（服务端初始为监听地址，连接后可更新为客户端地址）。</summary>
        public string? Ip { get; init; }

        /// <summary>端口。</summary>
        public int Port { get; init; }

        /// <summary>true=服务端；false=客户端。</summary>
        public bool IsServer { get; init; }

        /// <summary>连接状态（TransportConnectionState 枚举字符串）。</summary>
        public string State { get; init; } = string.Empty;

        /// <summary>实现类型名（排障用，可选）。</summary>
        public string Impl { get; init; } = string.Empty;
    }
}