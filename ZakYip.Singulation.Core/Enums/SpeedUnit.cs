using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 速度值的单位类型。
    /// 用于标记 <see cref="SpeedSet"/> 中的速度集合所采用的物理单位。
    /// </summary>
    public enum SpeedUnit {

        /// <summary>
        /// 以米每秒 (m/s) 表示的线速度。
        /// 常见于视觉系统直接测得的输送带线速度。
        /// </summary>
        [Description("米每秒 (m/s)")]
        MetersPerSecond = 0,

        /// <summary>
        /// 以每分钟转数 (RPM) 表示的电机转速。
        /// 常见于驱动器层面的速度指令或反馈。
        /// </summary>
        [Description("转每分 (RPM)")]
        Rpm = 1
    }
}