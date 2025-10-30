using Microsoft.Extensions.ObjectPool;

namespace ZakYip.Singulation.Host.SignalR {
    /// <summary>
    /// MessageEnvelope 对象池策略。
    /// </summary>
    public sealed class MessageEnvelopePoolPolicy : PooledObjectPolicy<MessageEnvelope> {
        /// <summary>
        /// 创建新的 MessageEnvelope 对象。
        /// </summary>
        public override MessageEnvelope Create() => new MessageEnvelope();

        /// <summary>
        /// 归还对象到池中时的处理。
        /// </summary>
        public override bool Return(MessageEnvelope obj) {
            obj.Reset();
            return true;
        }
    }
}
