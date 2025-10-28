# 上游TCP连接热更新功能说明

## 概述

上游TCP连接热更新功能允许在运行时动态更新TCP连接配置，实现配置即时生效，无需重启应用程序。此功能适用于需要在生产环境中快速调整连接参数的场景。

## 功能特性

### 1. 零停机更新
- 新连接创建成功后才会释放旧连接
- 保证服务连续性，避免数据丢失
- 支持平滑过渡，对业务影响最小化

### 2. 全参数支持
支持以下所有连接参数的热更新：
- `host`: 远程主机地址（Client模式）
- `speedPort`: 速度通道端口号
- `positionPort`: 位置通道端口号  
- `heartbeatPort`: 心跳通道端口号
- `role`: 连接角色（Client/Server）
- `validateCrc`: CRC校验开关

### 3. 多通道同步更新
- Speed、Position、Heartbeat 三路连接同时更新
- 确保所有通道使用一致的配置
- 避免配置不一致导致的问题

### 4. 异常安全
- 更新失败自动回滚到旧配置
- 线程安全的连接切换机制
- 完整的错误日志记录

## 架构设计

### 核心组件

#### 1. UpstreamTransportManager
传输管理器，负责管理所有上游TCP连接的生命周期：
- 维护当前活跃的传输连接实例
- 提供热更新接口 `ReloadTransportsAsync`
- 线程安全的连接切换机制
- 自动清理旧连接资源

#### 2. UpstreamTcpInjection
依赖注入配置，注册传输服务：
- 注册 `UpstreamTransportManager` 为单例
- 通过Keyed DI提供三路传输访问
- 延迟初始化，按需创建连接

#### 3. UpstreamController
REST API控制器：
- `PUT /api/upstream/configs`: 更新配置并触发热更新
- `GET /api/upstream/configs`: 获取当前配置
- `GET /api/upstream/connections`: 查看连接状态

### 工作流程

```
用户调用API更新配置
    ↓
保存配置到LiteDB
    ↓
UpstreamTransportManager.ReloadTransportsAsync()
    ↓
创建新的Transport实例（使用新配置）
    ↓
启动新Transport连接
    ↓
在锁保护下交换新旧Transport引用
    ↓
异步停止并释放旧Transport
    ↓
返回更新结果
```

## 使用指南

### 1. 通过API更新配置

#### 示例1：修改连接地址和端口
```bash
curl -X PUT http://localhost:5000/api/upstream/configs \
  -H "Content-Type: application/json" \
  -d '{
    "host": "192.168.1.100",
    "speedPort": 6001,
    "positionPort": 6002,
    "heartbeatPort": 6003,
    "role": "Client",
    "validateCrc": true
  }'
```

响应：
```json
{
  "success": true,
  "data": "配置已保存并热更新成功",
  "message": null,
  "timestamp": "2025-10-28T12:00:00Z"
}
```

#### 示例2：从Client模式切换到Server模式
```bash
curl -X PUT http://localhost:5000/api/upstream/configs \
  -H "Content-Type: application/json" \
  -d '{
    "speedPort": 5001,
    "positionPort": 5002,
    "heartbeatPort": 5003,
    "role": "Server",
    "validateCrc": true
  }'
```

**注意**：Server模式下不需要指定`host`字段，会监听所有网络接口。

### 2. 查看连接状态

```bash
curl -X GET http://localhost:5000/api/upstream/connections
```

响应示例：
```json
{
  "success": true,
  "data": {
    "enabled": true,
    "items": [
      {
        "index": 1,
        "ip": "192.168.1.100",
        "port": 6001,
        "isServer": false,
        "state": "Connected",
        "impl": "TouchClientByteTransport"
      },
      {
        "index": 2,
        "ip": "192.168.1.100",
        "port": 6002,
        "isServer": false,
        "state": "Connected",
        "impl": "TouchClientByteTransport"
      },
      {
        "index": 3,
        "ip": "192.168.1.100",
        "port": 6003,
        "isServer": false,
        "state": "Connected",
        "impl": "TouchClientByteTransport"
      }
    ]
  }
}
```

### 3. 手动触发重连

如果只需要重连而不更改配置：
```bash
curl -X POST http://localhost:5000/api/upstream/connections/0/reconnect
```

## 技术细节

### 线程安全机制

使用 `lock` 保护关键资源访问：
```csharp
lock (_gate) {
    oldSpeed = _speedTransport;
    oldPosition = _positionTransport;
    oldHeartbeat = _heartbeatTransport;

    _speedTransport = newSpeed;
    _positionTransport = newPosition;
    _heartbeatTransport = newHeartbeat;
    _currentOptions = newConfig;
}
```

### 异常处理与回滚

```csharp
try {
    // 创建新连接
    newSpeed = CreateTransport(newOptions, "speed", newOptions.SpeedPort);
    // 启动新连接
    await StartAllTransportsAsync(ct);
    // 交换引用
    SwapTransports();
}
catch (Exception ex) {
    // 回滚到旧连接
    RestoreOldTransports();
    // 清理失败的新连接
    await DisposeNewTransports();
    throw;
}
```

### 资源清理

旧连接在后台异步释放，避免阻塞API响应：
```csharp
_ = Task.Run(async () => {
    await StopAndDisposeTransportAsync(oldSpeed, "old-speed");
    await StopAndDisposeTransportAsync(oldPosition, "old-position");
    await StopAndDisposeTransportAsync(oldHeartbeat, "old-heartbeat");
}, CancellationToken.None);
```

## 日志监控

### 关键日志点

#### 1. 热更新开始
```
[UpstreamTransportManager] Reloading transports with new config: Host=192.168.1.100, Role=Client, SpeedPort=6001, PositionPort=6002, HeartbeatPort=6003
```

#### 2. 新连接创建成功
```
[UpstreamTransportManager] New transports created and swapped successfully
```

#### 3. 旧连接释放
```
[UpstreamTransportManager] Transport 'old-speed' stopped and disposed
```

#### 4. 热更新失败
```
[UpstreamTransportManager] Failed to reload transports: <错误详情>
```

### 日志级别建议

#### Information 级别
用于记录正常的操作流程：
```
[UpstreamController] Upstream config saved to database: Host=192.168.1.100, Role=Client
[UpstreamTransportManager] Reloading transports with new config
[UpstreamTransportManager] Transport 'speed' started
```

#### Warning 级别
用于记录非关键错误，不影响主要功能：
```
[UpstreamTransportManager] Error stopping transport 'old-speed': Connection already closed
[UpstreamTransportManager] Error disposing transport: Object already disposed
```

#### Error 级别
用于记录严重错误，需要人工介入：
```
[UpstreamController] Failed to update upstream config or reload transports: Connection timeout
[UpstreamTransportManager] Failed to reload transports: Port 5001 already in use
```

## 常见问题

### Q1: 热更新会导致数据丢失吗？
**A**: 不会。新连接建立成功后才会释放旧连接，保证了数据传输的连续性。正在传输中的数据会在旧连接关闭前完成。

### Q2: 热更新失败会怎样？
**A**: 系统会自动回滚到旧配置，旧连接继续工作。API会返回错误信息，管理员可以根据日志排查问题。

### Q3: 支持部分参数更新吗？
**A**: 必须提供完整的配置对象。建议先通过 `GET /api/upstream/configs` 获取当前配置，修改需要的字段后再提交。

### Q4: 热更新需要多长时间？
**A**: 通常在1-2秒内完成。具体时间取决于网络状况和连接建立速度。

### Q5: 能否在高负载下执行热更新？
**A**: 可以，但建议在业务低峰期执行。虽然设计了平滑切换机制，但连接切换瞬间可能有微小的性能波动。

## 性能考量

### 1. 内存使用
- 热更新期间会同时存在新旧两套连接实例
- 旧连接会在后台异步释放，峰值内存使用约为正常的2倍
- 通常在数秒内恢复正常

### 2. CPU开销
- 连接创建和销毁的CPU开销很小
- 主要开销在网络连接建立（TCP握手）
- 对系统整体性能影响可忽略

### 3. 网络影响
- Client模式：主动发起连接，网络延迟取决于远程服务器响应
- Server模式：立即监听，等待客户端连接

## 最佳实践

### 1. 配置变更前的检查
- 确认新的端口未被其他程序占用
- 验证远程主机地址可达（Client模式）
- 在测试环境验证配置的正确性

### 2. 监控与告警
- 监控连接状态变化
- 设置热更新失败告警
- 记录每次配置变更的操作日志

### 3. 回滚预案
- 保留上一次可用的配置
- 准备快速恢复脚本
- 建立配置版本管理

### 4. 灰度发布
如果管理多个实例，建议：
1. 先在单个实例上测试
2. 观察连接状态和业务指标
3. 确认无问题后推广到其他实例

## 相关文档

- REST API参考 - 参见Swagger文档 http://localhost:5000/swagger
- 上游通信配置 - 参见本文档
- 系统架构设计 - 参见项目README.md

## 版本历史

### v1.0.0 (2025-10-28)
- ✅ 首次发布热更新功能
- ✅ 支持所有连接参数的动态更新
- ✅ 零停机切换机制
- ✅ 异常安全与自动回滚
- ✅ 完整的单元测试覆盖
