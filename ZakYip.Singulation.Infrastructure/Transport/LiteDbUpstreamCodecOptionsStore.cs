using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>LiteDB 实现：单文档配置（Id = "default"）。集合：upstream_codec_options</summary>
    public sealed class LiteDbUpstreamCodecOptionsStore : IUpstreamCodecOptionsStore, IDisposable {
        private const string CollName = "upstream_codec_options";
        private const string Key = "default";
        private readonly ILiteDatabase _db;
        private readonly object _gate = new();

        public LiteDbUpstreamCodecOptionsStore(ILiteDatabase db) {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
            col.EnsureIndex(x => x.Id, unique: true);
            if (col.FindById(Key) is null) col.Upsert(new UpstreamCodecOptionsDoc { Id = Key });
        }

        public Task<UpstreamCodecOptions> GetAsync(CancellationToken ct = default) {
            lock (_gate) {
                var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
                var doc = col.FindById(Key);
                return Task.FromResult((doc ?? new UpstreamCodecOptionsDoc { Id = Key }).ToOptions());
            }
        }

        public Task UpsertAsync(UpstreamCodecOptions options, CancellationToken ct = default) {
            if (options is null) throw new ArgumentNullException(nameof(options));
            lock (_gate) {
                var col = _db.GetCollection<UpstreamCodecOptionsDoc>(CollName);
                col.Upsert(options.ToDoc() with { Id = Key });
            }
            return Task.CompletedTask;
        }

        public void Dispose() {
            /* 由容器统一释放 ILiteDatabase */
        }
    }
}