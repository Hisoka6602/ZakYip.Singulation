using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto.Transport;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>
    /// 基于 LiteDB 的 UpstreamOptions 单文档存储实现。
    /// </summary>
    public sealed class LiteDbUpstreamOptionsStore : IUpstreamOptionsStore {
        private readonly ILiteCollection<UpstreamOptionsDto> _col;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbUpstreamOptionsStore(ILiteDatabase db) {
            _col = db.GetCollection<UpstreamOptionsDto>("upstream_options");
            // 单文档也建议建索引，加速按主键读写
            _col.EnsureIndex(x => x.Id, unique: true);

            // 如果完全没有文档，可以选择插入一个默认值（可选）
            if (_col.Count() == 0) {
                var seed = new UpstreamOptionsDto(); // 全默认
                _col.Insert(seed);
            }
        }

        /// <inheritdoc/>
        public Task<UpstreamOptionsDto?> GetAsync(CancellationToken ct = default) =>
            Task.Run(() => _col.FindById("upstream_options_singleton"), ct);

        /// <inheritdoc/>
        public Task SaveAsync(UpstreamOptionsDto dto, CancellationToken ct = default) =>
            Task.Run(() => {
                lock (_gate) {
                    // 强制单文档主键
                    dto.Id = "upstream_options_singleton";

                    var existed = _col.Exists(x => x.Id == dto.Id);
                    if (existed) _col.Update(dto);
                    else _col.Insert(dto);
                }
            }, ct);

        /// <inheritdoc/>
        public Task DeleteAsync(CancellationToken ct = default) =>
            Task.Run(() => {
                lock (_gate) {
                    _col.Delete("upstream_options_singleton");
                }
            }, ct);
    }
}