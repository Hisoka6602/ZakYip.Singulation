using System;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 表示一帧来自上游视觉的速度集合（单位：mm/s）。
    /// 该数据将直接输入速度规划器（ISpeedPlanner）。
    /// </summary>
    public readonly record struct SpeedSet {
        /// <summary>速度帧产生的 UTC 时间戳，用于时序与丢帧诊断。</summary>
        public DateTime TimestampUtc { get; init; }

        /// <summary>可选的帧序号；部分协议未提供，可置 0。</summary>
        public int Sequence { get; init; }

        /// <summary>分离段各小段的线速度（mm/s），按拓扑顺序排列。</summary>
        public IReadOnlyList<int> MainMmps { get; init; }

        /// <summary>疏散/扩散段的线速度（mm/s），按拓扑顺序排列。</summary>
        public IReadOnlyList<int> EjectMmps { get; init; }

        /// <summary>
        /// 通过构造函数创建速度集合。
        /// </summary>
        public SpeedSet(DateTime timestampUtc, int sequence, IReadOnlyList<int> mainMmps, IReadOnlyList<int> ejectMmps) {
            TimestampUtc = timestampUtc;
            Sequence = sequence;
            MainMmps = mainMmps;
            EjectMmps = ejectMmps;
        }
    }
}
