using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ZakYip.Singulation.Core.Enums;
using System.Security.Cryptography.Xml;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Protocol.Abstractions;
using ZakYip.Singulation.Transport.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;

namespace ZakYip.Singulation.Host.Workers {

    /// <summary>
    /// 极简低延迟事件泵：
    /// - Data：同步“快路径”扇出到 Hub（零排队、DropOldest 由订阅者各自通道承担）。
    /// - BytesReceived/StateChanged/Error：仍走“慢路径”写入有界通道做日志与统计。
    /// </summary>
    public sealed class TransportEventPump : BackgroundService {
        private readonly ILogger<TransportEventPump> _log;
        private readonly List<(string Name, IByteTransport Transport)> _transports = new();

        // 慢路径：仅承载低频事件，容量很小即可
        private readonly Channel<TransportEvent> _ctlChannel;

        // 轻量指标：按源计数写入/丢弃
        private readonly ConcurrentDictionary<string, long> _dropped = new();

        private readonly ConcurrentDictionary<string, long> _written = new();

        private readonly IUpstreamFrameHub _hub;
        private readonly IAxisEventAggregator _axisEventAggregator;
        private readonly IServiceProvider _sp;
        private bool _axisSubscribed;

        public TransportEventPump(
            ILogger<TransportEventPump> log,
            IServiceProvider sp,
            IUpstreamFrameHub hub,
            IAxisEventAggregator axisEventAggregator) {
            _log = log;
            _sp = sp;
            _hub = hub;
            _axisEventAggregator = axisEventAggregator;

            // 慢路径：小容量即可；DropOldest 防抖
            _ctlChannel = Channel.CreateBounded<TransportEvent>(new BoundedChannelOptions(256) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

            // 通过 Keyed DI 聚合（speed/position/heartbeat）
            var keys = new[] { "speed", "position", "heartbeat" };
            foreach (var key in keys) {
                var t = _sp.GetKeyedService<IByteTransport>(key);
                if (t != null) _transports.Add((key, t));
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            SubscribeAxisEventsOnce();
            // 订阅（只一次）
            foreach (var (name, t) in _transports) Subscribe(name, t); // 原实现在这里做订阅 :contentReference[oaicite:1]{index=1}

            // 启动所有传输
            foreach (var (name, t) in _transports) {
                try {
                    await t.StartAsync(stoppingToken).ConfigureAwait(false);
                    _log.LogInformation("[transport:{Name}] started, status={Status}", name, t.Status);
                }
                catch (Exception ex) {
                    _log.LogError(ex, "[transport:{Name}] start failed", name);
                }
            }

            // 仅消费“慢路径”通道
            await foreach (var ev in _ctlChannel.Reader.ReadAllAsync(stoppingToken)) {
                try {
                    switch (ev.Type) {
                        case TransportEventType.BytesReceived:
                            _log.LogDebug("[transport.rx] {Source} bytes={Len}", ev.Source, ev.Payload.Length);
                            break;

                        case TransportEventType.StateChanged:
                            _log.LogInformation("[transport:{Source}] state={State}", ev.Source, ev.Conn);
                            break;

                        case TransportEventType.Error:
                            _log.LogError(ev.Exception, "[transport:{Source}] error", ev.Source);
                            break;

                        // Data 永远不会走到这里（已走快路径）
                        case TransportEventType.Data:
                        default:
                            // 新增：轴侧 Data 的轻量可观测日志（避免把所有 Data 都打出来）
                            if (ev.Source.StartsWith("axis:", StringComparison.OrdinalIgnoreCase) && !ev.Payload.IsEmpty) {
                                _log.LogInformation("[axis.data] {Source} {Json}", ev.Source, Encoding.UTF8.GetString(ev.Payload.Span));
                            }
                            break;
                    }
                }
                catch (Exception ex) {
                    // 低频异常隔离
                    _log.LogError(ex, "[event-pump] ctl pipeline error");
                }
            }
        }

        public override async Task StopAsync(CancellationToken ct) {
            // 先停源，再收尾
            foreach (var (name, t) in _transports) {
                try { await t.StopAsync(ct).ConfigureAwait(false); }
                catch (Exception ex) { _log.LogDebug(ex, "[transport:{Name}] stop ignored", name); }
            }
            _ctlChannel.Writer.TryComplete();

            // 打点输出
            foreach (var (name, _) in _transports) {
                var dropped = _dropped.TryGetValue(name, out var d) ? d : 0;
                var written = _written.TryGetValue(name, out var w) ? w : 0;
                if (dropped > 0)
                    _log.LogWarning("[transport:{Name}] dropped={Dropped} written={Written}", name, dropped, written);
                else
                    _log.LogInformation("[transport:{Name}] written={Written}", name, written);
            }

            await base.StopAsync(ct).ConfigureAwait(false);
        }

        private void Subscribe(string name, IByteTransport t) {
            // ============= 关键优化：Data 走“快路径” =============
            // 直接在事件回调里同步广播到 Hub，避免先入队再出队的额外 hop 和分配。
            t.Data += mem => {
                // 按源类型扇出（你已用 speed/position/heartbeat 命名注册）
                // 心跳与位置若需要也可加 PublishXxx。
                switch (name) {
                    case "speed":
                        _hub.PublishSpeed(mem); // 新实现：在订阅处直发，不经 _channel
                        break;

                    case "heartbeat":
                        _hub.PublishHeartbeat(mem);
                        break;

                    case "position":
                        _hub.PublishPosition(mem);
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
                    name, TransportEventType.BytesReceived, default, e.Buffer.Length, default, null))) {
                    _written.AddOrUpdate(name, 1, static (_, v) => v + 1);
                }
                else {
                    _dropped.AddOrUpdate(name, 1, static (_, v) => v + 1);
                }
            };

            // 低频关键事件：允许轻度背压
            t.StateChanged += (s, e) => {
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    name, TransportEventType.StateChanged, default, 0, e.State, null)).AsTask();
            };

            t.Error += (s, e) => {
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    name, TransportEventType.Error, default, 0, default, e.Exception)).AsTask();
            };
        }

        private void SubscribeAxisEventsOnce() {
            if (_axisSubscribed) return;

            // 轴错误
            _axisEventAggregator.AxisFaulted += (s, e) => {
                // 走慢路径，类型沿用 Error，Source 统一前缀 axis
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    $"axis:{e.Axis}", TransportEventType.Error, default, 0, default, e.Exception
                ));
            };

            // 轴断开/掉线
            _axisEventAggregator.AxisDisconnected += (s, e) => {
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    $"axis:{e.Axis}", TransportEventType.StateChanged, default, 0,
                    TransportConnectionState.Disconnected, null
                ));
            };

            // 驱动未加载
            _axisEventAggregator.DriverNotLoaded += (s, e) => {
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    $"{e.Message}", TransportEventType.Error, default, 0, default,
                    new InvalidOperationException($"Driver not loaded: {e.Message}")
                ));
            };

            // 命令已下发（作为可观测数据写入 Data；下面会在慢路径里专门打印 axis:* 的 Data）
            _axisEventAggregator.CommandIssued += (s, e) => {
                var json = JsonConvert.SerializeObject(new { e.Axis, e.Invocation, });
                var bytes = Encoding.UTF8.GetBytes(json);
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    $"axis:{e.Axis}", TransportEventType.Data, bytes, 0, default, null
                ));
            };

            // 速度/状态反馈（同上，写 Data；不阻塞、不重活）
            _axisEventAggregator.SpeedFeedback += (s, e) => {
                var json = JsonConvert.SerializeObject(new { e.Axis, e.Rpm, e.SpeedMps, e.TimestampUtc });
                var bytes = Encoding.UTF8.GetBytes(json);
                _ = _ctlChannel.Writer.WriteAsync(new TransportEvent(
                    $"axis:{e.Axis}", TransportEventType.Data, bytes, 0, default, null
                ));
            };

            _axisSubscribed = true;
        }
    }
}