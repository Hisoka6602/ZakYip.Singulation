using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>
    /// 线速度（mm/s）语义下的规划器参数。
    /// </summary>
    public sealed class LinearPlannerParams {

        /// <summary>控制周期（用于将 mm/s² 折算为每周期最大变化量）。</summary>
        public TimeSpan SamplingPeriod { get; set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>平滑窗口（≥1；1 表示不平滑）。</summary>
        public int SmoothWindow { get; set; } = 1;

        /// <summary>最大加速度（mm/s²）。≤0 表示不做斜率限制。</summary>
        public decimal MaxAccelMmps2 { get; set; } = 2500m;

        /// <summary>速度下限（mm/s）。</summary>
        public decimal MinMmps { get; set; } = 0m;

        /// <summary>速度上限（mm/s）。</summary>
        public decimal MaxMmps { get; set; } = 3000m;

        /// <summary>帧异常时是否保持上次输出。</summary>
        public bool HoldOnNoFrame { get; set; } = true;
    }
}