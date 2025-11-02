using System.Collections.Generic;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    /// <summary>
    /// 单个 IO 联动点的 LiteDB 文档实体。
    /// </summary>
    public sealed class IoLinkagePointDoc {
        /// <summary>IO 端口编号（0-1023）。</summary>
        public int BitNumber { get; set; }

        /// <summary>目标电平状态（0=ActiveHigh/高电平, 1=ActiveLow/低电平）。</summary>
        public int Level { get; set; }
    }

    /// <summary>
    /// IO 联动配置的 LiteDB 文档实体。
    /// </summary>
    public sealed class IoLinkageOptionsDoc {
        /// <summary>文档 ID（单例模式，固定为 "default"）。</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>是否启用 IO 联动功能。</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>运行中状态时联动的 IO 点列表。</summary>
        public List<IoLinkagePointDoc> RunningStateIos { get; set; } = new();

        /// <summary>停止/复位状态时联动的 IO 点列表。</summary>
        public List<IoLinkagePointDoc> StoppedStateIos { get; set; } = new();
    }
}
