using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>上游 TCP 连接项快照（单个 transport）。</summary>
    [SwaggerSchema(Description = "上游 TCP 连接的详细信息快照")]
    public sealed record class UpstreamConnectionDto {
        /// <summary>连接序号（用于前端排序）。</summary>
        [SwaggerSchema(Description = "连接的序号，从 1 开始，用于前端排序和显示")]
        [Required]
        public int Index { get; init; }

        /// <summary>对端 IP（服务端初始为监听地址，连接后可更新为客户端地址）。</summary>
        [SwaggerSchema(Description = "对端 IP 地址。服务端模式下初始为监听地址，连接后更新为客户端地址", Nullable = true)]
        public string? Ip { get; init; }

        /// <summary>端口。</summary>
        [SwaggerSchema(Description = "连接使用的端口号")]
        [Required]
        public int Port { get; init; }

        /// <summary>true=服务端；false=客户端。</summary>
        [SwaggerSchema(Description = "连接角色：true 表示服务端模式，false 表示客户端模式")]
        [Required]
        public bool IsServer { get; init; }

        /// <summary>连接状态（TransportConnectionState 枚举字符串）。</summary>
        [SwaggerSchema(Description = "连接状态，可能的值：Connecting（正在建立连接）、Connected（已连接）、Retrying（准备重连）、Disconnected（已断开）、Stopped（已停止）")]
        [Required]
        public string State { get; init; } = string.Empty;

        /// <summary>实现类型名（排障用，可选）。</summary>
        [SwaggerSchema(Description = "传输层实现的类型名称，用于调试和排障")]
        [Required]
        public string Impl { get; init; } = string.Empty;
    }
}
