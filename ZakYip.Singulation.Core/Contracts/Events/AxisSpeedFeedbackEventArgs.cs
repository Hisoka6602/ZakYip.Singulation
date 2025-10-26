using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Contracts.Events {

    /// <summary>
    /// 轴实时速度反馈事件参数。
    /// </summary>
    public sealed class AxisSpeedFeedbackEventArgs : EventArgs {

        /// <summary>轴标识。</summary>
        public required AxisId Axis { get; init; }

        /// <summary>
        /// 实际转速（rpm）。
        /// <para>数学：rpm = rev/s × 60。</para>
        /// </summary>
        public decimal Rpm { get; init; }

        /// <summary>
        /// 实际线速度（m/s）。
        /// <para>数学：v = (rpm / 60) × (π·D)。</para>
        /// </summary>
        public decimal SpeedMps { get; init; }

        /// <summary>
        /// 实际脉冲频率（pps, pulses per second）。
        /// <para>数学：pps = (rpm / 60) × PPR。</para>
        /// </summary>
        public decimal PulsesPerSec { get; init; }

        /// <summary>时间戳（UTC）。</summary>
        public DateTime TimestampUtc { get; init; }
    }
}