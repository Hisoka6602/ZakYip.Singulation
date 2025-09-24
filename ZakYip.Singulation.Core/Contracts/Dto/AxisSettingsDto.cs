using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>
    /// 持久化的轴设置 DTO。
    /// 与 DriverOptions 相似，但只保留可调整/需要长期存储的字段。
    /// </summary>
    public sealed class AxisSettingsDto {

        /// <summary>轴 ID（例如 1001/1002）。作为主键。</summary>
        public string AxisId { get; init; }

        /// <summary>卡号。</summary>
        public int Card { get; init; }

        /// <summary>端口编号。</summary>
        public ushort Port { get; init; }

        /// <summary>节点 ID。</summary>
        public ushort NodeId { get; init; }

        /// <summary>驱动 IP 地址（网络控制场景）。</summary>
        public string? IpAddress { get; init; }

        /// <summary>齿轮比（电机轴:负载轴）。</summary>
        public decimal GearRatio { get; init; } = 1m;

        /// <summary>丝杠螺距 (mm/转)。</summary>
        public decimal ScrewPitchMm { get; init; }

        /// <summary>皮带轮直径 (mm)。</summary>
        public decimal PulleyDiameterMm { get; init; }

        /// <summary>辊筒节径 (mm)。</summary>
        public decimal PulleyPitchDiameterMm { get; init; }

        /// <summary>最大转速 (rpm)。</summary>
        public decimal MaxRpm { get; init; } = 1813m;

        /// <summary>最大加速度 (rpm/s)。</summary>
        public decimal MaxAccelRpmPerSec { get; init; } = 1511m;

        /// <summary>最大减速度 (rpm/s)。</summary>
        public decimal MaxDecelRpmPerSec { get; init; } = 1511m;
    }
}