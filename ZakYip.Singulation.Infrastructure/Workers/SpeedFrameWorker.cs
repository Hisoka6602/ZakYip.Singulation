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
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Infrastructure.Telemetry;
using ZakYip.Singulation.Infrastructure.Services;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions;

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
        private readonly ICabinetPipeline _cabinetPipeline;
        private readonly IndicatorLightService? _indicatorLightService;
        private readonly ISystemClock _clock;

        public SpeedFrameWorker(
            ILogger<SpeedFrameWorker> log,
            IUpstreamFrameHub hub,
            IUpstreamCodec codec,
            IAxisController controller,
            IRealtimeNotifier rt,
            IAxisLayoutStore axisLayoutStore,
            IFrameGuard frameGuard,
            ICabinetPipeline cabinetPipeline,
            IndicatorLightService? indicatorLightService = null,
            ISystemClock? clock = null) {
            _log = log;
            _hub = hub;
            _codec = codec;
            _controller = controller;
            _rt = rt;
            _axisLayoutStore = axisLayoutStore;
            _frameGuard = frameGuard;
            _cabinetPipeline = cabinetPipeline;
            _indicatorLightService = indicatorLightService;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //获取布局
            var layoutOptions = await _axisLayoutStore.GetAsync(stoppingToken);
            var (reader, unsub) = _hub.SubscribeSpeed(capacity: 512);
            await _frameGuard.InitializeAsync(stoppingToken).ConfigureAwait(false);
            using (unsub) {
                await foreach (var mem in reader.ReadAllAsync(stoppingToken)) {
                    try {
                        // 【关键修复】本地模式下不处理上游数据，也不改变运行状态
                        if (!_cabinetPipeline.IsRemoteMode) {
                            _log.LogDebug("【本地模式】忽略上游速度数据，不处理也不改变运行状态");
                            continue;
                        }
                        
                        var sw = Stopwatch.StartNew();
                        if (!_codec.TryDecodeSpeed(mem.Span, out var speedSet))
                            continue;

                        var decision = _frameGuard.Evaluate(speedSet);
                        if (!decision.ShouldApply) {
                            _log.LogDebug("Frame dropped by guard: {Reason}", decision.Reason);
                            continue;
                        }

                        // 当远程模式下接收到上游下发速度时判断运行状态，如果不是运行中应该改成运行中，并且亮绿灯
                        // 重要：必须先确保轴已使能并更新状态，再应用速度，否则速度可能无法正确执行
                        if (_indicatorLightService != null) {
                            var currentState = _indicatorLightService.CurrentState;
                            if (currentState != SystemState.Running) {
                                _log.LogInformation("【远程模式速度接收】当前状态为 {State}，先使能轴并更新为运行中，再应用速度", currentState);
                                
                                // 先使能所有轴（如果系统启动时就是远程模式，轴可能还未使能）
                                try {
                                    await _controller.EnableAllAsync(stoppingToken).ConfigureAwait(false);
                                    _log.LogDebug("【远程模式速度接收】轴使能完成");
                                } catch (Exception ex) {
                                    _log.LogWarning(ex, "【远程模式速度接收】使能轴时发生异常，速度可能无法正确应用");
                                }
                                
                                // 更新状态为运行中并亮绿灯
                                await _indicatorLightService.UpdateStateAsync(SystemState.Running, stoppingToken).ConfigureAwait(false);
                                _log.LogInformation("【远程模式速度接收】状态已更新为运行中");
                            }
                        }

                        await _controller.ApplySpeedSetAsync(
                            _codec.SetGridLayout(decision.Output, layoutOptions.Rows),
                            stoppingToken).ConfigureAwait(false);

                        sw.Stop();
                        SingulationMetrics.Instance.LoopDuration.Record(sw.Elapsed.TotalMilliseconds);
                        if (speedSet.TimestampUtc != default) {
                            var rtt = (_clock.UtcNow - speedSet.TimestampUtc).TotalMilliseconds;
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