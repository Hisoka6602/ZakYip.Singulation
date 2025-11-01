using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 雷赛控制面板 IO 配置的持久化仓储接口。
    /// 用于保存 / 查询 LiteDB 中的 LeadshineCabinetIoOptions。
    /// </summary>
    public interface ILeadshineCabinetIoOptionsStore {

        /// <summary>读取配置（若不存在则返回默认值）。</summary>
        Task<LeadshineCabinetIoOptions> GetAsync(CancellationToken ct = default);

        /// <summary>写入配置（存在则更新，不存在则插入）。</summary>
        Task SaveAsync(LeadshineCabinetIoOptions options, CancellationToken ct = default);

        /// <summary>删除配置（恢复"无配置"状态）。</summary>
        Task DeleteAsync(CancellationToken ct = default);
    }
}
