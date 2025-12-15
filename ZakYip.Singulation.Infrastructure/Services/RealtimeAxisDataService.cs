using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Infrastructure.Logging;

namespace ZakYip.Singulation.Infrastructure.Services {
    /// <summary>
    /// 轴实时数据广播服务，定期推送轴的速度和位置数据
    /// </summary>
    public sealed class RealtimeAxisDataService : BackgroundService {
        private readonly ILogger<RealtimeAxisDataService> _logger;
        private readonly IAxisController _axisController;
        private readonly IHubContext<Hub> _hubContext;
        private readonly ExceptionAggregationService? _exceptionAggregation;
        private readonly LogSampler _logSampler;
        private readonly TimeSpan _broadcastInterval = TimeSpan.FromMilliseconds(200); // 5Hz 更新率
        private readonly ISystemClock _clock;

        public RealtimeAxisDataService(
            ILogger<RealtimeAxisDataService> logger,
            IAxisController axisController,
            IHubContext<Hub> hubContext,
            ISystemClock clock,
            ExceptionAggregationService? exceptionAggregation = null) {
            _logger = logger;
            _axisController = axisController;
            _hubContext = hubContext;
            _exceptionAggregation = exceptionAggregation;
            _logSampler = new LogSampler(clock);
            _clock = clock;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("轴实时数据广播服务启动，更新频率: {Frequency}Hz", 1000.0 / _broadcastInterval.TotalMilliseconds);

            try {
                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Delay(_broadcastInterval, stoppingToken);
                    await BroadcastAxisDataAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) {
                _logger.LogInformation("轴实时数据广播服务已取消");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "轴实时数据广播服务发生错误");
            }
        }

        private async Task BroadcastAxisDataAsync(CancellationToken ct) {
            try {
                var drives = _axisController.Drives.ToList();
                var timestamp = _clock.Now;

                foreach (var drive in drives) {
                    try {
                        var data = new RealtimeAxisDataDto {
                            AxisId = drive.Axis.ToString(),
                            CurrentSpeedMmps = drive.LastFeedbackMmps.HasValue ? (double)drive.LastFeedbackMmps.Value : null,
                            CurrentPositionMm = null, // Position not available in current interface
                            TargetSpeedMmps = drive.LastTargetMmps.HasValue ? (double)drive.LastTargetMmps.Value : null,
                            Enabled = drive.IsEnabled,
                            Timestamp = timestamp
                        };

                        // 推送到所有订阅该轴的客户端
                        await _hubContext.Clients.Group($"Axis_{drive.Axis}")
                            .SendAsync("ReceiveAxisData", data, ct);

                        // 同时推送到订阅所有轴的客户端
                        await _hubContext.Clients.Group("AllAxes")
                            .SendAsync("ReceiveAxisData", data, ct);
                    }
                    catch (Exception ex) {
                        // 使用日志采样避免高频错误日志泛滥
                        if (_logSampler.ShouldLog($"AxisDataBroadcast_{drive.Axis}", 100)) {
                            var count = _logSampler.GetCount($"AxisDataBroadcast_{drive.Axis}");
                            _logger.HighFrequencyOperationSampled("AxisDataBroadcast", count, 100);
                            _logger.LogDebug(ex, "推送轴 {AxisId} 数据失败 (已采样记录, 总次数: {Count})", drive.Axis, count);
                        }
                        _exceptionAggregation?.RecordException(ex, $"AxisDataBroadcast:Axis{drive.Axis}");
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "广播轴数据失败");
                _exceptionAggregation?.RecordException(ex, "AxisDataBroadcast:Global");
            }
        }
    }
}
