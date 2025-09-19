using System.Diagnostics;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    /// <summary>
    /// 轴的目标转速值对象，物理单位为 RPM（revolutions per minute）。
    /// 不可变、轻量（record struct），用于规划器输出与驱动下发的中间表示。
    /// 语义约束：仅表示“数值 + 单位”，不包含轴标识；与拓扑顺序配合使用。
    /// </summary>
    [DebuggerDisplay("{Value} RPM")]
    public readonly record struct AxisRpm(double Value) {
        /// <summary>零转速常量（0 RPM）。</summary>
        public static AxisRpm Zero => new(0);

        /// <summary>
        /// 将当前 RPM 限幅到 [min, max] 区间，返回新的值对象。
        /// </summary>
        public AxisRpm Clamp(double min, double max) => new(Math.Min(max, Math.Max(min, Value)));

        /// <summary>
        /// 转换为“每秒转数”（RPS, revolutions per second）。
        /// </summary>
        public double ToRevPerSec() => Value / 60.0;

        /// <summary>
        /// 按给定 PPR（每转脉冲数）换算为“每秒脉冲数”（PPS）。
        /// 常用于驱动器脉冲频率输出：(RPM / 60) * PPR。
        /// </summary>
        public double ToPulsePerSec(int pulsesPerRev) => (Value / 60.0) * pulsesPerRev;

        /// <summary>
        /// 从线速度（m/s）按直径与齿轮比换算为 RPM 的工厂方法。
        /// rpm = (v / (π * 直径)) * 齿轮比 * 60
        /// </summary>
        public static AxisRpm FromMetersPerSecond(double metersPerSec, double beltDiameterMeters, double gearRatio = 1.0) {
            if (beltDiameterMeters <= 0) return default;
            var revPerSec = (metersPerSec / (Math.PI * beltDiameterMeters)) * gearRatio;
            return new AxisRpm(revPerSec * 60.0);
        }

        /// <summary>
        /// 显式转换为 double（单位保持 RPM）。
        /// 采用“显式”而非“隐式”，以杜绝单位误用。
        /// </summary>
        public static explicit operator double(AxisRpm rpm) => rpm.Value;

        /// <summary>
        /// 显式从 double（视为 RPM）构造 AxisRpm。
        /// </summary>
        public static explicit operator AxisRpm(double rpm) => new(rpm);
    }
}