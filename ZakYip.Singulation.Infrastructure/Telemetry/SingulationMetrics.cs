using System.Diagnostics.Metrics;

namespace ZakYip.Singulation.Infrastructure.Telemetry {

    public sealed class SingulationMetrics {
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

        public Counter<long> FrameProcessedCounter { get; }
        public Counter<long> FrameDroppedCounter { get; }
        public Counter<long> DegradeCounter { get; }
        public Counter<long> AxisFaultCounter { get; }
        public Counter<long> HeartbeatTimeoutCounter { get; }
        public Histogram<double> SpeedDelta { get; }
        public Histogram<double> LoopDuration { get; }
        public Histogram<double> FrameRtt { get; }
        public Histogram<double> CommissioningCycle { get; }
    }
}
