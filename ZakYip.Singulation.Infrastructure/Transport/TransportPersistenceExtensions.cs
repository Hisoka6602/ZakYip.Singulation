using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Core.Contracts.Dto.Transport;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>
    /// 注入：仅注册 LiteDB 单文档持久化（不做其他接入）。
    /// </summary>
    public static class TransportPersistenceExtensions {

        /// <summary>
        /// 仅持久化：注册 IUpstreamOptionsStore（LiteDB 实现）。
        /// </summary>
        public static IServiceCollection AddUpstreamFromLiteDb(this IServiceCollection services, string filePath = "singulation.db") {
            services.AddSingleton<IUpstreamOptionsStore, LiteDbUpstreamOptionsStore>();
            services.AddSingleton<IUpstreamCodecOptionsStore, LiteDbUpstreamCodecOptionsStore>();
            return services;
        }

        /// <summary>
        /// （可选）从配置做一次性“种子写入/覆盖”，不依赖上游源，不启动任何后台任务。
        /// 仅在你主动调用时生效。
        /// </summary>
        public static async Task SeedUpstreamOptionsFromConfigAsync(this IServiceProvider sp, IConfiguration cfg, CancellationToken ct = default) {
            // 约定使用 "Upstream" 节点；不存在则不做任何事
            var sec = cfg.GetSection("Upstream");
            if (!sec.Exists()) return;

            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var dto = await store.GetAsync(ct) ?? new UpstreamOptionsDto();

            // 读取配置并合并（不存在则保留当前值）
            dto.Host = sec["Host"] ?? dto.Host;
            dto.SpeedPort = TryInt(sec["SpeedPort"], dto.SpeedPort);
            dto.PositionPort = TryInt(sec["PositionPort"], dto.PositionPort);
            dto.HeartbeatPort = TryInt(sec["HeartbeatPort"], dto.HeartbeatPort);
            dto.ValidateCrc = TryBool(sec["ValidateCrc"], dto.ValidateCrc);

            // 角色可选（Client/Server）。未配置则保持原值
            if (Enum.TryParse<TransportRole>(sec["Role"], true, out var role))
                dto.Role = role;

            await store.SaveAsync(dto, ct);

            static int TryInt(string? s, int fallback) => int.TryParse(s, out var v) ? v : fallback;
            static bool TryBool(string? s, bool fallback) => bool.TryParse(s, out var v) ? v : fallback;
        }
    }
}