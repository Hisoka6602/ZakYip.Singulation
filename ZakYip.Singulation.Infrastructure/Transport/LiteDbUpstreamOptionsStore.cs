using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Configs.Defaults;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>基于 LiteDB 的 UpstreamOptions 单文档存储实现。</summary>
    public sealed class LiteDbUpstreamOptionsStore : IUpstreamOptionsStore {
        private const string CollName = "upstream_options";
        private const string Key = "upstream_options_singleton";

        private readonly ILiteCollection<UpstreamOptionsDoc> _col;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbUpstreamOptionsStore(ILiteDatabase db) {
            _col = db.GetCollection<UpstreamOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
        }

        public Task<UpstreamOptions> GetAsync(CancellationToken ct = default)
            => Task.FromResult(_col.FindById(Key)?.ToDto() ?? ConfigDefaults.Upstream());

        public Task SaveAsync(UpstreamOptions dto, CancellationToken ct = default) =>
            Task.Run(() => {
                lock (_gate) {
                    var doc = dto.ToDoc();
                    doc.Id = Key;           // 强制单文档主键
                    _col.Upsert(doc);
                }
            }, ct);

        public Task DeleteAsync(CancellationToken ct = default) =>
            Task.Run(() => {
                lock (_gate) {
                    _col.Delete(Key);
                }
            }, ct);
    }
}