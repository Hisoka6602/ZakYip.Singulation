using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>
    /// LiteDB 实现：单文档配置（固定 Id = "default"）。
    /// 集合名：upstream_codec_options
    /// </summary>
    public sealed class LiteDbUpstreamCodecOptionsStore : IUpstreamCodecOptionsStore, IDisposable {
        private const string CollName = "upstream_codec_options";
        private static readonly BsonValue FixedId = "default";

        private readonly ILiteDatabase _db;
        private readonly object _gate = new();

        public LiteDbUpstreamCodecOptionsStore(ILiteDatabase db) {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            // 建索引（幂等）
            var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
            col.EnsureIndex(x => x.Id, unique: true);
        }

        public Task<UpstreamCodecOptions> GetAsync(CancellationToken ct = default) {
            lock (_gate) {
                var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
                var doc = col.FindById(FixedId);
                if (doc is null) {
                    // 不存在则返回默认值（并不强制写回）
                    return Task.FromResult(new UpstreamCodecOptions());
                }

                return Task.FromResult(new UpstreamCodecOptions {
                    MainCount = doc.MainCount,
                    EjectCount = doc.EjectCount
                });
            }
        }

        public Task UpsertAsync(UpstreamCodecOptions options, CancellationToken ct = default) {
            if (options is null) throw new ArgumentNullException(nameof(options));

            lock (_gate) {
                var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
                var doc = new UpstreamCodecOptionsDoc {
                    Id = FixedId,
                    MainCount = options.MainCount,
                    EjectCount = options.EjectCount
                };
                col.Upsert(doc);
            }

            return Task.CompletedTask;
        }

        public void Dispose() {
            // 如果 ILiteDatabase 是外部托管，就不要 Dispose。
            // 这里按依赖注入约定，通常由容器统一释放，不在此处 Dispose。
        }

        // --- 内部持久化模型（与领域模型解耦） ---
        private sealed class UpstreamCodecOptionsDoc {
            public BsonValue Id { get; set; } = FixedId;
            public int MainCount { get; set; }
            public int EjectCount { get; set; }
        }
    }
}