using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 上游编解码配置的持久化抽象（幂等、线程安全）。
    /// </summary>
    public interface IUpstreamCodecOptionsStore {

        /// <summary>读取；若不存在则返回默认值。</summary>
        Task<UpstreamCodecOptions> GetAsync(CancellationToken ct = default);

        /// <summary>写入（有则更新，无则插入）。</summary>
        Task UpsertAsync(UpstreamCodecOptions options, CancellationToken ct = default);
    }
}