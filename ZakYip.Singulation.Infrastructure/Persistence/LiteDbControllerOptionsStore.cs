using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;
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
    /// 基于 LiteDB 的控制器选项持久化存储。
    /// </summary>
    public sealed class LiteDbControllerOptionsStore : IControllerOptionsStore {
        private const string Key = "default";
        private const string ErrorMessage = "读取DB配置异常：ControllerOptions";
        
        private readonly ILiteCollection<ControllerOptionsDoc> _coll;
        private readonly ILogger<LiteDbControllerOptionsStore> _logger;
        private readonly ISafetyIsolator _safetyIsolator;

        /// <summary>
        /// 初始化 <see cref="LiteDbControllerOptionsStore"/> 类的新实例。
        /// </summary>
        /// <param name="db">LiteDB 数据库实例。</param>
        /// <param name="logger">日志记录器。</param>
        /// <param name="safetyIsolator">安全隔离器。</param>
        public LiteDbControllerOptionsStore(
            ILiteDatabase db,
            ILogger<LiteDbControllerOptionsStore> logger,
            ISafetyIsolator safetyIsolator) {
            _coll = db.GetCollection<ControllerOptionsDoc>("controller_options");
            _coll.EnsureIndex(x => x.Id, unique: true);
            if (_coll.FindById(Key) is null) _coll.Upsert(new ControllerOptionsDoc { Id = Key });
            _logger = logger;
            _safetyIsolator = safetyIsolator;
        }

        /// <inheritdoc />
        public Task<ControllerOptions> GetAsync(CancellationToken ct = default) {
            try {
                return Task.FromResult(_coll.FindById(Key)?.ToDto() ?? ConfigDefaults.Controller());
            }
            catch (Exception ex) {
                _logger.LogError(ex, ErrorMessage);
                _safetyIsolator.TryEnterDegraded(SafetyTriggerKind.Unknown, ErrorMessage);
                return Task.FromResult(ConfigDefaults.Controller());
            }
        }

        /// <inheritdoc />
        public Task UpsertAsync(ControllerOptions dto, CancellationToken ct = default) {
            _coll.Upsert(dto.ToDoc(Key));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteAsync(CancellationToken ct = default) {
            _coll.Delete(Key);
            return Task.CompletedTask;
        }
    }
}