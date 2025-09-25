using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// DriverOptions 的可序列化镜像（仅保留需长期持久化字段；NodeId 将在初始化中按轴自动填）。
    /// 注意：此 DTO 在 Core 中定义，避免 Core 直接依赖 Drivers.Common。
    /// </summary>
    public sealed record DriverOptionsTemplateDto {
        /// <summary>控制卡号（厂商 SDK 的卡序号）。</summary>
        public int Card { get; init; } = 8;

        /// <summary>端口编号（如总线/通道）。</summary>
        public ushort Port { get; init; } = 2;
        /// <summary>齿轮比（电机轴:负载轴）。</summary>
        public decimal GearRatio { get; init; } = 1m;

        /// <summary>丝杠螺距 (mm/转)。</summary>
        public decimal ScrewPitchMm { get; init; }

        /// <summary>皮带轮直径 (mm)。</summary>
        public decimal PulleyDiameterMm { get; init; }

        /// <summary>辊筒/节径直径 (mm)。</summary>
        public decimal PulleyPitchDiameterMm { get; init; }

        /// <summary>最大转速 (rpm)。</summary>
        public decimal MaxRpm { get; init; } = 1813m;

        /// <summary>最大加速度 (rpm/s)。</summary>
        public decimal MaxAccelRpmPerSec { get; init; } = 1511m;

        /// <summary>最大减速度 (rpm/s)。</summary>
        public decimal MaxDecelRpmPerSec { get; init; } = 1511m;

        /// <summary>写速度命令最小间隔。</summary>
        public int MinWriteInterval { get; init; } = 5;

        /// <summary>连续失败次数阈值。</summary>
        public int ConsecutiveFailThreshold { get; init; } = 5;

        /// <summary>启用健康监测。</summary>
        public bool EnableHealthMonitor { get; init; } = true;

        /// <summary>健康监测 Ping 间隔。</summary>
        public int HealthPingInterval { get; init; } = 500;
    }
}