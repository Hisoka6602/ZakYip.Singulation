# 上游 TCP 连接问题修复总结 / Upstream TCP Connection Fix Summary

## 中文版本

### 问题描述
系统无法连接上游 TCP 服务器（视觉系统）。

### 根本原因
在 `ZakYip.Singulation.Infrastructure/Transport/UpstreamTcpInjection.cs` 文件中，`AddUpstreamTcpFromLiteDb()` 方法在服务注册期间错误地调用了 `BuildServiceProvider()`，这是一个反模式，导致：

1. 依赖注入容器状态不一致
2. 可能的内存泄漏
3. 传输服务可能根本不被注册

### 修复方案
移除了临时 `ServiceProvider` 的构建，并改为在服务解析时读取配置：

**修复前的代码：**
```csharp
using var temp = services.BuildServiceProvider();  // ❌ 反模式
var store = temp.GetRequiredService<IUpstreamOptionsStore>();
var dto = store.GetAsync().GetAwaiter().GetResult();

if (dto.SpeedPort > 0) {  // ❌ 条件注册
    services.AddKeyedSingleton<IByteTransport>("speed", ...);
}
```

**修复后的代码：**
```csharp
// ✅ 始终注册，在解析时读取配置
services.AddKeyedSingleton<IByteTransport>("speed", (IServiceProvider sp, object? key) => {
    var cur = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult();
    return cur.Role == TransportRole.Server
        ? new TouchServerByteTransport(...)
        : new TouchClientByteTransport(...);
});
```

### 修复内容
1. ✅ 移除了 `BuildServiceProvider()` 反模式
2. ✅ 移除了端口 > 0 的条件检查
3. ✅ 始终注册所有三个传输通道（speed/position/heartbeat）
4. ✅ 修复了可空性警告（`object key` → `object? key`）
5. ✅ 添加了完整的测试用例
6. ✅ 创建了详细的文档说明

### 如何验证修复
1. 启动应用程序
2. 检查日志中的传输启动信息：
   ```
   [transport:speed] started, status=Running
   [transport:position] started, status=Running
   [transport:heartbeat] started, status=Running
   ```
3. 如果配置为 Client 模式，会看到连接状态：
   ```
   [transport:speed] state=Connecting
   [transport:speed] state=Connected
   ```

### 配置说明
默认配置（在 LiteDB 数据库中）：
- Host: `127.0.0.1`
- SpeedPort: `5001`
- PositionPort: `5002`
- HeartbeatPort: `5003`
- Role: `Client`（主动连接上游）

如需修改为 Server 模式（等待上游连接），可以通过 API 或直接修改数据库。

---

## English Version

### Problem Description
The system was unable to connect to the upstream TCP server (vision system).

### Root Cause
In `ZakYip.Singulation.Infrastructure/Transport/UpstreamTcpInjection.cs`, the `AddUpstreamTcpFromLiteDb()` method incorrectly called `BuildServiceProvider()` during service registration, which is an anti-pattern that caused:

1. Inconsistent dependency injection container state
2. Potential memory leaks
3. Transport services might not be registered at all

### Solution
Removed the temporary `ServiceProvider` construction and changed to read configuration during service resolution:

**Before (Problematic):**
```csharp
using var temp = services.BuildServiceProvider();  // ❌ Anti-pattern
var store = temp.GetRequiredService<IUpstreamOptionsStore>();
var dto = store.GetAsync().GetAwaiter().GetResult();

if (dto.SpeedPort > 0) {  // ❌ Conditional registration
    services.AddKeyedSingleton<IByteTransport>("speed", ...);
}
```

**After (Fixed):**
```csharp
// ✅ Always register, read configuration at resolution time
services.AddKeyedSingleton<IByteTransport>("speed", (IServiceProvider sp, object? key) => {
    var cur = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult();
    return cur.Role == TransportRole.Server
        ? new TouchServerByteTransport(...)
        : new TouchClientByteTransport(...);
});
```

### Changes Made
1. ✅ Removed `BuildServiceProvider()` anti-pattern
2. ✅ Removed port > 0 conditional checks
3. ✅ Always register all three transport channels (speed/position/heartbeat)
4. ✅ Fixed nullability warnings (`object key` → `object? key`)
5. ✅ Added comprehensive test cases
6. ✅ Created detailed documentation

### How to Verify the Fix
1. Start the application
2. Check logs for transport startup messages:
   ```
   [transport:speed] started, status=Running
   [transport:position] started, status=Running
   [transport:heartbeat] started, status=Running
   ```
3. If configured in Client mode, you'll see connection status:
   ```
   [transport:speed] state=Connecting
   [transport:speed] state=Connected
   ```

### Configuration
Default configuration (stored in LiteDB):
- Host: `127.0.0.1`
- SpeedPort: `5001`
- PositionPort: `5002`
- HeartbeatPort: `5003`
- Role: `Client` (actively connect to upstream)

To change to Server mode (wait for upstream to connect), modify via API or directly in the database.

---

## Technical Details / 技术细节

### Transport Lifecycle / 传输生命周期
1. **Registration / 注册**: Services registered in `Program.cs`
2. **Resolution / 解析**: Factory creates transport instance on first use
3. **Startup / 启动**: `TransportEventPump` starts all transports
4. **Connection / 连接**: 
   - Client: Auto-retry with exponential backoff
   - Server: Listen on configured port
5. **Data Flow / 数据流**: Received data published to `UpstreamFrameHub`

### Files Changed / 修改的文件
- `ZakYip.Singulation.Infrastructure/Transport/UpstreamTcpInjection.cs` - Main fix
- `ZakYip.Singulation.Tests/UpstreamTcpInjectionTests.cs` - New tests
- `ZakYip.Singulation.Tests/FrameGuardTests.cs` - Namespace fix
- `UPSTREAM_TCP_FIX.md` - Detailed documentation
- `UPSTREAM_TCP_FIX_SUMMARY.md` - This summary

### Related Components / 相关组件
- `TouchClientByteTransport` - TCP client with auto-retry
- `TouchServerByteTransport` - TCP server
- `TransportEventPump` - Background service that starts transports
- `UpstreamFrameHub` - Distributes received data to consumers
- `SpeedFrameWorker` - Processes speed frames

### References / 参考资料
- [ASP.NET Core Dependency Injection Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- See `UPSTREAM_TCP_FIX.md` for troubleshooting guide
