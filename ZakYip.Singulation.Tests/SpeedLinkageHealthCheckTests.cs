using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Host.SignalR;
using ZakYip.Singulation.Infrastructure.Services;
using ZakYip.Singulation.Infrastructure.Runtime;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试速度联动服务健康检查功能。
    /// </summary>
    internal sealed class SpeedLinkageHealthCheckTests {

        [MiniFact]
        public async Task HealthCheck_WithMockService_ReturnsHealthy() {
            // 创建模拟服务
            var mockService = new MockSpeedLinkageService();
            
            // 创建健康检查
            var healthCheck = new SpeedLinkageHealthCheck(mockService, CreateClock());
            
            // 执行健康检查
            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);
            
            // 验证结果
            MiniAssert.Equal(HealthStatus.Healthy, result.Status, "服务应该是健康的");
            MiniAssert.NotNull(result.Data, "应该有数据");
        }

        [MiniFact]
        public async Task HealthCheck_ServiceWithGoodStats_ReturnsHealthyWithData() {
            // 创建模拟服务并设置统计信息
            var mockService = new MockSpeedLinkageService {
                Stats = new SpeedLinkageStatistics {
                    TotalChecks = 1000,
                    TotalStateChanges = 10,
                    TotalIoWrites = 20,
                    FailedIoWrites = 0,
                    TotalErrors = 0,
                    LastCheckTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsRunning = true,
                    ActiveGroupsCount = 2
                }
            };
            
            // 创建健康检查
            var healthCheck = new SpeedLinkageHealthCheck(mockService, CreateClock());
            
            // 执行健康检查
            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);
            
            // 验证结果
            MiniAssert.Equal(HealthStatus.Healthy, result.Status, "服务应该是健康的");
            MiniAssert.True(result.Data.ContainsKey("totalChecks"), "应该包含总检查次数");
            MiniAssert.True(result.Data.ContainsKey("healthScore"), "应该包含健康度评分");
            MiniAssert.Equal(1000L, result.Data["totalChecks"], "总检查次数应该正确");
        }

        [MiniFact]
        public async Task HealthCheck_ServiceWithModerateErrors_ReturnsDegraded() {
            // 创建模拟服务并设置错误统计 (5% error rate)
            var mockService = new MockSpeedLinkageService {
                Stats = new SpeedLinkageStatistics {
                    TotalChecks = 100,
                    TotalErrors = 5,
                    LastCheckTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LastErrorTime = new DateTime(2023, 12, 31, 23, 58, 0, DateTimeKind.Utc),
                    LastError = "测试错误",
                    IsRunning = true
                }
            };
            
            // 创建健康检查
            var healthCheck = new SpeedLinkageHealthCheck(mockService, CreateClock());
            
            // 执行健康检查
            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);
            
            // 验证结果
            MiniAssert.Equal(HealthStatus.Degraded, result.Status, "服务应该是降级状态");
        }

        [MiniFact]
        public async Task HealthCheck_ServiceNotRunning_ReturnsUnhealthy() {
            // 创建模拟服务并设置为很久没有检查
            var mockService = new MockSpeedLinkageService {
                Stats = new SpeedLinkageStatistics {
                    TotalChecks = 100,
                    LastCheckTime = new DateTime(2023, 12, 31, 23, 55, 0, DateTimeKind.Utc), // 5分钟没有检查
                    IsRunning = false
                }
            };
            
            // 创建健康检查
            var healthCheck = new SpeedLinkageHealthCheck(mockService, CreateClock());
            
            // 执行健康检查
            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);
            
            // 验证结果
            MiniAssert.Equal(HealthStatus.Unhealthy, result.Status, "服务应该是不健康的");
        }

        [MiniFact]
        public async Task HealthCheck_HighErrorRate_ReturnsUnhealthy() {
            // 创建模拟服务并设置高错误率 (15% error rate)
            var mockService = new MockSpeedLinkageService {
                Stats = new SpeedLinkageStatistics {
                    TotalChecks = 100,
                    TotalErrors = 15,
                    LastCheckTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsRunning = true
                }
            };
            
            // 创建健康检查
            var healthCheck = new SpeedLinkageHealthCheck(mockService, CreateClock());
            
            // 执行健康检查
            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);
            
            // 验证结果
            MiniAssert.Equal(HealthStatus.Unhealthy, result.Status, "服务应该是不健康的（错误率过高）");
        }

        [MiniFact]
        public async Task HealthCheck_HighIoFailureRate_ReturnsDegraded() {
            // 创建模拟服务并设置高IO失败率 (25% failure rate)
            var mockService = new MockSpeedLinkageService {
                Stats = new SpeedLinkageStatistics {
                    TotalChecks = 100,
                    TotalIoWrites = 100,
                    FailedIoWrites = 25,
                    LastCheckTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsRunning = true
                }
            };
            
            // 创建健康检查
            var healthCheck = new SpeedLinkageHealthCheck(mockService, CreateClock());
            
            // 执行健康检查
            var context = new HealthCheckContext();
            var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);
            
            // 验证结果
            MiniAssert.Equal(HealthStatus.Degraded, result.Status, "服务应该是降级状态（IO失败率较高）");
        }

        [MiniFact]
        public void Statistics_DefaultValues_AreCorrect() {
            // 创建统计信息对象
            var stats = new SpeedLinkageStatistics {
                TotalChecks = 500,
                TotalStateChanges = 25,
                TotalIoWrites = 50,
                FailedIoWrites = 2,
                TotalErrors = 1,
                ActiveGroupsCount = 3
            };
            
            // 验证统计信息
            MiniAssert.Equal(500L, stats.TotalChecks, "总检查次数应该正确");
            MiniAssert.Equal(25L, stats.TotalStateChanges, "总状态变化次数应该正确");
            MiniAssert.Equal(50L, stats.TotalIoWrites, "总IO写入次数应该正确");
            MiniAssert.Equal(2L, stats.FailedIoWrites, "失败的IO写入次数应该正确");
            MiniAssert.Equal(1L, stats.TotalErrors, "总错误次数应该正确");
            MiniAssert.Equal(3, stats.ActiveGroupsCount, "活跃组数量应该正确");
        }

        private static ISystemClock CreateClock() => new SystemClock();
    }

    /// <summary>
    /// 模拟的速度联动服务，用于测试健康检查
    /// </summary>
    internal sealed class MockSpeedLinkageService : ZakYip.Singulation.Infrastructure.Services.ISpeedLinkageService {
        public SpeedLinkageStatistics Stats { get; set; } = new SpeedLinkageStatistics {
            TotalChecks = 0,
            TotalStateChanges = 0,
            TotalIoWrites = 0,
            FailedIoWrites = 0,
            TotalErrors = 0,
            LastCheckTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastErrorTime = DateTime.MinValue,
            LastError = null,
            IsRunning = true,
            ActiveGroupsCount = 0
        };

        public SpeedLinkageStatistics GetStatistics() {
            return Stats;
        }
    }
}
