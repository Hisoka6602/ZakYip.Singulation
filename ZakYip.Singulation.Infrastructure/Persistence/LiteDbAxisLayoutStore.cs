using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>
    /// 基于 LiteDB 的轴布局存储（简化版）：直接存 AxisGridLayoutDto。
    /// 使用固定主键 "singleton" 表示唯一布局文档。
    /// </summary>
    public sealed class LiteDbAxisLayoutStore : IAxisLayoutStore {
        private const string DocId = "singleton";
        private readonly ILiteDatabase _db;
        private readonly ILiteCollection<AxisGridLayoutDoc> _coll;

        private sealed class AxisGridLayoutDoc {
            [BsonId] public string Id { get; set; } = DocId;
            public int Rows { get; set; }
            public int Cols { get; set; }
        }

        public LiteDbAxisLayoutStore(ILiteDatabase db) {
            _db = db;
            _coll = _db.GetCollection<AxisGridLayoutDoc>("axis_layout");
        }

        public Task<AxisGridLayoutDto?> GetAsync(CancellationToken ct = default) {
            var doc = _coll.FindById(DocId);
            if (doc is null) return Task.FromResult<AxisGridLayoutDto?>(null);
            return Task.FromResult<AxisGridLayoutDto?>(new AxisGridLayoutDto {
                Rows = doc.Rows,
                Cols = doc.Cols,
            });
        }

        public Task UpsertAsync(AxisGridLayoutDto layout, CancellationToken ct = default) {
            var doc = new AxisGridLayoutDoc {
                Id = DocId,
                Rows = layout.Rows,
                Cols = layout.Cols,
            };
            _coll.Upsert(doc);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default) {
            _coll.Delete(DocId);
            return Task.CompletedTask;
        }
    }
}