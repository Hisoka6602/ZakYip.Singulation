# SignalR 实现分析与优化建议

## 执行摘要

本文档分析了 ZakYip.Singulation 项目中 SignalR 的当前实现状态，识别了已实现的功能特性，并提供了详细的优化建议。

**生成日期**: 2025-10-30

---

## 1. 当前实现概述

### 1.1 已实现的核心组件

#### 1.1.1 服务端组件 (Host 层)

1. **EventsHub** (`ZakYip.Singulation.Host/SignalR/Hubs/EventsHub.cs`)
   - 基础 SignalR Hub 实现
   - 提供 `Join(channel)` 和 `Leave(channel)` 方法，支持频道组管理
   - 实现简洁，符合 SignalR 最佳实践

2. **SignalRRealtimeNotifier** (`ZakYip.Singulation.Host/SignalR/SignalRRealtimeNotifier.cs`)
   - 实现 `IRealtimeNotifier` 接口
   - **非阻塞设计**：使用 `Channel<SignalRQueueItem>` 进行异步消息队列
   - `TryWrite` 策略：写入失败不抛异常，避免阻塞业务逻辑

3. **RealtimeDispatchService** (`ZakYip.Singulation.Host/SignalR/RealtimeDispatchService.cs`)
   - 后台服务 (BackgroundService)，负责从 Channel 读取消息并广播
   - **消息封装**：自动添加版本号、类型名、时间戳、traceId 和序列号
   - **序列号管理**：为每个频道维护独立的序列号，便于客户端检测消息丢失
   - **异常处理**：广播失败不影响其他消息，仅记录警告日志

4. **SignalRQueueItem** (`ZakYip.Singulation.Host/SignalR/SignalRQueueItem.cs`)
   - 简洁的消息队列项数据结构
   - 使用 `record class` 实现不可变性

5. **SignalRSetup** (`ZakYip.Singulation.Host/Extensions/SignalRSetup.cs`)
   - SignalR 配置扩展
   - **配置参数**：
     - HandshakeTimeout: 1 分钟
     - EnableDetailedErrors: true（便于调试）
     - MaximumReceiveMessageSize: null（无限制）
     - KeepAliveInterval: 1 分钟
     - ClientTimeoutInterval: 5 分钟
     - MaximumParallelInvocationsPerClient: 10
     - StreamBufferCapacity: int.MaxValue
   - **消息队列配置**：
     - 有界通道 (BoundedChannel)，容量 10,000
     - FullMode: DropOldest（丢旧保新策略）
     - SingleReader: true（优化性能）
     - SingleWriter: false（支持多个发布者）
   - 使用 Newtonsoft.Json 协议，与全局 JSON 配置一致

#### 1.1.2 客户端组件 (MauiApp 层)

1. **SignalRClientFactory** (`ZakYip.Singulation.MauiApp/Services/SignalRClientFactory.cs`)
   - 完整的客户端工厂实现
   - **自动重连**：使用指数退避策略 (0s, 2s, 10s)
   - **事件订阅**：
     - MessageReceived
     - SpeedChanged
     - SafetyEventOccurred
     - ConnectionStateChanged
     - LatencyUpdated
   - **延迟监测**：5 秒定时器，通过 Ping 检测连接延迟
   - **连接管理**：完整的连接生命周期管理，包括断开和资源释放

#### 1.1.3 核心抽象层 (Core 层)

1. **IRealtimeNotifier** (`ZakYip.Singulation.Core/Abstractions/Realtime/IRealtimeNotifier.cs`)
   - 跨层实时通知抽象接口
   - **便利方法**：
     - `PublishAsync(channel, payload)` - 通用广播
     - `PublishAsync(payload)` - 系统级广播 (/sys)
     - `PublishDeviceAsync(payload)` - 设备广播 (/device)
     - `PublishVisionAsync(payload)` - 视觉广播 (/vision)
     - `PublishErrorAsync(payload)` - 错误广播 (/errors)

### 1.2 实际使用场景

SignalR 在以下模块中被广泛使用：

1. **IoStatusWorker** - IO 状态监控，定期广播 IO 状态
2. **LogEventPump** - 日志事件泵，聚合并广播日志事件
3. **TransportEventPump** - 传输事件泵，广播视觉和设备事件
4. **SpeedFrameWorker** - 速度帧工作器，广播速度变化
5. **SafetyPipeline** - 安全管线，广播安全事件
6. **SafetyIsolator** - 安全隔离器，广播隔离事件

### 1.3 架构优势

1. **解耦设计**：业务层通过 `IRealtimeNotifier` 接口与 SignalR 解耦
2. **非阻塞**：使用 Channel 实现生产者-消费者模式，避免阻塞业务逻辑
3. **背压处理**：有界通道 + DropOldest 策略，防止消息堆积
4. **可测试性**：接口抽象便于单元测试（见 ConsoleRealtimeNotifier）
5. **消息完整性**：序列号机制帮助检测消息丢失

---

## 2. 识别的问题

### 2.1 性能问题

#### 2.1.1 EventsHub 缺少 Ping 方法 ⚠️ **高优先级**

**问题描述**：
客户端 `SignalRClientFactory` 尝试调用 `_hubConnection.InvokeAsync("Ping")`，但 `EventsHub` 未实现该方法。

```csharp
// 客户端代码 (SignalRClientFactory.cs:142)
await _hubConnection.InvokeAsync("Ping");
```

**影响**：
- 延迟监测功能无法正常工作
- 每 5 秒会产生一次异常
- 客户端无法准确测量网络延迟

**推荐修复**：
在 EventsHub 中添加 Ping 方法：

```csharp
public Task Ping() => Task.CompletedTask;
```

#### 2.1.2 消息序列化可能产生大对象 ⚠️ **中优先级**

**问题描述**：
`RealtimeDispatchService` 在每次广播时都创建匿名对象：

```csharp
var envelope = new {
    v = 1,
    type = item.Payload.GetType().Name,
    ts = DateTimeOffset.Now,
    channel = item.Channel,
    data = item.Payload,
    traceId = Activity.Current?.Id,
    seq
};
```

**影响**：
- 高频消息场景下可能导致 GC 压力
- 每个消息都会创建新对象

**推荐优化**：
1. 考虑使用 `struct` 或对象池
2. 评估是否需要在每条消息中包含 `type` 和 `traceId`（可选字段）

#### 2.1.3 Channel 容量可能过小 ⚠️ **中优先级**

**当前配置**：10,000 条消息

**问题场景**：
- 多个 Worker 同时高频发送消息（如 IoStatusWorker 100ms 一次，LogEventPump 100ms 一次）
- 网络抖动或客户端响应慢导致消息堆积
- DropOldest 策略会丢失重要消息

**推荐**：
1. 增加 Channel 容量到 50,000 或更高
2. 添加监控，记录消息丢弃次数
3. 考虑按优先级分离不同的 Channel

### 2.2 可靠性问题

#### 2.2.1 缺少健康检查 ⚠️ **高优先级**

**问题描述**：
当前没有监控 SignalR 服务的健康状态。

**推荐**：
1. 添加 SignalR 健康检查
2. 监控活跃连接数
3. 监控消息队列深度
4. 监控消息丢弃率

#### 2.2.2 异常处理不够细致 ⚠️ **中优先级**

**问题描述**：
`RealtimeDispatchService.ExecuteAsync` 中的异常处理使用通用 catch：

```csharp
catch (Exception ex) {
    _logger.LogWarning(ex, "SignalR broadcast failed for {Channel}", item.Channel);
}
```

**影响**：
- 无法区分不同类型的错误
- 可能掩盖严重问题（如内存不足、连接池耗尽）

**推荐**：
1. 区分网络错误、序列化错误、Hub 错误
2. 对于严重错误应该升级为 Error 级别
3. 考虑断路器模式，避免持续失败

#### 2.2.3 客户端重连策略有限 ⚠️ **低优先级**

**当前策略**：
```csharp
.WithAutomaticReconnect(new[] { 
    TimeSpan.Zero, 
    TimeSpan.FromSeconds(2), 
    TimeSpan.FromSeconds(10)
})
```

**问题**：
- 只尝试 3 次重连
- 10 秒后不再重连

**推荐**：
1. 实现自定义 `IRetryPolicy`，支持无限重连
2. 使用指数退避，最大间隔 30-60 秒
3. 添加手动重连接口

### 2.3 安全性问题

#### 2.3.1 缺少身份验证和授权 ⚠️ **高优先级**

**问题描述**：
当前 EventsHub 和 SignalR 连接没有任何身份验证。

**风险**：
- 任何人都可以连接到 Hub
- 任何人都可以加入任意频道
- 潜在的信息泄露风险

**推荐**：
1. 添加 JWT 或 API Key 认证
2. 在 `Join/Leave` 方法中验证用户权限
3. 实现频道级别的访问控制

#### 2.3.2 缺少消息大小限制 ⚠️ **中优先级**

**问题描述**：
`MaximumReceiveMessageSize: null` - 无限制

**风险**：
- 恶意客户端可以发送超大消息，导致 DoS
- 内存溢出风险

**推荐**：
设置合理的消息大小限制，如 1MB：

```csharp
opt.MaximumReceiveMessageSize = 1024 * 1024;
```

#### 2.3.3 缺少速率限制 ⚠️ **中优先级**

**问题描述**：
客户端可以无限频率地调用 Hub 方法。

**推荐**：
1. 添加客户端级别的速率限制
2. 使用 ASP.NET Core Rate Limiting 中间件
3. 防止单个客户端占用所有资源

### 2.4 可观测性问题

#### 2.4.1 缺少详细的性能指标 ⚠️ **中优先级**

**当前状态**：
- 只有消息序列号
- 没有性能监控

**推荐添加**：
1. 消息发送速率（每秒消息数）
2. 消息延迟（从入队到发送的时间）
3. Channel 队列深度
4. 活跃连接数
5. 每个频道的订阅者数量
6. 消息丢弃率

#### 2.4.2 日志级别不一致 ⚠️ **低优先级**

**问题**：
广播失败使用 `LogWarning`，但某些场景可能需要 `LogError`。

**推荐**：
1. 建立日志级别标准
2. 严重错误使用 Error
3. 临时性错误使用 Warning

### 2.5 功能性问题

#### 2.5.1 缺少客户端到服务端的消息发送 ⚠️ **低优先级**

**当前状态**：
- 只支持服务端到客户端的单向推送
- 客户端只能通过 REST API 发送命令

**推荐**：
1. 添加客户端到服务端的消息通道
2. 支持客户端通过 SignalR 发送命令（如果业务需要）
3. 实现请求-响应模式（如果需要同步返回）

#### 2.5.2 缺少消息持久化 ⚠️ **低优先级**

**问题**：
- 客户端离线期间的消息会丢失
- 没有消息历史记录

**推荐**（如果业务需要）：
1. 实现消息持久化到数据库
2. 客户端重连后可以获取离线期间的消息
3. 添加消息 TTL 机制

#### 2.5.3 缺少消息压缩 ⚠️ **低优先级**

**问题**：
高频消息场景下带宽消耗大。

**推荐**：
启用 SignalR 消息压缩：

```csharp
services.AddSignalR(opt => {
    // 现有配置...
})
.AddMessagePackProtocol() // 更高效的二进制协议
```

或启用 Response Compression：

```csharp
services.AddResponseCompression(opts => {
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
```

---

## 3. 优化建议优先级排序

### 3.1 高优先级（必须修复）

1. ✅ **添加 EventsHub.Ping() 方法** - 修复客户端延迟检测
2. ✅ **实现身份验证和授权** - 提升安全性
3. ✅ **添加健康检查** - 提升可观测性

### 3.2 中优先级（强烈推荐）

4. ✅ **优化消息对象创建** - 减少 GC 压力
5. ✅ **增加 Channel 容量和监控** - 防止消息丢失
6. ✅ **细化异常处理** - 更好的错误诊断
7. ✅ **设置消息大小限制** - 防止 DoS
8. ✅ **添加速率限制** - 防止滥用
9. ✅ **添加性能指标** - 监控系统健康

### 3.3 低优先级（可选优化）

10. ✅ **改进客户端重连策略** - 更好的用户体验
11. ✅ **标准化日志级别** - 一致性
12. ✅ **添加双向通信** - 功能增强（按需）
13. ✅ **消息持久化** - 功能增强（按需）
14. ✅ **消息压缩** - 性能优化（按需）

---

## 4. 具体优化实施方案

### 4.1 高优先级修复

#### 4.1.1 添加 Ping 方法

**文件**: `ZakYip.Singulation.Host/SignalR/Hubs/EventsHub.cs`

```csharp
/// <summary>
/// 心跳检测方法，用于测量客户端延迟。
/// </summary>
/// <returns>异步任务。</returns>
public Task Ping() => Task.CompletedTask;
```

**影响**: 最小化，无副作用

#### 4.1.2 实现身份验证

**文件**: `ZakYip.Singulation.Host/Extensions/SignalRSetup.cs`

```csharp
services.AddSignalR(opt => {
    // 现有配置...
    opt.EnableDetailedErrors = false; // 生产环境关闭详细错误
})
.AddNewtonsoftJsonProtocol();

// 添加 JWT 认证
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Events = new JwtBearerEvents {
            OnMessageReceived = context => {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && 
                    path.StartsWithSegments("/hubs")) {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

**文件**: `ZakYip.Singulation.Host/SignalR/Hubs/EventsHub.cs`

```csharp
[Authorize] // 添加授权特性
public sealed class EventsHub : Hub {
    // 现有代码...
    
    public Task Join(string channel) {
        // 添加频道权限验证
        if (!IsAuthorizedForChannel(channel)) {
            throw new HubException("Unauthorized access to channel");
        }
        return Groups.AddToGroupAsync(Context.ConnectionId, channel);
    }
    
    private bool IsAuthorizedForChannel(string channel) {
        // 实现频道级别权限检查
        return true; // 示例：根据用户角色判断
    }
}
```

#### 4.1.3 添加健康检查

**文件**: `ZakYip.Singulation.Host/SignalR/SignalRHealthCheck.cs` (新建)

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ZakYip.Singulation.Host.SignalR {
    public class SignalRHealthCheck : IHealthCheck {
        private readonly IHubContext<EventsHub> _hubContext;
        private readonly Channel<SignalRQueueItem> _channel;
        
        public SignalRHealthCheck(
            IHubContext<EventsHub> hubContext,
            Channel<SignalRQueueItem> channel) {
            _hubContext = hubContext;
            _channel = channel;
        }
        
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default) {
            
            var queueCount = _channel.Reader.Count;
            
            if (queueCount > 8000) {
                return Task.FromResult(
                    HealthCheckResult.Degraded(
                        $"SignalR queue nearly full: {queueCount}/10000"));
            }
            
            return Task.FromResult(
                HealthCheckResult.Healthy(
                    $"SignalR operational, queue: {queueCount}/10000"));
        }
    }
}
```

**注册健康检查**：

```csharp
services.AddHealthChecks()
    .AddCheck<SignalRHealthCheck>("signalr");
```

### 4.2 中优先级优化

#### 4.2.1 消息对象池化

**文件**: `ZakYip.Singulation.Host/SignalR/MessageEnvelope.cs` (新建)

```csharp
namespace ZakYip.Singulation.Host.SignalR {
    public sealed class MessageEnvelope {
        public int Version { get; set; } = 1;
        public string Type { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string Channel { get; set; } = string.Empty;
        public object Data { get; set; } = default!;
        public string? TraceId { get; set; }
        public long Sequence { get; set; }
        
        public void Reset() {
            Version = 1;
            Type = string.Empty;
            Timestamp = default;
            Channel = string.Empty;
            Data = default!;
            TraceId = null;
            Sequence = 0;
        }
    }
}
```

**文件**: `ZakYip.Singulation.Host/SignalR/RealtimeDispatchService.cs`

```csharp
private readonly ObjectPool<MessageEnvelope> _envelopePool;

public RealtimeDispatchService(...) {
    // ...
    _envelopePool = new DefaultObjectPool<MessageEnvelope>(
        new DefaultPooledObjectPolicy<MessageEnvelope>());
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    await foreach (var item in _chan.Reader.ReadAllAsync(stoppingToken)) {
        try {
            var seq = _seq.AddOrUpdate(item.Channel, 1, (_, old) => old + 1);
            
            var envelope = _envelopePool.Get();
            try {
                envelope.Type = item.Payload.GetType().Name;
                envelope.Timestamp = DateTimeOffset.Now;
                envelope.Channel = item.Channel;
                envelope.Data = item.Payload;
                envelope.TraceId = Activity.Current?.Id;
                envelope.Sequence = seq;
                
                await _hub.Clients.Group(item.Channel)
                    .SendAsync("event", envelope, stoppingToken);
            }
            finally {
                envelope.Reset();
                _envelopePool.Return(envelope);
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "SignalR broadcast failed for {Channel}", 
                item.Channel);
        }
    }
}
```

#### 4.2.2 增加 Channel 容量和监控

**文件**: `ZakYip.Singulation.Host/Extensions/SignalRSetup.cs`

```csharp
var chan = Channel.CreateBounded<SignalRQueueItem>(
    new BoundedChannelOptions(50_000) { // 增加容量
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
```

**添加监控**：

```csharp
public sealed class RealtimeDispatchService : BackgroundService {
    private long _messagesProcessed = 0;
    private long _messagesDropped = 0;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // 启动监控任务
        _ = Task.Run(() => MonitorQueueDepth(stoppingToken), stoppingToken);
        
        await foreach (var item in _chan.Reader.ReadAllAsync(stoppingToken)) {
            _messagesProcessed++;
            // ... 现有代码
        }
    }
    
    private async Task MonitorQueueDepth(CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            var count = _chan.Reader.Count;
            if (count > 40000) {
                _logger.LogWarning(
                    "SignalR queue depth high: {Count}/50000, " +
                    "Processed: {Processed}, Dropped: {Dropped}",
                    count, _messagesProcessed, _messagesDropped);
            }
            await Task.Delay(10000, ct); // 每 10 秒检查一次
        }
    }
}
```

#### 4.2.3 细化异常处理

**文件**: `ZakYip.Singulation.Host/SignalR/RealtimeDispatchService.cs`

```csharp
catch (HubException hex) {
    // Hub 级别错误
    _logger.LogError(hex, "Hub error broadcasting to {Channel}", 
        item.Channel);
}
catch (JsonException jex) {
    // 序列化错误
    _logger.LogError(jex, "Serialization error for {Channel}, Type: {Type}",
        item.Channel, item.Payload.GetType().Name);
}
catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
    // 正常关闭
    break;
}
catch (Exception ex) {
    // 其他未知错误
    _logger.LogWarning(ex, "Unexpected error broadcasting to {Channel}", 
        item.Channel);
}
```

#### 4.2.4 添加性能指标

使用 `System.Diagnostics.Metrics`：

```csharp
public sealed class RealtimeDispatchService : BackgroundService {
    private static readonly Meter _meter = new("ZakYip.Singulation.SignalR");
    private static readonly Counter<long> _messagesProcessed = 
        _meter.CreateCounter<long>("signalr.messages.processed");
    private static readonly Counter<long> _messagesDropped = 
        _meter.CreateCounter<long>("signalr.messages.dropped");
    private static readonly Histogram<double> _messageLatency = 
        _meter.CreateHistogram<double>("signalr.message.latency.ms");
    private static readonly ObservableGauge<int> _queueDepth;
    
    static RealtimeDispatchService() {
        _queueDepth = _meter.CreateObservableGauge("signalr.queue.depth", 
            () => /* return current queue depth */);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await foreach (var item in _chan.Reader.ReadAllAsync(stoppingToken)) {
            var startTime = DateTimeOffset.UtcNow;
            try {
                // ... 现有代码
                _messagesProcessed.Add(1, new KeyValuePair<string, object?>(
                    "channel", item.Channel));
                
                var latency = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _messageLatency.Record(latency);
            }
            catch { /* ... */ }
        }
    }
}
```

### 4.3 低优先级优化

#### 4.3.1 改进重连策略

**文件**: `ZakYip.Singulation.MauiApp/Services/SignalRClientFactory.cs`

```csharp
public class CustomRetryPolicy : IRetryPolicy {
    private static readonly TimeSpan[] _delays = {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60)
    };
    
    public TimeSpan? NextRetryDelay(RetryContext retryContext) {
        if (retryContext.PreviousRetryCount < _delays.Length) {
            return _delays[retryContext.PreviousRetryCount];
        }
        return TimeSpan.FromSeconds(60); // 持续以 60 秒间隔重连
    }
}

// 使用
_hubConnection = new HubConnectionBuilder()
    .WithUrl($"{_baseUrl}{hubPath}")
    .WithAutomaticReconnect(new CustomRetryPolicy())
    .Build();
```

#### 4.3.2 启用消息压缩

**选项 1: MessagePack 协议**

```csharp
services.AddSignalR(opt => {
    // 现有配置...
})
.AddMessagePackProtocol();
```

**选项 2: Response Compression**

```csharp
services.AddResponseCompression(opts => {
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
});

app.UseResponseCompression();
```

---

## 5. 测试建议

### 5.1 单元测试

1. **SignalRRealtimeNotifier 测试**
   - 验证 TryWrite 行为
   - 验证非阻塞特性

2. **RealtimeDispatchService 测试**
   - 验证消息封装格式
   - 验证序列号递增
   - 验证异常处理

3. **EventsHub 测试**
   - 验证 Join/Leave 功能
   - 验证 Ping 响应

### 5.2 集成测试

1. **端到端消息传递测试**
   - 发布消息到频道
   - 验证客户端接收

2. **重连测试**
   - 模拟服务端重启
   - 验证客户端自动重连

3. **负载测试**
   - 高频消息场景
   - 多客户端连接场景
   - 验证 Channel 背压处理

### 5.3 性能测试

1. **吞吐量测试**
   - 测量每秒可处理的消息数
   - 找出瓶颈

2. **延迟测试**
   - 测量从发布到接收的端到端延迟
   - 不同负载下的延迟分布

3. **资源使用测试**
   - CPU 使用率
   - 内存使用和 GC 压力
   - 网络带宽消耗

---

## 6. 总结

### 6.1 当前实现的优点

1. ✅ **架构清晰**：分层设计，职责明确
2. ✅ **非阻塞设计**：使用 Channel 避免阻塞业务逻辑
3. ✅ **背压处理**：DropOldest 策略防止消息堆积
4. ✅ **可测试性**：接口抽象便于测试
5. ✅ **消息完整性**：序列号机制
6. ✅ **自动重连**：客户端支持自动重连
7. ✅ **延迟监测**：客户端支持 Ping（需要服务端配合）

### 6.2 需要改进的地方

1. ⚠️ **安全性不足**：缺少身份验证和授权
2. ⚠️ **监控缺失**：缺少健康检查和性能指标
3. ⚠️ **Ping 方法缺失**：影响客户端延迟监测
4. ⚠️ **异常处理粗糙**：无法区分错误类型
5. ⚠️ **性能优化空间**：消息对象创建、Channel 容量

### 6.3 实施路线图

**阶段 1: 紧急修复（1-2 天）**
- 添加 EventsHub.Ping() 方法
- 设置消息大小限制
- 添加基本健康检查

**阶段 2: 安全性提升（3-5 天）**
- 实现 JWT 认证
- 添加频道级别授权
- 添加速率限制

**阶段 3: 可观测性增强（5-7 天）**
- 添加详细性能指标
- 改进日志记录
- 添加监控仪表盘

**阶段 4: 性能优化（7-10 天）**
- 消息对象池化
- 增加 Channel 容量
- 细化异常处理
- 性能测试和调优

**阶段 5: 功能增强（可选）**
- 改进重连策略
- 消息压缩
- 消息持久化（按需）
- 双向通信（按需）

---

## 7. 参考资料

1. [ASP.NET Core SignalR 官方文档](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
2. [SignalR 性能最佳实践](https://learn.microsoft.com/en-us/aspnet/core/signalr/performance)
3. [SignalR 安全性指南](https://learn.microsoft.com/en-us/aspnet/core/signalr/security)
4. [System.Threading.Channels](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels)
5. [对象池化最佳实践](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.objectpool)

---

**文档版本**: 1.0  
**最后更新**: 2025-10-30  
**作者**: GitHub Copilot Agent  
**审核状态**: 待审核
