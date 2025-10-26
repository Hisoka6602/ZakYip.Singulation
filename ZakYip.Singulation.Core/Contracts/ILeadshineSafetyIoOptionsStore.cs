using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 雷赛安全 IO 配置的持久化仓储接口。
    /// 用于保存 / 查询 LiteDB 中的 LeadshineSafetyIoOptions。
    /// </summary>
    public interface ILeadshineSafetyIoOptionsStore {

        /// <summary>读取配置（若不存在则返回默认值）。</summary>
        Task<LeadshineSafetyIoOptions> GetAsync(CancellationToken ct = default);

        /// <summary>写入配置（存在则更新，不存在则插入）。</summary>
        Task SaveAsync(LeadshineSafetyIoOptions options, CancellationToken ct = default);

        /// <summary>删除配置（恢复"无配置"状态）。</summary>
        Task DeleteAsync(CancellationToken ct = default);
    }
}
