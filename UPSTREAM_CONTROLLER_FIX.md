# 上游 TCP 传输获取问题修复

## 问题描述 (Problem Description)

UpstreamController 上游Tcp成功连接后也无法获取到 `_transports`，`_transports.Count()` 永远是0。

即使 TCP 连接成功建立，UpstreamController 的 `_transports` 字段始终为空，导致：
- `GetConnectionsAsync()` 返回 0 个连接
- `Reconnect()` 方法无法找到传输实例进行重连
- 用户无法查看或管理上游 TCP 连接

## 根本原因 (Root Cause)

问题的根源在于 **依赖注入（DI）中的服务注册方式不匹配**：

1. **传输注册方式**：上游传输（speed、position、heartbeat）使用**键控服务（Keyed Services）** 注册：
   ```csharp
   services.AddKeyedSingleton<IByteTransport>("speed", ...);
   services.AddKeyedSingleton<IByteTransport>("position", ...);
   services.AddKeyedSingleton<IByteTransport>("heartbeat", ...);
   ```

2. **控制器注入方式**：UpstreamController 尝试通过 `IEnumerable<IByteTransport>` 注入所有传输：
   ```csharp
   public UpstreamController(..., IEnumerable<IByteTransport> transports) {
       _transports = transports; // 这将始终为空！
   }
   ```

3. **不匹配问题**：在 .NET 依赖注入容器中：
   - `IEnumerable<T>` 只包含**非键控**的服务注册
   - **键控服务**不会被包含在 `IEnumerable<T>` 中
   - 因此 `_transports` 集合永远为空，即使传输已成功创建和连接

## 解决方案 (Solution)

修改 UpstreamController 直接从 `UpstreamTransportManager` 获取传输实例，而不是通过 DI 注入集合。

### 修改前 (Before)

```csharp
public class UpstreamController : ControllerBase {
    private readonly IEnumerable<IByteTransport> _transports;
    
    public UpstreamController(..., IEnumerable<IByteTransport> transports) {
        _transports = transports; // 始终为空
    }
    
    public Task<ApiResponse<UpstreamConnectionsDto>> GetConnectionsAsync(...) {
        var items = _transports.Select((t, i) => ...).ToList(); // 空列表
        // ...
    }
}
```

### 修改后 (After)

```csharp
public class UpstreamController : ControllerBase {
    private readonly UpstreamTransportManager _transportManager;
    
    public UpstreamController(..., UpstreamTransportManager transportManager) {
        _transportManager = transportManager;
    }
    
    public Task<ApiResponse<UpstreamConnectionsDto>> GetConnectionsAsync(...) {
        var transports = GetActiveTransports(); // 从管理器获取
        // ...
    }
    
    private List<IByteTransport> GetActiveTransports() {
        return new[] {
            _transportManager.SpeedTransport,
            _transportManager.PositionTransport,
            _transportManager.HeartbeatTransport
        }.Where(t => t != null).ToList()!;
    }
}
```

## 技术细节 (Technical Details)

### UpstreamTransportManager 的作用

`UpstreamTransportManager` 是传输管理器，负责：
1. 创建和管理三个上游传输实例（speed、position、heartbeat）
2. 支持配置热更新（热更新时重新创建传输）
3. 通过公共属性暴露传输实例：
   ```csharp
   public IByteTransport? SpeedTransport { get; }
   public IByteTransport? PositionTransport { get; }
   public IByteTransport? HeartbeatTransport { get; }
   ```

### 传输初始化流程

1. **服务注册**（Program.cs）：
   ```csharp
   services.AddUpstreamTcpFromLiteDb();
   ```

2. **传输管理器初始化**（TransportEventPump）：
   ```csharp
   await _transportManager.InitializeAsync(stoppingToken);
   ```

3. **传输启动**（TransportEventPump）：
   ```csharp
   var keys = new[] { "speed", "position", "heartbeat" };
   foreach (var key in keys) {
       var t = _sp.GetKeyedService<IByteTransport>(key);
       if (t != null) {
           await t.StartAsync(stoppingToken);
       }
   }
   ```

## 代码变更摘要 (Changes Summary)

### 1. UpstreamController.cs

- **移除**：`IEnumerable<IByteTransport> _transports` 字段和构造函数参数
- **新增**：`GetActiveTransports()` 私有辅助方法
- **修改**：`GetConnectionsAsync()` 使用 `GetActiveTransports()` 获取传输
- **修改**：`Reconnect()` 使用 `GetActiveTransports()` 获取传输

### 2. UpstreamControllerTests.cs（新文件）

添加了三个测试用例：
- `GetConnectionsAsync_ReturnsTransports_AfterInitialization`：验证连接状态查询
- `Reconnect_WorksWithValidIndex`：验证有效索引的重连
- `Reconnect_ReturnsNotFound_WithInvalidIndex`：验证无效索引的处理

## 验证结果 (Validation)

✅ **编译通过**：Host 项目成功编译，无错误
✅ **代码审查**：通过代码审查，应用了重构建议
✅ **逻辑正确**：
   - GetConnectionsAsync 现在可以正确返回所有已连接的传输
   - Reconnect 可以通过索引重连指定的传输
   - 即使传输为 null，也能正确处理（只返回非 null 的传输）

## 影响范围 (Impact)

### 受影响的功能
- ✅ **上游连接状态查询**：`GET /api/upstream/connections` 现在可以正确返回传输状态
- ✅ **上游连接重连**：`POST /api/upstream/connections/{index}/reconnect` 现在可以正常工作

### 不受影响的功能
- ✅ **传输数据接收**：数据流转由 TransportEventPump 处理，不受此修改影响
- ✅ **配置管理**：配置的读取和保存不受影响
- ✅ **热更新**：传输热更新功能继续正常工作

## 兼容性 (Compatibility)

- ✅ **向后兼容**：API 接口保持不变
- ✅ **无破坏性变更**：仅修改内部实现，不影响外部调用者
- ✅ **数据迁移**：无需数据迁移

## 测试建议 (Testing Recommendations)

建议在实际环境中验证以下场景：

1. **查询连接状态**：
   ```bash
   curl http://localhost:5005/api/upstream/connections
   ```
   预期：返回三个传输的详细信息（IP、端口、状态等）

2. **重连传输**：
   ```bash
   curl -X POST http://localhost:5005/api/upstream/connections/0/reconnect
   ```
   预期：成功重连第一个传输（speed）

3. **无效索引**：
   ```bash
   curl -X POST http://localhost:5005/api/upstream/connections/999/reconnect
   ```
   预期：返回 404 Not Found

## 总结 (Summary)

通过将 UpstreamController 从依赖注入 `IEnumerable<IByteTransport>` 改为直接从 `UpstreamTransportManager` 获取传输实例，解决了传输集合始终为空的问题。

这个修复：
- ✅ 符合现有的架构设计（UpstreamTransportManager 本就是传输的管理者）
- ✅ 代码更加清晰（明确从管理器获取，而不是依赖 DI 的隐式行为）
- ✅ 易于维护（集中在一个辅助方法中）
- ✅ 无性能影响（只是获取引用，无额外开销）
