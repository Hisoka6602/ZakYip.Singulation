using System;
using System.Linq;
using System.Text;
using System.Buffers;

using System.Buffers;

using System.Threading;
using System.Net.Sockets;

using System.Net.Sockets;

using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport {

    public sealed class TcpClientByteTransport : IByteTransport, IAsyncDisposable {
        private readonly TcpClientOptions _opt;

        private readonly object _gate = new();
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        private TransportConnectionState _status;
        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;
        TransportConnectionState IByteTransport.Status => _status;

        public event Action<ReadOnlyMemory<byte>>? Data;

        public event EventHandler<BytesReceivedEventArgs>? BytesReceived;

        public event EventHandler<TransportStateChangedEventArgs>? StateChanged;

        public event EventHandler<TransportErrorEventArgs>? Error;

        public TcpClientByteTransport(TcpClientOptions opt) => _opt = opt;

        public Task StartAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status == TransportStatus.Running) return Task.CompletedTask;
                Status = TransportStatus.Starting;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                Status = TransportStatus.Running;
            }

            // 后台连接+接收总循环（含重连）
            _ = RunAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default) {
            CancellationTokenSource? ctsLocal;
            TcpClient? clientLocal;
            NetworkStream? streamLocal;

            lock (_gate) {
                if (Status == TransportStatus.Stopped) return Task.CompletedTask;
                Status = TransportStatus.Stopped;

                ctsLocal = _cts;
                _cts = null;

                streamLocal = _stream;
                _stream = null;

                clientLocal = _client;
                _client = null;

                SetConnState(TransportConnectionState.Disconnected);
            }

            // 依次尝试关闭，忽略异常
            TryCancel(ctsLocal);
            TryClose(streamLocal);
            TryClose(clientLocal);

            ctsLocal?.Dispose();
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        // ================== 内部实现 ==================

        private async Task RunAsync(CancellationToken ct) {
            var rnd = new Random();
            var attempt = 0;

            while (!ct.IsCancellationRequested && Status == TransportStatus.Running) {
                try {
                    await ConnectOnceAsync(ct).ConfigureAwait(false);
                    SetConnState(TransportConnectionState.Connected);

                    // 成功后清零退避
                    attempt = 0;

                    // 进入接收循环；正常退出或异常都视为断开，随后进入重连
                    await ReceiveLoopAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    // Stop 时或外部取消，直接退出总循环
                    break;
                }
                catch (Exception ex) {
                    PublishError(ex);
                }

                // 走到这里即“未连接或掉线”
                SetConnState(TransportConnectionState.Disconnected);

                if (Status != TransportStatus.Running || ct.IsCancellationRequested) break;

                // 指数退避（上限 10s，+ 抖动 0~500ms）
                attempt++;
                var exp = Math.Min(Math.Pow(2, attempt), 20); // 2^n * 500ms，<=10s
                var delay = TimeSpan.FromMilliseconds(500 * exp);
                if (delay > TimeSpan.FromSeconds(10)) delay = TimeSpan.FromSeconds(10);
                delay += TimeSpan.FromMilliseconds(rnd.Next(0, 500));

                try { await Task.Delay(delay, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ConnectOnceAsync(CancellationToken ct) {
            var client = new TcpClient {
                NoDelay = _opt.NoDelay,
                ReceiveBufferSize = _opt.ReceiveBufferSize
            };

            await client.ConnectAsync(_opt.Host, _opt.Port, ct).ConfigureAwait(false);

            lock (_gate) {
                // Stop 期间抢占的情况：立即关闭新建连接
                if (Status != TransportStatus.Running || ct.IsCancellationRequested) {
                    TryClose(client);
                    throw new OperationCanceledException(ct);
                }

                _client = client;
                _stream = client.GetStream();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct) {
            var stream = _stream!;
            var buffer = ArrayPool<byte>.Shared.Rent(_opt.ReceiveBufferSize);

            try {
                while (!ct.IsCancellationRequested && Status == TransportStatus.Running && _client?.Connected == true) {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break; // 被动断开

                    // 紧凑复制，避免把租借缓冲暴露给上层
                    var payload = new byte[read];
                    Buffer.BlockCopy(buffer, 0, payload, 0, read);

                    // 事件异步投递，避免外部订阅阻塞接收环
                    PublishData(payload);
                    PublishBytesReceived(buffer);
                }
            }
            catch (OperationCanceledException) {
                // 正常退出
            }
            catch (Exception ex) {
                PublishError(ex);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
                // 断开清理（不改变 TransportStatus，只更新连接态）
                CleanupConnection();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CleanupConnection() {
            TcpClient? clientLocal;
            NetworkStream? streamLocal;

            lock (_gate) {
                streamLocal = _stream;
                clientLocal = _client;
                _stream = null;
                _client = null;
            }

            TryClose(streamLocal);
            TryClose(clientLocal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetConnState(TransportConnectionState s) {
            if (_status == s) return;
            _status = s;
            SafeQueue(() => StateChanged?.Invoke(this, new TransportStateChangedEventArgs {
                State = s
            }));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PublishData(ReadOnlyMemory<byte> mem)
            => SafeQueue(() => Data?.Invoke(mem));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PublishBytesReceived(byte[] buffer)
            => SafeQueue(() => BytesReceived?.Invoke(this, new BytesReceivedEventArgs {
                Buffer = buffer
            }));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PublishError(Exception ex)
            => SafeQueue(() => Error?.Invoke(this, new TransportErrorEventArgs {
                Message = ex.Message
            }));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SafeQueue(Action act) {
            // 事件投递：立即返回，不阻塞接收环；订阅方异常不影响主循环
            ThreadPool.UnsafeQueueUserWorkItem(_ => {
                try { act(); } catch { /* ignore listener exceptions */ }
            }, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryClose(IDisposable? d) {
            try { d?.Dispose(); } catch { /* ignore */ }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryCancel(CancellationTokenSource? cts) {
            try { cts?.Cancel(); } catch { /* ignore */ }
        }
    }
}