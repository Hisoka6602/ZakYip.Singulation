using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 速度联动配置选项。
    /// 定义基于轴速度变化自动控制 IO 端口的规则。
    /// </summary>
    public sealed record class SpeedLinkageOptions {
        /// <summary>
        /// 是否启用速度联动功能。
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// 速度联动组列表。
        /// 每个组定义一组轴和要联动的 IO 端口。
        /// </summary>
        public List<SpeedLinkageGroup> LinkageGroups { get; init; } = new();
    }
}
