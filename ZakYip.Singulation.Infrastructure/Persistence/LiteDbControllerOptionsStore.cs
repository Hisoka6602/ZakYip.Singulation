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
using ZakYip.Singulation.Core.Configs.Defaults;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>
    /// 基于 LiteDB 的控制器选项持久化存储，使用内存缓存减少数据库访问。
    /// </summary>
    public sealed class LiteDbControllerOptionsStore : IControllerOptionsStore {
        private const string Key = "default";
        private const string CacheKey = "controller_options_cache";
        private const string ErrorMessage = "读取DB配置异常：ControllerOptions";
        
        private readonly ILiteCollection<ControllerOptionsDoc> _coll;
        private readonly ILogger<LiteDbControllerOptionsStore> _logger;
        private readonly ICabinetIsolator _safetyIsolator;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// 初始化 <see cref="LiteDbControllerOptionsStore"/> 类的新实例。
        /// </summary>
        /// <param name="db">LiteDB 数据库实例。</param>
        /// <param name="logger">日志记录器。</param>
        /// <param name="safetyIsolator">安全隔离器。</param>
        /// <param name="cache">内存缓存实例。</param>
        public LiteDbControllerOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbControllerOptionsStore> logger,
            ICabinetIsolator safetyIsolator,
            IMemoryCache cache) {
            _coll = db.GetCollection<ControllerOptionsDoc>("controller_options");
            _coll.EnsureIndex(x => x.Id, unique: true);
            if (_coll.FindById(Key) is null) _coll.Upsert(new ControllerOptionsDoc { Id = Key });
            _logger = logger;
            _safetyIsolator = safetyIsolator;
            _cache = cache;
        }

        /// <inheritdoc />
        public Task<ControllerOptions> GetAsync(CancellationToken ct = default) {
            // 尝试从缓存获取
            if (_cache.TryGetValue<ControllerOptions>(CacheKey, out var cached)) {
                return Task.FromResult(cached!);
            }

            try {
                var result = _coll.FindById(Key)?.ToDto() ?? ConfigDefaults.Controller();
                
                // 缓存配置，5分钟过期
                var cacheOptions = new MemoryCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                };
                _cache.Set(CacheKey, result, cacheOptions);
                
                return Task.FromResult(result);
            }
            catch (Exception ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(CabinetTriggerKind.Unknown, ErrorMessage);
                return Task.FromResult(ConfigDefaults.Controller());
            }
        }

        /// <inheritdoc />
        public Task UpsertAsync(ControllerOptions dto, CancellationToken ct = default) {
            _coll.Upsert(dto.ToDoc(Key));
            // 更新后立即失效缓存
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteAsync(CancellationToken ct = default) {
            _coll.Delete(Key);
            // 删除后立即失效缓存
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }
    }
}