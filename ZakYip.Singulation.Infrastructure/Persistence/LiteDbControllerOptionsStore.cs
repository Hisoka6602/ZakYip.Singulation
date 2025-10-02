using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Configs.Defaults;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    public sealed class LiteDbControllerOptionsStore : IControllerOptionsStore {
        private const string Key = "default";
        private readonly ILiteCollection<ControllerOptionsDoc> _coll;

        public LiteDbControllerOptionsStore(ILiteDatabase db) {
            _coll = db.GetCollection<ControllerOptionsDoc>("controller_options");
            _coll.EnsureIndex(x => x.Id, unique: true);
            if (_coll.FindById(Key) is null) _coll.Upsert(new ControllerOptionsDoc { Id = Key });
        }

        public Task<ControllerOptions> GetAsync(CancellationToken ct = default)
            => Task.FromResult(_coll.FindById(Key)?.ToDto() ?? ConfigDefaults.Controller());

        public Task UpsertAsync(ControllerOptions dto, CancellationToken ct = default) {
            _coll.Upsert(dto.ToDoc(Key));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default) {
            _coll.Delete(Key);
            return Task.CompletedTask;
        }
    }
}