# 安全按键系统使用指南

## 概述

ZakYip.Singulation 项目提供完整的安全按键系统，支持物理按键（急停、启动、停止、复位）和远程 API 命令两种控制方式。

## 架构设计

### 核心组件

1. **ISafetyIoModule** - 安全 IO 模块接口
   - 定义了四种安全事件：EmergencyStop、StopRequested、StartRequested、ResetRequested
   - 支持多种实现：硬件 IO 模块、软件模拟模块等

2. **SafetyPipeline** - 安全管线
   - 汇聚所有安全事件（IO 按键、远程命令、轴故障等）
   - 统一处理安全逻辑，触发停机、隔离等动作
   - 实时推送安全事件到 SignalR 客户端

3. **SafetyController** - REST API 控制器
   - 提供远程安全命令接口：`POST /api/safety/commands`
   - 支持命令：Start、Stop、Reset、EmergencyStop

4. **SafetyIsolator** - 安全隔离器
   - 管理系统安全状态：Normal、Degraded、Isolated
   - 防止重复触发，提供状态恢复机制

## 物理按键集成

### 1. 雷赛控制器硬件接入

项目已实现 `LeadshineSafetyIoModule`，通过雷赛控制器的数字输入端口读取物理按键状态。

#### 硬件接线要求

- **急停按键**：连接到控制器输入端口 IN0（可配置）
- **停止按键**：连接到控制器输入端口 IN1（可配置）
- **启动按键**：连接到控制器输入端口 IN2（可配置）
- **复位按键**：连接到控制器输入端口 IN3（可配置）

按键类型支持：
- 常开触点（默认）：按下时输入高电平
- 常闭触点：需在配置中启用 `InvertLogic`

#### 配置方法

编辑 `appsettings.json`：

```json
{
  "LeadshineSafetyIo": {
    "Enabled": true,                    // 启用物理按键
    "EmergencyStopBit": 0,              // 急停按键输入端口号
    "StopBit": 1,                       // 停止按键输入端口号
    "StartBit": 2,                      // 启动按键输入端口号
    "ResetBit": 3,                      // 复位按键输入端口号
    "PollingIntervalMs": 50,            // 轮询间隔（毫秒）
    "InvertLogic": false                // 是否反转逻辑（常闭按键设为 true）
  }
}
```

**配置说明**：
- `Enabled`: 设为 `true` 启用硬件按键，`false` 使用软件模拟模式
- `EmergencyStopBit/StopBit/StartBit/ResetBit`: 输入端口号，-1 表示禁用该按键
- `PollingIntervalMs`: 轮询间隔，推荐 50ms（20Hz），可根据响应要求调整
- `InvertLogic`: 
  - `false`（默认）：按下=高电平，适用于常开按键
  - `true`：按下=低电平，适用于常闭按键

#### 边沿检测机制

系统采用上升沿检测，避免重复触发：
- 只在按键从"未按下"到"按下"的瞬间触发事件
- 持续按住不会重复触发
- 支持高频轮询，响应延迟 < 100ms

### 2. 回环测试模式

当 `LeadshineSafetyIo.Enabled = false` 时，系统自动使用 `LoopbackSafetyIoModule`，用于开发测试：

```csharp
// 在代码中手动触发安全事件（仅测试环境）
var loopback = serviceProvider.GetRequiredService<LoopbackSafetyIoModule>();
loopback.TriggerEmergencyStop("测试急停");
loopback.TriggerStart("测试启动");
```

## 远程 API 控制

### 安全命令 API

**端点**：`POST /api/safety/commands`

**请求体**：
```json
{
  "command": 0,      // 0=None, 1=Start, 2=Stop, 3=Reset, 4=EmergencyStop
  "reason": "操作原因说明"
}
```

**响应**：
```json
{
  "result": true,
  "data": {
    "accepted": true
  },
  "msg": "安全命令已受理"
}
```

### 命令详解

| 命令值 | 命令名称 | 说明 | 触发类型 |
|--------|---------|------|----------|
| 1 | Start | 启动系统 | RemoteStartCommand |
| 2 | Stop | 停止系统（降级模式） | RemoteStopCommand |
| 3 | Reset | 复位（从隔离/降级恢复） | RemoteResetCommand |
| 4 | EmergencyStop | 急停（立即停机+隔离） | EmergencyStop |

### 使用示例

#### cURL
```bash
# 启动系统
curl -X POST http://localhost:5005/api/safety/commands \
  -H "Content-Type: application/json" \
  -d '{"command": 1, "reason": "远程启动测试"}'

# 急停
curl -X POST http://localhost:5005/api/safety/commands \
  -H "Content-Type: application/json" \
  -d '{"command": 4, "reason": "紧急停机"}'

# 复位
curl -X POST http://localhost:5005/api/safety/commands \
  -H "Content-Type: application/json" \
  -d '{"command": 3, "reason": "故障已排除，复位系统"}'
```

#### C# HttpClient
```csharp
var client = new HttpClient { BaseAddress = new Uri("http://localhost:5005") };
var request = new { command = 4, reason = "紧急停机" };
var response = await client.PostAsJsonAsync("/api/safety/commands", request);
```

#### JavaScript/TypeScript
```typescript
fetch('http://localhost:5005/api/safety/commands', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ command: 4, reason: '紧急停机' })
});
```

## 安全逻辑流程

### 1. 启动流程

```
用户按下启动按键 / API 启动命令
    ↓
SafetyPipeline 接收 StartRequested 事件
    ↓
检查安全隔离状态
    ├─ 如果已隔离 → 拒绝启动，记录日志
    └─ 如果正常/降级 → 触发 StartRequested 事件
        ↓
    通过 SignalR 推送启动通知
```

### 2. 停止流程

```
用户按下停止按键 / API 停止命令
    ↓
SafetyPipeline 接收 StopRequested 事件
    ↓
触发 StopRequested 事件
    ↓
系统进入降级模式（TryEnterDegraded）
    ↓
通过 SignalR 推送停止通知
```

### 3. 急停流程

```
用户按下急停按键 / API 急停命令
    ↓
SafetyPipeline 接收 EmergencyStop 事件
    ↓
立即执行 StopAllAsync：
    ├─ 所有轴速度清零
    ├─ 所有轴停止运动
    └─ 系统进入降级模式
        ↓
    通过 SignalR 推送急停通知
```

### 4. 复位流程

```
用户按下复位按键 / API 复位命令
    ↓
SafetyPipeline 接收 ResetRequested 事件
    ↓
检查当前状态
    ├─ 如果隔离 → TryResetIsolation（解除隔离）
    ├─ 如果降级 → TryRecoverFromDegraded（恢复正常）
    └─ 如果正常 → 无操作
        ↓
    记录复位结果日志
```

### 5. 轴故障自动处理

```
检测到轴故障 / 轴掉线
    ↓
SafetyPipeline 接收 AxisHealth 事件
    ↓
根据故障类型
    ├─ 轴故障 → 进入降级模式（TryEnterDegraded）
    └─ 轴掉线 → 触发隔离（TryTrip）
        ↓
    自动执行 StopAllAsync
        ↓
    记录故障日志，累计故障计数
```

## 状态机说明

### SafetyIsolationState

| 状态 | 说明 | 允许操作 |
|------|------|---------|
| Normal | 正常运行 | 启动、停止、轴运动 |
| Degraded | 降级运行 | 停止、复位；不允许启动 |
| Isolated | 完全隔离 | 仅允许复位；所有运动被禁止 |

### 状态转换

```
Normal ─────┐
    ↑       │ 停止按键 / 轴故障
    │       ↓
    │   Degraded
    │       ↓ 轴掉线 / 严重故障
    │   Isolated
    └───────┘ 复位成功
```

## SignalR 实时推送

安全事件会通过 SignalR 实时推送到所有连接的客户端：

### 推送消息格式

```json
{
  "kind": "safety.start",        // safety.start / safety.stopall
  "reason": "物理启动按键",
  "timestamp": "2025-10-21T07:14:24.456Z"
}
```

### 客户端订阅示例

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5005/hubs/realtime")
    .Build();

connection.On<object>("OnDeviceEvent", evt => {
    Console.WriteLine($"安全事件：{evt}");
});

await connection.StartAsync();
```

## 安全最佳实践

### 1. 物理按键配置建议

✅ **推荐配置**：
- 急停按键使用独立硬件急停开关（常闭触点）
- 急停开关应符合 GB/T 16855.1（IEC 60947-5-1）标准
- 使用红色蘑菇头急停按钮，直径 ≥ 40mm
- 急停按钮安装在操作人员易于触及的位置

⚠️ **注意事项**：
- 急停触发后，需手动复位才能恢复
- 急停按钮应采用机械锁定机构
- 定期检查按键功能和接线状态

### 2. 轮询间隔调优

| 场景 | 推荐间隔 | 说明 |
|------|---------|------|
| 高速生产线 | 20-50ms | 响应快，CPU 占用略高 |
| 一般应用 | 50-100ms | 平衡响应和性能 |
| 低速设备 | 100-200ms | 降低 CPU 占用 |

### 3. 日志审计

所有安全操作都会记录日志，包括：
- 操作类型（启动/停止/急停/复位）
- 触发来源（物理按键/远程命令/自动触发）
- 操作时间和原因说明
- 系统状态变化

**查看日志**：
```bash
# 查看最近的安全日志
grep "安全\|急停\|Safety" logs/app-*.log
```

### 4. 故障排查

#### 按键无响应

1. 检查配置：`LeadshineSafetyIo.Enabled = true`
2. 检查端口号配置是否正确
3. 检查接线和按键硬件
4. 查看日志中是否有错误信息：`grep "安全 IO" logs/app-*.log`

#### 误触发

1. 检查 `InvertLogic` 配置是否与按键类型匹配
2. 增加 `PollingIntervalMs` 以降低噪声影响
3. 检查按键防抖电路

#### 响应延迟

1. 降低 `PollingIntervalMs`（最低 20ms）
2. 检查系统 CPU 负载
3. 检查雷赛控制器通信状态

## 测试验证

### 功能测试清单

- [ ] 急停按键：按下后系统立即停机，所有轴速度归零
- [ ] 停止按键：按下后系统进入降级模式，拒绝新的启动命令
- [ ] 启动按键：在正常状态下触发启动事件
- [ ] 复位按键：从降级/隔离状态恢复到正常状态
- [ ] 边沿检测：持续按住不会重复触发
- [ ] 远程 API：通过 API 调用各安全命令正常工作
- [ ] SignalR 推送：客户端能实时接收安全事件通知
- [ ] 轴故障联动：轴故障时自动触发安全机制
- [ ] 日志记录：所有操作都有详细日志记录

### 压力测试

1. **高频按键测试**：快速连续按下按键，系统应稳定不崩溃
2. **并发命令测试**：同时通过 API 和按键发送命令，系统应正确处理
3. **长时间运行测试**：7x24 小时运行，检查内存泄漏和稳定性

## 生产环境部署

### 部署前检查

- [ ] 确认物理按键接线正确
- [ ] 配置文件中启用硬件 IO 模块
- [ ] 测试所有按键功能正常
- [ ] 验证 SignalR 实时推送工作正常
- [ ] 确认日志记录完整
- [ ] 制定应急预案（参见 ops/EMERGENCY_RESPONSE.md）

### 启动服务

```bash
cd ZakYip.Singulation.Host
dotnet run --configuration Release
```

或使用 systemd 守护进程（参见 docs/DEPLOYMENT.md）。

## 相关文档

- [运维手册](../ops/OPERATIONS_MANUAL.md)
- [应急响应预案](../ops/EMERGENCY_RESPONSE.md)
- [API 文档](API.md)
- [部署指南](DEPLOYMENT.md)
- [故障排查手册](TROUBLESHOOTING.md)

## 技术支持

如有问题，请参考：
1. 查看日志：`logs/app-*.log`
2. 检查配置：`appsettings.json`
3. 查阅故障排查手册
4. 提交 GitHub Issue
