using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 上游帧 Hub：仅负责扇出广播，不做业务解码。
    /// 支持多订阅者同时接收同一帧（每个订阅者一个独立的有界通道）。
    /// </summary>
    public interface IUpstreamFrameHub {

        /// <summary>
        /// 订阅“速度帧原始字节”。每次调用都会创建一个新的独立通道（DropOldest）。
        /// 返回(Reader, Unsubscribe)；调用方在释放时应 Dispose。
        /// </summary>
        (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe)
            SubscribeSpeed(int capacity = 256);

        /// <summary>发布一帧速度原始字节（泵侧调用）。</summary>
        void PublishSpeed(ReadOnlyMemory<byte> payload);

        /// <summary>订阅“心跳帧原始字节”。同 SubscribeSpeed。</summary>
        (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe)
            SubscribeHeartbeat(int capacity = 64);

        /// <summary>发布一帧心跳原始字节（泵侧调用）。</summary>
        void PublishHeartbeat(ReadOnlyMemory<byte> payload);

        (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) SubscribePosition(int capacity = 256);

        void PublishPosition(ReadOnlyMemory<byte> payload);
    }
}