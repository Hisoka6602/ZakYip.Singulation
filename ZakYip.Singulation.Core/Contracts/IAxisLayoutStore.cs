using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 轴布局存储抽象：用于持久化网格布局单例资源。
    /// </summary>
    public interface IAxisLayoutStore {

        /// <summary>读取当前网格布局；不存在则返回 null。</summary>
        Task<AxisGridLayoutOptions?> GetAsync(CancellationToken ct = default);

        /// <summary>写入/覆盖网格布局（整体 upsert）。</summary>
        Task UpsertAsync(AxisGridLayoutOptions layout, CancellationToken ct = default);

        /// <summary>清除网格布局。</summary>
        Task DeleteAsync(CancellationToken ct = default);
    }
}