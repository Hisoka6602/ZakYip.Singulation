using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>上游视觉给到的速度集合（单位按协议，可由 PlannerParams 转换为 RPM）。</summary>
    public sealed class SpeedSet {
        public long FrameSeq { get; init; }          // 帧序号（乱序/丢帧检测）
        public DateTimeOffset Timestamp { get; init; }
        public ReadOnlyMemory<double> SegmentSpeeds { get; init; } // 每小带速度
        public SpeedUnit Unit { get; init; } = SpeedUnit.MetersPerSecond;   // 默认以 m/s 为例
        public SourceFlags Source { get; init; } = SourceFlags.Vision;
    }
}