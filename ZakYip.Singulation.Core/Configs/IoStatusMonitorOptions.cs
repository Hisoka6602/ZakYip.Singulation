using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// IO 状态监控配置选项。
    /// </summary>
    public sealed record class IoStatusMonitorOptions {
        /// <summary>是否启用 IO 状态实时监控和广播。</summary>
        public bool Enabled { get; init; } = true;

        /// <summary>输入 IO 起始位编号，默认 0。</summary>
        [Range(0, 1000, ErrorMessage = "输入 IO 起始位编号必须在 0 到 1000 之间")]
        public int InputStart { get; init; } = 0;

        /// <summary>输入 IO 数量，默认 32。</summary>
        [Range(1, 1024, ErrorMessage = "输入 IO 数量必须在 1 到 1024 之间")]
        public int InputCount { get; init; } = 32;

        /// <summary>输出 IO 起始位编号，默认 0。</summary>
        [Range(0, 1000, ErrorMessage = "输出 IO 起始位编号必须在 0 到 1000 之间")]
        public int OutputStart { get; init; } = 0;

        /// <summary>输出 IO 数量，默认 32。</summary>
        [Range(1, 1024, ErrorMessage = "输出 IO 数量必须在 1 到 1024 之间")]
        public int OutputCount { get; init; } = 32;

        /// <summary>轮询间隔（毫秒），默认 500ms。</summary>
        [Range(100, 10000, ErrorMessage = "轮询间隔必须在 100 到 10000 毫秒之间")]
        public int PollingIntervalMs { get; init; } = 500;

        /// <summary>SignalR 广播频道名称。</summary>
        public string SignalRChannel { get; init; } = "/io/status";
    }
}
