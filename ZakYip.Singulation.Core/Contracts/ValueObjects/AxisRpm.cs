using ZakYip.Singulation.Core.Utils;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    /// <summary>
    /// 轴转速值对象，统一封装 rpm 数值。
    /// </summary>
    public readonly record struct AxisRpm {
        /// <summary>转速数值（rpm）。</summary>
        public decimal Value { get; init; }

        /// <summary>
        /// 使用数值构造转速值对象。
        /// </summary>
        public AxisRpm(decimal value) {
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString("F2");

        /// <summary>将当前转速换算为线速度（mm/s）。</summary>
        public decimal ToMmPerSec(decimal pulleyPitchDiameterMm, decimal gearRatio, decimal screwPitchMm = 0m) =>
            AxisKinematics.RpmToMmPerSec(Value, screwPitchMm, pulleyPitchDiameterMm, gearRatio);

        /// <summary>将当前转速换算为脉冲频率（pps）。</summary>
        public decimal ToPulsePerSec(int pulsesPerRev) => AxisKinematics.RpmToPulsePerSec(Value, pulsesPerRev);

        /// <summary>根据线速度（mm/s）构造 RPM 值对象。</summary>
        public static AxisRpm FromMmPerSec(decimal mmPerSec, decimal pulleyPitchDiameterMm, decimal gearRatio, decimal screwPitchMm = 0m) =>
            new(AxisKinematics.MmPerSecToRpm(mmPerSec, screwPitchMm, pulleyPitchDiameterMm, gearRatio));

        /// <summary>线速度转脉冲频率（pps）。</summary>
        public static decimal MmPerSecToPps(decimal mmPerSec, decimal pulleyPitchDiameterMm, int pulsesPerRev, decimal gearRatio, decimal screwPitchMm = 0m) =>
            AxisKinematics.MmPerSecToPulsePerSec(mmPerSec, screwPitchMm, pulleyPitchDiameterMm, gearRatio, pulsesPerRev);

        /// <summary>线加速度（mm/s²）转 rpm/s。</summary>
        public static decimal MmPerSec2ToRpmPerSec(decimal accelMmPerSec2, decimal pulleyPitchDiameterMm, decimal gearRatio, decimal screwPitchMm = 0m) =>
            AxisKinematics.MmPerSec2ToRpmPerSec(accelMmPerSec2, screwPitchMm, pulleyPitchDiameterMm, gearRatio);

        /// <summary>rpm/s 转线加速度（mm/s²）。</summary>
        public static decimal RpmPerSecToMmPerSec2(decimal accelRpmPerSec, decimal pulleyPitchDiameterMm, decimal gearRatio, decimal screwPitchMm = 0m) =>
            AxisKinematics.RpmPerSecToMmPerSec2(accelRpmPerSec, screwPitchMm, pulleyPitchDiameterMm, gearRatio);
    }
}
