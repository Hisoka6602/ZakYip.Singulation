using System;
using System.Runtime.CompilerServices;

namespace ZakYip.Singulation.Core.Utils {

    /// <summary>
    /// 提供轴向运动学的常用换算：线速度/加速度、RPM 与编码器脉冲的互转。
    /// 默认以“电机轴”为参考；齿轮比为“电机:负载”。
    /// </summary>
    public static class AxisKinematics {
        private const decimal Pi = 3.1415926535897932384626433833m;

        /// <summary>
        /// 计算“电机轴每转”对应的线位移（mm）。
        /// 先优先使用丝杠螺距；若未提供，则退回皮带/辊筒直径。
        /// </summary>
        public static decimal ComputeLinearTravelPerMotorRevMm(decimal screwPitchMm, decimal pulleyPitchDiameterMm, decimal gearRatio) {
            if (gearRatio <= 0m) return 0m;

            if (screwPitchMm > 0m) {
                return screwPitchMm / gearRatio;
            }

            if (pulleyPitchDiameterMm > 0m) {
                var circumference = Pi * pulleyPitchDiameterMm;
                return circumference / gearRatio;
            }

            return 0m;
        }

        /// <summary>将线速度（mm/s）换算为电机轴转速（rpm）。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal MmPerSecToRpm(decimal mmPerSec, decimal screwPitchMm, decimal pulleyPitchDiameterMm, decimal gearRatio) {
            var travel = ComputeLinearTravelPerMotorRevMm(screwPitchMm, pulleyPitchDiameterMm, gearRatio);
            if (travel <= 0m) return 0m;
            return mmPerSec * 60m / travel;
        }

        /// <summary>将电机轴转速（rpm）换算为线速度（mm/s）。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal RpmToMmPerSec(decimal rpm, decimal screwPitchMm, decimal pulleyPitchDiameterMm, decimal gearRatio) {
            var travel = ComputeLinearTravelPerMotorRevMm(screwPitchMm, pulleyPitchDiameterMm, gearRatio);
            if (travel <= 0m) return 0m;
            return rpm * travel / 60m;
        }

        /// <summary>将线加速度（mm/s²）换算为 rpm/s。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal MmPerSec2ToRpmPerSec(decimal accelMmPerSec2, decimal screwPitchMm, decimal pulleyPitchDiameterMm, decimal gearRatio) {
            var travel = ComputeLinearTravelPerMotorRevMm(screwPitchMm, pulleyPitchDiameterMm, gearRatio);
            if (travel <= 0m) return 0m;
            return accelMmPerSec2 * 60m / travel;
        }

        /// <summary>将 rpm/s 换算为线加速度（mm/s²）。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal RpmPerSecToMmPerSec2(decimal accelRpmPerSec, decimal screwPitchMm, decimal pulleyPitchDiameterMm, decimal gearRatio) {
            var travel = ComputeLinearTravelPerMotorRevMm(screwPitchMm, pulleyPitchDiameterMm, gearRatio);
            if (travel <= 0m) return 0m;
            return accelRpmPerSec * travel / 60m;
        }

        /// <summary>将线速度（mm/s）换算为编码器脉冲频率（pps）。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal MmPerSecToPulsePerSec(decimal mmPerSec, decimal screwPitchMm, decimal pulleyPitchDiameterMm, decimal gearRatio, int pulsesPerRev) {
            if (pulsesPerRev <= 0) return 0m;
            var rpm = MmPerSecToRpm(mmPerSec, screwPitchMm, pulleyPitchDiameterMm, gearRatio);
            return RpmToPulsePerSec(rpm, pulsesPerRev);
        }

        /// <summary>将转速（rpm）换算为编码器脉冲频率（pps）。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal RpmToPulsePerSec(decimal rpm, int pulsesPerRev) {
            if (pulsesPerRev <= 0) return 0m;
            return rpm / 60m * pulsesPerRev;
        }
    }
}
