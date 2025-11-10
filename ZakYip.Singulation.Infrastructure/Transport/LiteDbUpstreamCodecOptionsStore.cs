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

    /// <summary>LiteDB 实现：单文档配置（Id = "default"），使用内存缓存减少数据库访问。集合：upstream_codec_options</summary>
    public sealed class LiteDbUpstreamCodecOptionsStore : IUpstreamCodecOptionsStore, IDisposable {
        private const string CollName = "upstream_codec_options";
        private const string Key = "default";
        private const string CacheKey = "upstream_codec_options_cache";
        private const string ErrorMessage = "读取DB配置异常：UpstreamCodecOptions";
        
        private readonly ILiteDatabase _db;
        private readonly ILogger<LiteDbUpstreamCodecOptionsStore> _logger;
        private readonly ICabinetIsolator _safetyIsolator;
        private readonly IMemoryCache _cache;
        private readonly object _gate = new();

        public LiteDbUpstreamCodecOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbUpstreamCodecOptionsStore> logger,
            ICabinetIsolator safetyIsolator,
            IMemoryCache cache) {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
            col.EnsureIndex(x => x.Id, unique: true);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
            _cache = cache;
        }

        public Task<UpstreamCodecOptions> GetAsync(CancellationToken ct = default) {
            // 尝试从缓存获取
            if (_cache.TryGetValue<UpstreamCodecOptions>(CacheKey, out var cached)) {
                return Task.FromResult(cached!);
            }

            try {
                lock (_gate) {
                    var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
                    var doc = col.FindById(Key);
                    var result = doc?.ToOptions() ?? ConfigDefaults.Codec();
                    
                    // 缓存配置，使用配置的过期时间
                    var cacheOptions = new MemoryCacheEntryOptions {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(InfrastructureConstants.Cache.ConfigAbsoluteExpirationMinutes),
                        SlidingExpiration = TimeSpan.FromMinutes(InfrastructureConstants.Cache.ConfigSlidingExpirationMinutes)
                    };
                    _cache.Set(CacheKey, result, cacheOptions);
                    
                    return Task.FromResult(result);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(CabinetTriggerKind.Unknown, ErrorMessage);
                return Task.FromResult(ConfigDefaults.Codec());
            }
        }

        public Task UpsertAsync(UpstreamCodecOptions options, CancellationToken ct = default) {
            if (options is null) throw new ArgumentNullException(nameof(options));
            lock (_gate) {
                var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
                col.Upsert(options.ToDoc() with { Id = Key });
                // 更新后立即失效缓存
                _cache.Remove(CacheKey);
            }
            return Task.CompletedTask;
        }

        public void Dispose() {
            /* 由容器统一释放 ILiteDatabase */
        }
    }
}