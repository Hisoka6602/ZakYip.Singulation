using System;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport {

    public sealed class TcpServerByteTransport : IByteTransport, IAsyncDisposable {
        private readonly TcpServerOptions _opt;

        private readonly object _gate = new();
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        private TcpClient? _activeClient;
        private NetworkStream? _activeStream;

        private TransportConnectionState _status;
        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;
        TransportConnectionState IByteTransport.Status => _status;

        public event Action<ReadOnlyMemory<byte>>? Data;

        public event EventHandler<BytesReceivedEventArgs>? BytesReceived;

        public event EventHandler<TransportStateChangedEventArgs>? StateChanged;

        public event EventHandler<TransportErrorEventArgs>? Error;

        public TcpServerByteTransport(TcpServerOptions opt) => _opt = opt;

        public Task StartAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status == TransportStatus.Running) return Task.CompletedTask;
                Status = TransportStatus.Starting;

                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _listener = new TcpListener(_opt.Address, _opt.Port);
                // 预设监听 socket 的接收缓冲（影响已接受连接前的队列缓存）
                _listener.Server.ReceiveBufferSize = _opt.ReceiveBufferSize;
                _listener.Start(_opt.Backlog);

                Status = TransportStatus.Running;
            }

            // 接受连接总循环
            _ = AcceptLoopAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default) {
            CancellationTokenSource? ctsLocal;
            TcpListener? listenerLocal;
            TcpClient? clientLocal;
            NetworkStream? streamLocal;

            lock (_gate) {
                if (Status == TransportStatus.Stopped) return Task.CompletedTask;
                Status = TransportStatus.Stopped;

                ctsLocal = _cts; _cts = null;
                listenerLocal = _listener; _listener = null;
                streamLocal = _activeStream; _activeStream = null;
                clientLocal = _activeClient; _activeClient = null;

                SetConnState(TransportConnectionState.Disconnected);
            }

            TryCancel(ctsLocal);
            TryStop(listenerLocal);
            TryClose(streamLocal);
            TryClose(clientLocal);

            ctsLocal?.Dispose();
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        // ================== 接受与接收循环 ==================

        private async Task AcceptLoopAsync(CancellationToken ct) {
            try {
                while (!ct.IsCancellationRequested && Status == TransportStatus.Running) {
                    TcpClient? client = null;
                    try {
#if NET8_0_OR_GREATER
                        client = await _listener!.AcceptTcpClientAsync(ct).ConfigureAwait(false);
#else
                    client = await _listener!.AcceptTcpClientAsync().ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
#endif
                    }
                    catch (OperationCanceledException) { break; }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception ex) {
                        PublishError(ex);
                        // 短暂等待，避免异常风暴；继续监听
                        try { await Task.Delay(200, ct).ConfigureAwait(false); } catch { /* ignore */ }
                        continue;
                    }

                    // 配置新连接
                    try {
                        client.NoDelay = true;
                        client.ReceiveBufferSize = _opt.ReceiveBufferSize;

                        // 替换活动连接
                        ReplaceActive(client);

                        // 为当前活动连接启动接收循环
                        var stream = _activeStream!;
                        _ = ReceiveLoopAsync(client, stream, ct);
                    }
                    catch (Exception ex) {
                        PublishError(ex);
                        TryClose(client);
                    }
                }
            }
            finally {
                // 监听退出，但不改变 Status（StopAsync 会改）；若是异常退出，依旧保持资源已按 Stop 清理。
            }
        }

        private void ReplaceActive(TcpClient client) {
            TcpClient? oldClient = null;
            NetworkStream? oldStream = null;

            lock (_gate) {
                // 关闭旧连接
                oldStream = _activeStream; _activeStream = null;
                oldClient = _activeClient; _activeClient = null;

                _activeClient = client;
                _activeStream = client.GetStream();

                SetConnState(TransportConnectionState.Connected);
            }

            TryClose(oldStream);
            TryClose(oldClient);
        }

        private async Task ReceiveLoopAsync(TcpClient client, NetworkStream stream, CancellationToken ct) {
            var buffer = ArrayPool<byte>.Shared.Rent(_opt.ReceiveBufferSize);
            try {
                while (!ct.IsCancellationRequested && Status == TransportStatus.Running && client.Connected) {
                    int read;
                    try {
#if NET8_0_OR_GREATER
                        read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
#else
                    read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
#endif
                    }
                    catch (OperationCanceledException) { break; }
                    catch (IOException ioex) {
                        PublishError(ioex);
                        break;
                    }
                    catch (SocketException sex) {
                        PublishError(sex);
                        break;
                    }
                    catch (Exception ex) {
                        PublishError(ex);
                        break;
                    }

                    if (read <= 0) break; // 被动断开

                    // 紧凑复制后异步发布
                    var payload = new byte[read];
                    Buffer.BlockCopy(buffer, 0, payload, 0, read);

                    PublishData(payload);
                    PublishBytesReceived(buffer);
                }
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);

                // 如果此连接仍是活动连接，清理并置为 Disconnected，但不停止监听
                bool wasActive;
                lock (_gate) {
                    wasActive = ReferenceEquals(client, _activeClient);
                    if (wasActive) {
                        _activeStream = null;
                        _activeClient = null;
                        SetConnState(TransportConnectionState.Disconnected);
                    }
                }

                TryClose(stream);
                TryClose(client);
            }
        }

        // ================== 事件与工具 ==================

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
            ThreadPool.UnsafeQueueUserWorkItem(_ => {
                try { act(); } catch { /* 订阅者异常不影响接收环 */ }
            }, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryClose(IDisposable? d) {
            try { d?.Dispose(); } catch { /* ignore */ }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryStop(TcpListener? l) {
            try { l?.Stop(); } catch { /* ignore */ }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryCancel(CancellationTokenSource? cts) {
            try { cts?.Cancel(); } catch { /* ignore */ }
        }
    }
}