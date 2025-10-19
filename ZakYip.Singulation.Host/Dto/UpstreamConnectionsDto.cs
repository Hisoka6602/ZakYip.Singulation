using System;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>上游连接列表响应数据。</summary>
    public sealed record class UpstreamConnectionsDto {
        /// <summary>当前是否启用上游连接。</summary>
        public bool Enabled { get; init; }

        /// <summary>上游连接的详细条目集合。</summary>
        public IReadOnlyList<UpstreamConnectionDto> Items { get; init; } = Array.Empty<UpstreamConnectionDto>();
    }
}
