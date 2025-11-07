namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 系统健康度数据传输对象
    /// </summary>
    public sealed record class SystemHealthDto {
        /// <summary>
        /// 健康度评分 (0-100)
        /// </summary>
        public double Score { get; init; }

        /// <summary>
        /// 健康状态等级
        /// </summary>
        public HealthLevel Level { get; init; }

        /// <summary>
        /// 在线轴数量
        /// </summary>
        public int OnlineAxisCount { get; init; }

        /// <summary>
        /// 总轴数量
        /// </summary>
        public int TotalAxisCount { get; init; }

        /// <summary>
        /// 错误轴数量
        /// </summary>
        public int FaultedAxisCount { get; init; }

        /// <summary>
        /// 平均响应时间（ms）
        /// </summary>
        public double AverageResponseTimeMs { get; init; }

        /// <summary>
        /// 错误率 (0-1)
        /// </summary>
        public double ErrorRate { get; init; }

        /// <summary>
        /// 详细说明
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; init; }
    }

    /// <summary>
    /// 健康等级
    /// </summary>
    public enum HealthLevel {
        /// <summary>危急 (0-40)</summary>
        Critical = 0,
        /// <summary>警告 (40-70)</summary>
        Warning = 1,
        /// <summary>良好 (70-90)</summary>
        Good = 2,
        /// <summary>优秀 (90-100)</summary>
        Excellent = 3
    }
}
