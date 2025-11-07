using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Infrastructure.Telemetry;

namespace ZakYip.Singulation.Infrastructure.Services {
    /// <summary>
    /// 系统健康监控服务，定期计算系统健康度并通过 SignalR 推送
    /// </summary>
    public sealed class SystemHealthMonitorService : BackgroundService {
        private readonly ILogger<SystemHealthMonitorService> _logger;
        private readonly IAxisController _axisController;
        private readonly IHubContext<Hub> _hubContext;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

        // 性能指标滑动窗口
        private readonly Queue<double> _responseTimeWindow = new(20);
        private readonly Queue<bool> _operationResultWindow = new(100);
        private readonly object _metricsLock = new();

        public SystemHealthMonitorService(
            ILogger<SystemHealthMonitorService> logger,
            IAxisController axisController,
            IHubContext<Hub> hubContext) {
            _logger = logger;
            _axisController = axisController;
            _hubContext = hubContext;
        }

        /// <summary>
        /// 记录操作响应时间
        /// </summary>
        public void RecordResponseTime(double milliseconds) {
            lock (_metricsLock) {
                _responseTimeWindow.Enqueue(milliseconds);
                if (_responseTimeWindow.Count > 20) {
                    _responseTimeWindow.Dequeue();
                }
            }
        }

        /// <summary>
        /// 记录操作结果
        /// </summary>
        public void RecordOperationResult(bool success) {
            lock (_metricsLock) {
                _operationResultWindow.Enqueue(success);
                if (_operationResultWindow.Count > 100) {
                    _operationResultWindow.Dequeue();
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("系统健康监控服务启动");

            try {
                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Delay(_checkInterval, stoppingToken);
                    await CheckAndBroadcastHealthAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) {
                _logger.LogInformation("系统健康监控服务已取消");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "系统健康监控服务发生错误");
            }
        }

        private async Task CheckAndBroadcastHealthAsync(CancellationToken ct) {
            try {
                var health = CalculateSystemHealth();
                
                // 通过 SignalR 推送到订阅的客户端
                await _hubContext.Clients.Group("SystemHealth")
                    .SendAsync("ReceiveHealthData", health, ct);

                if (health.Level == HealthLevel.Critical || health.Level == HealthLevel.Warning) {
                    _logger.LogWarning("系统健康度: {Score}, 等级: {Level}, 说明: {Description}", 
                        health.Score, health.Level, health.Description);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "计算和推送系统健康度失败");
            }
        }

        private SystemHealthDto CalculateSystemHealth() {
            var drives = _axisController.GetAllDrives().ToList();
            var totalCount = drives.Count;
            var onlineCount = drives.Count(d => d.Status == DriverStatus.Ready || d.Status == DriverStatus.Enabled);
            var faultedCount = drives.Count(d => d.Status == DriverStatus.Error || d.Status == DriverStatus.Disconnected);

            double avgResponseTime;
            double errorRate;

            lock (_metricsLock) {
                avgResponseTime = _responseTimeWindow.Count > 0 ? _responseTimeWindow.Average() : 0;
                errorRate = _operationResultWindow.Count > 0 
                    ? (double)_operationResultWindow.Count(r => !r) / _operationResultWindow.Count 
                    : 0;
            }

            // 计算健康度评分 (0-100)
            double score = 100.0;

            // 在线率影响 (最多扣40分)
            if (totalCount > 0) {
                var onlineRate = (double)onlineCount / totalCount;
                score -= (1.0 - onlineRate) * 40;
            }

            // 故障率影响 (最多扣30分)
            if (totalCount > 0) {
                var faultRate = (double)faultedCount / totalCount;
                score -= faultRate * 30;
            }

            // 错误率影响 (最多扣20分)
            score -= errorRate * 20;

            // 响应时间影响 (最多扣10分)
            if (avgResponseTime > 100) {
                score -= Math.Min(10, (avgResponseTime - 100) / 50);
            }

            score = Math.Max(0, Math.Min(100, score));

            var level = score switch {
                >= 90 => HealthLevel.Excellent,
                >= 70 => HealthLevel.Good,
                >= 40 => HealthLevel.Warning,
                _ => HealthLevel.Critical
            };

            var description = level switch {
                HealthLevel.Excellent => "系统运行优秀，所有指标正常",
                HealthLevel.Good => "系统运行良好",
                HealthLevel.Warning => $"系统存在警告: 在线率 {onlineCount}/{totalCount}, 错误率 {errorRate:P1}",
                HealthLevel.Critical => $"系统状态严重: 故障轴 {faultedCount}, 在线轴 {onlineCount}/{totalCount}",
                _ => "未知状态"
            };

            return new SystemHealthDto {
                Score = Math.Round(score, 2),
                Level = level,
                OnlineAxisCount = onlineCount,
                TotalAxisCount = totalCount,
                FaultedAxisCount = faultedCount,
                AverageResponseTimeMs = Math.Round(avgResponseTime, 2),
                ErrorRate = Math.Round(errorRate, 4),
                Description = description,
                Timestamp = DateTime.Now
            };
        }
    }
}
