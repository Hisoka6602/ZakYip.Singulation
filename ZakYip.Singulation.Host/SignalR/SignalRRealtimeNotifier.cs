using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Abstractions.Realtime;

namespace ZakYip.Singulation.Host.SignalR {

    /// <summary>
    /// IRealtimeNotifier 的 SignalR 实现：非阻塞投递到 Channel，后台服务负责真正广播。
    /// </summary>
    public sealed class SignalRRealtimeNotifier : IRealtimeNotifier {
        public sealed record QueueItem(string Channel, object Payload);

        private readonly Channel<QueueItem> _chan;

        public SignalRRealtimeNotifier(Channel<QueueItem> chan) => _chan = chan;

        public ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default) {
            // 非阻塞：写不进去就丢（由 BoundedChannelFullMode 控制），不抛异常
            _ = _chan.Writer.TryWrite(new QueueItem(channel, payload));
            return ValueTask.CompletedTask;
        }
    }
}