# SignalR Client Custom Retry Policy

## 概述

为了实现无限重连功能（最大延迟 8 秒），客户端应使用自定义 `IRetryPolicy`。

## 实现代码

在客户端项目中（如 MauiApp），添加以下类：

```csharp
using Microsoft.AspNetCore.SignalR.Client;

namespace YourClientNamespace {
    /// <summary>
    /// 自定义 SignalR 重连策略，支持无限重连，最大延迟 8 秒。
    /// </summary>
    public sealed class UnlimitedRetryPolicy : IRetryPolicy {
        private static readonly TimeSpan[] RetryDelays = {
            TimeSpan.Zero,              // 第 1 次：立即重连
            TimeSpan.FromSeconds(1),    // 第 2 次：1 秒后
            TimeSpan.FromSeconds(2),    // 第 3 次：2 秒后
            TimeSpan.FromSeconds(4),    // 第 4 次：4 秒后
            TimeSpan.FromSeconds(8)     // 第 5 次及以后：8 秒后
        };

        /// <summary>
        /// 获取下一次重连的延迟时间。
        /// </summary>
        /// <param name="retryContext">重连上下文。</param>
        /// <returns>延迟时间，null 表示停止重连。</returns>
        public TimeSpan? NextRetryDelay(RetryContext retryContext) {
            // 无限重连：永远不返回 null
            if (retryContext.PreviousRetryCount < RetryDelays.Length) {
                return RetryDelays[retryContext.PreviousRetryCount];
            }
            // 超过预定义延迟数组后，持续使用最大延迟（8 秒）
            return RetryDelays[^1];
        }
    }
}
```

## 使用方法

在客户端创建 HubConnection 时使用此策略：

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://your-server-url/hubs/events")
    .WithAutomaticReconnect(new UnlimitedRetryPolicy())
    .Build();
```

## 替换现有代码

当前 `SignalRClientFactory.cs` 中的重连策略：

```csharp
// 旧代码：只尝试 3 次重连
.WithAutomaticReconnect(new[] { 
    TimeSpan.Zero, 
    TimeSpan.FromSeconds(2), 
    TimeSpan.FromSeconds(10)
})
```

应替换为：

```csharp
// 新代码：无限重连
.WithAutomaticReconnect(new UnlimitedRetryPolicy())
```

## 优点

1. **无限重连**：即使网络长时间中断，客户端也会持续尝试重连
2. **指数退避**：避免过于频繁的重连尝试，降低服务器压力
3. **最大延迟控制**：最长等待时间为 8 秒，保证及时响应网络恢复
4. **用户体验**：自动处理网络波动，无需手动重连

## 注意事项

- 客户端需要引用 `Microsoft.AspNetCore.SignalR.Client` NuGet 包
- 此策略仅在网络连接丢失时触发，不处理服务器主动关闭连接的情况
- 建议配合连接状态监听（Reconnecting、Reconnected、Closed 事件）提供用户反馈
