using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Runtime;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Protocol.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;

namespace ZakYip.Singulation.Host.Workers {

    /// <summary>
    /// 从 Hub 订阅速度帧：每个 Worker 都拿到“同一帧”的独立通道，不互相影响。
    /// 单消费者、顺序处理；不使用 Task.Run。
    /// </summary>
    public sealed class SpeedFrameWorker : BackgroundService {
        private readonly ILogger<SpeedFrameWorker> _log;
        private readonly IUpstreamFrameHub _hub;
        private readonly IUpstreamCodec _codec;
        private readonly IAxisController _controller;
        private readonly IRealtimeNotifier _rt;

        public SpeedFrameWorker(
            ILogger<SpeedFrameWorker> log,
            IUpstreamFrameHub hub,
            IUpstreamCodec codec,
            IAxisController controller,
            IRealtimeNotifier rt) {
            _log = log;
            _hub = hub;
            _codec = codec;
            _controller = controller;
            _rt = rt;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var (reader, unsub) = _hub.SubscribeSpeed(capacity: 512);
            using (unsub) {
                await foreach (var mem in reader.ReadAllAsync(stoppingToken)) {
                    try {
                        if (!_codec.TryDecodeSpeed(mem.Span, out var speedSet))
                            continue;

                        await _controller.ApplySpeedSetAsync(speedSet, stoppingToken).ConfigureAwait(false);

                        // 广播解码后的“轻量”视图
                        _ = _rt.PublishVisionAsync(new {
                            kind = "speed.decoded",
                            sequence = speedSet.Sequence,
                            mainCount = speedSet.MainMmps?.Count ?? 0,
                            ejectCount = speedSet.EjectMmps?.Count ?? 0
                        }, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                    catch (Exception ex) {
                        _log.LogError(ex, "SpeedFrameWorker failed.");
                    }
                }
            }
        }
    }
}