using System;
using System.Linq;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport {

    /// <summary>
    /// 基于 TouchSocket 的 TCP 服务端传输：视觉作为 Client 连接进来。
    /// 仅做字节搬运（监听、收包、上抛），不解析业务。
    /// </summary>
    public sealed class TouchServerByteTransport : IByteTransport {
        private readonly TcpServerOptions _opt;
        private TcpService? _service;

        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;

        /// <summary>推模式：收到任意字节即回调。</summary>
        public event Action<ReadOnlyMemory<byte>>? Data;

        public TouchServerByteTransport(TcpServerOptions opt) => _opt = opt;

        public async Task StartAsync(CancellationToken ct = default) {
            if (Status == TransportStatus.Running) return;
            Status = TransportStatus.Starting;

            var service = new TcpService();

            // 新连接建立/关闭 → 更新状态（文档“创建TcpService/简单创建”说明可直接订阅委托）
            service.Connected = (client, e) => {
                Status = TransportStatus.Running;
                return EasyTask.CompletedTask;
            };
            service.Closed = (client, e) => {
                // 如果没有其它连接，保持 Stopped；简单起见，统一标记为 Stopped
                Status = TransportStatus.Stopped;
                return EasyTask.CompletedTask;
            };

            // 服务器侧的统一收包口：e.ByteBlock 为接收到的原始数据块
            // 文档“创建TcpService/接收数据/Received委托处理”
            service.Received = (client, e) => {
                var bytes = e.ByteBlock.Span.ToArray();
                Data?.Invoke(bytes);
                return EasyTask.CompletedTask;
            };

            // 正确的监听配置：Setup + SetListenIPHosts（支持 IPAddress+Port 的 IPHost 重载）
            await service.SetupAsync(new TouchSocketConfig()
                .SetListenIPHosts(new IPHost(_opt.Address, _opt.Port))

            );

            try {
                await service.StartAsync(); // 非阻塞启动（文档提示 StartAsync 不会阻塞当前线程）
                _service = service;
            }
            catch {
                Status = TransportStatus.Faulted;
                service.SafeDispose();
                throw;
            }
        }

        public async Task StopAsync(CancellationToken ct = default) {
            if (_service is not null) {
                await _service.StopAsync(ct);
                _service?.Dispose();
            }

            _service = null;
            Status = TransportStatus.Stopped;
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
    }
}