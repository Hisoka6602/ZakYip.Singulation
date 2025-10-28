# 上游 TCP 无限连接支持 / Unlimited TCP Connections Support

## 中文版本

### 变更说明
修改了 `TcpServerOptions` 中的 `MaxActiveConnections` 默认值，从 1 改为 -1（表示无限制），以满足"上游TCP连接端口如果大于0需要能无限连接"的需求。

### 修改内容
**文件**: `ZakYip.Singulation.Transport/Tcp/TcpServerOptions.cs`

**修改前**:
```csharp
/// <summary>最多只保留 1 个活动连接（视觉一般只连一个）。</summary>
public int MaxActiveConnections { get; init; } = 1;
```

**修改后**:
```csharp
/// <summary>最大活动连接数，-1 表示无限制。</summary>
public int MaxActiveConnections { get; init; } = -1;
```

### 技术说明

#### 实现现状
- `TouchServerByteTransport` 已经支持多个并发连接
- 通过 `_connCount` 字段跟踪活动连接数
- `MaxActiveConnections` 属性虽然存在，但在实现中**未被强制执行**
- 因此，即使之前默认值为 1，服务器实际上已经能够接受多个连接

#### 本次修改的影响
1. **配置默认值**: 从限制 1 个连接改为无限制（-1）
2. **文档更新**: 明确说明 -1 表示无限制
3. **行为一致性**: 使默认配置与实际实现行为保持一致

#### 连接处理机制
```csharp
// TouchServerByteTransport.cs
service.Connected = (client, e) => {
    var n = Interlocked.Increment(ref _connCount);  // 增加连接计数
    if (n == 1) {
        Status = TransportStatus.Running;
        SetConnState(TransportConnectionState.Connected, endpoint, reason: "first client connected");
    }
    return EasyTask.CompletedTask;
};

service.Closed = (client, e) => {
    var n = Interlocked.Decrement(ref _connCount);  // 减少连接计数
    if (n <= 0 && !_stopping) {
        SetConnState(TransportConnectionState.Disconnected, endpoint, reason: "no clients", passive: true);
    }
    return EasyTask.CompletedTask;
};
```

### 使用场景

#### 服务器模式（TransportRole.Server）
当配置为服务器模式且端口 > 0 时：
- 本地监听指定端口（例如 5001, 5002, 5003）
- 接受来自上游客户端（如视觉系统）的多个连接
- 每个新连接都会被接受和处理

#### 配置示例
```csharp
services.AddKeyedSingleton<IByteTransport>("speed", (sp, key) => {
    var options = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult();
    if (options.Role == TransportRole.Server && options.SpeedPort > 0) {
        return new TouchServerByteTransport(new TcpServerOptions {
            Address = IPAddress.Any,
            Port = options.SpeedPort,
            // MaxActiveConnections 默认为 -1（无限制）
        });
    }
    // ...
});
```

### 安全考虑

#### 潜在风险
1. **资源耗尽**: 允许无限连接可能导致：
   - 内存使用增加（每个连接需要缓冲区）
   - 线程/异步任务数量增加
   - 系统资源耗尽

2. **拒绝服务（DoS）**: 恶意客户端可能创建大量连接

#### 缓解措施
1. **操作系统限制**: 
   - Socket backlog 设置为 100（`Backlog = 100`）
   - 操作系统的文件描述符限制
   - TCP/IP 栈的连接限制

2. **网络层保护**:
   - 防火墙规则限制来源 IP
   - 速率限制和连接节流（如果需要可在未来实现）

3. **监控和日志**:
   - 通过 `_connCount` 监控活动连接数
   - 日志记录所有连接/断开事件
   - 可通过 `/api/upstream/connections` API 查看连接状态

#### 最佳实践建议
对于生产环境，建议：
1. 配置防火墙只允许已知上游 IP 访问
2. 监控连接数和资源使用情况
3. 如需限制连接数，可在创建 `TcpServerOptions` 时显式设置：
   ```csharp
   new TcpServerOptions {
       Address = IPAddress.Any,
       Port = 5001,
       MaxActiveConnections = 10  // 限制为 10 个连接
   }
   ```
   注意：需要在 `TouchServerByteTransport` 中实现此限制的强制执行

### 未来改进

如果需要强制执行连接数限制，可以在 `TouchServerByteTransport.Connected` 回调中添加：

```csharp
service.Connected = (client, e) => {
    var n = Interlocked.Increment(ref _connCount);
    
    // 检查是否超过最大连接数
    if (_opt.MaxActiveConnections > 0 && n > _opt.MaxActiveConnections) {
        Interlocked.Decrement(ref _connCount);
        try { client.Close(); } catch { /* ignore */ }
        _logger?.LogWarning("Connection rejected: max connections ({Max}) exceeded", _opt.MaxActiveConnections);
        return EasyTask.CompletedTask;
    }
    
    if (n == 1) {
        Status = TransportStatus.Running;
        SetConnState(TransportConnectionState.Connected, endpoint, reason: "first client connected");
    }
    return EasyTask.CompletedTask;
};
```

---

## English Version

### Change Description
Modified the default value of `MaxActiveConnections` in `TcpServerOptions` from 1 to -1 (unlimited) to meet the requirement: "If upstream TCP connection port is greater than 0, it should allow unlimited connections."

### Changes Made
**File**: `ZakYip.Singulation.Transport/Tcp/TcpServerOptions.cs`

**Before**:
```csharp
/// <summary>最多只保留 1 个活动连接（视觉一般只连一个）。</summary>
public int MaxActiveConnections { get; init; } = 1;
```

**After**:
```csharp
/// <summary>最大活动连接数，-1 表示无限制。</summary>
public int MaxActiveConnections { get; init; } = -1;
```

### Technical Details

#### Current Implementation
- `TouchServerByteTransport` already supports multiple concurrent connections
- Tracks active connections using the `_connCount` field
- The `MaxActiveConnections` property exists but is **NOT enforced** in the implementation
- Therefore, even with the previous default of 1, the server could actually accept multiple connections

#### Impact of This Change
1. **Configuration Default**: Changed from limiting to 1 connection to unlimited (-1)
2. **Documentation Update**: Explicitly states that -1 means unlimited
3. **Behavioral Consistency**: Aligns the default configuration with actual implementation behavior

#### Connection Handling Mechanism
```csharp
// TouchServerByteTransport.cs
service.Connected = (client, e) => {
    var n = Interlocked.Increment(ref _connCount);  // Increment connection count
    if (n == 1) {
        Status = TransportStatus.Running;
        SetConnState(TransportConnectionState.Connected, endpoint, reason: "first client connected");
    }
    return EasyTask.CompletedTask;
};

service.Closed = (client, e) => {
    var n = Interlocked.Decrement(ref _connCount);  // Decrement connection count
    if (n <= 0 && !_stopping) {
        SetConnState(TransportConnectionState.Disconnected, endpoint, reason: "no clients", passive: true);
    }
    return EasyTask.CompletedTask;
};
```

### Use Cases

#### Server Mode (TransportRole.Server)
When configured as server mode with port > 0:
- Listens on specified local port (e.g., 5001, 5002, 5003)
- Accepts multiple connections from upstream clients (e.g., vision systems)
- Each new connection is accepted and handled

#### Configuration Example
```csharp
services.AddKeyedSingleton<IByteTransport>("speed", (sp, key) => {
    var options = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult();
    if (options.Role == TransportRole.Server && options.SpeedPort > 0) {
        return new TouchServerByteTransport(new TcpServerOptions {
            Address = IPAddress.Any,
            Port = options.SpeedPort,
            // MaxActiveConnections defaults to -1 (unlimited)
        });
    }
    // ...
});
```

### Security Considerations

#### Potential Risks
1. **Resource Exhaustion**: Allowing unlimited connections may lead to:
   - Increased memory usage (each connection needs buffers)
   - Increased number of threads/async tasks
   - System resource exhaustion

2. **Denial of Service (DoS)**: Malicious clients could create many connections

#### Mitigation Measures
1. **Operating System Limits**:
   - Socket backlog set to 100 (`Backlog = 100`)
   - OS file descriptor limits
   - TCP/IP stack connection limits

2. **Network Layer Protection**:
   - Firewall rules to restrict source IPs
   - Rate limiting and connection throttling (can be implemented in the future if needed)

3. **Monitoring and Logging**:
   - Monitor active connections via `_connCount`
   - Log all connection/disconnection events
   - Check connection status via `/api/upstream/connections` API

#### Best Practice Recommendations
For production environments, it's recommended to:
1. Configure firewall to only allow known upstream IPs
2. Monitor connection count and resource usage
3. If connection limit is needed, explicitly set it when creating `TcpServerOptions`:
   ```csharp
   new TcpServerOptions {
       Address = IPAddress.Any,
       Port = 5001,
       MaxActiveConnections = 10  // Limit to 10 connections
   }
   ```
   Note: Enforcement of this limit needs to be implemented in `TouchServerByteTransport`

### Future Improvements

If connection limit enforcement is needed, add the following to `TouchServerByteTransport.Connected` callback:

```csharp
service.Connected = (client, e) => {
    var n = Interlocked.Increment(ref _connCount);
    
    // Check if max connections exceeded
    if (_opt.MaxActiveConnections > 0 && n > _opt.MaxActiveConnections) {
        Interlocked.Decrement(ref _connCount);
        try { client.Close(); } catch { /* ignore */ }
        _logger?.LogWarning("Connection rejected: max connections ({Max}) exceeded", _opt.MaxActiveConnections);
        return EasyTask.CompletedTask;
    }
    
    if (n == 1) {
        Status = TransportStatus.Running;
        SetConnState(TransportConnectionState.Connected, endpoint, reason: "first client connected");
    }
    return EasyTask.CompletedTask;
};
```

---

## Summary / 总结

### Key Points / 要点
- ✅ Changed default `MaxActiveConnections` from 1 to -1 (unlimited) / 将默认 `MaxActiveConnections` 从 1 改为 -1（无限制）
- ✅ Documentation updated to clarify -1 means unlimited / 更新文档说明 -1 表示无限制
- ✅ Aligns configuration with actual implementation behavior / 使配置与实际实现行为一致
- ⚠️ Property is not enforced in current implementation / 当前实现中未强制执行此属性
- ⚠️ Consider security implications for production use / 生产环境使用需考虑安全影响

### Related Files / 相关文件
- `ZakYip.Singulation.Transport/Tcp/TcpServerOptions.cs` - Modified
- `ZakYip.Singulation.Transport/Tcp/TcpServerByteTransport/TouchServerByteTransport.cs` - Implementation (no changes)
- `ZakYip.Singulation.Infrastructure/Transport/UpstreamTcpInjection.cs` - Uses TcpServerOptions (no changes)
