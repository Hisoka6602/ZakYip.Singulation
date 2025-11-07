using System;

namespace ZakYip.Singulation.Infrastructure.Services {

    /// <summary>
    /// 速度联动服务统计信息
    /// </summary>
    public sealed class SpeedLinkageStatistics {
        /// <summary>
        /// 总检查次数
        /// </summary>
        public long TotalChecks { get; set; }

        /// <summary>
        /// 总状态变化次数
        /// </summary>
        public long TotalStateChanges { get; set; }

        /// <summary>
        /// 总IO写入次数
        /// </summary>
        public long TotalIoWrites { get; set; }

        /// <summary>
        /// 失败的IO写入次数
        /// </summary>
        public long FailedIoWrites { get; set; }

        /// <summary>
        /// 总错误次数
        /// </summary>
        public long TotalErrors { get; set; }

        /// <summary>
        /// 最后检查时间
        /// </summary>
        public DateTime LastCheckTime { get; set; }

        /// <summary>
        /// 最后错误时间
        /// </summary>
        public DateTime LastErrorTime { get; set; }

        /// <summary>
        /// 最后错误消息
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// 服务是否正在运行
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// 活跃的联动组数量
        /// </summary>
        public int ActiveGroupsCount { get; set; }
    }
}
