using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Core.Abstractions.Cabinet {
    /// <summary>
    /// 速度帧保护器输出的决策结果。
    /// </summary>
    public readonly record struct FrameGuardDecision {
        /// <summary>是否应该应用当前速度帧。</summary>
        public bool ShouldApply { get; init; }

        /// <summary>处理后的速度输出。</summary>
        public SpeedSet Output { get; init; }

        /// <summary>是否应用了降级策略。</summary>
        public bool DegradedApplied { get; init; }

        /// <summary>附加说明。</summary>
        public string? Reason { get; init; }

        /// <summary>
        /// 创建决策结果。
        /// </summary>
        public FrameGuardDecision(bool shouldApply, SpeedSet output, bool degradedApplied, string? reason) {
            ShouldApply = shouldApply;
            Output = output;
            DegradedApplied = degradedApplied;
            Reason = reason;
        }
    }
}
