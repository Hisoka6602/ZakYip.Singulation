using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 上游连接参数的持久化仓储接口。
    /// 用于保存 / 查询 / 切换 LiteDB 中的 UpstreamOptions。
    /// </summary>
    public interface IUpstreamOptionsStore {

        /// <summary>读取配置（若不存在则返回 null）。</summary>
        Task<UpstreamOptions?> GetAsync(CancellationToken ct = default);

        /// <summary>写入配置（存在则更新，不存在则插入）。</summary>
        Task SaveAsync(UpstreamOptions dto, CancellationToken ct = default);

        /// <summary>删除配置（恢复“无配置”状态）。</summary>
        Task DeleteAsync(CancellationToken ct = default);
    }
}