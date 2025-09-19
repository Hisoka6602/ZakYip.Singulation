using Polly;
using System;
using Polly.Retry;
using System.Linq;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Transport.Enums;
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

        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;

        /// <summary>推模式：收到任意字节即回调。</summary>
        public event Action<ReadOnlyMemory<byte>>? Data;

        public TouchClientByteTransport(TcpClientOptions opt) => _opt = opt;

        public Task StartAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status == TransportStatus.Running || Status == TransportStatus.Starting)
                    return Task.CompletedTask;

                _stopping = false;
                Status = TransportStatus.Starting;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _connectLoopTask = Task.Run(() => ConnectLoopAsync(_cts.Token), _cts.Token);
            }
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct = default) {
            Task? loop;
            lock (_gate) {
                if (Status == TransportStatus.Stopped)
                    return;

                _stopping = true;
                Status = TransportStatus.Stopped;
                try { _cts?.Cancel(); } catch { /* ignore */ }
                loop = _connectLoopTask;

                // 关闭现有连接
                SafeCloseClient();
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

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        // =========================
        // 内部：连接/重连循环
        // =========================
        private async Task ConnectLoopAsync(CancellationToken token) {
            var retry = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    // 指数退避 + 抖动，最大 10s
                    DelayGenerator = args => {
                        var exp = Math.Min(Math.Pow(2, args.AttemptNumber), 20); // 2^n * 500ms
                        var delay = TimeSpan.FromMilliseconds(500 * exp);
                        if (delay > TimeSpan.FromSeconds(10)) delay = TimeSpan.FromSeconds(10);
                        var jitterMs = new Random().Next(0, 500);
                        return new ValueTask<TimeSpan?>(delay + TimeSpan.FromMilliseconds(jitterMs));
                    },
                    MaxRetryAttempts = int.MaxValue
                })
                .Build();

            while (!token.IsCancellationRequested && !_stopping) {
                try {
                    await retry.ExecuteAsync(async ct => {
                        var client = new TcpClient();

                        // ① 为“被动断开”准备一个一次性信号
                        var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                        client.Connected = (c, e) => {
                            SetStatus(TransportStatus.Running);
                            return EasyTask.CompletedTask;
                        };

                        client.Closed = (c, e) => {
                            // 标记断开 → 触发下一次重连
                            if (!_stopping) {
                                SetStatus(TransportStatus.Faulted);
                                closedTcs.TrySetResult();
                            }
                            return EasyTask.CompletedTask;
                        };

                        client.Received = (c, e) => {
                            var payload = e.ByteBlock.Span.ToArray();
                            Data?.Invoke(payload);
                            return EasyTask.CompletedTask;
                        };

                        await client.SetupAsync(new TouchSocketConfig()
                            .SetRemoteIPHost(new IPHost($"{_opt.Host}:{_opt.Port}"))

                        );

                        // 建议用无超时的 ConnectAsync(ct)；若你确实要 1000ms 超时，可保留你原来的重载
                        await client.ConnectAsync(1000, ct);

                        lock (_gate) {
                            SafeCloseClient();   // 防御性关闭旧实例
                            _client = client;
                        }

                        // ② 等待“被动断开”或取消信号，一旦发生就抛异常让 Retry 接管
                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        var finished = await Task.WhenAny(closedTcs.Task, Task.Delay(Timeout.Infinite, linked.Token));
                        if (finished == closedTcs.Task) {
                            throw new Exception("Disconnected"); // 进入下一轮重连
                        }
                    }, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    break; // 正常退出
                }
                catch {
                    if (token.IsCancellationRequested || _stopping) break;
                    SetStatus(TransportStatus.Faulted);
                    // 无需额外延时，这里马上由 Retry 策略安排下一次尝试
                }
            }
        }

        private void SafeCloseClient() {
            try { _client?.Close(); } catch { /* ignore */ }
            try { _client?.Dispose(); } catch { /* ignore */ }
            _client = null;
        }

        private void SetStatus(TransportStatus s) {
            // 保持状态机单调合理：Stop 时不再跳回 Running
            if (_stopping && s != TransportStatus.Stopped) return;
            Status = s;
        }
    }
}