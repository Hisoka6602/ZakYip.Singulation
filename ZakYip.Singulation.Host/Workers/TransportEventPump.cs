using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Protocol.Abstractions;
using ZakYip.Singulation.Transport.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Host.Workers {

    public sealed class TransportEventPump : BackgroundService {
        private readonly ILogger<TransportEventPump> _log;
        private readonly List<(string Name, IByteTransport Transport)> _transports = new();
        private readonly Channel<TransportEvent> _channel;
        private readonly IServiceProvider _sp;
        private readonly IUpstreamCodec _upstreamCodec;

        public TransportEventPump(ILogger<TransportEventPump> log, IServiceProvider sp,
            IUpstreamCodec upstreamCodec) {
            _log = log;
            _sp = sp;
            _upstreamCodec = upstreamCodec;

            _channel = Channel.CreateBounded<TransportEvent>(new BoundedChannelOptions(4096) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

            // 你可以从配置/数据库动态生成 keys；这里举例：
            var keys = new[] { "speed", "position", "heartbeat" };

            foreach (var key in keys) {
                var t = _sp.GetKeyedService<IByteTransport>(key); // 找不到 => null，不会抛
                if (t != null) _transports.Add((key, t));
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // 订阅所有传输的事件
            foreach (var (name, t) in _transports) {
                Subscribe(name, t);
            }

            // 启动所有传输（若在别处已启动，这段可省）
            foreach (var (_, t) in _transports) {
                try { await t.StartAsync(stoppingToken); }
                catch (Exception ex) { _log.LogError(ex, "Start {T} failed", t.GetType().Name); }
            }

            // 单线程消费，保持事件顺序（每个源内部顺序基本可保持）
            await foreach (var ev in _channel.Reader.ReadAllAsync(stoppingToken)) {
                try {
                    switch (ev.Type) {
                        case TransportEventType.Data:
                            // TODO: 在这里做协议解码/路由/聚合/转发（SignalR、gRPC、队列等）
                            // DecodeAndDispatch(ev.Source, ev.Payload.Span);
                            Console.WriteLine(BitConverter.ToString(ev.Payload.ToArray()).Replace("-", " "));
                            _upstreamCodec.TryDecodeSpeed(ev.Payload.Span, out var speedSet);
                            Console.WriteLine(JsonConvert.SerializeObject(speedSet));
                            break;

                        case TransportEventType.BytesReceived:
                            // 轻量指标：吞吐、速率
                            // Metrics.IncBytes(ev.Source, ev.Count);
                            break;

                        case TransportEventType.StateChanged:
                            _log.LogInformation("[{src}] {state}", ev.Source, ev.Conn);
                            break;

                        case TransportEventType.Error:
                            _log.LogWarning(ev.Exception, "[{src}] transport error", ev.Source);
                            break;
                    }
                }
                catch (Exception ex) {
                    // 解码或下游异常在这里被隔离，不影响传输接收环
                    _log.LogError(ex, "Event pipeline error");
                }
            }
        }

        public override async Task StopAsync(CancellationToken ct) {
            // 优雅停止：先停源，再等通道消费完
            foreach (var (_, t) in _transports) {
                try { await t.StopAsync(ct); } catch { /* ignore */ }
            }
            _channel.Writer.TryComplete(); // 触发 ReadAllAsync 退出
            await base.StopAsync(ct);
        }

        private void Subscribe(string name, IByteTransport t) {
            // 注意：IByteTransport 内部已经用线程池异步投递，这里只是再汇聚到通道
            t.Data += mem =>
                _channel.Writer.TryWrite(new TransportEvent(name, TransportEventType.Data, mem, 0, default, null));

            t.BytesReceived += (s, e) =>
                _channel.Writer.TryWrite(new TransportEvent(name, TransportEventType.BytesReceived, e.Buffer, e.Buffer.Length, default, null));

            t.StateChanged += (s, e) =>
                _channel.Writer.TryWrite(new TransportEvent(name, TransportEventType.StateChanged, default, 0, e.State, null));

            t.Error += (s, e) =>
                _channel.Writer.TryWrite(new TransportEvent(name, TransportEventType.Error, default, 0, default, e.Exception));
        }
    }
}