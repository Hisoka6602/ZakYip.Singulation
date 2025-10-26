using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Infrastructure.Persistence;

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
        /// </summary>
        public static async Task SeedUpstreamOptionsFromConfigAsync(this IServiceProvider sp, IConfiguration cfg, CancellationToken ct = default) {
            // 约定使用 "Upstream" 节点；不存在则不做任何事
            var sec = cfg.GetSection("Upstream");
            if (!sec.Exists()) return;

            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var dto = await store.GetAsync(ct);

            // 读取配置并合并（不存在则保留当前值）- 使用 with 表达式创建新对象
            dto = dto with {
                Host = sec["Host"] ?? dto.Host,
                SpeedPort = TryInt(sec["SpeedPort"], dto.SpeedPort),
                PositionPort = TryInt(sec["PositionPort"], dto.PositionPort),
                HeartbeatPort = TryInt(sec["HeartbeatPort"], dto.HeartbeatPort),
                ValidateCrc = TryBool(sec["ValidateCrc"], dto.ValidateCrc),
                Role = Enum.TryParse<TransportRole>(sec["Role"], true, out var role) ? role : dto.Role
            };

            await store.SaveAsync(dto, ct);

            static int TryInt(string? s, int fallback) => int.TryParse(s, out var v) ? v : fallback;
            static bool TryBool(string? s, bool fallback) => bool.TryParse(s, out var v) ? v : fallback;
        }
    }
}