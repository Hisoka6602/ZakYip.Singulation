using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Infrastructure.Configs.Vendors.Leadshine.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;
using ZakYip.Singulation.Infrastructure.Configuration;
using ZakYip.Singulation.Infrastructure.Persistence;

namespace ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine {

    /// <summary>基于 LiteDB 的 LeadshineCabinetIoOptions 单文档存储实现，使用内存缓存减少数据库访问。</summary>
    public sealed class LiteDbLeadshineCabinetIoOptionsStore : ILeadshineCabinetIoOptionsStore {
        private const string CollName = "leadshine_cabinet_io_options";
        private const string Key = LiteDbConstants.DefaultKey;
        private const string CacheKey = "leadshine_cabinet_io_options_cache";
        private const string ErrorMessage = "读取DB配置异常：LeadshineCabinetIoOptions";

        private readonly ILiteCollection<LeadshineCabinetIoOptionsDoc> _col;
        private readonly ILogger<LiteDbLeadshineCabinetIoOptionsStore> _logger;
        private readonly ICabinetIsolator _safetyIsolator;
        private readonly IMemoryCache _cache;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbLeadshineCabinetIoOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbLeadshineCabinetIoOptionsStore> logger,
            ICabinetIsolator safetyIsolator,
            IMemoryCache cache) {
            _col = db.GetCollection<LeadshineCabinetIoOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
            _cache = cache;
        }

        public Task<LeadshineCabinetIoOptions> GetAsync(CancellationToken ct = default) {
            // 尝试从缓存获取
            if (_cache.TryGetValue<LeadshineCabinetIoOptions>(CacheKey, out var cached)) {
                return Task.FromResult(cached!);
            }

            try {
                var doc = _col.FindById(Key);
                var result = doc?.ToOptions() ?? new LeadshineCabinetIoOptions();
                
                // 缓存配置，使用配置的过期时间
                var cacheOptions = new MemoryCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(InfrastructureConstants.Cache.ConfigAbsoluteExpirationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(InfrastructureConstants.Cache.ConfigSlidingExpirationMinutes)
                };
                _cache.Set(CacheKey, result, cacheOptions);
                
                return Task.FromResult(result);
            }
            catch (LiteException ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(CabinetTriggerKind.Unknown, ErrorMessage);
                // 返回默认值
                return Task.FromResult(new LeadshineCabinetIoOptions());
            }
            // 让其他异常继续传播
        }

        public Task SaveAsync(LeadshineCabinetIoOptions options, CancellationToken ct = default)
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
