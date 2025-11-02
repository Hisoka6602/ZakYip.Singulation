using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// IO 联动配置选项。
    /// 定义在不同系统状态下要联动的 IO 端口组。
    /// </summary>
    public sealed record class IoLinkageOptions {
        /// <summary>
        /// 是否启用 IO 联动功能。
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// 运行中状态时联动的 IO 点列表。
        /// 例如：运行中时将 IO 3、5、6 设置为低电平。
        /// </summary>
        public List<IoLinkagePoint> RunningStateIos { get; init; } = new();

        /// <summary>
        /// 停止/复位状态时联动的 IO 点列表。
        /// 例如：停止/复位时将 IO 3、5、6 设置为高电平。
        /// </summary>
        public List<IoLinkagePoint> StoppedStateIos { get; init; } = new();
    }
}
