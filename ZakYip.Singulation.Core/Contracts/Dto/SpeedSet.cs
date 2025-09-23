using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 表示一帧来自上游视觉的速度集合（单位：mm/s）。
    /// 该数据将直接输入速度规划器（ISpeedPlanner）。
    /// </summary>
    /// <param name="TimestampUtc">速度帧产生的 UTC 时间戳，用于时序与丢帧诊断。</param>
    /// <param name="Sequence">可选的帧序号；部分协议未提供，可置 0。</param>
    /// <param name="MainMmps">分离段各小段的线速度（mm/s），按拓扑顺序排列。</param>
    /// <param name="EjectMmps">疏散/扩散段的线速度（mm/s），按拓扑顺序排列。</param>
    public readonly record struct SpeedSet(
        DateTime TimestampUtc,
        int Sequence,
        IReadOnlyList<int> MainMmps,
        IReadOnlyList<int> EjectMmps
    );
}