using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>规划器参数：限幅/斜率/平滑等。</summary>
    public sealed class PlannerParams {
        public double MaxRpm { get; init; } = 3000;
        public double MinRpm { get; init; } = 0;
        public double MaxAccelRpmPerSec { get; init; } = 1000; // 斜率限制
        public int SmoothWindow { get; init; } = 3;            // 简单滑窗平滑
        public TimeSpan SamplingPeriod { get; init; } = TimeSpan.FromMilliseconds(10);
        public bool HoldOnNoFrame { get; init; } = true;       // 缺帧保持/缓降策略（Core 决策）
    }
}