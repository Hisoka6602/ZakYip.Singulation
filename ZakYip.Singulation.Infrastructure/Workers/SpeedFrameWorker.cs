using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
﻿using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using ZakYip.Singulation.Infrastructure.Runtime;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Protocol.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Infrastructure.Telemetry;

namespace ZakYip.Singulation.Infrastructure.Workers {

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
        private readonly IAxisLayoutStore _axisLayoutStore;
        private readonly IFrameGuard _frameGuard;

        public SpeedFrameWorker(
            ILogger<SpeedFrameWorker> log,
            IUpstreamFrameHub hub,
            IUpstreamCodec codec,
            IAxisController controller,
            IRealtimeNotifier rt,
            IAxisLayoutStore axisLayoutStore,
            IFrameGuard frameGuard) {
            _log = log;
            _hub = hub;
            _codec = codec;
            _controller = controller;
            _rt = rt;
            _axisLayoutStore = axisLayoutStore;
            _frameGuard = frameGuard;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //获取布局
            var layoutOptions = await _axisLayoutStore.GetAsync(stoppingToken);
            var (reader, unsub) = _hub.SubscribeSpeed(capacity: 512);
            await _frameGuard.InitializeAsync(stoppingToken).ConfigureAwait(false);
            using (unsub) {
                await foreach (var mem in reader.ReadAllAsync(stoppingToken)) {
                    try {
                        var sw = Stopwatch.StartNew();
                        if (!_codec.TryDecodeSpeed(mem.Span, out var speedSet))
                            continue;

                        var decision = _frameGuard.Evaluate(speedSet);
                        if (!decision.ShouldApply) {
                            _log.LogDebug("Frame dropped by guard: {Reason}", decision.Reason);
                            continue;
                        }

                        await _controller.ApplySpeedSetAsync(
                            _codec.SetGridLayout(decision.Output, layoutOptions.Rows),
                            stoppingToken).ConfigureAwait(false);

                        sw.Stop();
                        SingulationMetrics.Instance.LoopDuration.Record(sw.Elapsed.TotalMilliseconds);
                        if (speedSet.TimestampUtc != default) {
                            var rtt = (DateTime.UtcNow - speedSet.TimestampUtc).TotalMilliseconds;
                            if (rtt >= 0)
                                SingulationMetrics.Instance.FrameRtt.Record(rtt);
                        }

                        // 广播解码后的“轻量”视图
                        _ = _rt.PublishVisionAsync(new {
                            kind = "speed.decoded",
                            sequence = speedSet.Sequence,
                            mainCount = speedSet.MainMmps?.Count ?? 0,
                            ejectCount = speedSet.EjectMmps?.Count ?? 0,
                            degraded = decision.DegradedApplied
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