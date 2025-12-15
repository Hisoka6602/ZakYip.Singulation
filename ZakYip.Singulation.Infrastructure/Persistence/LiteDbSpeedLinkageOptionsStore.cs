using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;
using ZakYip.Singulation.Infrastructure.Configuration;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>基于 LiteDB 的 SpeedLinkageOptions 单文档存储实现，使用内存缓存减少数据库访问。</summary>
    public sealed class LiteDbSpeedLinkageOptionsStore : ISpeedLinkageOptionsStore {
        private const string CollName = "speed_linkage_options";
        private const string Key = LiteDbConstants.DefaultKey;
        private const string CacheKey = "speed_linkage_options_cache";
        private const string ErrorMessage = "读取DB配置异常：SpeedLinkageOptions";

        private readonly ILiteCollection<SpeedLinkageOptionsDoc> _col;
        private readonly ILogger<LiteDbSpeedLinkageOptionsStore> _logger;
        private readonly ICabinetIsolator _safetyIsolator;
        private readonly IMemoryCache _cache;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbSpeedLinkageOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbSpeedLinkageOptionsStore> logger,
            ICabinetIsolator safetyIsolator,
            IMemoryCache cache) {
            _col = db.GetCollection<SpeedLinkageOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
            // 为 Enabled 字段创建索引，用于快速查找启用的配置
            _col.EnsureIndex(x => x.Enabled);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
            _cache = cache;
        }

        public Task<SpeedLinkageOptions> GetAsync(CancellationToken ct = default) {
            // 尝试从缓存获取
            if (_cache.TryGetValue<SpeedLinkageOptions>(CacheKey, out var cached)) {
                return Task.FromResult(cached!);
            }

            try {
                var doc = _col.FindById(Key);
                var result = doc?.ToOptions() ?? new SpeedLinkageOptions();
                
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
                _safetyIsolator.TryEnterDegraded(Core.Enums.CabinetTriggerKind.Unknown, ErrorMessage);
                // 返回默认值
                return Task.FromResult(new SpeedLinkageOptions());
            }
        }

        public Task SaveAsync(SpeedLinkageOptions options, CancellationToken ct = default)
        {
            lock (_gate)
            {
                var doc = options.ToDoc();
                doc.Id = Key; // 强制单文档主键
                _col.Upsert(doc);
                // 更新后立即失效缓存
                _cache.Remove(CacheKey);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default)
        {
            lock (_gate)
            {
                _col.Delete(Key);
                // 删除后立即失效缓存
                _cache.Remove(CacheKey);
            }
            return Task.CompletedTask;
        }
    }
}
