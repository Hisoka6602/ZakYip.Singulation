# 雷赛（Leadshine）运动控制驱动

## 概述

本目录包含了雷赛（Leadshine）LTDMC 系列运动控制器的驱动实现，支持通过 EtherCAT 总线进行轴控制。该驱动实现了完整的 CiA 402 状态机，提供速度模式控制、状态监测、故障恢复等功能。

## 硬件支持

### 支持的控制器型号
- **LTDMC 系列**：雷赛 LTDMC 系列 EtherCAT 运动控制器
- **通信接口**：以太网（Ethernet）
- **协议**：EtherCAT + CiA 402（IEC 61800-7-201）

### 硬件要求
- **操作系统**：Windows（x64 或 x86）
- **依赖库**：LTDMC.dll（雷赛官方 SDK）
- **网络**：千兆以太网（推荐）
- **控制器 IP**：需要配置控制器的 IP 地址

## 核心组件

### 1. LeadshineLtdmcBusAdapter
**文件**：`LeadshineLtdmcBusAdapter.cs`

**功能**：总线适配器，负责与雷赛控制器建立连接和管理通信。

**主要方法**：
- `InitializeAsync()`：初始化以太网连接（dmc_board_init_eth）
- `GetTotalSlavesAsync()`：获取总线上从站数量
- `GetLastErrorCodeAsync()`：获取最后一次错误代码
- `ResetAsync()`：冷复位控制器
- `CloseAsync()`：关闭连接

**错误处理**：
- 所有底层 SDK 异常均被捕获并转换为错误消息
- 通过 `LastErrorMessage` 属性获取错误信息
- 通过 `ErrorOccurred` 事件订阅错误通知

**示例配置**：
```csharp
var adapter = new LeadshineLtdmcBusAdapter(
    cardNo: 0,
    portNo: 2,
    controllerIp: "192.168.1.100"
);

await adapter.InitializeAsync();
var slaveCount = await adapter.GetTotalSlavesAsync();
```

### 2. LeadshineLtdmcAxisDrive
**文件**：`LeadshineLtdmcAxisDrive.cs`

**功能**：单轴驱动，实现 `IAxisDrive` 接口，提供完整的轴控制功能。

**核心特性**：
- **速度控制**：支持 mm/s 和 RPM 两种单位
- **CiA 402 状态机**：完整实现 402 标准状态机（使能/禁用/故障恢复）
- **PDO 通信**：直接读写 PDO 对象（0x60FF、0x6041 等）
- **速度反馈**：实时读取实际速度（0x606C）并发布事件
- **加减速控制**：支持设置加速度/减速度（0x6083/0x6084）
- **断路器模式**：连续故障后自动降级，定期尝试恢复
- **健康监测**：可选的健康监测功能，自动检测连接状态

**主要接口方法**：
```csharp
// 写入目标速度（mm/s）
await drive.WriteSpeedAsync(1000.0m, ct);

// 设置加减速（mm/s²）
await drive.SetAccelDecelByLinearAsync(500.0m, 500.0m, ct);

// 使能轴
await drive.EnableAsync(ct);

// 禁用轴
await drive.DisableAsync(ct);

// 停止轴
await drive.StopAsync(ct);

// 检测轴状态
var isOk = await drive.PingAsync(ct);
```

**事件订阅**：
```csharp
// 轴故障事件
drive.AxisFaulted += (sender, args) => {
    Console.WriteLine($"轴故障: {args.Error.Message}");
};

// 速度反馈事件
drive.SpeedFeedback += (sender, args) => {
    Console.WriteLine($"轴速度: {args.MmPerSec} mm/s");
};

// 轴断线事件
drive.AxisDisconnected += (sender, args) => {
    Console.WriteLine($"轴断线: {args.Reason}");
};

// 驱动未加载事件
drive.DriverNotLoaded += (sender, args) => {
    Console.WriteLine($"驱动未加载: {args.Reason}");
};
```

### 3. LeadshineProtocolMap
**文件**：`LeadshineProtocolMap.cs`

**功能**：CiA 402 对象字典索引映射。

**常用对象索引**：
- **0x6040**：控制字（ControlWord）
- **0x6041**：状态字（StatusWord）
- **0x6060**：操作模式（ModesOfOperation）
- **0x6061**：操作模式显示（ModesOfOperationDisplay）
- **0x6083**：加速度（ProfileAcceleration）
- **0x6084**：减速度（ProfileDeceleration）
- **0x60FF**：目标速度（TargetVelocity）
- **0x606C**：实际速度（ActualVelocity）
- **0x6502**：支持驱动模式（SupportedDriveModes）

## 驱动配置

### DriverOptions 配置参数

```csharp
public class DriverOptions {
    // 节点标识
    public ushort NodeId { get; set; }                    // 从站节点 ID（通常为 1000 + nodeIndex）
    
    // 机械参数
    public decimal PulleyPitchDiameterMm { get; set; }    // 同步带轮节圆直径（mm）
    public decimal GearRatio { get; set; } = 1.0m;         // 齿轮传动比
    public decimal ScrewPitchMm { get; set; }             // 丝杠螺距（mm）
    
    // 速度和加速度限制
    public decimal MaxSpeedMmPerSec { get; set; }         // 最大速度（mm/s）
    public decimal MaxAccelRpmPerSec { get; set; }        // 最大加速度（RPM/s）
    public decimal MaxDecelRpmPerSec { get; set; }        // 最大减速度（RPM/s）
    
    // 方向控制
    public bool IsReverse { get; set; }                   // 是否反向
    
    // 容错配置
    public int ConsecutiveFailThreshold { get; set; } = 3;  // 连续失败阈值
    public TimeSpan HealthPingInterval { get; set; }       // 健康检查间隔
    public bool EnableHealthMonitor { get; set; }          // 是否启用健康监测
    
    // 节流配置
    public TimeSpan ThrottleInterval { get; set; }         // 命令节流间隔
}
```

**配置示例**：
```json
{
  "DriverOptions": {
    "NodeId": 1000,
    "PulleyPitchDiameterMm": 20.0,
    "GearRatio": 1.0,
    "ScrewPitchMm": 0.0,
    "MaxSpeedMmPerSec": 3000.0,
    "MaxAccelRpmPerSec": 5000.0,
    "MaxDecelRpmPerSec": 5000.0,
    "IsReverse": false,
    "ConsecutiveFailThreshold": 3,
    "HealthPingInterval": "00:00:05",
    "EnableHealthMonitor": true,
    "ThrottleInterval": "00:00:00.050"
  }
}
```

## 通信协议

### PDO 通信
驱动使用 PDO（Process Data Object）进行高速实时通信：

**写 PDO（RxPDO）**：
```csharp
// nmc_write_rxpdo(cardNo, portNo, nodeId, index, subindex, dataLength, data)
```

**读 PDO（TxPDO）**：
```csharp
// nmc_read_txpdo(cardNo, portNo, nodeId, index, subindex, dataLength, data)
```

**固定参数**：
- **cardNo**：卡号，通常为 0
- **portNo**：端口号，固定为 2（EtherCAT）
- **nodeId**：节点 ID = 1000 + nodeIndex

### CiA 402 状态机

驱动实现完整的 CiA 402 状态机流程：

```
[Not Ready to Switch On] 
    ↓ (自动)
[Switch On Disabled]
    ↓ (Shutdown: 0x0006)
[Ready to Switch On]
    ↓ (Switch On: 0x0007)
[Switched On]
    ↓ (Enable Operation: 0x000F)
[Operation Enabled] ← 正常运行状态
    ↓ (Disable Operation: 0x0007)
[Switched On]
    ↓ (Shutdown: 0x0006)
[Ready to Switch On]
    ↓ (Disable Voltage: 0x0000)
[Switch On Disabled]
```

**控制字序列**：
1. `0x0006` - Shutdown（关闭）
2. `0x0007` - Switch On（接通）
3. `0x000F` - Enable Operation（使能操作）
4. `0x0080` - Fault Reset（故障复位，仅在故障时）

### 速度单位换算

**mm/s → PPS（脉冲每秒）**：
```
PPS = (mm/s) × PPR / (π × D × GearRatio)
```
其中：
- PPR：编码器分辨率（Pulses Per Revolution）
- D：同步带轮节圆直径（mm）
- GearRatio：齿轮传动比

**RPM → mm/s**：
```
mm/s = RPM × (π × D) × GearRatio / 60
```

## 错误处理

### 错误码说明
错误码由 `nmc_get_errcode` 返回，常见错误码：

| 错误码 | 说明 |
|--------|------|
| 0 | 成功 |
| -1 | 通信超时 |
| -2 | 参数错误 |
| -3 | 设备未就绪 |
| -1000 | EtherCAT 通信错误 |

### 断路器模式
驱动内置 Polly 断路器，连续失败达到阈值后：
1. 自动进入 `Degraded` 状态
2. 触发 `AxisDisconnected` 事件
3. 如启用健康监测，定期尝试 `PingAsync()` 恢复
4. 恢复成功后自动切换回 `Connected` 状态

### 故障恢复流程
```csharp
// 1. 检测到故障
if (drive.Status == DriverStatus.Degraded) {
    // 2. 尝试故障复位
    await drive.EnableAsync(ct);  // 内部会执行 Fault Reset
    
    // 3. 等待恢复
    await Task.Delay(1000);
    
    // 4. 重新使能
    if (drive.Status == DriverStatus.Connected) {
        await drive.EnableAsync(ct);
    }
}
```

## 性能优化

### 速度反馈节流
为避免过于频繁的事件通知，驱动内置速度反馈节流机制：
- **时间节流**：最小反馈间隔 20ms（可配置）
- **阈值节流**：速度变化小于 1.0 mm/s 时不发布事件

### PPR 缓存
编码器分辨率（PPR）仅在首次使能时读取一次（0x608F），后续使用缓存值，避免频繁读取。

### 命令节流
通过 `ThrottleInterval` 配置命令最小间隔，防止过快写总线导致丢包。

## 调试与故障排查

### 日志输出
驱动使用 `Debug.WriteLine` 输出关键信息：
```csharp
Debug.WriteLine($"[Leadshine] Enable: cardNo={_opts.CardNo}, nodeId={nodeId}, controlWord=0x{ctrlWord:X4}");
```

### 常见问题

**1. 驱动未加载错误**
- **症状**：触发 `DriverNotLoaded` 事件，提示"找不到 LTDMC.dll"
- **原因**：
  - LTDMC.dll 不在程序目录或 PATH 中
  - DLL 位宽不匹配（x64/x86）
  - 缺少 VC++ 运行库
- **解决**：
  - 将 LTDMC.dll 复制到程序根目录
  - 确保 DLL 位宽与程序一致
  - 安装 Visual C++ Redistributable

**2. 连续失败降级**
- **症状**：驱动状态变为 `Degraded`
- **原因**：
  - 网络连接中断
  - 控制器掉电或故障
  - EtherCAT 总线通信异常
- **解决**：
  - 检查网络连接和控制器电源
  - 查看 `LastErrorMessage` 获取详细错误
  - 启用健康监测自动恢复

**3. 速度控制不生效**
- **症状**：写入速度后轴不动
- **原因**：
  - 轴未使能（需先调用 `EnableAsync()`）
  - 操作模式不正确（需为速度模式 0x03）
  - 安全管线拦截（Emergency Stop）
- **解决**：
  - 确保先调用 `EnableAsync()`
  - 检查状态字是否为 `Operation Enabled`
  - 检查安全管线状态

**4. 速度反馈为 0**
- **症状**：实际速度始终为 0
- **原因**：
  - 编码器未连接或损坏
  - 编码器分辨率配置错误
  - 方向参数 `IsReverse` 设置不当
- **解决**：
  - 检查编码器物理连接
  - 验证 PPR 读取是否正确（0x608F）
  - 调整 `IsReverse` 参数

## 最佳实践

1. **使能顺序**：先 `EnableAsync()`，再 `WriteSpeedAsync()`
2. **错误处理**：订阅所有事件，记录错误日志
3. **优雅停止**：程序退出前调用 `StopAsync()` 和 `DisableAsync()`
4. **配置验证**：启动时验证机械参数（PulleyPitchDiameterMm、ScrewPitchMm）
5. **健康监测**：生产环境建议启用 `EnableHealthMonitor = true`
6. **超时控制**：所有异步方法传入 `CancellationToken`，设置合理超时

## 示例代码

### 完整控制流程
```csharp
// 1. 创建总线适配器
var adapter = new LeadshineLtdmcBusAdapter(0, 2, "192.168.1.100");
await adapter.InitializeAsync();

// 2. 创建轴驱动
var options = new DriverOptions {
    NodeId = 1000,
    PulleyPitchDiameterMm = 20.0m,
    MaxSpeedMmPerSec = 3000.0m,
    EnableHealthMonitor = true
};
var drive = new LeadshineLtdmcAxisDrive(options);

// 3. 订阅事件
drive.SpeedFeedback += (s, e) => Console.WriteLine($"Speed: {e.MmPerSec} mm/s");
drive.AxisFaulted += (s, e) => Console.WriteLine($"Fault: {e.Error.Message}");

// 4. 使能轴
await drive.EnableAsync();

// 5. 设置加减速
await drive.SetAccelDecelByLinearAsync(500, 500);

// 6. 写入速度
await drive.WriteSpeedAsync(1000.0m);

// 7. 等待运行
await Task.Delay(5000);

// 8. 停止
await drive.StopAsync();

// 9. 禁用
await drive.DisableAsync();

// 10. 关闭连接
await adapter.CloseAsync();
```

## 技术支持

如遇到问题，请提供以下信息：
- 控制器型号和固件版本
- `LastErrorMessage` 错误信息
- 完整的事件日志
- 配置参数（DriverOptions）
- 网络拓扑和 IP 配置

## 相关文档
- [CiA 402 标准](https://www.can-cia.org/can-knowledge/canopen/cia402/)
- [雷赛官方文档](https://www.leadshine.com/)
- [项目架构文档](../../docs/ARCHITECTURE.md)
- [API 文档](../../docs/API.md)

## EMC 硬件资源分布式锁

### 概述

EMC 驱动硬件是共享资源，允许多个进程实例同时连接。当某个实例执行冷复位或热复位操作时，会影响所有连接到同一 EMC 硬件的进程实例。为了避免冲突和数据丢失，我们实现了**分布式锁机制**和**进程间复位通知系统**。

### 核心组件

#### 1. IEmcResourceLock 接口
**文件**：`IEmcResourceLock.cs`

定义了 EMC 资源分布式锁的标准接口：
- `TryAcquireAsync()`：尝试获取锁（支持超时和取消）
- `Release()`：释放锁
- `IsLockHeld`：检查是否持有锁
- `LockIdentifier`：锁的唯一标识符

#### 2. EmcNamedMutexLock 类
**文件**：`EmcNamedMutexLock.cs`

基于 Windows 命名互斥锁（Named Mutex）的分布式锁实现：
- 使用 `Global\` 命名空间前缀，跨会话可见
- 支持异常退出后的锁恢复（AbandonedMutexException）
- 线程安全，支持幂等操作

**示例**：
```csharp
using var resourceLock = new EmcNamedMutexLock("CardNo_0");

// 尝试获取锁（30 秒超时）
if (await resourceLock.TryAcquireAsync(TimeSpan.FromSeconds(30)))
{
    try
    {
        // 执行需要独占访问的操作
        await PerformResetOperations();
    }
    finally
    {
        // 释放锁
        resourceLock.Release();
    }
}
```

#### 3. EmcResetNotification 类
**文件**：`EmcResetNotification.cs`

封装 EMC 复位通知消息：
- **CardNo**：控制器卡号
- **ResetType**：复位类型（Cold 或 Warm）
- **ProcessId**：发起复位的进程 ID
- **ProcessName**：发起复位的进程名称
- **Timestamp**：通知时间戳
- **EstimatedRecoverySeconds**：预计恢复时间

支持序列化/反序列化，用于进程间传输。

#### 4. EmcResetCoordinator 类
**文件**：`EmcResetCoordinator.cs`

EMC 复位协调器，负责进程间通信：
- 使用**内存映射文件**（Memory-Mapped File）作为 IPC 机制
- 自动轮询接收其他进程的复位通知
- 过滤自己发送的通知，避免循环
- 自动丢弃过期通知（超过 30 秒）

**事件**：
- `ResetNotificationReceived`：当接收到其他进程的复位通知时触发

**示例**：
```csharp
using var coordinator = new EmcResetCoordinator(cardNo: 0);

// 订阅通知事件
coordinator.ResetNotificationReceived += (sender, args) =>
{
    var notification = args.Notification;
    Console.WriteLine($"收到复位通知：类型={notification.ResetType}, " +
                      $"来源={notification.ProcessName}, " +
                      $"预计恢复时间={notification.EstimatedRecoverySeconds}秒");
    
    // 采取应对措施
    PrepareForReset();
};

// 广播复位通知
await coordinator.BroadcastResetNotificationAsync(EmcResetType.Cold);
```

### LeadshineLtdmcBusAdapter 集成

`LeadshineLtdmcBusAdapter` 已集成分布式锁和复位协调器：

#### 自动功能
1. **构造函数**：自动创建分布式锁和复位协调器
2. **ResetAsync()**：执行冷复位时：
   - 获取分布式锁（确保独占访问）
   - 广播复位通知到其他进程
   - 等待其他进程准备（500ms）
   - 执行冷复位操作
   - 释放分布式锁
3. **WarmResetAsync()**：执行热复位时：
   - 获取分布式锁
   - 广播复位通知
   - 等待其他进程准备（300ms）
   - 执行热复位操作
   - 释放分布式锁

#### 新增事件
```csharp
// 订阅复位通知事件
adapter.EmcResetNotificationReceived += (sender, args) =>
{
    var notification = args.Notification;
    _logger.Warn($"收到 EMC 复位通知：类型={notification.ResetType}, " +
                 $"来源={notification.ProcessName}({notification.ProcessId}), " +
                 $"预计恢复时间={notification.EstimatedRecoverySeconds}秒");
    
    // 应对措施：
    // 1. 暂停当前操作
    // 2. 保存状态
    // 3. 等待复位完成
    // 4. 重新初始化连接
};
```

### 使用场景

#### 场景 1：多实例环境
多个应用程序实例同时连接到同一个 EMC 控制器：

```
实例 A (进程 1234)  ──┐
实例 B (进程 5678)  ──┼──→ EMC 控制器 (CardNo: 0)
实例 C (进程 9012)  ──┘
```

**问题**：如果实例 A 执行冷复位，实例 B 和 C 的连接会失效。

**解决方案**：
1. 实例 A 获取分布式锁
2. 实例 A 广播复位通知
3. 实例 B 和 C 收到通知，暂停操作并保存状态
4. 实例 A 执行复位并等待恢复
5. 实例 A 释放锁
6. 实例 B 和 C 重新初始化连接

#### 场景 2：错误恢复
当 EMC 控制器出现错误（错误码非 0）时，需要执行复位：

```csharp
var errorCode = await adapter.GetErrorCodeAsync();
if (errorCode != 0)
{
    // 执行冷复位（会自动处理锁和通知）
    await adapter.ResetAsync();
}
```

其他实例会自动收到通知并准备应对。

### 最佳实践

1. **订阅通知事件**：
   ```csharp
   adapter.EmcResetNotificationReceived += OnResetNotification;
   ```

2. **实现应对策略**：
   ```csharp
   private async void OnResetNotification(object? sender, EmcResetEventArgs e)
   {
       // 1. 暂停所有运动控制操作
       await StopAllAxes();
       
       // 2. 保存当前状态到持久化存储
       await SaveState();
       
       // 3. 等待预计的恢复时间
       await Task.Delay(TimeSpan.FromSeconds(e.Notification.EstimatedRecoverySeconds + 2));
       
       // 4. 重新初始化连接
       await adapter.InitializeAsync();
       
       // 5. 恢复状态
       await RestoreState();
   }
   ```

3. **超时处理**：
   ```csharp
   var lockTimeout = TimeSpan.FromSeconds(30);
   if (!await resourceLock.TryAcquireAsync(lockTimeout))
   {
       _logger.Error("无法获取 EMC 资源锁，可能有其他实例正在执行复位");
       throw new TimeoutException("EMC resource lock acquisition timeout");
   }
   ```

4. **异常安全**：
   ```csharp
   using var resourceLock = new EmcNamedMutexLock("CardNo_0");
   try
   {
       await resourceLock.TryAcquireAsync(TimeSpan.FromSeconds(30));
       // 执行操作
   }
   finally
   {
       resourceLock.Release(); // 确保锁被释放
   }
   ```

### 技术细节

#### 命名互斥锁
- **命名格式**：`Global\ZakYip_EMC_CardNo_{cardNo}`
- **作用域**：系统范围（跨会话）
- **特性**：
  - 自动处理放弃的互斥锁（AbandonedMutexException）
  - 线程安全
  - 支持超时和取消令牌

#### 内存映射文件
- **命名格式**：`Global\ZakYip_EMC_Reset_Card{cardNo}`
- **大小**：4KB
- **结构**：
  - 前 4 字节：数据长度
  - 后续字节：序列化的通知消息
- **轮询间隔**：500ms（可配置）

#### 通知消息格式
```
{CardNo}|{ResetType}|{ProcessId}|{ProcessName}|{Timestamp}
```
例如：
```
0|Cold|1234|MyApp|2024-01-01T12:00:00.0000000Z
```

### 故障排查

#### 问题 1：无法获取锁超时
**症状**：`TryAcquireAsync()` 返回 false

**原因**：
- 其他进程正在执行复位操作
- 前一个进程异常退出未释放锁

**解决**：
- 增加超时时间
- 检查是否有其他进程在使用 EMC
- 重启所有相关进程

#### 问题 2：收不到复位通知
**症状**：`EmcResetNotificationReceived` 事件未触发

**原因**：
- 协调器未启用轮询（`enablePolling: false`）
- 通知已过期（超过 30 秒）
- 内存映射文件权限问题

**解决**：
- 确保 `enablePolling: true`
- 检查系统时间同步
- 以管理员权限运行

#### 问题 3：锁未释放
**症状**：后续操作一直超时

**原因**：
- 进程异常退出未释放锁
- `finally` 块未执行

**解决**：
- 使用 `using` 语句确保释放
- 检查进程是否正常退出
- 手动重启所有进程

### 性能影响

- **锁获取延迟**：< 1ms（正常情况）
- **锁释放延迟**：< 1ms
- **通知广播延迟**：< 5ms
- **通知接收延迟**：< 500ms（取决于轮询间隔）
- **内存占用**：每个协调器约 4KB

### 安全性

- **命名空间隔离**：使用 `ZakYip_EMC_` 前缀避免冲突
- **过期检查**：自动丢弃超过 30 秒的通知
- **进程过滤**：不处理自己发送的通知
- **异常恢复**：处理放弃的互斥锁

### 相关文档
- [CiA 402 标准](https://www.can-cia.org/can-knowledge/canopen/cia402/)
- [Windows 命名互斥锁](https://learn.microsoft.com/en-us/windows/win32/sync/using-named-objects)
- [内存映射文件](https://learn.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files)
