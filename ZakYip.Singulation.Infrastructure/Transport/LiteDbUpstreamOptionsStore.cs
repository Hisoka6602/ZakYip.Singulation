using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto.Transport;
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

            // 种子：没有就写默认
            if (_col.FindById(Key) is null) {
                _col.Upsert(new UpstreamOptionsDoc { Id = Key });
            }
        }

        public Task<UpstreamOptionsDto?> GetAsync(CancellationToken ct = default) =>
            Task.Run(() => _col.FindById(Key)?.ToDto(), ct);

        public Task SaveAsync(UpstreamOptionsDto dto, CancellationToken ct = default) =>
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