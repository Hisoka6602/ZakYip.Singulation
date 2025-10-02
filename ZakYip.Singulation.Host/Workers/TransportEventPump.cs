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
        private readonly IServiceProvider _sp;

        public TransportEventPump(
            ILogger<TransportEventPump> log,
            IServiceProvider sp,
            IUpstreamFrameHub hub) {
            _log = log;
            _sp = sp;
            _hub = hub;

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
                        // 如需：_hub.PublishPosition(mem);
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
    }
}