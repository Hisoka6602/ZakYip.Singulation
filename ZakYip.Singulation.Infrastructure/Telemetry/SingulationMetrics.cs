using System.Diagnostics.Metrics;

namespace ZakYip.Singulation.Infrastructure.Telemetry {

    /// <summary>
    /// 单粒化系统的指标收集器，用于监控和追踪系统性能。
    /// </summary>
    public sealed class SingulationMetrics {
        /// <summary>
        /// 获取 <see cref="SingulationMetrics"/> 的单例实例。
        /// </summary>
        public static SingulationMetrics Instance { get; } = new();

        private readonly Meter _meter;

        private SingulationMetrics() {
            _meter = new Meter("ZakYip.Singulation", "1.0.0");
            FrameProcessedCounter = _meter.CreateCounter<long>("singulation_frames_processed");
            FrameDroppedCounter = _meter.CreateCounter<long>("singulation_frames_dropped");
            DegradeCounter = _meter.CreateCounter<long>("singulation_degrade_total");
            AxisFaultCounter = _meter.CreateCounter<long>("singulation_axis_fault_total");
            HeartbeatTimeoutCounter = _meter.CreateCounter<long>("singulation_heartbeat_timeout_total");
            SpeedDelta = _meter.CreateHistogram<double>("singulation_speed_delta_mmps");
            LoopDuration = _meter.CreateHistogram<double>("singulation_frame_loop_ms");
            FrameRtt = _meter.CreateHistogram<double>("singulation_frame_rtt_ms");
            CommissioningCycle = _meter.CreateHistogram<double>("singulation_commissioning_ms");
        }

        /// <summary>
        /// 已处理帧数计数器。
        /// </summary>
        public Counter<long> FrameProcessedCounter { get; }

        /// <summary>
        /// 丢弃帧数计数器。
        /// </summary>
        public Counter<long> FrameDroppedCounter { get; }

        /// <summary>
        /// 降级事件计数器。
        /// </summary>
        public Counter<long> DegradeCounter { get; }

        /// <summary>
        /// 轴故障计数器。
        /// </summary>
        public Counter<long> AxisFaultCounter { get; }

        /// <summary>
        /// 心跳超时计数器。
        /// </summary>
        public Counter<long> HeartbeatTimeoutCounter { get; }

        /// <summary>
        /// 速度差值直方图（单位：mm/s）。
        /// </summary>
        public Histogram<double> SpeedDelta { get; }

        /// <summary>
        /// 循环处理时间直方图（单位：ms）。
        /// </summary>
        public Histogram<double> LoopDuration { get; }

        /// <summary>
        /// 帧往返时间直方图（单位：ms）。
        /// </summary>
        public Histogram<double> FrameRtt { get; }

        /// <summary>
        /// 调试周期时间直方图（单位：ms）。
        /// </summary>
        public Histogram<double> CommissioningCycle { get; }
    }
}
