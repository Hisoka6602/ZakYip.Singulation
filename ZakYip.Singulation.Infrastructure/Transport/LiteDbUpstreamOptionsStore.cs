using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Configs.Defaults;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;
using ZakYip.Singulation.Infrastructure.Configuration;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>基于 LiteDB 的 UpstreamOptions 单文档存储实现，使用内存缓存减少数据库访问。</summary>
    public sealed class LiteDbUpstreamOptionsStore : IUpstreamOptionsStore {
        private const string CollName = "upstream_options";
        private const string Key = "upstream_options_singleton";
        private const string CacheKey = "upstream_options_cache";
        private const string ErrorMessage = "读取DB配置异常：UpstreamOptions";

        private readonly ILiteCollection<UpstreamOptionsDoc> _col;
        private readonly ILogger<LiteDbUpstreamOptionsStore> _logger;
        private readonly ICabinetIsolator _safetyIsolator;
        private readonly IMemoryCache _cache;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbUpstreamOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbUpstreamOptionsStore> logger,
            ICabinetIsolator safetyIsolator,
            IMemoryCache cache) {
            _col = db.GetCollection<UpstreamOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
            _cache = cache;
        }

        public Task<UpstreamOptions> GetAsync(CancellationToken ct = default) {
            // 尝试从缓存获取
            if (_cache.TryGetValue<UpstreamOptions>(CacheKey, out var cached)) {
                return Task.FromResult(cached!);
            }

            try {
                var result = _col.FindById(Key)?.ToDto() ?? ConfigDefaults.Upstream();
                
                // 缓存配置，使用配置的过期时间
                var cacheOptions = new MemoryCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(InfrastructureConstants.Cache.ConfigAbsoluteExpirationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(InfrastructureConstants.Cache.ConfigSlidingExpirationMinutes)
                };
                _cache.Set(CacheKey, result, cacheOptions);
                
                return Task.FromResult(result);
            }
            catch (Exception ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(CabinetTriggerKind.Unknown, ErrorMessage);
                return Task.FromResult(ConfigDefaults.Upstream());
            }
        }

        public Task SaveAsync(UpstreamOptions dto, CancellationToken ct = default) =>
            Task.Run(() => {
                lock (_gate) {
                    var doc = dto.ToDoc();
                    doc.Id = Key;           // 强制单文档主键
                    _col.Upsert(doc);
                    // 更新后立即失效缓存
                    _cache.Remove(CacheKey);
                }
            }, ct);

        public Task DeleteAsync(CancellationToken ct = default) =>
            Task.Run(() => {
                lock (_gate) {
                    _col.Delete(Key);
                    // 删除后立即失效缓存
                    _cache.Remove(CacheKey);
                }
            }, ct);
    }
}