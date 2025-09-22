using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Abstractions.Events {

    /// <summary>
    /// 轴实时速度反馈事件参数。
    /// </summary>
    public sealed class AxisSpeedFeedbackEventArgs : EventArgs {

        /// <summary>轴标识。</summary>
        public AxisId Axis { get; }

        /// <summary>
        /// 实际转速（rpm）。
        /// <para>数学：rpm = rev/s × 60。</para>
        /// </summary>
        public double Rpm { get; }

        /// <summary>
        /// 实际线速度（m/s）。
        /// <para>数学：v = (rpm / 60) × (π·D)。</para>
        /// </summary>
        public double SpeedMps { get; }

        /// <summary>
        /// 实际脉冲频率（pps, pulses per second）。
        /// <para>数学：pps = (rpm / 60) × PPR。</para>
        /// </summary>
        public double PulsesPerSec { get; }

        /// <summary>时间戳（UTC）。</summary>
        public DateTime TimestampUtc { get; }

        public AxisSpeedFeedbackEventArgs(
            AxisId axis, double rpm, double speedMps, double pulsesPerSec, DateTime timestampUtc) {
            Axis = axis;
            Rpm = rpm;
            SpeedMps = speedMps;
            PulsesPerSec = pulsesPerSec;
            TimestampUtc = timestampUtc;
        }
    }
}