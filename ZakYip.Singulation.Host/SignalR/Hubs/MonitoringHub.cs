using Microsoft.AspNetCore.SignalR;

namespace ZakYip.Singulation.Host.SignalR.Hubs {
    /// <summary>
    /// 监控数据 SignalR Hub，用于推送实时监控数据到客户端
    /// </summary>
    public sealed class MonitoringHub : Hub {
        /// <summary>
        /// 订阅轴实时数据
        /// </summary>
        /// <param name="axisId">轴标识符，为空则订阅所有轴</param>
        /// <returns>异步任务</returns>
        public Task SubscribeAxisData(string? axisId = null) {
            var groupName = string.IsNullOrEmpty(axisId) ? "AllAxes" : $"Axis_{axisId}";
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// 取消订阅轴实时数据
        /// </summary>
        /// <param name="axisId">轴标识符</param>
        /// <returns>异步任务</returns>
        public Task UnsubscribeAxisData(string? axisId = null) {
            var groupName = string.IsNullOrEmpty(axisId) ? "AllAxes" : $"Axis_{axisId}";
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// 订阅系统健康度数据
        /// </summary>
        /// <returns>异步任务</returns>
        public Task SubscribeHealthData()
            => Groups.AddToGroupAsync(Context.ConnectionId, "SystemHealth");

        /// <summary>
        /// 取消订阅系统健康度数据
        /// </summary>
        /// <returns>异步任务</returns>
        public Task UnsubscribeHealthData()
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, "SystemHealth");

        /// <summary>
        /// 订阅 IO 状态变化
        /// </summary>
        /// <returns>异步任务</returns>
        public Task SubscribeIoStatus()
            => Groups.AddToGroupAsync(Context.ConnectionId, "IoStatus");

        /// <summary>
        /// 取消订阅 IO 状态变化
        /// </summary>
        /// <returns>异步任务</returns>
        public Task UnsubscribeIoStatus()
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, "IoStatus");

        /// <summary>
        /// 心跳检测
        /// </summary>
        /// <returns>异步任务</returns>
        public Task Ping() => Task.CompletedTask;
    }
}
