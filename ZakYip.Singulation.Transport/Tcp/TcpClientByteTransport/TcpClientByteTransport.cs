using System;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport {

    /// <summary>
    /// TCP 客户端实现：连接视觉作为 Server 的场景。
    /// 仅负责把收到的字节通过 Data 事件推送给上层；不关心协议语义。
    /// </summary>
    public sealed class TcpClientByteTransport : IByteTransport {
        private readonly TcpClientOptions _opt;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        private readonly object _gate = new();

        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;

        public event Action<ReadOnlyMemory<byte>>? Data;

        public TcpClientByteTransport(TcpClientOptions opt) {
            _opt = opt;
        }

        public async Task StartAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status == TransportStatus.Running) return;
                Status = TransportStatus.Starting;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            }

            try {
                var client = new TcpClient {
                    NoDelay = _opt.NoDelay,
                    ReceiveBufferSize = _opt.ReceiveBufferSize
                };
                await client.ConnectAsync(_opt.Host, _opt.Port, ct).ConfigureAwait(false);

                lock (_gate) {
                    _client = client;
                    _stream = client.GetStream();
                    Status = TransportStatus.Running;
                }

                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), ct);
            }
            catch {
                await StopAsync(ct); // 归位
                Status = TransportStatus.Faulted;
                throw;
            }
        }

        public Task StopAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status == TransportStatus.Stopped) return Task.CompletedTask;
                Status = TransportStatus.Stopped;
                try {
                    _cts?.Cancel();
                }
                catch {
                    /* ignore */
                }

                try {
                    _stream?.Close();
                }
                catch {
                    /* ignore */
                }

                try {
                    _client?.Close();
                }
                catch {
                    /* ignore */
                }

                _stream = null;
                _client = null;
                _cts?.Dispose();
                _cts = null;
            }

            return Task.CompletedTask;
        }

        private async Task ReceiveLoopAsync(CancellationToken ct) {
            var stream = _stream!;
            var buffer = ArrayPool<byte>.Shared.Rent(_opt.ReceiveBufferSize);
            try {
                while (!ct.IsCancellationRequested) {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break;

                    // 注意：不要直接把租借缓冲区暴露出去；复制到紧凑数组再发布。
                    var payload = new byte[read];
                    Buffer.BlockCopy(buffer, 0, payload, 0, read);
                    Data?.Invoke(payload);
                }
            }
            catch (OperationCanceledException) {
                /* 正常退出 */
            }
            catch {
                // 异常视为断开
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
                await StopAsync(ct).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync() {
            await StopAsync().ConfigureAwait(false);
        }
    }
}