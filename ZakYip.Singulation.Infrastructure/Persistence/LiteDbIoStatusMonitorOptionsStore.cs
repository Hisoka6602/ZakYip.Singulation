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

    /// <summary>基于 LiteDB 的 IoStatusMonitorOptions 单文档存储实现。</summary>
    public sealed class LiteDbIoStatusMonitorOptionsStore : IIoStatusMonitorOptionsStore {
        private const string CollName = "io_status_monitor_options";
        private const string Key = "default";
        private const string ErrorMessage = "读取DB配置异常：IoStatusMonitorOptions";

        private readonly ILiteCollection<IoStatusMonitorOptionsDoc> _col;
        private readonly ILogger<LiteDbIoStatusMonitorOptionsStore> _logger;
        private readonly ISafetyIsolator _safetyIsolator;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbIoStatusMonitorOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbIoStatusMonitorOptionsStore> logger,
            ISafetyIsolator safetyIsolator) {
            _col = db.GetCollection<IoStatusMonitorOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
        }

        public Task<IoStatusMonitorOptions> GetAsync(CancellationToken ct = default) {
            try {
                var doc = _col.FindById(Key);
                if (doc is null) {
                    // 返回默认值
                    return Task.FromResult(new IoStatusMonitorOptions());
                }
                return Task.FromResult(doc.ToOptions());
            }
            catch (Exception ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(SafetyTriggerKind.Unknown, ErrorMessage);
                // 返回默认值
                return Task.FromResult(new IoStatusMonitorOptions());
            }
        }

        public Task SaveAsync(IoStatusMonitorOptions options, CancellationToken ct = default)
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
