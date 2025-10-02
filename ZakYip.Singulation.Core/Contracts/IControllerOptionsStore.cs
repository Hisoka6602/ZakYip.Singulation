using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 控制器级 DriverOptions 模板的持久化抽象。
    /// 用于 IAxisController.InitializeAsync 调用时提供 vendor 与模板参数。
    /// </summary>
    public interface IControllerOptionsStore {

        /// <summary>读取当前控制器模板；不存在返回 null。</summary>
        Task<ControllerOptions> GetAsync(CancellationToken ct = default);

        /// <summary>写入/更新控制器模板（幂等）。</summary>
        Task UpsertAsync(ControllerOptions dto, CancellationToken ct = default);
    }
}