using System;

namespace ZakYip.Singulation.Infrastructure.Safety {

    /// <summary>
    /// 帧保护选项，用于配置帧序列检测和心跳监控。
    /// </summary>
    public sealed class FrameGuardOptions {
        /// <summary>
        /// 序列窗口大小，用于检测重复或乱序的帧。默认为 32。
        /// </summary>
        public int SequenceWindow { get; set; } = 32;

        /// <summary>
        /// 心跳超时时间。默认为 3 秒。
        /// </summary>
        public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// 降级模式下的速度缩放比例。默认为 0.3（30%）。
        /// </summary>
        public decimal DegradeScale { get; set; } = 0.3m;
    }
}
