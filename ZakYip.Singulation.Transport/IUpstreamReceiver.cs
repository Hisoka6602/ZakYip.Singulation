using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Protocol;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Protocol.Enums;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Transport {

    /// <summary>将字节流 + 编解码组合成 SpeedSet 事件流。</summary>
    public interface IUpstreamReceiver : IAsyncDisposable {
        TransportStatus Status { get; }

        event Action<SpeedSet, CodecFlags>? OnSpeedSet;

        Task StartAsync(IByteTransport transport, IUpstreamCodec codec, CancellationToken ct = default);

        Task StopAsync(CancellationToken ct = default);
    }
}