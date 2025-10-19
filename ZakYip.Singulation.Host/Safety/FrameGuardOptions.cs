using System;

namespace ZakYip.Singulation.Host.Safety {

    public sealed class FrameGuardOptions {
        public int SequenceWindow { get; set; } = 32;
        public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(3);
        public decimal DegradeScale { get; set; } = 0.3m;
    }
}
