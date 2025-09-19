using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 上游速度数据的来源标记。
    /// 支持位标记（Flags），允许多种来源同时存在。
    /// 例如：Vision | Simulated。
    /// </summary>
    [Flags]
    public enum SourceFlags {

        /// <summary>
        /// 无来源（默认值）。
        /// 通常表示未初始化或未知来源。
        /// </summary>
        [Description("None - 无来源")]
        None = 0,

        /// <summary>
        /// 来源：真实视觉系统。
        /// 表示数据由上游相机/视觉检测模块提供。
        /// </summary>
        [Description("Vision - 视觉系统")]
        Vision = 1,

        /// <summary>
        /// 来源：仿真数据。
        /// 常用于测试/回放，不依赖真实硬件。
        /// </summary>
        [Description("Simulated - 仿真生成")]
        Simulated = 2
    }
}