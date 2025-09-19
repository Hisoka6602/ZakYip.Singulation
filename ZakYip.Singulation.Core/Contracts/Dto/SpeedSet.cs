using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>
    /// 上游视觉系统发来的速度集合。
    /// 每一帧对应一次全局观测，描述在该时刻各小皮带的目标速度。
    /// </summary>
    public sealed class SpeedSet {

        /// <summary>
        /// 帧序号（由上游生成）。
        /// 可用于检测乱序、丢帧、重放等问题。
        /// </summary>
        public long FrameSeq { get; init; }

        /// <summary>
        /// 该帧的时间戳（上游产生的采样时间）。
        /// 用于对齐多源数据或延时补偿。
        /// </summary>
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// 每个小皮带的速度值集合。
        /// 顺序对应物理布局，长度与 AxisCount 一致。
        /// 采用 ReadOnlyMemory 以减少复制开销。
        /// </summary>
        public ReadOnlyMemory<double> SegmentSpeeds { get; init; }

        /// <summary>
        /// 当前速度值的单位（例如 m/s 或 RPM）。
        /// 解析器负责填充，Planner 可根据需要统一转换。
        /// </summary>
        public SpeedUnit Unit { get; init; } = SpeedUnit.MetersPerSecond;

        /// <summary>
        /// 数据来源标识，例如 Vision、Simulator、Fallback。
        /// 方便调试与监控。
        /// </summary>
        public SourceFlags Source { get; init; } = SourceFlags.Vision;
    }
}