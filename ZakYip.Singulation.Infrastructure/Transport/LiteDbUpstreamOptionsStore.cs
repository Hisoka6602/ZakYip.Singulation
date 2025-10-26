using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Configs.Defaults;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>基于 LiteDB 的 UpstreamOptions 单文档存储实现。</summary>
    public sealed class LiteDbUpstreamOptionsStore : IUpstreamOptionsStore {
        private const string CollName = "upstream_options";
        private const string Key = "upstream_options_singleton";

        private readonly ILiteCollection<UpstreamOptionsDoc> _col;
        private readonly ILogger<LiteDbUpstreamOptionsStore> _logger;
        private readonly ISafetyIsolator _safetyIsolator;
        private readonly object _gate = new(); // 写入串行锁，避免并发竞态

        public LiteDbUpstreamOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbUpstreamOptionsStore> logger,
            ISafetyIsolator safetyIsolator) {
            _col = db.GetCollection<UpstreamOptionsDoc>(CollName);
            _col.EnsureIndex(x => x.Id, unique: true);
            _logger = logger;
            _safetyIsolator = safetyIsolator;
        }

        public Task<UpstreamOptions> GetAsync(CancellationToken ct = default) {
            try {
                return Task.FromResult(_col.FindById(Key)?.ToDto() ?? ConfigDefaults.Upstream());
            }
            catch (Exception ex) {
                _logger.LogError(ex, "读取DB配置异常：UpstreamOptions");
                _safetyIsolator.TryEnterDegraded(SafetyTriggerKind.Unknown, "读取DB配置异常：UpstreamOptions");
                return Task.FromResult(ConfigDefaults.Upstream());
            }
        }

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