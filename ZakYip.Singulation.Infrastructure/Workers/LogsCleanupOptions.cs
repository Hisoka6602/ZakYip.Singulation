namespace ZakYip.Singulation.Infrastructure.Workers {

    /// <summary>
    /// 日志清理服务配置选项
    /// </summary>
    public sealed record class LogsCleanupOptions {
        /// <summary>
        /// 主日志保留天数。默认为 2 天。
        /// </summary>
        public int MainLogRetentionDays { get; init; } = 2;

        /// <summary>
        /// 高频日志保留天数（UDP、Transport、IoStatus）。默认为 2 天。
        /// </summary>
        public int HighFreqLogRetentionDays { get; init; } = 2;

        /// <summary>
        /// 错误日志保留天数。默认为 2 天。
        /// </summary>
        public int ErrorLogRetentionDays { get; init; } = 2;
    }
}
