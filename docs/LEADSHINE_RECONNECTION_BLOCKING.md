# Leadshine Operation Blocking During Reconnection

## 概述 (Overview)

本文档描述了雷赛（Leadshine）控制器适配器在重新连接期间自动阻止操作的实现。

This document describes the implementation of automatic operation blocking during Leadshine controller adapter reconnection.

## 问题背景 (Background)

当其他程序解锁雷赛分布式锁并触发复位时，需要执行以下操作：
1. 调用雷赛 Close
2. 等待恢复时间
3. 重新连接

**关键要求：在此过程中，必须禁止本项目的其他地方调用雷赛的方法或者监控雷赛 IO，重新连接后才能调用。**

When another program releases the Leisai distributed lock and triggers a reset, the following actions need to be performed:
1. Call Leisai Close
2. Wait for recovery time
3. Reconnect

**Key requirement: During this process, other parts of the project must be prevented from calling Leisai methods or monitoring Leisai IO. Only after reconnection can calls be made.**

## 解决方案 (Solution)

### 核心机制 (Core Mechanisms)

#### 1. 操作信号量 (Operation Semaphore)
使用 `SemaphoreSlim` 来保护所有雷赛操作：
```csharp
private readonly SemaphoreSlim _operationSemaphore = new SemaphoreSlim(1, 1);
```

所有公共方法（`InitializeAsync`, `CloseAsync`, `GetAxisCountAsync`, `GetErrorCodeAsync`, `ResetAsync`, `WarmResetAsync`, `BatchWriteMultipleAxesAsync`, `BatchReadMultipleAxesAsync`）都会：
1. 检查 `_isReconnecting` 标志
2. 获取信号量
3. 执行操作
4. 释放信号量

All public methods acquire this semaphore before executing, ensuring exclusive access during reconnection.

#### 2. 重新连接状态 (Reconnection State)
```csharp
private volatile bool _isReconnecting;
public bool IsReconnecting => _isReconnecting;
```

外部代码可以检查 `IsReconnecting` 属性来确定当前是否正在重新连接。

External code can check the `IsReconnecting` property to determine if reconnection is in progress.

#### 3. 事件通知 (Event Notifications)
```csharp
public event EventHandler? ReconnectionStarting;
public event EventHandler? ReconnectionCompleted;
```

应用程序可以订阅这些事件来：
- 在重新连接开始时暂停操作并保存状态
- 在重新连接完成时恢复操作和状态

Applications can subscribe to these events to:
- Pause operations and save state when reconnection starts
- Resume operations and restore state when reconnection completes

#### 4. IO 轮询暂停 (IO Polling Pause)
`LeadshineCabinetIoModule` 添加了暂停/恢复机制：
```csharp
public void PausePolling();
public void ResumePolling();
```

在重新连接期间可以暂停 IO 轮询，避免在控制器离线时产生错误。

IO polling can be paused during reconnection to avoid errors while the controller is offline.

### 自动重新连接流程 (Automatic Reconnection Flow)

当收到来自其他进程的复位通知时，适配器会自动执行以下步骤：

When a reset notification is received from another process, the adapter automatically:

```
1. OnResetNotificationReceived 触发
   ↓
2. 触发 HandleReconnectionAsync (异步)
   ↓
3. 获取操作信号量 (阻止所有其他操作)
   ↓
4. 设置 _isReconnecting = true
   ↓
5. 触发 ReconnectionStarting 事件
   ↓
6. 调用 CloseAsync() - 关闭当前连接
   ↓
7. 等待恢复时间 (冷复位: 15秒 + 2秒缓冲, 热复位: 2秒 + 2秒缓冲)
   ↓
8. 调用 InitializeInternalAsync() - 重新初始化连接
   ↓
9. 触发 ReconnectionCompleted 事件
   ↓
10. 设置 _isReconnecting = false
   ↓
11. 释放操作信号量 (解除操作阻止)
```

**关键点 (Key Points):**
- 步骤 3-11 期间，所有其他操作被阻止
- 在重新连接期间调用的方法会：
  - 检查 `_isReconnecting` 并记录警告
  - 返回默认值（例如 `GetAxisCountAsync` 返回 0）
  - 或等待信号量（会阻塞直到重新连接完成）

During steps 3-11, all other operations are blocked. Methods called during reconnection will either return default values or wait for the semaphore.

## 使用示例 (Usage Examples)

### 基本用法 (Basic Usage)

```csharp
var adapter = new LeadshineLtdmcBusAdapter(0, 2, "192.168.1.100");

// 订阅重新连接事件
adapter.ReconnectionStarting += (sender, e) =>
{
    Console.WriteLine("重新连接开始 - 暂停操作");
    // 暂停业务逻辑
    // 保存状态
};

adapter.ReconnectionCompleted += (sender, e) =>
{
    Console.WriteLine("重新连接完成 - 恢复操作");
    // 恢复业务逻辑
    // 恢复状态
};

// 适配器会自动处理来自其他进程的复位通知
await adapter.InitializeAsync();
```

### 与 IO 模块集成 (Integration with IO Module)

```csharp
var adapter = new LeadshineLtdmcBusAdapter(0, 2, "192.168.1.100");
var ioModule = new LeadshineCabinetIoModule(logger, 0, options);

// 在重新连接时自动暂停 IO 轮询
adapter.ReconnectionStarting += (sender, e) =>
{
    ioModule.PausePolling();
};

adapter.ReconnectionCompleted += (sender, e) =>
{
    ioModule.ResumePolling();
};

await adapter.InitializeAsync();
await ioModule.StartAsync(cancellationToken);
```

### 检查重新连接状态 (Checking Reconnection State)

```csharp
if (!adapter.IsReconnecting)
{
    // 安全地执行操作
    var axisCount = await adapter.GetAxisCountAsync();
}
else
{
    // 跳过操作或等待
    Console.WriteLine("适配器正在重新连接，请稍后重试");
}
```

## 技术细节 (Technical Details)

### 线程安全 (Thread Safety)

- `SemaphoreSlim` 用于异步友好的互斥
- `volatile` 关键字用于 `_isReconnecting` 标志，确保可见性
- 所有公共方法都是线程安全的

### 性能考虑 (Performance Considerations)

- 信号量仅在方法调用边界获取，不会长时间持有
- 重新连接期间的操作会立即检查 `_isReconnecting` 并快速返回
- IO 轮询在暂停时只是跳过循环，不消耗 CPU

### 错误处理 (Error Handling)

- 重新连接失败会记录错误但不会抛出异常
- 上层可以通过 `ErrorOccurred` 事件监控错误
- 重新连接失败后，`IsInitialized` 将为 `false`

## 测试 (Testing)

参见 `LeadshineReconnectionTests.cs` 了解基本的 API 验证测试。

完整的集成测试需要：
1. 实际的雷赛控制器硬件
2. 或者模拟的硬件环境
3. 多进程测试环境

See `LeadshineReconnectionTests.cs` for basic API verification tests.

Full integration tests require actual Leadshine controller hardware or a simulated environment.

## 相关文件 (Related Files)

- `LeadshineLtdmcBusAdapter.cs` - 主要实现
- `LeadshineCabinetIoModule.cs` - IO 模块暂停/恢复
- `EmcDistributedLockExample.cs` - 使用示例
- `LeadshineReconnectionTests.cs` - 测试代码

## 更新日志 (Changelog)

### 版本 1.0 (2024)
- 实现操作阻止机制
- 添加自动重新连接流程
- 添加 IO 轮询暂停/恢复
- 添加事件通知系统
- 添加文档和测试
