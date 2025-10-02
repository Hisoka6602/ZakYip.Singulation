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

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>
    /// 基于 LiteDB 的轴布局存储：单文档（Id = "singleton"）。
    /// </summary>
    public sealed class LiteDbAxisLayoutStore : IAxisLayoutStore {
        private const string Key = "singleton";
        private readonly ILiteCollection<AxisGridLayoutDoc> _coll;

        public LiteDbAxisLayoutStore(ILiteDatabase db) {
            _coll = db.GetCollection<AxisGridLayoutDoc>("axis_layout");
            _coll.EnsureIndex(x => x.Id, unique: true);
            if (_coll.FindById(Key) is null)
                _coll.Upsert(new AxisGridLayoutDoc { Id = Key, Rows = 0, Cols = 0 });
        }

        public Task<AxisGridLayoutOptions> GetAsync(CancellationToken ct = default)
            => Task.FromResult(_coll.FindById(Key)?.ToDto() ?? ConfigDefaults.AxisGrid());

        public Task UpsertAsync(AxisGridLayoutOptions layout, CancellationToken ct = default) {
            _coll.Upsert(layout.ToDoc());
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default) {
            _coll.Delete(Key);
            return Task.CompletedTask;
        }
    }
}