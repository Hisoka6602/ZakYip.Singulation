using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {
    /// <summary>
    /// 驱动参数模板配置对象，用于持久化驱动的核心参数。
    /// 此对象在 Core 层定义，避免直接依赖 Drivers.Common 层。
    /// NodeId 将在初始化过程中按轴自动填充。
    /// </summary>
    public sealed record class DriverOptionsTemplateOptions {
        /// <summary>
        /// 控制卡号，表示厂商 SDK 中的卡序号。
        /// </summary>
        [Range(0, 255, ErrorMessage = "控制卡号必须在 0 到 255 之间")]
        public int Card { get; init; } = 8;

        /// <summary>
        /// 端口编号，表示总线或通道编号。
        /// </summary>
        [Range(0, ushort.MaxValue, ErrorMessage = "端口编号必须在有效范围内")]
        public ushort Port { get; init; } = 2;

        /// <summary>
        /// 齿轮比，表示电机轴与负载轴之间的传动比。
        /// </summary>
        [Range(0.001, 1000, ErrorMessage = "齿轮比必须在 0.001 到 1000 之间")]
        public decimal GearRatio { get; init; } = 1m;

        /// <summary>
        /// 丝杠螺距，单位为毫米每转（mm/转）。
        /// </summary>
        [Range(0, 1000, ErrorMessage = "丝杠螺距必须在 0 到 1000 之间")]
        public decimal ScrewPitchMm { get; init; }

        /// <summary>
        /// 皮带轮直径，单位为毫米（mm）。
        /// </summary>
        [Range(0, 10000, ErrorMessage = "皮带轮直径必须在 0 到 10000 之间")]
        public decimal PulleyDiameterMm { get; init; }

        /// <summary>
        /// 辊筒或节径直径，单位为毫米（mm）。
        /// </summary>
        [Range(0, 10000, ErrorMessage = "辊筒直径必须在 0 到 10000 之间")]
        public decimal PulleyPitchDiameterMm { get; init; }

        /// <summary>
        /// 最大转速，单位为每分钟转数（rpm）。
        /// </summary>
        [Range(0, 10000, ErrorMessage = "最大转速必须在 0 到 10000 之间")]
        public decimal MaxRpm { get; init; } = 1813m;

        /// <summary>
        /// 最大加速度，单位为每秒转数变化（rpm/s）。
        /// </summary>
        [Range(0, 100000, ErrorMessage = "最大加速度必须在 0 到 100000 之间")]
        public decimal MaxAccelRpmPerSec { get; init; } = 1511m;

        /// <summary>
        /// 最大减速度，单位为每秒转数变化（rpm/s）。
        /// </summary>
        [Range(0, 100000, ErrorMessage = "最大减速度必须在 0 到 100000 之间")]
        public decimal MaxDecelRpmPerSec { get; init; } = 1511m;

        /// <summary>
        /// 写速度命令的最小间隔时间，单位为毫秒（ms）。
        /// </summary>
        [Range(0, 10000, ErrorMessage = "最小写入间隔必须在 0 到 10000 之间")]
        public int MinWriteInterval { get; init; } = 5;

        /// <summary>
        /// 连续失败次数阈值，达到此值时触发错误处理。
        /// </summary>
        [Range(1, 100, ErrorMessage = "连续失败阈值必须在 1 到 100 之间")]
        public int ConsecutiveFailThreshold { get; init; } = 5;

        /// <summary>
        /// 是否启用健康监测功能。
        /// </summary>
        public bool EnableHealthMonitor { get; init; } = true;

        /// <summary>
        /// 健康监测 Ping 间隔时间，单位为毫秒（ms）。
        /// </summary>
        [Range(100, 60000, ErrorMessage = "健康 Ping 间隔必须在 100 到 60000 之间")]
        public int HealthPingInterval { get; init; } = 500;
    }
}
