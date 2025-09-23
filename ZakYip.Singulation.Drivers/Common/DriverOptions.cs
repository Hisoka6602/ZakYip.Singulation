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
    public record DriverOptions {
        /// <summary>控制卡号（厂商 SDK 的卡序号）。</summary>
        public required int Card { get; init; }

        /// <summary>端口编号（如总线/通道）。</summary>
        public required ushort Port { get; init; }

        /// <summary>节点 ID（驱动器从站地址）。</summary>
        public required byte NodeId { get; init; }

        /// <summary>齿轮比（电机轴:负载轴），例如 2.5 表示电机转 2.5 圈，负载 1 圈。</summary>
        public required decimal GearRatio { get; init; } = 1m;
        /// <summary>是否反转</summary>
        public required bool IsReverse { get; init; }
        /// <summary>丝杠螺距 (mm/转)</summary>
        public decimal ScrewPitchMm { get; init; }

        /// <summary>皮带轮直径 (mm)</summary>
        public decimal PulleyDiameterMm { get; init; }
        /// <summary>辊筒直径 (mm)</summary>
        public required decimal PulleyPitchDiameterMm { get; init; }

        /// <summary>最大转速 (rpm)。写速前限幅。</summary>
        public decimal MaxRpm { get; init; } = 1813m;

        /// <summary>最大加速度 (rpm/s)。写 0x6083 前限幅。</summary>
        public decimal MaxAccelRpmPerSec { get; init; } = 1511m;

        /// <summary>最大减速度 (rpm/s)。写 0x6084 前限幅。</summary>
        public decimal MaxDecelRpmPerSec { get; init; } = 1511m;

        /// <summary>写速度命令最小间隔（节流防抖）。</summary>
        public TimeSpan MinWriteInterval { get; init; } = TimeSpan.FromMilliseconds(5);

        /// <summary>连续失败次数阈值，超过后触发降级/断线。</summary>
        public int ConsecutiveFailThreshold { get; init; } = 5;

        /// <summary>启用健康监测（进入降级后启动 Ping 循环）。</summary>
        public bool EnableHealthMonitor { get; init; } = true;

        /// <summary>健康监测 Ping 间隔。</summary>
        public TimeSpan HealthPingInterval { get; init; } = TimeSpan.FromMilliseconds(500);
    }
}