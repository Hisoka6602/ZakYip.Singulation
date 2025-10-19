using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Abstractions.Realtime;

namespace ZakYip.Singulation.Host.SignalR {
    /// <summary>
    /// IRealtimeNotifier 的 SignalR 实现：非阻塞投递到 Channel，后台服务负责真正广播。
    /// </summary>
    public sealed class SignalRRealtimeNotifier : IRealtimeNotifier {
        private readonly Channel<SignalRQueueItem> _chan;

        /// <summary>
        /// 通过外部注入的通道初始化通知器。
        /// </summary>
        public SignalRRealtimeNotifier(Channel<SignalRQueueItem> chan) {
            _chan = chan;
        }

        /// <inheritdoc />
        public ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default) {
            // 非阻塞：写不进去就丢（由 BoundedChannelFullMode 控制），不抛异常
            _ = _chan.Writer.TryWrite(new SignalRQueueItem(channel, payload));
            return ValueTask.CompletedTask;
        }
    }
}
