using Polly;
using System;
using System.Buffers;
using Polly.Retry;
using System.Linq;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport {

    /// <summary>
    /// 基于 TouchSocket 的 TCP 客户端传输：支持 Polly 无限重连（指数退避+抖动）。
    /// 场景：上游视觉作为 Server，本端主动连接；仅搬运字节，不解析业务语义。
    /// </summary>
    public sealed class TouchClientByteTransport : IByteTransport {
        private readonly TcpClientOptions _opt;

        private TcpClient? _client;
        private CancellationTokenSource? _cts;       // 控制连接循环
        private Task? _connectLoopTask;              // 后台连接/重连循环
        private readonly object _gate = new();       // 保护状态与字段
        private volatile bool _stopping;             // Stop 标志，避免“关停时重连”
        private TransportConnectionState _connState; // IByteTransport 的连接状态

        /// <summary>运行层状态（保持兼容）。</summary>
        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;

        public string? RemoteIp { get; }
        public int RemotePort { get; }
        public bool IsServer { get; }

        /// <summary>IByteTransport 要求的连接状态（与上面 Status 并行维护）。</summary>
        TransportConnectionState IByteTransport.Status => _connState;

        /// <summary>推模式：收到任意字节即回调（轻量）。</summary>
        public event Action<ReadOnlyMemory<byte>>? Data;

        /// <summary>带元信息的字节事件（端口、时间戳）。</summary>
        public event EventHandler<BytesReceivedEventArgs>? BytesReceived;

        /// <summary>连接生命周期事件：Connecting/Connected/Retrying/Disconnected/Stopped。</summary>
        public event EventHandler<TransportStateChangedEventArgs>? StateChanged;

        /// <summary>错误/告警事件：不外抛异常。</summary>
        public event EventHandler<TransportErrorEventArgs>? Error;

        public TouchClientByteTransport(TcpClientOptions opt) {
            _opt = opt;
            RemoteIp = opt.Host;
            RemotePort = opt.Port;
            IsServer = false;
        }

        public Task StartAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status is TransportStatus.Running or TransportStatus.Starting)
                    return Task.CompletedTask;

                _stopping = false;
                Status = TransportStatus.Starting;
                SetConnState(TransportConnectionState.Connecting, reason: "start");

                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _connectLoopTask = Task.Run(() => ConnectLoopAsync(_cts.Token), _cts.Token);
            }
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct = default) {
            Task? loop;
            var clientToDispose = default(TcpClient?);
            lock (_gate) {
                if (Status == TransportStatus.Stopped)
                    return;

                _stopping = true;
                Status = TransportStatus.Stopped;
                SetConnState(TransportConnectionState.Stopped, reason: "stop requested");

                try { _cts?.Cancel(); } catch { /* ignore */ }
                loop = _connectLoopTask;

                clientToDispose = _client;
                _client = null;
            }

            if (clientToDispose is not null) {
                await CloseAndDisposeAsync(clientToDispose).ConfigureAwait(false);
            }

            // 等待连接循环退出（给一个短超时，避免阻塞 Stop）
            if (loop is not null) {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));
                try { await Task.WhenAny(loop, Task.Delay(Timeout.Infinite, timeoutCts.Token)); }
                catch { /* ignore */ }
            }

            lock (_gate) {
                _cts?.Dispose();
                _cts = null;
                _connectLoopTask = null;
            }
        }

        public async Task RestartAsync(CancellationToken ct = default) {
            try {
                await StopAsync(ct).ConfigureAwait(false);
                // 客户端通常不需要额外延时；如需可加 50~100ms
                await StartAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                //不抛异常，事件上报
                RaiseError($"client restart failed: {ex.Message}", ex, transient: true,
                    endpoint: $"{_opt.Host}:{_opt.Port}", port: _opt.Port);
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        // =========================
        // 内部：连接/重连循环
        // =========================
        private async Task ConnectLoopAsync(CancellationToken token) {
            var endpoint = $"{_opt.Host}:{_opt.Port}";

            var retry = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    MaxRetryAttempts = int.MaxValue,
                    // 指数退避 + 抖动，最大 10s，并在这里上报 Retrying（attempt/nextDelay）
                    DelayGenerator = args => {
                        var exp = Math.Min(Math.Pow(2, args.AttemptNumber), 20); // 0.5,1,2,4,8,16, ...
                        var delay = TimeSpan.FromMilliseconds(500 * exp);
                        if (delay > TimeSpan.FromSeconds(10)) delay = TimeSpan.FromSeconds(10);
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                        var next = delay + jitter;

                        RaiseState(TransportConnectionState.Retrying, endpoint, attempt: args.AttemptNumber, nextDelay: next, reason: "retry scheduled");
                        return new ValueTask<TimeSpan?>(next);
                    }
                })
                .Build();

            try {
                while (!token.IsCancellationRequested && !_stopping) {
                    try {
                        await retry.ExecuteAsync(async ct => {
                            TcpClient? client = null; // 未交接前局部持有，finally 兜底释放
                            var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                            try {
                                SetConnState(TransportConnectionState.Connecting, reason: "dial");

                                client = new TcpClient();

                                client.Connected = (c, e) => {
                                    SetStatus(TransportStatus.Running);
                                    SetConnState(TransportConnectionState.Connected, endpoint, reason: "connected");
                                    return EasyTask.CompletedTask;
                                };

                                client.Closed = (c, e) => {
                                    // 被动断开：进入 Faulted，并触发下一轮重连
                                    if (!_stopping) {
                                        SetStatus(TransportStatus.Faulted);
                                        SetConnState(TransportConnectionState.Disconnected, endpoint, reason: "closed by remote", passive: true);
                                        closedTcs.TrySetResult();
                                    }
                                    return EasyTask.CompletedTask;
                                };

                                client.Received = (c, e) => {
                                    // 必须拷贝：ByteBlock 的生命周期短于异步处理流程
                                    // 使用 ToArray() 保证线程安全和数据完整性
                                    var payload = e.ByteBlock.Span.ToArray();

                                    // 轻量 Data（无元信息）
                                    RaiseData(payload);

                                    // 带元信息事件
                                    RaiseBytesReceived(payload, _opt.Port);

                                    return EasyTask.CompletedTask;
                                };

                                await client.SetupAsync(new TouchSocketConfig()
                                    .SetRemoteIPHost(new IPHost(endpoint))
                                ).ConfigureAwait(false);

                                // 若偏好由外部取消控制超时，也可改为 await client.ConnectAsync(ct);
                                await client.ConnectAsync(1000, ct).ConfigureAwait(false);

                                // 交接生命周期到字段，并关闭旧连接
                                var previousClient = default(TcpClient?);
                                lock (_gate) {
                                    previousClient = _client;
                                    _client = client;
                                    client = null; // 避免 finally 二次释放
                                }

                                if (previousClient is not null) {
                                    await CloseAndDisposeAsync(previousClient).ConfigureAwait(false);
                                }

                                // 等待：被动断开 或 取消
                                var finished = await Task.WhenAny(
                                    closedTcs.Task,
                                    Task.Delay(Timeout.Infinite, ct)
                                ).ConfigureAwait(false);

                                if (finished == closedTcs.Task) {
                                    // 触发 Retry：抛出受理异常
                                    throw new Exception("Disconnected");
                                }
                            }
                            finally {
                                // 仅当未成功交接到 _client 时才兜底释放
                                if (client is not null) {
                                    await CloseAndDisposeAsync(client).ConfigureAwait(false);
                                }
                            }
                        }, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        break; // 正常退出
                    }
                    catch (Exception ex) {
                        if (token.IsCancellationRequested || _stopping) break;

                        SetStatus(TransportStatus.Faulted);
                        RaiseError($"connect/retry failed: {ex.Message}", ex, transient: true, endpoint: endpoint, port: _opt.Port);
                        // Retry 策略会安排下一次尝试，这里无需额外延时
                    }
                }
            }
            finally {
                // 退出循环：宣告停止
                SetConnState(TransportConnectionState.Stopped, endpoint, reason: "loop exit");
            }
        }

        // =========================
        // 内部：通用工具
        // =========================

        private static async Task CloseAndDisposeAsync(TcpClient client) {
            try { await client.CloseAsync().ConfigureAwait(false); } catch { /* ignore */ }
            try { client.Dispose(); } catch { /* ignore */ }
        }

        private void SetStatus(TransportStatus s) {
            // 保持状态机单调合理：Stop 时不再跳回 Running
            if (_stopping && s != TransportStatus.Stopped) return;
            Status = s;
        }

        private void SetConnState(TransportConnectionState s, string? endpoint = null, string? reason = null, bool passive = false) {
            _connState = s;
            RaiseState(s, endpoint, reason: reason, passive: passive);
        }

        private void RaiseState(TransportConnectionState s, string? endpoint = null, string? reason = null,
                                Exception? ex = null, int? attempt = null, TimeSpan? nextDelay = null, bool passive = false) {
            var handler = StateChanged;
            if (handler is null) return;

            var args = new TransportStateChangedEventArgs {
                State = s,
                Endpoint = endpoint,
                Reason = reason,
                Exception = ex,
                Attempt = attempt,
                NextDelay = nextDelay,
                PassiveClose = passive
            };

            // 隔离订阅方：每个订阅者在独立任务中执行，互不影响
            foreach (var @delegate in handler.GetInvocationList()) {
                var single = (EventHandler<TransportStateChangedEventArgs>)@delegate;
                _ = Task.Run(() => {
                    try { single(this, args); } catch { /* 吃掉订阅方异常，避免影响主循环 */ }
                });
            }
        }

        private void RaiseData(ReadOnlyMemory<byte> payload) {
            var d = Data;
            if (d is null) return;

            foreach (var @delegate in d.GetInvocationList()) {
                var single = (Action<ReadOnlyMemory<byte>>)@delegate;
                var mem = payload; // 捕获到闭包中
                _ = Task.Run(() => {
                    try { single(mem); } catch { /* ignore */ }
                });
            }
        }

        private void RaiseBytesReceived(ReadOnlyMemory<byte> payload, int port) {
            var handler = BytesReceived;
            if (handler is null) return;

            var args = new BytesReceivedEventArgs {
                Buffer = payload,
                Port = port,
                TimestampUtc = DateTime.UtcNow
            };

            foreach (var @delegate in handler.GetInvocationList()) {
                var single = (EventHandler<BytesReceivedEventArgs>)@delegate;
                _ = Task.Run(() => {
                    try { single(this, args); } catch { /* ignore */ }
                });
            }
        }

        private void RaiseError(string message, Exception? ex = null, bool transient = true, string? endpoint = null, int? port = null) {
            var handler = Error;
            if (handler is null) return;

            var args = new TransportErrorEventArgs {
                Message = message,
                Exception = ex,
                IsTransient = transient,
                Endpoint = endpoint,
                Port = port,
                TimestampUtc = DateTime.UtcNow
            };

            foreach (var @delegate in handler.GetInvocationList()) {
                var single = (EventHandler<TransportErrorEventArgs>)@delegate;
                _ = Task.Run(() => {
                    try { single(this, args); } catch { /* ignore */ }
                });
            }
        }
    }
}