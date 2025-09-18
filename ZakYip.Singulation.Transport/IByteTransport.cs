using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Transport.Enums;

namespace ZakYip.Singulation.Transport {

    /// <summary>最底层的字节传输适配（UDP/TCP/串口/SDK 回调）。</summary>
    public interface IByteTransport : IAsyncDisposable {
        TransportStatus Status { get; }

        event Action<ReadOnlyMemory<byte>>? Data; // 推模式：有数据就触发

        Task StartAsync(CancellationToken ct = default);

        Task StopAsync(CancellationToken ct = default);
    }
}