using System;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport {

    /// <summary>
    /// TCP 服务端实现：视觉作为 Client 连接进来的场景。
    /// 只保留 1 个活动连接（可通过选项调整）；新连接将替换旧连接。
    /// </summary>
    public sealed class TcpServerByteTransport : IByteTransport {
        private readonly TcpServerOptions _opt;

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        private TcpClient? _activeClient;
        private NetworkStream? _activeStream;

        private readonly object _gate = new();

        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;

        public event Action<ReadOnlyMemory<byte>>? Data;

        public TcpServerByteTransport(TcpServerOptions opt) {
            _opt = opt;
        }

        public async Task StartAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status == TransportStatus.Running) return;
                Status = TransportStatus.Starting;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _listener = new TcpListener(_opt.Address, _opt.Port);
            }

            _listener!.Server.ReceiveBufferSize = _opt.ReceiveBufferSize;
            _listener.Start(_opt.Backlog);

            _ = Task.Run(() => AcceptLoopAsync(_cts.Token), ct);
            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status == TransportStatus.Stopped) return Task.CompletedTask;
                Status = TransportStatus.Stopped;

                try { _cts?.Cancel(); } catch { /* ignore */ }
                try { _activeStream?.Close(); } catch { /* ignore */ }
                try { _activeClient?.Close(); } catch { /* ignore */ }
                try { _listener?.Stop(); } catch { /* ignore */ }

                _activeStream = null;
                _activeClient = null;
                _listener = null;

                _cts?.Dispose();
                _cts = null;
            }
            return Task.CompletedTask;
        }

        private async Task AcceptLoopAsync(CancellationToken ct) {
            try {
                while (!ct.IsCancellationRequested) {
                    var client = await _listener!.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                    client.NoDelay = true;
                    client.ReceiveBufferSize = _opt.ReceiveBufferSize;

                    ReplaceActive(client);

                    _ = Task.Run(() => ReceiveLoopAsync(client, _activeStream!, ct), ct);
                }
            }
            catch (OperationCanceledException) { /* stop */ }
            catch {
                // listener fault -> stop
                await StopAsync(ct).ConfigureAwait(false);
            }
        }

        private void ReplaceActive(TcpClient client) {
            lock (_gate) {
                // 关闭旧连接
                try { _activeStream?.Close(); } catch { /* ignore */ }
                try { _activeClient?.Close(); } catch { /* ignore */ }

                _activeClient = client;
                _activeStream = client.GetStream();
                Status = TransportStatus.Running;
            }
        }

        private async Task ReceiveLoopAsync(TcpClient client, NetworkStream stream, CancellationToken ct) {
            var buffer = ArrayPool<byte>.Shared.Rent(_opt.ReceiveBufferSize);
            try {
                while (!ct.IsCancellationRequested) {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break;

                    var payload = new byte[read];
                    Buffer.BlockCopy(buffer, 0, payload, 0, read);
                    Data?.Invoke(payload);
                }
            }
            catch (OperationCanceledException) { /* normal */ }
            catch {
                // socket fault
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
                if (ReferenceEquals(client, _activeClient)) {
                    await StopAsync(ct).ConfigureAwait(false);
                }
                else {
                    try { client.Close(); } catch { /* ignore */ }
                }
            }
        }

        public async ValueTask DisposeAsync() {
            await StopAsync().ConfigureAwait(false);
        }
    }
}