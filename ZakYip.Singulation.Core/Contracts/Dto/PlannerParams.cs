using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>
    /// 规划器运行时参数：决定算法如何处理输入速度并生成输出命令。
    /// 不描述机械本体，而是描述控制策略。
    /// </summary>
    public sealed class PlannerParams {

        /// <summary>
        /// 允许的最大转速（单位：RPM）。
        /// 用于限幅，超过此值会被截断。
        /// </summary>
        public decimal MaxRpm { get; init; } = 3000;

        /// <summary>
        /// 允许的最小转速（单位：RPM）。
        /// 常用于防止电机进入负转或失控。
        /// </summary>
        public decimal MinRpm { get; init; } = 0;

        /// <summary>
        /// 最大加速度限制（单位：RPM 每秒）。
        /// 决定转速斜坡的陡峭程度，过大可能导致电机冲击。
        /// </summary>
        public decimal MaxAccelRpmPerSec { get; init; } = 1000;

        /// <summary>
        /// 平滑窗口大小（单位：采样点数）。
        /// 表示简单滑动平均滤波的窗口长度，用于去抖动。
        /// </summary>
        public int SmoothWindow { get; init; } = 3;

        /// <summary>
        /// 采样周期（单位：时间）。
        /// 控制器每次迭代的时间间隔，例如 10ms。
        /// </summary>
        public TimeSpan SamplingPeriod { get; init; } = TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// 当上游缺帧时的策略：
        /// True = 保持上一次的输出，或缓慢衰减；
        /// False = 立即降为 0。
        /// </summary>
        public bool HoldOnNoFrame { get; init; } = true;
    }
}