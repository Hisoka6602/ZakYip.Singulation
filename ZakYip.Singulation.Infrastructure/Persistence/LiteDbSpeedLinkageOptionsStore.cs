using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>基于 LiteDB 的 SpeedLinkageOptions 单文档存储实现。</summary>
    public sealed class LiteDbSpeedLinkageOptionsStore : ISpeedLinkageOptionsStore {
        private const string CollName = "speed_linkage_options";
        private const string Key = "default";
        private const string ErrorMessage = "读取DB配置异常：SpeedLinkageOptions";

        private readonly ILiteCollection<SpeedLinkageOptionsDoc> _col;
        private readonly ILogger<LiteDbSpeedLinkageOptionsStore> _logger;
        private readonly ICabinetIsolator _safetyIsolator;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbSpeedLinkageOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbSpeedLinkageOptionsStore> logger,
            ICabinetIsolator safetyIsolator) {
            _col = db.GetCollection<SpeedLinkageOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
        }

        public Task<SpeedLinkageOptions> GetAsync(CancellationToken ct = default) {
            try {
                var doc = _col.FindById(Key);
                if (doc is null) {
                    // 返回默认值
                    return Task.FromResult(new SpeedLinkageOptions());
                }
                return Task.FromResult(doc.ToOptions());
            }
            catch (Exception ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(Core.Enums.CabinetTriggerKind.Unknown, ErrorMessage);
                // 返回默认值
                return Task.FromResult(new SpeedLinkageOptions());
            }
        }

        public Task SaveAsync(SpeedLinkageOptions options, CancellationToken ct = default)
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
