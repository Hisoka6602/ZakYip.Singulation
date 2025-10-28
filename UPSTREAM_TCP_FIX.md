# 上游 TCP 连接问题修复说明

## 问题描述

系统无法连接上游 TCP 服务器。经过分析，发现根本原因是 `UpstreamTcpInjection.AddUpstreamTcpFromLiteDb()` 方法在服务注册阶段使用了反模式。

## 问题根源

在 `ZakYip.Singulation.Infrastructure/Transport/UpstreamTcpInjection.cs` 文件中，原始代码在服务注册期间构建了一个临时的 `ServiceProvider`：

```csharp
// 问题代码（已修复）
using var temp = services.BuildServiceProvider();
var store = temp.GetRequiredService<IUpstreamOptionsStore>();
var dto = store.GetAsync().GetAwaiter().GetResult();

// 然后只在端口 > 0 时注册传输
if (dto.SpeedPort > 0) {
    services.AddKeyedSingleton<IByteTransport>("speed", ...);
}
```

这种做法存在以下问题：

1. **反模式**：在服务注册期间调用 `BuildServiceProvider()` 是 ASP.NET Core 的反模式，可能导致：
   - 创建的服务实例与运行时实例不一致
   - 依赖项可能未完全注册
   - 潜在的内存泄漏和资源浪费

2. **依赖顺序问题**：`LiteDbUpstreamOptionsStore` 依赖于 `ILiteDatabase` 和 `ISafetyIsolator`，如果这些服务在调用时还未注册，会导致异常。

3. **配置灵活性受限**：如果数据库中的端口配置为 0 或负数，传输服务根本不会被注册，导致系统无法启动这些传输。

## 解决方案

### 修复内容

1. **移除临时 ServiceProvider 构建**：不再在注册阶段读取配置
2. **无条件注册所有传输**：始终注册 speed、position、heartbeat 三个传输服务
3. **延迟配置读取**：在服务解析时（而非注册时）读取配置
4. **修复 nullability 警告**：将 `object key` 改为 `object? key`

### 修复后的代码

```csharp
public static IServiceCollection AddUpstreamTcpFromLiteDb(this IServiceCollection services) {
    // 注意：确保在此之前已经调用了 AddUpstreamFromLiteDb(...) 注册 IUpstreamOptionsStore

    // ---- speed ----
    services.AddKeyedSingleton<IByteTransport>("speed", (IServiceProvider sp, object? key) => {
        var cur = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult();
        return cur.Role == TransportRole.Server
            ? new TouchServerByteTransport(new TcpServerOptions {
                Address = IPAddress.Any,
                Port = cur.SpeedPort,
            })
            : new TouchClientByteTransport(new TcpClientOptions {
                Host = cur.Host,
                Port = cur.SpeedPort
            });
    });

    // ---- position 和 heartbeat 类似 ----
    // ...

    return services;
}
```

## 工作原理

### 修复前的流程

1. 在 `Program.cs` 中调用 `AddUpstreamTcpFromLiteDb()`
2. 方法内部构建临时 `ServiceProvider` ⚠️
3. 从临时 provider 获取配置
4. 仅当端口 > 0 时注册传输服务
5. 传输可能根本不被注册

### 修复后的流程

1. 在 `Program.cs` 中调用 `AddUpstreamTcpFromLiteDb()`
2. **始终**注册所有三个传输服务（speed/position/heartbeat）
3. 传输服务使用工厂模式 - 仅在首次解析时创建实例
4. 工厂在解析时从正确的 `ServiceProvider` 获取配置
5. 根据配置创建相应的 Client 或 Server 传输

## 传输启动流程

传输服务注册后，由 `TransportEventPump` 后台服务启动：

```csharp
// TransportEventPump.cs - ExecuteAsync 方法
foreach (var (name, t) in _transports) {
    try {
        await t.StartAsync(stoppingToken).ConfigureAwait(false);
        _log.LogInformation("[transport:{Name}] started, status={Status}", name, t.Status);
    }
    catch (Exception ex) {
        _log.LogError(ex, "[transport:{Name}] start failed", name);
    }
}
```

### TCP 客户端连接机制

`TouchClientByteTransport` 使用 Polly 重试策略实现自动重连：

- 连接超时：1000ms (1秒)
- 重试策略：无限重试，指数退避 + 抖动
- 最大延迟：10秒
- 自动重连：连接断开后自动重新连接

### TCP 服务器监听机制

`TouchServerByteTransport` 监听指定端口等待客户端连接：

- 监听地址：`IPAddress.Any`（所有网络接口）
- 端口：从配置读取（默认 5001/5002/5003）
- 多客户端支持：可接受多个客户端连接

## 配置说明

### 默认配置

在 `UpstreamOptions.cs` 中定义：

```csharp
public sealed record class UpstreamOptions {
    public string Host { get; init; } = "127.0.0.1";
    public int SpeedPort { get; init; } = 5001;
    public int PositionPort { get; init; } = 5002;
    public int HeartbeatPort { get; init; } = 5003;
    public bool ValidateCrc { get; init; } = true;
    public TransportRole Role { get; init; } = TransportRole.Client;
}
```

### 配置存储

配置存储在 LiteDB 数据库中：

- 数据库文件：`data/singulation.db`
- 集合名称：`upstream_options`
- 单文档模式：使用固定 ID `"upstream_options_singleton"`

### 修改配置

可以通过以下方式修改配置：

1. **通过 API**：使用 Web API 端点（如果有提供）
2. **通过代码**：注入 `IUpstreamOptionsStore` 并调用 `SaveAsync()`
3. **通过配置文件**：在 `appsettings.json` 中添加 `Upstream` 节点

## 测试验证

创建了 `UpstreamTcpInjectionTests.cs` 测试文件，包含三个测试用例：

1. **AllThreeTransportsAreRegistered**：验证所有三个传输都被正确注册
2. **TransportUsesCorrectConfiguration**：验证传输使用正确的配置
3. **ServerModeTransportsAreCreated**：验证服务器模式传输被正确创建

## 日志排查

如果仍然无法连接，可以通过以下日志进行排查：

1. **传输启动日志**：
   ```
   [transport:speed] started, status=Running
   [transport:position] started, status=Running
   [transport:heartbeat] started, status=Running
   ```

2. **连接状态变化日志**：
   ```
   [transport:speed] state=Connecting
   [transport:speed] state=Connected
   [transport:speed] state=Retrying (attempt=1, delay=500ms)
   ```

3. **错误日志**：
   ```
   [transport:speed] error: Connection refused
   [transport:speed] connect/retry failed: ...
   ```

## 注意事项

1. **确保上游服务器可达**：如果配置为 Client 模式，需要确保上游服务器（如视觉系统）正在运行并监听指定端口

2. **防火墙设置**：检查防火墙是否允许相应端口的 TCP 连接

3. **网络配置**：
   - Client 模式：确保 `Host` 配置正确（默认 127.0.0.1）
   - Server 模式：确保端口未被占用

4. **角色选择**：
   - `TransportRole.Client`：本地主动连接上游服务器
   - `TransportRole.Server`：本地监听，等待上游连接

## 总结

通过移除服务注册期间的 `BuildServiceProvider()` 调用，并始终注册所有传输服务，解决了上游 TCP 连接问题。修复后的代码：

- ✅ 遵循 ASP.NET Core 最佳实践
- ✅ 避免了依赖顺序问题
- ✅ 提高了配置灵活性
- ✅ 简化了代码逻辑
- ✅ 消除了编译器警告

传输服务现在可以正确注册并由 `TransportEventPump` 启动，实现与上游系统的 TCP 连接。
