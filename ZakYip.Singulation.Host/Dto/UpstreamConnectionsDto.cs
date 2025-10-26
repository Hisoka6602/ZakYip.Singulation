using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>上游连接列表响应数据。</summary>
    [SwaggerSchema(Description = "上游 TCP 连接列表的响应数据对象")]
    public sealed record class UpstreamConnectionsDto {
        /// <summary>当前是否启用上游连接。</summary>
        [SwaggerSchema(Description = "当前是否启用上游连接功能")]
        [Required]
        public bool Enabled { get; init; }

        /// <summary>上游连接的详细条目集合。</summary>
        [SwaggerSchema(Description = "上游连接的详细条目列表")]
        [Required]
        public IReadOnlyList<UpstreamConnectionDto> Items { get; init; } = Array.Empty<UpstreamConnectionDto>();
    }
}
