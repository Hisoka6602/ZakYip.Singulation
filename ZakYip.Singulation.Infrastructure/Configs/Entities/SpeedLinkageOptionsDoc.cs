using System.Collections.Generic;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    /// <summary>
    /// 速度联动 IO 点的 LiteDB 文档实体。
    /// </summary>
    public sealed class SpeedLinkageIoPointDoc {
        /// <summary>IO 端口编号（0-1023）。</summary>
        public int BitNumber { get; set; }

        /// <summary>当所有轴速度为0时的目标电平状态（0=ActiveHigh/高电平, 1=ActiveLow/低电平）。</summary>
        public int LevelWhenStopped { get; set; }
    }

    /// <summary>
    /// 速度联动组的 LiteDB 文档实体。
    /// </summary>
    public sealed class SpeedLinkageGroupDoc {
        /// <summary>组内的轴 ID 列表。</summary>
        public List<int> AxisIds { get; set; } = new();

        /// <summary>联动的 IO 点列表。</summary>
        public List<SpeedLinkageIoPointDoc> IoPoints { get; set; } = new();
    }

    /// <summary>
    /// 速度联动配置的 LiteDB 文档实体。
    /// </summary>
    public sealed class SpeedLinkageOptionsDoc {
        /// <summary>文档 ID（单例模式，固定为 "default"）。</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>是否启用速度联动功能。</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>速度联动组列表。</summary>
        public List<SpeedLinkageGroupDoc> LinkageGroups { get; set; } = new();
    }
}
