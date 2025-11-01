using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Channels;
using System.Collections.Concurrent;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Transport.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Infrastructure.Transport;
using ZakYip.Singulation.Infrastructure.Configuration;
using ZakYip.Singulation.Infrastructure.Logging;
using ZakYip.Singulation.Infrastructure.Services;

namespace ZakYip.Singulation.Infrastructure.Workers {

    /// <summary>
    /// 极简低延迟事件泵：
    /// - Transport.Data：同步“快路径”扇出到 Hub（零排队，订阅者各自通道做 DropOldest）。
    /// - Transport.BytesReceived/StateChanged/Error：走“慢路径”写入有界通道做日志与统计。
    /// - Axis.*（Faulted/Disconnected/DriverNotLoaded）：走独立 Axis 通道，集中落日志（不再伪装成 TransportEvent）。
    /// </summary>
    public sealed class TransportEventPump : BackgroundService {
        private readonly ILogger<TransportEventPump> _log;
        private readonly List<(string Name, IByteTransport Transport)> _transports = new();

        // 传输侧“慢路径”：仅承载低频事件，容量很小即可
        private readonly Channel<TransportEvent> _ctlChannel;

        // 轴侧事件：独立通道，避免把非字节数据塞进 TransportEvent
        private readonly Channel<AxisEvent> _axisChannel = Channel.CreateBounded<AxisEvent>(new BoundedChannelOptions(InfrastructureConstants.AxisEventChannelCapacity) {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        // 轻量指标：按源计数写入/丢弃
        private readonly ConcurrentDictionary<string, long> _dropped = new();

        private readonly ConcurrentDictionary<string, long> _written = new();

        private readonly IUpstreamFrameHub _hub;
        private readonly IAxisEventAggregator _axisEventAggregator;
        private readonly IServiceProvider _sp;
        private readonly IRealtimeNotifier _rt;
        private readonly IAxisController _axisController;
        private readonly UpstreamTransportManager _transportManager;
        private readonly IndicatorLightService? _indicatorLightService;

        private bool _axisSubscribed;
        private long _axisDropped;
        private long _axisWritten;

        public TransportEventPump(
            ILogger<TransportEventPump> log,
            IServiceProvider sp,
            IUpstreamFrameHub hub,
            IAxisEventAggregator axisEventAggregator,
            IRealtimeNotifier rt,
            IAxisController axisController,
            UpstreamTransportManager transportManager,
            IndicatorLightService? indicatorLightService = null) {
            _log = log;
            _sp = sp;
            _hub = hub;
            _axisEventAggregator = axisEventAggregator;
            _rt = rt;
            _axisController = axisController;
            _transportManager = transportManager;
            _indicatorLightService = indicatorLightService;

            // 传输侧慢路径：小容量即可；DropOldest 防抖
            _ctlChannel = Channel.CreateBounded<TransportEvent>(new BoundedChannelOptions(InfrastructureConstants.TransportControlEventChannelCapacity) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // 初始化传输管理器（读取配置并创建传输实例，但不启动）
            try {
                await _transportManager.InitializeAsync(stoppingToken).ConfigureAwait(false);
                _log.TransportManagerInitialized();
            }
            catch (Exception ex) {
                _log.TransportManagerInitializationFailed(ex);
                // 继续执行，因为传输可能稍后会被初始化
            }

            // 从传输管理器获取所有已创建的传输（跳过端口 <= 0 的传输）
            // Explicitly map transport names to their instances to avoid index-based errors
            var speedTransport = _transportManager.SpeedTransport;
            if (speedTransport != null) {
                _transports.Add(("speed", speedTransport));
                _log.LogInformation("Transport '{Name}' resolved successfully", "speed");
            }
            var positionTransport = _transportManager.PositionTransport;
            if (positionTransport != null) {
                _transports.Add(("position", positionTransport));
                _log.LogInformation("Transport '{Name}' resolved successfully", "position");
            }
            var heartbeatTransport = _transportManager.HeartbeatTransport;
            if (heartbeatTransport != null) {
                _transports.Add(("heartbeat", heartbeatTransport));
                _log.LogInformation("Transport '{Name}' resolved successfully", "heartbeat");
            }

            // Ensure essential transports are present
            var essentialTransports = new[] { "speed" }; // Add more keys if needed
            foreach (var essential in essentialTransports)
            {
                if (!_transports.Any(t => t.Item1 == essential))
                {
                    _log.EssentialTransportMissing(essential);
                    throw new InvalidOperationException($"Essential transport '{essential}' is missing.");
                }
            }
            SubscribeAxisEventsOnce();

            // 订阅（只一次）
            foreach (var (name, t) in _transports) Subscribe(name, t);

            // 启动所有传输
            foreach (var (name, t) in _transports) {
                try {
                    await t.StartAsync(stoppingToken).ConfigureAwait(false);
                    _log.TransportStartedWithStatus(name, t.Status.ToString());
                }
                catch (Exception ex) {
                    _log.TransportStartFailed(ex, name);
                }
            }

            var ctlReader = _ctlChannel.Reader;
            var axisReader = _axisChannel.Reader;

            // 合并处理：优先 drain 两侧通道，空时小睡 2ms，避免忙等
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    // 1) 先尽量清空传输侧慢路径
                    while (ctlReader.TryRead(out var tev)) {
                        ProcessTransportEvent(tev);
                    }
                    // 2) 再清空轴侧事件
                    while (axisReader.TryRead(out var aev)) {
                        ProcessAxisEvent(aev);
                    }
                    // 3) 都空了，稍作等待
                    if (!ctlReader.TryPeek(out _) && !axisReader.TryPeek(out _)) {
                        await Task.Delay(InfrastructureConstants.EventPumpIdleDelayMs, stoppingToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) {
                    // 任意一侧处理异常都要兜底，避免吞事件
                    _log.EventPumpPipelineError(ex);
                }
            }
        }

        public override async Task StopAsync(CancellationToken ct) {
            // 先停源，再收尾
            foreach (var (name, t) in _transports) {
                try { await t.StopAsync(ct).ConfigureAwait(false); }
                catch (OperationCanceledException ex) { _log.TransportStopIgnored(ex, name); }
                catch (Exception ex)
                {
                    _log.TransportStopIgnored(ex, name);
                    throw;
                }
            }
            _ctlChannel.Writer.TryComplete();
            _axisChannel.Writer.TryComplete();

            // 打点输出（停止时统计更准确）
            foreach (var (name, _) in _transports) {
                var dropped = _dropped.TryGetValue(name, out var d) ? d : 0;
                var written = _written.TryGetValue(name, out var w) ? w : 0;
                if (dropped > 0)
                    _log.TransportStatsWithDrops(name, dropped, written);
                else
                    _log.TransportStatsNoDrops(name, written);
            }

            // 补：轴侧总统计
            if (_axisDropped > 0)
                _log.AxisStatsWithDrops(_axisDropped, _axisWritten);
            else
                _log.AxisStatsNoDrops(_axisWritten);

            await base.StopAsync(ct).ConfigureAwait(false);
        }

        private void Subscribe(string name, IByteTransport t) {
            // ============= 关键优化：Data 走“快路径” =============
            // 直接在事件回调里同步广播到 Hub，避免队列 hop
            t.Data += mem => {
                switch (name) {
                    case "speed":
                        _hub.PublishSpeed(mem);
                        _ = _rt.PublishVisionAsync(new {
                            kind = "speed.raw",
                            len = mem.Length,
                            hex = BitConverter.ToString(mem.ToArray()).Replace("-", " ")
                        });
                        break;

                    case "heartbeat":
                        _hub.PublishHeartbeat(mem);
                        _ = _rt.PublishVisionAsync(new {
                            kind = "heartbeat.raw",
                            len = mem.Length
                        });
                        break;

                    case "position":
                        _hub.PublishPosition(mem);
                        _ = _rt.PublishVisionAsync(new {
                            kind = "position.raw",
                            len = mem.Length
                        });
                        break;

                    default:
                        // 未知名的通道仍可走控制事件日志通道
                        _ = _ctlChannel.Writer.TryWrite(new TransportEvent(
                            name, TransportEventType.Data, mem, 0, default, null
                        ));
                        break;
                }
                // 轻量记账（不打日志，避免干扰快路径）
                _written.AddOrUpdate(name, 1, static (_, v) => v + 1);
            };

            // ============= 慢路径：统计/状态/错误 =============
            t.BytesReceived += (s, e) => {
                if (_ctlChannel.Writer.TryWrite(new TransportEvent(
                    name, TransportEventType.BytesReceived, e.Buffer, e.Buffer.Length, default, null))) {
                    _written.AddOrUpdate(name, 1, static (_, v) => v + 1);
                    _ = _rt.PublishDeviceAsync(new {
                        kind = "transport.bytes",
                        source = name,
                        len = e.Buffer.Length
                    }); // 即刻广播
                }
                else {
                    _dropped.AddOrUpdate(name, 1, static (_, v) => v + 1);
                }
            };

            // 低频关键事件：允许轻度背压
            t.StateChanged += (s, e) => {
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    name, TransportEventType.StateChanged, default, 0, e.State, null)).AsTask();

                _ = _rt.PublishDeviceAsync(new {
                    kind = "transport.state",
                    source = name,
                    state = e.State.ToString()
                });
            };

            t.Error += (s, e) => {
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    name, TransportEventType.Error, default, 0, default, e.Exception)).AsTask();
                _ = _rt.PublishErrorAsync(new {
                    kind = "transport.error",
                    source = name,
                    error = e.Exception?.Message
                });
            };
        }

        private void SubscribeAxisEventsOnce() {
            if (_axisSubscribed) return;
            _axisController.ControllerFaulted += (sender, s) => {
                var ok = _axisChannel.Writer.TryWrite(new AxisEvent(
                    source: "axisController",
                    type: AxisEventType.ControllerFaulted,
                    axisId: new AxisId(0),
                    reason: s,
                    exception: null
                ));
                if (!ok) Interlocked.Increment(ref _axisDropped);
                else Interlocked.Increment(ref _axisWritten);
            };
            // 轴故障（真正异常对象）→ AxisEvent.Faulted
            _axisEventAggregator.AxisFaulted += (s, e) => {
                var ok = _axisChannel.Writer.TryWrite(new AxisEvent(
                    source: $"axis:{e.Axis}",
                    type: AxisEventType.Faulted,
                    axisId: e.Axis,
                    reason: null,
                    exception: e.Exception
                ));
                if (!ok) Interlocked.Increment(ref _axisDropped);
                else Interlocked.Increment(ref _axisWritten);
            };

            // 轴断开/掉线（文本原因）→ AxisEvent.Disconnected
            _axisEventAggregator.AxisDisconnected += (s, e) => {
                var ok = _axisChannel.Writer.TryWrite(new AxisEvent(
                    source: $"axis:{e.Axis}",
                    type: AxisEventType.Disconnected,
                    axisId: e.Axis,
                    reason: e.Reason,
                    exception: null
                ));
                if (!ok) Interlocked.Increment(ref _axisDropped);
                else Interlocked.Increment(ref _axisWritten);
            };

            // 驱动未加载（库级问题）→ AxisEvent.DriverNotLoaded
            _axisEventAggregator.DriverNotLoaded += (s, e) => {
                var ok = _axisChannel.Writer.TryWrite(new AxisEvent(
                    source: $"driver:{e.LibraryName}",
                    type: AxisEventType.DriverNotLoaded,
                    axisId: null,
                    reason: e.Message,
                    exception: null
                ));
                if (!ok) Interlocked.Increment(ref _axisDropped);
                else Interlocked.Increment(ref _axisWritten);
            };

            // —— 以下两类仍作为“可观测数据”走 TransportEvent.Data（JSON 轻量记录），保持原有做法 ——

            // 命令已下发（仅观测，不要阻塞，不要 Task.Run）
            _axisEventAggregator.CommandIssued += (s, e) => {
                var json = JsonConvert.SerializeObject(new { e.Axis, e.Invocation });
                var bytes = Encoding.UTF8.GetBytes(json);
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    $"axis:{e.Axis}", TransportEventType.Data, bytes, 0, default, null
                ));
            };

            // 速度/状态反馈（仅观测）
            _axisEventAggregator.SpeedFeedback += (s, e) => {
                var json = JsonConvert.SerializeObject(new { e.Axis, e.Rpm, e.SpeedMps, e.TimestampUtc });
                var bytes = Encoding.UTF8.GetBytes(json);
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    $"axis:{e.Axis}", TransportEventType.Data, bytes, 0, default, null
                ));
            };

            _axisSubscribed = true;
        }

        // ========== 事件处理：传输侧 ==========
        private void ProcessTransportEvent(TransportEvent ev) {
            switch (ev.Type) {
                case TransportEventType.BytesReceived:
                    /*var len = ev.Count > 0 ? ev.Count : ev.Payload.Length;
                    var replace = BitConverter.ToString(ev.Payload.ToArray()).Replace("-", " ");
                    _log.LogDebug("[transport.rx] {Source} bytes={Len}  {type} bytesString={replace}", ev.Source, len, ev.Source, replace);*/
                    break;

                case TransportEventType.StateChanged:
                    _log.TransportStateChanged(ev.Source, ev.Conn.ToString());
                    // 更新远程连接指示灯状态
                    UpdateRemoteConnectionLight(ev.Conn);
                    break;

                case TransportEventType.Error:
                    _log.TransportErrorOccurred(ev.Exception!, ev.Source);
                    break;

                case TransportEventType.Data:
                default:
                    // 仅在 axis:* 的“观测数据”时打印一条信息日志（不会刷屏）
                    if (ev.Source.StartsWith("axis:", StringComparison.OrdinalIgnoreCase) && !ev.Payload.IsEmpty) {
                        // _log.LogInformation("[axis.data] {Source} {Json}", ev.Source, Encoding.UTF8.GetString(ev.Payload.Span));
                    }
                    break;
            }
        }

        // ========== 事件处理：轴侧 ==========
        // Note: Axis event logging uses structured logging instead of LoggerMessage
        // due to technical limitations with AxisId value object conversion in source generator
        private void ProcessAxisEvent(AxisEvent ev) {
            switch (ev.Type) {
                case AxisEventType.Faulted:
                    _log.LogError(ev.Exception, "[{Source}] axis faulted (axis={Axis}), Reason({Reason}), Exception({Exception})", 
                        ev.Source, ev.AxisId.Value, ev.Reason ?? string.Empty, ev.Exception);
                    break;

                case AxisEventType.Disconnected:
                    _log.LogWarning("[{Source}] axis disconnected (axis={Axis}) reason={Reason}", 
                        ev.Source, ev.AxisId.Value, ev.Reason ?? string.Empty);
                    break;

                case AxisEventType.DriverNotLoaded:
                    _log.LogError("[{Source}] driver not loaded: {Reason}", ev.Source, ev.Reason ?? string.Empty);
                    break;

                case AxisEventType.ControllerFaulted:
                    _log.LogError("[{Source}] controller fault: {Reason}", ev.Source, ev.Reason ?? string.Empty);
                    break;

                default:
                    _log.LogWarning("[{Source}] unhandled axis event type={Type}", ev.Source, ev.Type);
                    break;
            }
        }

        /// <summary>
        /// 根据上游传输连接状态更新远程连接指示灯。
        /// </summary>
        /// <param name="state">传输连接状态</param>
        private void UpdateRemoteConnectionLight(TransportConnectionState state) {
            if (_indicatorLightService == null) {
                return;
            }

            // 只有当任一上游传输连接成功时才点亮指示灯
            bool isAnyConnected = _transportManager.GetAllTransports()
                .Any(t => t.Status == TransportConnectionState.Connected);

            try {
                // 使用 Task.Run 避免潜在的死锁问题
                _ = Task.Run(async () => {
                    await _indicatorLightService.UpdateRemoteConnectionStateAsync(isAnyConnected, CancellationToken.None)
                        .ConfigureAwait(false);
                }, CancellationToken.None);
            }
            catch (Exception ex) {
                _log.LogWarning(ex, "更新远程连接指示灯状态失败");
            }
        }
    }
}