using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>上游连接列表响应数据。</summary>
    public sealed record class UpstreamConnectionsDto {
        public bool Enabled { get; init; }
        public IReadOnlyList<UpstreamConnectionDto> Items { get; init; } = Array.Empty<UpstreamConnectionDto>();
    }
}