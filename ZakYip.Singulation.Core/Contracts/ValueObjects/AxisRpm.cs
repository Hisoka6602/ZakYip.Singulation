using System.Diagnostics;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    [DebuggerDisplay("{Value} RPM")]
    public readonly record struct AxisRpm(decimal Value) {
        public static AxisRpm Zero => new(0);

        public AxisRpm Clamp(decimal min, decimal max) => new(Math.Min(max, Math.Max(min, Value)));

        /// RPM → RPS（rev/s）
        public decimal ToRevPerSec() => Value / 60.0m;

        /// RPM → m/s：v = (rpm / 60) × (π·D) / gearRatio × dir × scale
        public decimal ToMetersPerSec(
            decimal drumDiameterMeters,
            decimal gearRatio = 1.0m,
            int directionSign = +1,
            decimal linearScale = 1.0m) {
            if (drumDiameterMeters <= 0 || gearRatio <= 0) return 0;
            var v = ToRevPerSec() * (decimal)Math.PI * drumDiameterMeters / gearRatio;
            return v * directionSign * linearScale;
        }

        /// RPM → mm/s（便捷：直径单位为 mm）
        public decimal ToMmPerSec(
            decimal drumDiameterMm,
            decimal gearRatio = 1.0m,
            int directionSign = +1,
            decimal linearScale = 1.0m)
            => 1000.0m * ToMetersPerSec(drumDiameterMm / 1000.0m, gearRatio, directionSign, linearScale);

        /// m/s → RPM：rpm = (v / (π·D)) × gearRatio × 60 × dir × scale
        public static AxisRpm FromMetersPerSecond(
            decimal metersPerSec,
            decimal drumDiameterMeters,
            decimal gearRatio = 1.0m,
            int directionSign = +1,
            decimal linearScale = 1.0m) {
            if (drumDiameterMeters <= 0 || gearRatio <= 0) return default;
            var v = metersPerSec / (directionSign == 0 ? 1 : directionSign) / (linearScale <= 0 ? 1 : linearScale);
            var rps = (v / ((decimal)Math.PI * drumDiameterMeters)) * gearRatio;
            return new AxisRpm(rps * 60.0m);
        }

        /// mm/s → RPM（便捷：直径单位为 mm）
        public static AxisRpm FromMmPerSec(
            decimal mmPerSec,
            decimal drumDiameterMm,
            decimal gearRatio = 1.0m,
            int directionSign = +1,
            decimal linearScale = 1.0m)
            => FromMetersPerSecond(mmPerSec / 1000.0m, drumDiameterMm / 1000.0m, gearRatio, directionSign, linearScale);

        /// RPM → PPS：pps = (rpm / 60) × PPR / gearRatio
        public decimal ToPulsePerSec(int pulsesPerRev, decimal gearRatio = 1.0m)
            => (pulsesPerRev <= 0 || gearRatio <= 0) ? 0 : (Value / 60.0m) * pulsesPerRev / gearRatio;

        /// PPS → RPM：rpm = (pps / PPR) × 60
        public static AxisRpm FromPulsePerSec(decimal pulsesPerSec, int pulsesPerRev)
            => pulsesPerRev <= 0 ? default : new AxisRpm((pulsesPerSec / pulsesPerRev) * 60.0m);

        /// 线速度（mm/s）→ PPS：pps = ( v / (π·D) ) × PPR / gearRatio
        public static decimal MmPerSecToPps(decimal mmPerSec, decimal drumDiameterMm, int pulsesPerRev, decimal gearRatio = 1.0m)
            => (drumDiameterMm <= 0 || pulsesPerRev <= 0 || gearRatio <= 0) ? 0
             : (mmPerSec / ((decimal)Math.PI * drumDiameterMm)) * pulsesPerRev / gearRatio;

        /// PPS → 线速度（mm/s）：v = ( pps / PPR ) × (π·D) × gearRatio
        public static decimal PpsToMmPerSec(decimal pulsesPerSec, decimal drumDiameterMm, int pulsesPerRev, decimal gearRatio = 1.0m)
            => (drumDiameterMm <= 0 || pulsesPerRev <= 0 || gearRatio <= 0) ? 0
             : (pulsesPerSec / pulsesPerRev) * ((decimal)Math.PI * drumDiameterMm) * gearRatio;

        public decimal ToMmPerSec(KinematicParams k)
            => ToMmPerSec((decimal)(k.MmPerRev / Math.PI) * (decimal)Math.PI /*占位*/,
                k.GearRatio, k.DirectionSign, (decimal)k.LinearScale);
        public static explicit operator decimal(AxisRpm rpm) => rpm.Value;
        public static explicit operator AxisRpm(decimal rpm) => new(rpm);
    }
}