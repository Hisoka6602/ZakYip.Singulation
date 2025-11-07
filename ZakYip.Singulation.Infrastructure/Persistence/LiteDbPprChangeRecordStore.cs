using LiteDB;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Infrastructure.Persistence {
    /// <summary>
    /// PPR 变化记录 LiteDB 存储实现
    /// </summary>
    public sealed class LiteDbPprChangeRecordStore : IPprChangeRecordStore {
        private readonly ILiteDatabase _db;
        private const string CollectionName = "ppr_change_records";

        public LiteDbPprChangeRecordStore(ILiteDatabase db) {
            _db = db;
            
            // 创建索引以提高查询性能
            var col = _db.GetCollection<PprChangeRecord>(CollectionName);
            col.EnsureIndex(x => x.AxisId);
            col.EnsureIndex(x => x.ChangedAt);
            col.EnsureIndex(x => x.IsAnomalous);
        }

        public Task SaveAsync(PprChangeRecord record, CancellationToken ct = default) {
            var col = _db.GetCollection<PprChangeRecord>(CollectionName);
            col.Upsert(record);
            return Task.CompletedTask;
        }

        public Task<List<PprChangeRecord>> GetByAxisIdAsync(string axisId, CancellationToken ct = default) {
            var col = _db.GetCollection<PprChangeRecord>(CollectionName);
            var records = col.Find(x => x.AxisId == axisId)
                .OrderByDescending(x => x.ChangedAt)
                .ToList();
            return Task.FromResult(records);
        }

        public Task<List<PprChangeRecord>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default) {
            var col = _db.GetCollection<PprChangeRecord>(CollectionName);
            var records = col.FindAll()
                .OrderByDescending(x => x.ChangedAt)
                .Skip(skip)
                .Take(take)
                .ToList();
            return Task.FromResult(records);
        }

        public Task<List<PprChangeRecord>> GetAnomalousAsync(CancellationToken ct = default) {
            var col = _db.GetCollection<PprChangeRecord>(CollectionName);
            var records = col.Find(x => x.IsAnomalous)
                .OrderByDescending(x => x.ChangedAt)
                .ToList();
            return Task.FromResult(records);
        }

        public Task DeleteOlderThanAsync(DateTime before, CancellationToken ct = default) {
            var col = _db.GetCollection<PprChangeRecord>(CollectionName);
            col.DeleteMany(x => x.ChangedAt < before);
            return Task.CompletedTask;
        }
    }
}
