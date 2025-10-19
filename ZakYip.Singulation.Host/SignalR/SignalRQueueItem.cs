namespace ZakYip.Singulation.Host.SignalR {
    /// <summary>
    /// SignalR 推送队列中的消息项。
    /// </summary>
    public sealed record class SignalRQueueItem {
        /// <summary>SignalR 通道名称。</summary>
        public string Channel { get; init; } = string.Empty;

        /// <summary>推送的负载对象。</summary>
        public object Payload { get; init; } = default!;

        /// <summary>
        /// 使用指定参数创建消息项。
        /// </summary>
        public SignalRQueueItem(string channel, object payload) {
            Channel = channel;
            Payload = payload;
        }
    }
}
