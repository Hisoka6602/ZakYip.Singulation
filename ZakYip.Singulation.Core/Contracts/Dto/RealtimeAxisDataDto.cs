namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 轴实时数据传输对象，用于 SignalR 推送
    /// </summary>
    public sealed record class RealtimeAxisDataDto {
        /// <summary>
        /// 轴标识符
        /// </summary>
        public required string AxisId { get; init; }

        /// <summary>
        /// 当前线速度（mm/s）
        /// </summary>
        public double? CurrentSpeedMmps { get; init; }

        /// <summary>
        /// 当前位置（mm）
        /// </summary>
        public double? CurrentPositionMm { get; init; }

        /// <summary>
        /// 目标速度（mm/s）
        /// </summary>
        public double? TargetSpeedMmps { get; init; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// 是否使能
        /// </summary>
        public bool? Enabled { get; init; }
    }
}
