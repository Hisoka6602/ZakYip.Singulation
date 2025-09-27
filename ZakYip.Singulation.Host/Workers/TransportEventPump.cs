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
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Protocol.Abstractions;
using ZakYip.Singulation.Transport.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Host.Workers {

    public sealed class TransportEventPump : BackgroundService {
        private readonly ILogger<TransportEventPump> _log;
        private readonly List<(string Name, IByteTransport Transport)> _transports = new();
        private readonly Channel<TransportEvent> _channel;
        private readonly IServiceProvider _sp;
        private readonly IUpstreamCodec _upstreamCodec;
        private readonly ILogEventWriter _logWriter;
        private readonly IAxisController _axisController;

        // 轻量指标：按通道计数的写入/丢弃次数
        private readonly ConcurrentDictionary<string, long> _dropped = new();

        private readonly ConcurrentDictionary<string, long> _written = new();

        public TransportEventPump(ILogger<TransportEventPump> log,
            IServiceProvider sp,
            IUpstreamCodec upstreamCodec,
            ILogEventWriter logWriter,
            IAxisController axisController) {
            _log = log;
            _sp = sp;
            _upstreamCodec = upstreamCodec;
            _logWriter = logWriter;
            _axisController = axisController;

            _channel = Channel.CreateBounded<TransportEvent>(new BoundedChannelOptions(4096) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest // 数据面保最新视图
            });

            // 如果你稍后做成从配置/DB读取，只要把 keys 换掉即可
            var keys = new[] { "speed", "position", "heartbeat" };
            foreach (var key in keys) {
                var t = _sp.GetKeyedService<IByteTransport>(key);
                if (t != null) _transports.Add((key, t));
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // 订阅事件（只订阅一次）
            foreach (var (name, t) in _transports) Subscribe(name, t);

            // 尝试启动所有传输
            foreach (var (name, t) in _transports) {
                try {
                    await t.StartAsync(stoppingToken);
                    _log.LogInformation("[transport:{Name}] started, status={Status}", name, t.Status);
                }
                catch (Exception ex) {
                    _log.LogError(ex, "[transport:{Name}] start failed", name);
                }
            }

            await foreach (var ev in _channel.Reader.ReadAllAsync(stoppingToken)) {
                try {
                    switch (ev.Type) {
                        case TransportEventType.Data: {
                                // 轻解 + 下游投递（重活放到下游worker）
                                // 如果你的 IUpstreamCodec 返回 SpeedSet，就保持原调用
                                if (_upstreamCodec.TryDecodeSpeed(ev.Payload.Span, out var speedSet)) {
                                    _ = _axisController.ApplySpeedSetAsync(speedSet, stoppingToken);
                                }
                                // 可选：实时调试日志（走日志泵，避免异常路径）
                                _logWriter.TryWrite(new LogEvent {
                                    Kind = LogKind.Debug,
                                    Category = "transport.data",
                                    Message = "speed-frame",
                                    Props = new Dictionary<string, object> {
                                        ["channel"] = ev.Source,
                                        ["len"] = ev.Payload.Length
                                    }
                                });
                                break;
                            }

                        case TransportEventType.BytesReceived: {
                                _logWriter.TryWrite(new LogEvent {
                                    Kind = LogKind.Debug,
                                    Category = "transport.rx",
                                    Message = "bytes",
                                    Props = new Dictionary<string, object> {
                                        ["channel"] = ev.Source,
                                        ["len"] = ev.Payload.Length
                                    }
                                });
                                break;
                            }

                        case TransportEventType.StateChanged: {
                                _logWriter.TryWrite(new LogEvent {
                                    Kind = LogKind.Info,
                                    Category = "transport",
                                    Message = $"state={ev.Conn}",
                                    Props = new Dictionary<string, object> {
                                        ["channel"] = ev.Source
                                    }
                                });
                                break;
                            }

                        case TransportEventType.Error: {
                                // 异常走立即写盘，不进日志泵
                                _log.LogError(ev.Exception, "[transport:{Source}] {Msg}", ev.Source, ev.Exception?.Message);
                                break;
                            }
                    }
                }
                catch (Exception ex) {
                    // 解码或下游异常被隔离，不影响接收环
                    _log.LogError(ex, "[event-pump] pipeline error");
                }
            }
        }

        public override async Task StopAsync(CancellationToken ct) {
            // 先停源，再完成通道
            foreach (var (name, t) in _transports) {
                try { await t.StopAsync(ct); }
                catch (Exception ex) { _log.LogDebug(ex, "[transport:{Name}] stop error (ignored)", name); }
            }
            _channel.Writer.TryComplete();

            // 打点：各通道丢弃与写入统计
            foreach (var (name, _) in _transports) {
                var dropped = _dropped.TryGetValue(name, out var d) ? d : 0;
                var written = _written.TryGetValue(name, out var w) ? w : 0;
                if (dropped > 0)
                    _log.LogWarning("[transport:{Name}] dropped={Dropped} written={Written}", name, dropped, written);
                else
                    _log.LogInformation("[transport:{Name}] written={Written}", name, written);
            }

            await base.StopAsync(ct);
        }

        private void Subscribe(string name, IByteTransport t) {
            // 高频数据：TryWrite（丢旧保新）+ 记账
            t.Data += mem => {
                if (_channel.Writer.TryWrite(new TransportEvent(name, TransportEventType.Data, mem, 0, default, null))) {
                    _written.AddOrUpdate(name, 1, (_, v) => v + 1);
                }
                else {
                    _dropped.AddOrUpdate(name, 1, (_, v) => v + 1);
                }
            };

            t.BytesReceived += (s, e) => {
                if (_channel.Writer.TryWrite(new TransportEvent(name, TransportEventType.BytesReceived, default, e.Buffer.Length, default, null))) {
                    _written.AddOrUpdate(name, 1, (_, v) => v + 1);
                }
                else {
                    _dropped.AddOrUpdate(name, 1, (_, v) => v + 1);
                }
            };

            // 低频关键：绝不丢，用 WriteAsync 背压一下写端
            t.StateChanged += (s, e) => {
                _ = _channel.Writer.WriteAsync(
                    new TransportEvent(name, TransportEventType.StateChanged, default, 0, e.State, null)
                ).AsTask();
            };

            t.Error += (s, e) => {
                _ = _channel.Writer.WriteAsync(
                    new TransportEvent(name, TransportEventType.Error, default, 0, default, e.Exception)
                ).AsTask();
            };
        }
    }
}