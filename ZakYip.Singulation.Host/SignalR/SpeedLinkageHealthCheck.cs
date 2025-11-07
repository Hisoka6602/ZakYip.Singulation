using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Singulation.Infrastructure.Services;

namespace ZakYip.Singulation.Host.SignalR {

    /// <summary>
    /// 速度联动服务健康检查，监控服务运行状态和性能指标。
    /// </summary>
    public sealed class SpeedLinkageHealthCheck : IHealthCheck {
        // 健康检查阈值常量
        private const double UnhealthyErrorRateThreshold = 0.1;      // 10% 错误率视为不健康
        private const double DegradedErrorRateThreshold = 0.01;      // 1% 错误率视为降级
        private const double DegradedIoFailureThreshold = 0.2;       // 20% IO失败率视为降级
        private static readonly TimeSpan MaxAllowedTimeSinceLastCheck = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan RecentErrorTimeWindow = TimeSpan.FromMinutes(5);

        private readonly ISpeedLinkageService _speedLinkageService;

        /// <summary>
        /// 初始化 <see cref="SpeedLinkageHealthCheck"/> 类的新实例。
        /// </summary>
        public SpeedLinkageHealthCheck(ISpeedLinkageService speedLinkageService) {
            _speedLinkageService = speedLinkageService;
        }

        /// <summary>
        /// 执行健康检查。
        /// </summary>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) {

            try {
                var stats = _speedLinkageService.GetStatistics();

                var data = new Dictionary<string, object> {
                    ["totalChecks"] = stats.TotalChecks,
                    ["totalStateChanges"] = stats.TotalStateChanges,
                    ["totalIoWrites"] = stats.TotalIoWrites,
                    ["failedIoWrites"] = stats.FailedIoWrites,
                    ["totalErrors"] = stats.TotalErrors,
                    ["lastCheckTime"] = stats.LastCheckTime,
                    ["lastErrorTime"] = stats.LastErrorTime,
                    ["activeGroupsCount"] = stats.ActiveGroupsCount,
                    ["isRunning"] = stats.IsRunning
                };

                // 计算健康度评分（0-100）
                double healthScore = CalculateHealthScore(stats);
                data["healthScore"] = healthScore;

                // 检查服务是否长时间未运行
                var timeSinceLastCheck = DateTime.UtcNow - stats.LastCheckTime;
                if (stats.LastCheckTime != DateTime.MinValue && timeSinceLastCheck > MaxAllowedTimeSinceLastCheck) {
                    return Task.FromResult(
                        HealthCheckResult.Unhealthy(
                            $"速度联动服务未运行，上次检查时间：{stats.LastCheckTime:yyyy-MM-dd HH:mm:ss}",
                            data: data));
                }

                // 检查错误率
                if (stats.TotalErrors > 0) {
                    var errorRate = stats.TotalChecks > 0 ? (double)stats.TotalErrors / stats.TotalChecks : 0;
                    if (errorRate > UnhealthyErrorRateThreshold) {
                        return Task.FromResult(
                            HealthCheckResult.Unhealthy(
                                $"速度联动服务错误率过高：{errorRate:P2} ({stats.TotalErrors}/{stats.TotalChecks})",
                                data: data));
                    }
                    if (errorRate > DegradedErrorRateThreshold) {
                        return Task.FromResult(
                            HealthCheckResult.Degraded(
                                $"速度联动服务存在少量错误：{errorRate:P2} ({stats.TotalErrors}/{stats.TotalChecks})",
                                data: data));
                    }
                }

                // 检查IO写入失败率
                if (stats.TotalIoWrites > 0) {
                    var ioFailureRate = (double)stats.FailedIoWrites / stats.TotalIoWrites;
                    if (ioFailureRate > DegradedIoFailureThreshold) {
                        return Task.FromResult(
                            HealthCheckResult.Degraded(
                                $"速度联动IO写入失败率较高：{ioFailureRate:P2} ({stats.FailedIoWrites}/{stats.TotalIoWrites})",
                                data: data));
                    }
                }

                // 检查最近是否有错误
                if (stats.LastErrorTime != DateTime.MinValue) {
                    var timeSinceLastError = DateTime.UtcNow - stats.LastErrorTime;
                    if (timeSinceLastError < RecentErrorTimeWindow) {
                        data["recentError"] = stats.LastError ?? "未知错误";
                        return Task.FromResult(
                            HealthCheckResult.Degraded(
                                $"速度联动服务最近发生错误（{timeSinceLastError.TotalMinutes:F1}分钟前）：{stats.LastError}",
                                data: data));
                    }
                }

                return Task.FromResult(
                    HealthCheckResult.Healthy(
                        $"速度联动服务运行正常，健康度评分：{healthScore:F1}/100",
                        data: data));
            }
            catch (Exception ex) {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        "速度联动健康检查失败",
                        ex));
            }
        }

        /// <summary>
        /// 计算健康度评分（0-100）
        /// </summary>
        private double CalculateHealthScore(SpeedLinkageStatistics stats) {
            double score = 100.0;

            // 扣分因素1：错误率（最多扣40分）
            if (stats.TotalChecks > 0 && stats.TotalErrors > 0) {
                var errorRate = (double)stats.TotalErrors / stats.TotalChecks;
                score -= Math.Min(40, errorRate * 400);
            }

            // 扣分因素2：IO写入失败率（最多扣30分）
            if (stats.TotalIoWrites > 0 && stats.FailedIoWrites > 0) {
                var ioFailureRate = (double)stats.FailedIoWrites / stats.TotalIoWrites;
                score -= Math.Min(30, ioFailureRate * 150);
            }

            // 扣分因素3：最近错误（最多扣20分）
            if (stats.LastErrorTime != DateTime.MinValue) {
                var timeSinceLastError = DateTime.UtcNow - stats.LastErrorTime;
                if (timeSinceLastError < TimeSpan.FromHours(1)) {
                    var recentErrorPenalty = 20 * (1 - timeSinceLastError.TotalMinutes / 60);
                    score -= recentErrorPenalty;
                }
            }

            // 扣分因素4：长时间未检查（最多扣10分）
            if (stats.LastCheckTime != DateTime.MinValue) {
                var timeSinceLastCheck = DateTime.UtcNow - stats.LastCheckTime;
                if (timeSinceLastCheck > TimeSpan.FromSeconds(10)) {
                    score -= Math.Min(10, timeSinceLastCheck.TotalSeconds / 6);
                }
            }

            return Math.Max(0, score);
        }
    }
}
