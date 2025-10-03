using System;
using System.Linq;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport {

    /// <summary>
    /// 基于 TouchSocket 的 TCP 服务端传输：视觉作为 Client 连接进来。
    /// 仅做字节搬运（监听、收包、上抛），不解析业务。
    /// </summary>
    public sealed class TouchServerByteTransport : IByteTransport {
        private readonly TcpServerOptions _opt;
        private TcpService? _service;
        private readonly object _gate = new();           // 保护 Start/Stop 并发
        private volatile bool _stopping;                 // Stop 标志
        private int _connCount;                          // 当前连接数
        private TransportConnectionState _connState;     // 连接状态（IByteTransport）

        public TransportStatus Status { get; private set; } = TransportStatus.Stopped;
        public string? RemoteIp { get; private set; }
        public int RemotePort { get; private set; }
        public bool IsServer { get; }

        TransportConnectionState IByteTransport.Status => _connState;

        /// <summary>推模式：收到任意字节即回调。</summary>
        public event Action<ReadOnlyMemory<byte>>? Data;

        public event EventHandler<BytesReceivedEventArgs>? BytesReceived;

        public event EventHandler<TransportStateChangedEventArgs>? StateChanged;

        public event EventHandler<TransportErrorEventArgs>? Error;

        public TouchServerByteTransport(TcpServerOptions opt) {
            _opt = opt;
            IsServer = true;
        }

        public async Task StartAsync(CancellationToken ct = default) {
            lock (_gate) {
                if (Status is TransportStatus.Running or TransportStatus.Starting)
                    return;

                _stopping = false;
                Status = TransportStatus.Starting;
            }

            var endpoint = $"{_opt.Address}:{_opt.Port}";
            SetConnState(TransportConnectionState.Connecting, endpoint, reason: "listen start");

            var service = new TcpService();

            // 连接建立
            service.Connected = (client, e) => {
                try {
                    if (client.MainSocket?.RemoteEndPoint is System.Net.IPEndPoint ep) {
                        RemoteIp = ep.Address.ToString();
                        RemotePort = ep.Port;
                    }
                }
                catch { /* ignore */ }
                var n = Interlocked.Increment(ref _connCount);
                if (n == 1) // 首个客户端上来
                {
                    Status = TransportStatus.Running;
                    SetConnState(TransportConnectionState.Connected, endpoint, reason: "first client connected");
                }
                return EasyTask.CompletedTask;
            };

            // 连接关闭
            service.Closed = (client, e) => {
                var n = Interlocked.Decrement(ref _connCount);
                if (n <= 0 && !_stopping) {
                    // 服务仍在监听，但此刻无任何客户端
                    SetConnState(TransportConnectionState.Disconnected, endpoint, reason: "no clients", passive: true);
                }
                return EasyTask.CompletedTask;
            };

            // 收包：隔离订阅者
            service.Received = (client, e) => {
                var payload = e.ByteBlock.Span.ToArray(); // 安全起见拷贝；可替换为池化方案
                RaiseData(payload);
                RaiseBytesReceived(payload, _opt.Port);
                return EasyTask.CompletedTask;
            };

            try {
                await service.SetupAsync(new TouchSocketConfig()
                    .SetListenIPHosts(new IPHost(_opt.Address, _opt.Port))
                ).ConfigureAwait(false);

                await service.StartAsync().ConfigureAwait(false);

                lock (_gate) {
                    _service = service;
                    Status = TransportStatus.Running;
                }

                // 刚启动监听，尚无客户端 ⇒ 标记为 Disconnected（服务可用但未连接）
                SetConnState(TransportConnectionState.Disconnected, endpoint, reason: "listening");
            }
            catch (Exception ex) {
                Status = TransportStatus.Faulted;
                RaiseError($"server start failed: {ex.Message}", ex, transient: false, endpoint: endpoint, port: _opt.Port);

                try { service.SafeDispose(); } catch { /* ignore */ }
            }
        }

        public async Task StopAsync(CancellationToken ct = default) {
            TcpService? svc;
            lock (_gate) {
                if (_service is null) {
                    Status = TransportStatus.Stopped;
                    SetConnState(TransportConnectionState.Stopped, $"{_opt.Address}:{_opt.Port}", reason: "stop (no service)");
                    return;
                }

                _stopping = true;
                svc = _service;
                _service = null; // 防止并发重复 Stop
            }

            try {
                // 解除委托，避免 Stop 过程触发回调
                svc.Connected = null;
                svc.Closed = null;
                svc.Received = null;

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(TimeSpan.FromSeconds(2)); // 软超时
                try { await svc.StopAsync(linked.Token).ConfigureAwait(false); } catch { /* ignore */ }
                try { svc.Dispose(); } catch { /* ignore */ }
            }
            finally {
                Status = TransportStatus.Stopped;
                Interlocked.Exchange(ref _connCount, 0);
                SetConnState(TransportConnectionState.Stopped, $"{_opt.Address}:{_opt.Port}", reason: "stopped");
                _stopping = false;
            }
        }

        public async Task RestartAsync(CancellationToken ct = default) {
            var endpoint = $"{_opt.Address}:{_opt.Port}";
            try {
                await StopAsync(ct).ConfigureAwait(false);

                // 服务端重启给个很小的间隔，降低端口复用抖动（TIME_WAIT 等）
                try { await Task.Delay(200, ct).ConfigureAwait(false); } catch { /* ignore */ }

                await StartAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                RaiseError($"server restart failed: {ex.Message}", ex, transient: true,
                    endpoint: endpoint, port: _opt.Port);
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        // =========================
        // 内部：通用工具 & 事件隔离
        // =========================

        private void SetConnState(TransportConnectionState s, string? endpoint = null, string? reason = null,
                                  Exception? ex = null, int? attempt = null, TimeSpan? nextDelay = null, bool passive = false) {
            _connState = s;
            RaiseState(s, endpoint, reason, ex, attempt, nextDelay, passive);
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

            foreach (var @delegate in handler.GetInvocationList()) {
                var single = (EventHandler<TransportStateChangedEventArgs>)@delegate;
                _ = Task.Run(() => {
                    try { single(this, args); } catch { /* 忽略订阅方异常 */ }
                });
            }
        }

        private void RaiseData(ReadOnlyMemory<byte> payload) {
            var d = Data;
            if (d is null) return;

            foreach (var @delegate in d.GetInvocationList()) {
                var single = (Action<ReadOnlyMemory<byte>>)@delegate;
                var mem = payload; // 捕获
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