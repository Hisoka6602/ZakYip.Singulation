using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>基于 LiteDB 的 LeadshineSafetyIoOptions 单文档存储实现。</summary>
    public sealed class LiteDbLeadshineSafetyIoOptionsStore : ILeadshineSafetyIoOptionsStore {
        private const string CollName = "leadshine_safety_io_options";
        private const string Key = "default";

        private readonly ILiteCollection<LeadshineSafetyIoOptionsDoc> _col;
        private readonly ILogger<LiteDbLeadshineSafetyIoOptionsStore> _logger;
        private readonly ISafetyIsolator _safetyIsolator;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbLeadshineSafetyIoOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbLeadshineSafetyIoOptionsStore> logger,
            ISafetyIsolator safetyIsolator) {
            _col = db.GetCollection<LeadshineSafetyIoOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
        }

        public Task<LeadshineSafetyIoOptions> GetAsync(CancellationToken ct = default) {
            try {
                var doc = _col.FindById(Key);
                if (doc is null) {
                    // 返回默认值
                    return Task.FromResult(new LeadshineSafetyIoOptions());
                }
                return Task.FromResult(doc.ToOptions());
            }
            catch (Exception ex) {
                _logger.LogError(ex, "读取DB配置异常：LeadshineSafetyIoOptions");
                _safetyIsolator.TryEnterDegraded(SafetyTriggerKind.Unknown, "读取DB配置异常：LeadshineSafetyIoOptions");
                // 返回默认值
                return Task.FromResult(new LeadshineSafetyIoOptions());
            }
        }

        public Task SaveAsync(LeadshineSafetyIoOptions options, CancellationToken ct = default)
        {
            lock (_gate)
            {
                var doc = options.ToDoc();
                doc.Id = Key; // 强制单文档主键
                _col.Upsert(doc);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default)
        {
            lock (_gate)
            {
                _col.Delete(Key);
            }
            return Task.CompletedTask;
        }
    }
}
