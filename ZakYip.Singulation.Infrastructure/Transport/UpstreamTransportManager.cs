using System.Net;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Transport.Tcp;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Transport.Abstractions;
using ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport;
using ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport;

namespace ZakYip.Singulation.Infrastructure.Transport {

    /// <summary>
    /// 上游传输管理器：支持配置热更新。
    /// 当配置变更时，自动停止旧连接、创建新连接并启动。
    /// </summary>
    public sealed class UpstreamTransportManager : IDisposable {
        private readonly ILogger<UpstreamTransportManager> _logger;
        private readonly IUpstreamOptionsStore _store;
        private readonly object _gate = new();
        
        private IByteTransport? _speedTransport;
        private IByteTransport? _positionTransport;
        private IByteTransport? _heartbeatTransport;
        
        private UpstreamOptions? _currentOptions;
        private bool _disposed;

        public UpstreamTransportManager(
            ILogger<UpstreamTransportManager> logger,
            IUpstreamOptionsStore store) {
            _logger = logger;
            _store = store;
        }

        /// <summary>获取 Speed 传输实例（用于依赖注入）</summary>
        public IByteTransport? SpeedTransport {
            get {
                lock (_gate) return _speedTransport;
            }
        }

        /// <summary>获取 Position 传输实例（用于依赖注入）</summary>
        public IByteTransport? PositionTransport {
            get {
                lock (_gate) return _positionTransport;
            }
        }

        /// <summary>获取 Heartbeat 传输实例（用于依赖注入）</summary>
        public IByteTransport? HeartbeatTransport {
            get {
                lock (_gate) return _heartbeatTransport;
            }
        }

        /// <summary>获取所有非空的传输实例</summary>
        public IEnumerable<IByteTransport> GetAllTransports() {
            lock (_gate) {
                if (_speedTransport != null) yield return _speedTransport;
                if (_positionTransport != null) yield return _positionTransport;
                if (_heartbeatTransport != null) yield return _heartbeatTransport;
            }
        }

        /// <summary>初始化传输（读取配置并创建传输实例）</summary>
        public async Task InitializeAsync(CancellationToken ct = default) {
            var options = await _store.GetAsync(ct).ConfigureAwait(false);
            await ReloadTransportsAsync(options, startImmediately: false, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// 热更新传输：使用新配置重新创建所有传输实例。
        /// </summary>
        /// <param name="newOptions">新的配置参数</param>
        /// <param name="startImmediately">是否立即启动新传输（默认为 true）</param>
        /// <param name="ct">取消令牌</param>
        public async Task ReloadTransportsAsync(
            UpstreamOptions newOptions,
            bool startImmediately = true,
            CancellationToken ct = default) {
            
            if (_disposed) {
                throw new ObjectDisposedException(nameof(UpstreamTransportManager));
            }

            _logger.LogInformation(
                "[UpstreamTransportManager] Reloading transports with new config: Host={Host}, Role={Role}, " +
                "SpeedPort={SpeedPort}, PositionPort={PositionPort}, HeartbeatPort={HeartbeatPort}",
                newOptions.Host, newOptions.Role, newOptions.SpeedPort, 
                newOptions.PositionPort, newOptions.HeartbeatPort);

            IByteTransport? oldSpeed = null;
            IByteTransport? oldPosition = null;
            IByteTransport? oldHeartbeat = null;

            IByteTransport? newSpeed = null;
            IByteTransport? newPosition = null;
            IByteTransport? newHeartbeat = null;

            try {
                // 创建新传输实例
                newSpeed = CreateTransport(newOptions, "speed", newOptions.SpeedPort);
                newPosition = CreateTransport(newOptions, "position", newOptions.PositionPort);
                newHeartbeat = CreateTransport(newOptions, "heartbeat", newOptions.HeartbeatPort);

                // 交换引用（在锁内进行，保证线程安全）
                lock (_gate) {
                    oldSpeed = _speedTransport;
                    oldPosition = _positionTransport;
                    oldHeartbeat = _heartbeatTransport;

                    _speedTransport = newSpeed;
                    _positionTransport = newPosition;
                    _heartbeatTransport = newHeartbeat;
                    _currentOptions = newOptions;
                }

                // 启动新传输（如果需要）
                if (startImmediately) {
                    await StartAllTransportsAsync(ct).ConfigureAwait(false);
                }

                _logger.LogInformation("[UpstreamTransportManager] New transports created and swapped successfully");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[UpstreamTransportManager] Failed to reload transports");
                
                // 回滚：恢复旧传输
                lock (_gate) {
                    _speedTransport = oldSpeed;
                    _positionTransport = oldPosition;
                    _heartbeatTransport = oldHeartbeat;
                }
                
                // 清理创建失败的新传输
                await DisposeTransportAsync(newSpeed).ConfigureAwait(false);
                await DisposeTransportAsync(newPosition).ConfigureAwait(false);
                await DisposeTransportAsync(newHeartbeat).ConfigureAwait(false);
                
                throw;
            }
            finally {
                // 停止并释放旧传输（异步进行，不阻塞）
                _ = Task.Run(async () => {
                    await StopAndDisposeTransportAsync(oldSpeed, "old-speed").ConfigureAwait(false);
                    await StopAndDisposeTransportAsync(oldPosition, "old-position").ConfigureAwait(false);
                    await StopAndDisposeTransportAsync(oldHeartbeat, "old-heartbeat").ConfigureAwait(false);
                }, CancellationToken.None);
            }
        }

        /// <summary>启动所有传输</summary>
        public async Task StartAllTransportsAsync(CancellationToken ct = default) {
            IByteTransport? speed, position, heartbeat;
            
            lock (_gate) {
                speed = _speedTransport;
                position = _positionTransport;
                heartbeat = _heartbeatTransport;
            }

            await StartTransportAsync(speed, "speed", ct).ConfigureAwait(false);
            await StartTransportAsync(position, "position", ct).ConfigureAwait(false);
            await StartTransportAsync(heartbeat, "heartbeat", ct).ConfigureAwait(false);
        }

        /// <summary>停止所有传输</summary>
        public async Task StopAllTransportsAsync(CancellationToken ct = default) {
            IByteTransport? speed, position, heartbeat;
            
            lock (_gate) {
                speed = _speedTransport;
                position = _positionTransport;
                heartbeat = _heartbeatTransport;
            }

            await StopTransportAsync(speed, "speed", ct).ConfigureAwait(false);
            await StopTransportAsync(position, "position", ct).ConfigureAwait(false);
            await StopTransportAsync(heartbeat, "heartbeat", ct).ConfigureAwait(false);
        }

        /// <summary>根据配置创建传输实例，如果端口 <= 0 则返回 null</summary>
        private IByteTransport? CreateTransport(UpstreamOptions options, string name, int port) {
            // 端口 <= 0 时不创建传输，避免无效连接尝试
            if (port <= 0) {
                _logger.LogInformation("[UpstreamTransportManager] Skipping transport '{Name}' creation (port={Port} is invalid)", name, port);
                return null;
            }
            
            return options.Role == TransportRole.Server
                ? new TouchServerByteTransport(new TcpServerOptions {
                    Address = IPAddress.Any,
                    Port = port,
                })
                : new TouchClientByteTransport(new TcpClientOptions {
                    Host = options.Host,
                    Port = port
                });
        }

        private async Task StartTransportAsync(IByteTransport? transport, string name, CancellationToken ct) {
            if (transport == null) return;
            
            try {
                await transport.StartAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[UpstreamTransportManager] Transport '{Name}' started", name);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[UpstreamTransportManager] Failed to start transport '{Name}'", name);
            }
        }

        private async Task StopTransportAsync(IByteTransport? transport, string name, CancellationToken ct) {
            if (transport == null) return;
            
            try {
                await transport.StopAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("[UpstreamTransportManager] Transport '{Name}' stopped", name);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "[UpstreamTransportManager] Error stopping transport '{Name}'", name);
            }
        }

        private async Task StopAndDisposeTransportAsync(IByteTransport? transport, string name) {
            if (transport == null) return;
            
            try {
                await transport.StopAsync(CancellationToken.None).ConfigureAwait(false);
                await transport.DisposeAsync().ConfigureAwait(false);
                _logger.LogInformation("[UpstreamTransportManager] Transport '{Name}' stopped and disposed", name);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "[UpstreamTransportManager] Error disposing transport '{Name}'", name);
            }
        }

        private async Task DisposeTransportAsync(IByteTransport? transport) {
            if (transport == null) return;
            
            try {
                await transport.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "[UpstreamTransportManager] Error disposing transport");
            }
        }

        public void Dispose() {
            if (_disposed) return;
            
            lock (_gate) {
                if (_disposed) return;
                _disposed = true;
            }

            // 同步等待停止所有传输
            try {
                StopAllTransportsAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "[UpstreamTransportManager] Error during dispose");
            }

            // 释放资源
            _speedTransport?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _positionTransport?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _heartbeatTransport?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
