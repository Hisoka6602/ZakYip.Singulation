using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Common {

    /// <summary>
    /// 驱动运行参数（只读配置，常由配置文件绑定）。
    /// 用于限制命令下发速率、控制重试/退避行为，以及设定速度与加速度上限。
    /// </summary>
    public sealed class DriverOptions {

        /// <summary>
        /// 允许的最大转速（单位：RPM）。
        /// 实现方在下发速度前应做限幅，避免越界。
        /// </summary>
        /// <value>默认：3000。</value>
        public double MaxRpm { get; init; } = 3000;

        /// <summary>
        /// 允许的最大加速度（单位：RPM/s）。
        /// 供规划器或驱动内部做斜率限制，具体换算以协议/设备为准。
        /// </summary>
        /// <value>默认：2000 RPM/s。</value>
        public double MaxAccelRpmPerSec { get; init; } = 2000;

        /// <summary>
        /// 连续两条命令的最小间隔，用于节流（Throttle）。
        /// 防止总线/设备过载；实现应保证实际发送间隔 ≥ 该值。
        /// </summary>
        /// <value>默认：10ms。</value>
        public TimeSpan CommandMinInterval { get; init; } = TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// 指数退避的最大等待时间上限。
        /// 发送/请求失败时，重试延迟按倍数递增，但不超过该上限。
        /// </summary>
        /// <value>默认：10s。</value>
        public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// 允许的最大重试次数。
        /// 达到次数仍失败应向上抛出，由上层处理（标记离线/告警等）。
        /// </summary>
        /// <value>默认：5 次。</value>
        public int MaxRetries { get; init; } = 5;
    }
}