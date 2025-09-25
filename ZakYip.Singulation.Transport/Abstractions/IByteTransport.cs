using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Core.Contracts.Events;

namespace ZakYip.Singulation.Transport.Abstractions {

    /// <summary>最底层的字节传输适配（UDP/TCP/串口/SDK 回调）。</summary>
    public interface IByteTransport : IAsyncDisposable {

        /// <summary>当前状态（实现方自有的 TransportStatus，或直接以事件为准）。</summary>
        TransportConnectionState Status { get; }

        /// <summary>原始字节推送；订阅者请勿阻塞回调。</summary>
        event Action<ReadOnlyMemory<byte>>? Data;

        /// <summary>带元信息的字节事件（可选更丰富）。</summary>
        event EventHandler<BytesReceivedEventArgs>? BytesReceived;

        /// <summary>连接生命周期事件。</summary>
        event EventHandler<TransportStateChangedEventArgs>? StateChanged;

        /// <summary>错误/告警事件（不外抛异常）。</summary>
        event EventHandler<TransportErrorEventArgs>? Error;

        Task StartAsync(CancellationToken ct = default);

        Task StopAsync(CancellationToken ct = default);
    }
}