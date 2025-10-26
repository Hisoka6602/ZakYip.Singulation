namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    /// <summary>
    /// IO 状态监控配置的 LiteDB 文档实体。
    /// </summary>
    public sealed class IoStatusMonitorOptionsDoc {
        /// <summary>文档 ID（单例模式，固定为 "default"）。</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>是否启用 IO 状态实时监控和广播。</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>输入 IO 起始位编号，默认 0。</summary>
        public int InputStart { get; set; } = 0;

        /// <summary>输入 IO 数量，默认 32。</summary>
        public int InputCount { get; set; } = 32;

        /// <summary>输出 IO 起始位编号，默认 0。</summary>
        public int OutputStart { get; set; } = 0;

        /// <summary>输出 IO 数量，默认 32。</summary>
        public int OutputCount { get; set; } = 32;

        /// <summary>轮询间隔（毫秒），默认 500ms。</summary>
        public int PollingIntervalMs { get; set; } = 500;

        /// <summary>SignalR 广播频道名称。</summary>
        public string SignalRChannel { get; set; } = "/io/status";
    }
}
