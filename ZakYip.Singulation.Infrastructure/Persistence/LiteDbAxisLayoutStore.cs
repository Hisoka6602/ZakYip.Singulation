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

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>
    /// 基于 LiteDB 的轴布局存储：单文档（Id = "singleton"）。
    /// </summary>
    public sealed class LiteDbAxisLayoutStore : IAxisLayoutStore {
        private const string Key = "singleton";
        private const string ErrorMessage = "读取DB配置异常：AxisGridLayoutOptions";
        
        private readonly ILiteCollection<AxisGridLayoutDoc> _coll;
        private readonly ILogger<LiteDbAxisLayoutStore> _logger;
        private readonly ISafetyIsolator _safetyIsolator;

        public LiteDbAxisLayoutStore(
            ILiteDatabase db,
            ILogger<LiteDbAxisLayoutStore> logger,
            ISafetyIsolator safetyIsolator) {
            _coll = db.GetCollection<AxisGridLayoutDoc>("axis_layout");
            _coll.EnsureIndex(x => x.Id, unique: true);
            if (_coll.FindById(Key) is null)
                _coll.Upsert(new AxisGridLayoutDoc { Id = Key, Rows = 0, Cols = 0 });
            _logger = logger;
            _safetyIsolator = safetyIsolator;
        }

        public Task<AxisGridLayoutOptions> GetAsync(CancellationToken ct = default) {
            try {
                return Task.FromResult(_coll.FindById(Key)?.ToDto() ?? ConfigDefaults.AxisGrid());
            }
            catch (Exception ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(SafetyTriggerKind.Unknown, ErrorMessage);
                return Task.FromResult(ConfigDefaults.AxisGrid());
            }
        }

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