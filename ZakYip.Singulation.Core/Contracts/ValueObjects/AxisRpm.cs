using System.Diagnostics;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    [DebuggerDisplay("{Value} RPM")]
    public readonly record struct AxisRpm(double Value) {
        public static AxisRpm Zero => new(0);

        public AxisRpm Clamp(double min, double max) => new(Math.Min(max, Math.Max(min, Value)));

        /// <summary>RPM → RPS（rev/s）</summary>
        public double ToRevPerSec() => Value / 60.0;

        /// <summary>RPM → PPS（pulses/s）：pps = (rpm / 60) × PPR</summary>
        public double ToPulsePerSec(int pulsesPerRev) => (Value / 60.0) * pulsesPerRev;

        /// <summary>RPM → m/s（SI）：v = (rpm / 60) × (π·D) / gearRatio</summary>
        public double ToMetersPerSec(double drumDiameterMeters, double gearRatio = 1.0) {
            if (drumDiameterMeters <= 0 || gearRatio <= 0) return 0;
            return ToRevPerSec() * Math.PI * drumDiameterMeters / gearRatio;
        }

        /// <summary>RPM → m/s（便捷：直径单位为 mm）</summary>
        public double ToMetersPerSecByMm(double drumDiameterMm, double gearRatio = 1.0)
            => ToMetersPerSec(drumDiameterMm / 1000.0, gearRatio);

        /// <summary>PPS → RPM：rpm = (pps / PPR) × 60</summary>
        public static AxisRpm FromPulsePerSec(double pulsesPerSec, int pulsesPerRev) {
            if (pulsesPerRev <= 0) return default;
            var rps = pulsesPerSec / pulsesPerRev;
            return new AxisRpm(rps * 60.0);
        }

        /// <summary>m/s → RPM（SI）：rpm = (v / (π·D)) × gearRatio × 60</summary>
        public static AxisRpm FromMetersPerSecond(double metersPerSec, double drumDiameterMeters, double gearRatio = 1.0) {
            if (drumDiameterMeters <= 0 || gearRatio <= 0) return default;
            var rps = (metersPerSec / (Math.PI * drumDiameterMeters)) * gearRatio;
            return new AxisRpm(rps * 60.0);
        }

        /// <summary>m/s → RPM（便捷：直径单位为 mm）</summary>
        public static AxisRpm FromMetersPerSecondMm(double metersPerSec, double drumDiameterMm, double gearRatio = 1.0)
            => FromMetersPerSecond(metersPerSec, drumDiameterMm / 1000.0, gearRatio);
        /// <summary>
        /// 由线速度（mm/s）换算“每秒脉冲数”（PPS, pulses per second）。
        /// 数学公式：pps = ( v(mm/s) / (π·D(mm)) ) × PPR
        /// </summary>
        /// <param name="mmPerSec">线速度，单位 mm/s。</param>
        /// <param name="drumDiameterMm">驱动滚筒直径，单位 mm。</param>
        /// <param name="pulsesPerRev">每转脉冲数（PPR）。</param>
        /// <returns>脉冲频率 pps；入参非法时返回 0。</returns>
        public static double MmPerSecToPps(double mmPerSec, double drumDiameterMm, int pulsesPerRev) {
            if (drumDiameterMm <= 0 || pulsesPerRev <= 0) return 0;
            return (mmPerSec / (Math.PI * drumDiameterMm)) * pulsesPerRev;
        }

        /// <summary>
        /// 由“每秒脉冲数”（PPS）换算线速度（mm/s）。
        /// 数学公式：v(mm/s) = ( pps / PPR ) × (π·D(mm))
        /// </summary>
        /// <param name="pulsesPerSec">脉冲频率 pps。</param>
        /// <param name="drumDiameterMm">驱动滚筒直径，单位 mm。</param>
        /// <param name="pulsesPerRev">每转脉冲数（PPR）。</param>
        /// <returns>线速度 mm/s；入参非法时返回 0。</returns>
        public static double PpsToMmPerSec(double pulsesPerSec, double drumDiameterMm, int pulsesPerRev) {
            if (drumDiameterMm <= 0 || pulsesPerRev <= 0) return 0;
            return (pulsesPerSec / pulsesPerRev) * (Math.PI * drumDiameterMm);
        }
        public static explicit operator double(AxisRpm rpm) => rpm.Value;
        public static explicit operator AxisRpm(double rpm) => new(rpm);
    }
}