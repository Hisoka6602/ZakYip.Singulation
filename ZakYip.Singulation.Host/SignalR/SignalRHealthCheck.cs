using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using ZakYip.Singulation.Host.SignalR.Hubs;

namespace ZakYip.Singulation.Host.SignalR {
    /// <summary>
    /// SignalR 健康检查，监控消息队列深度和活跃连接状态。
    /// </summary>
    public sealed class SignalRHealthCheck : IHealthCheck {
        private readonly IHubContext<EventsHub> _hubContext;
        private readonly Channel<SignalRQueueItem> _channel;
        private readonly RealtimeDispatchService _dispatchService;

        /// <summary>
        /// 初始化 <see cref="SignalRHealthCheck"/> 类的新实例。
        /// </summary>
        public SignalRHealthCheck(
            IHubContext<EventsHub> hubContext,
            Channel<SignalRQueueItem> channel,
            RealtimeDispatchService dispatchService) {
            _hubContext = hubContext;
            _channel = channel;
            _dispatchService = dispatchService;
        }

        /// <summary>
        /// 执行健康检查。
        /// </summary>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) {

            try {
                var queueCount = _channel.Reader.Count;
                var stats = _dispatchService.GetStatistics();

                var data = new Dictionary<string, object> {
                    ["queueDepth"] = queueCount,
                    ["queueCapacity"] = 50000,
                    ["messagesProcessed"] = stats.Processed,
                    ["messagesFailed"] = stats.Failed,
                    ["messagesDropped"] = stats.Dropped
                };

                // 队列接近满时标记为降级
                if (queueCount > 40000) {
                    return Task.FromResult(
                        HealthCheckResult.Degraded(
                            $"SignalR queue nearly full: {queueCount}/50000",
                            data: data));
                }

                // 失败率过高时标记为不健康
                if (stats.Processed > 0 && stats.Failed > stats.Processed * 0.1) {
                    return Task.FromResult(
                        HealthCheckResult.Unhealthy(
                            $"SignalR high failure rate: {stats.Failed}/{stats.Processed}",
                            data: data));
                }

                return Task.FromResult(
                    HealthCheckResult.Healthy(
                        $"SignalR operational, queue: {queueCount}/50000",
                        data: data));
            }
            catch (Exception ex) {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        "SignalR health check failed",
                        ex));
            }
        }
    }
}
