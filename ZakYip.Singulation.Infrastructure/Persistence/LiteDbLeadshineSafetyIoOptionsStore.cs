using LiteDB;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>基于 LiteDB 的 LeadshineSafetyIoOptions 单文档存储实现。</summary>
    public sealed class LiteDbLeadshineSafetyIoOptionsStore : ILeadshineSafetyIoOptionsStore {
        private const string CollName = "leadshine_safety_io_options";
        private const string Key = "default";

        private readonly ILiteCollection<LeadshineSafetyIoOptionsDoc> _col;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbLeadshineSafetyIoOptionsStore(ILiteDatabase db) {
            _col = db.GetCollection<LeadshineSafetyIoOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
        }

        public Task<LeadshineSafetyIoOptions> GetAsync(CancellationToken ct = default) {
            var doc = _col.FindById(Key);
            if (doc is null) {
                // 返回默认值
                return Task.FromResult(new LeadshineSafetyIoOptions());
            }
            return Task.FromResult(doc.ToOptions());
        }

        public Task SaveAsync(LeadshineSafetyIoOptions options, CancellationToken ct = default) =>
            Task.Run(() => {
                lock (_gate) {
                    var doc = options.ToDoc();
                    doc.Id = Key; // 强制单文档主键
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
