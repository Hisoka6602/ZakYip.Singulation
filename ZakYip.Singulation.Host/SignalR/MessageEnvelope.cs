namespace ZakYip.Singulation.Host.SignalR {
    /// <summary>
    /// SignalR 消息封装对象，支持对象池化以减少 GC 压力。
    /// </summary>
    public sealed class MessageEnvelope {
        /// <summary>消息格式版本。</summary>
        public int Version { get; set; } = 1;

        /// <summary>消息类型名称（可选）。</summary>
        public string? Type { get; set; }

        /// <summary>消息时间戳。</summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>频道名称。</summary>
        public string Channel { get; set; } = string.Empty;

        /// <summary>消息数据。</summary>
        public object Data { get; set; } = default!;

        /// <summary>跟踪 ID（可选）。</summary>
        public string? TraceId { get; set; }

        /// <summary>消息序列号。</summary>
        public long Sequence { get; set; }

        /// <summary>
        /// 重置对象以便重用（对象池化）。
        /// </summary>
        public void Reset() {
            Version = 1;
            Type = null;
            Timestamp = default;
            Channel = string.Empty;
            Data = default!;
            TraceId = null;
            Sequence = 0;
        }
    }
}
