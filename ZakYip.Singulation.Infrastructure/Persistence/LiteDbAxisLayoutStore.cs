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

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>
    /// 基于 LiteDB 的轴布局存储：单文档（Id = "singleton"），使用内存缓存减少数据库访问。
    /// </summary>
    public sealed class LiteDbAxisLayoutStore : IAxisLayoutStore {
        private const string Key = "singleton";
        private const string CacheKey = "axis_layout_cache";
        private const string ErrorMessage = "读取DB配置异常：AxisGridLayoutOptions";
        
        private readonly ILiteCollection<AxisGridLayoutDoc> _coll;
        private readonly ILogger<LiteDbAxisLayoutStore> _logger;
        private readonly ICabinetIsolator _safetyIsolator;
        private readonly IMemoryCache _cache;

        public LiteDbAxisLayoutStore(
            ILiteDatabase db,
            ILogger<LiteDbAxisLayoutStore> logger,
            ICabinetIsolator safetyIsolator,
            IMemoryCache cache) {
            _coll = db.GetCollection<AxisGridLayoutDoc>("axis_layout");
            _coll.EnsureIndex(x => x.Id, unique: true);
            if (_coll.FindById(Key) is null)
                _coll.Upsert(new AxisGridLayoutDoc { Id = Key, Rows = 0, Cols = 0 });
            _logger = logger;
            _safetyIsolator = safetyIsolator;
            _cache = cache;
        }

        public Task<AxisGridLayoutOptions> GetAsync(CancellationToken ct = default) {
            // 尝试从缓存获取
            if (_cache.TryGetValue<AxisGridLayoutOptions>(CacheKey, out var cached)) {
                return Task.FromResult(cached!);
            }

            try {
                var result = _coll.FindById(Key)?.ToDto() ?? ConfigDefaults.AxisGrid();
                
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
                return Task.FromResult(ConfigDefaults.AxisGrid());
            }
        }

        public Task UpsertAsync(AxisGridLayoutOptions layout, CancellationToken ct = default) {
            _coll.Upsert(layout.ToDoc());
            // 更新后立即失效缓存
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default) {
            _coll.Delete(Key);
            // 删除后立即失效缓存
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }
    }
}