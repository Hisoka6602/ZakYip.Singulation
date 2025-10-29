# IO按钮启动和停止触发后整个流程执行慢 - 性能分析报告

## 问题描述
IO按钮触发启动和停止操作后，整个流程执行很慢，需要分析性能瓶颈。

## 关键发现

### 1. **顺序执行所有轴的操作 - 主要性能瓶颈**

#### 位置
文件：`ZakYip.Singulation.Drivers/Common/AxisController.cs`
方法：`ForEachDriveAsync` (第101-114行)

#### 问题
```csharp
private async Task ForEachDriveAsync(Func<IAxisDrive, Task> action, CancellationToken ct) {
    foreach (var d in _drives) {
        ct.ThrowIfCancellationRequested();
        try {
            await action(d);  // 串行等待每个轴完成
        }
        catch (Exception ex) {
            OnControllerFaulted($"Drive {d.Axis}: {ex.Message}");
        }

        // 间隔至少 2ms，避免指令过于密集
        await Task.Delay(2, ct);  // 每个轴之间额外延迟2ms
    }
}
```

#### 影响分析
此方法被以下操作调用：
- `EnableAllAsync()` - 启动流程步骤1
- `DisableAllAsync()` - 停止流程步骤2
- `WriteSpeedAllAsync()` - 启动流程步骤2和停止流程步骤1
- `StopAllAsync()` - 紧急停止
- `SetAccelDecelAllAsync()` - 加减速度设置

**性能影响计算（假设有10个轴）：**
- 启动流程：
  - EnableAllAsync: 10个轴 × (Enable耗时 + 2ms延迟)
  - WriteSpeedAllAsync: 10个轴 × (WriteSpeed耗时 + 2ms延迟)
  - 总计：至少 20ms 延迟 + 所有操作的累计时间

- 停止流程：
  - WriteSpeedAllAsync(0): 10个轴 × (WriteSpeed耗时 + 2ms延迟)
  - DisableAllAsync: 10个轴 × (Disable耗时 + 2ms延迟)
  - 总计：至少 20ms 延迟 + 所有操作的累计时间

### 2. **每个轴的Enable操作内部延迟**

#### 位置
文件：`ZakYip.Singulation.Drivers/Leadshine/LeadshineLtdmcAxisDrive.cs`
方法：`EnableAsync` (第358-409行)

#### 问题
Enable操作内部包含多个延迟：
```csharp
public async Task EnableAsync(CancellationToken ct = default) {
    // 0) 清除报警 → 清零
    if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.FaultReset, 
        LeadshineProtocolMap.DelayMs.AfterFaultReset)) return;
    if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.Clear, 
        LeadshineProtocolMap.DelayMs.AfterClear)) return;

    // 1) 设置模式：延迟
    await Task.Delay(LeadshineProtocolMap.DelayMs.AfterSetMode, ct);

    // 2) 402 状态机三步 - 每步之间都有延迟
    if (!await WriteCtrlAsync(..., LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
    if (!await WriteCtrlAsync(..., LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
    if (!await WriteCtrlAsync(..., LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;

    // 3) 读取PPR
    var ppr = await ReadAxisPulsesPerRevAsync(ct);
}
```

需要查看 `LeadshineProtocolMap` 中的延迟常量值来计算具体影响。

### 3. **Disable操作也包含延迟**

#### 位置
文件：`ZakYip.Singulation.Drivers/Leadshine/LeadshineLtdmcAxisDrive.cs`
方法：`DisableAsync` (第412-437行)

#### 问题
```csharp
public async ValueTask DisableAsync(CancellationToken ct = default) {
    await ThrottleAsync(ct);
    
    // 停止速度
    _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0, suppressLog: true);
    var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, (ushort)0x0002);
    // ...
    await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, ct);  // 延迟

    var cw = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, 
        LeadshineProtocolMap.ControlWord.Shutdown);
    // ...
    await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, ct);  // 又一次延迟
}
```

### 4. **写入速度操作的节流（Throttle）**

#### 位置
文件：`ZakYip.Singulation.Drivers/Leadshine/LeadshineLtdmcAxisDrive.cs`
方法：`ThrottleAsync` (第563-577行)

#### 问题
```csharp
private async ValueTask ThrottleAsync(CancellationToken ct) {
    var now = Stopwatch.GetTimestamp();
    var min = (long)Math.Round(_opts.MinWriteInterval.TotalSeconds * Stopwatch.Frequency);
    var last = Interlocked.Read(ref _lastStamp);
    var delta = now - last;

    if (delta < min) {
        var waitTicks = min - delta;
        var waitMs = (int)Math.Ceiling(waitTicks * 1000.0 / Stopwatch.Frequency);
        if (waitMs > 0) await Task.Delay(waitMs, ct);  // 根据 MinWriteInterval 延迟
        now = Stopwatch.GetTimestamp();
    }

    Interlocked.Exchange(ref _lastStamp, now);
}
```

每次写入操作（WriteSpeed、Enable、Disable等）都会先调用 ThrottleAsync，如果距离上次操作时间过短会额外等待。

### 5. **启动流程的复杂性**

#### 位置
文件：`ZakYip.Singulation.Infrastructure/Safety/SafetyPipeline.cs`
方法：`HandleCommandAsync` - Start命令处理 (第318-376行)

#### 启动流程步骤
```
1. 调用 IAxisController.EnableAllAsync() - 顺序使能所有轴
   └─> 每个轴：Enable操作 + 2ms延迟
   
2. 根据模式设置速度
   本地模式：
   └─> 读取配置 (await _controllerOptionsStore.GetAsync)
   └─> 调用 IAxisController.WriteSpeedAllAsync(fixedSpeed)
       └─> 每个轴：WriteSpeed操作 + 2ms延迟
   
   远程模式：
   └─> 等待 Upstream 推送速度（无操作）
   
3. 更新指示灯状态
   └─> 调用 IndicatorLightService.UpdateStateAsync(Running)
       └─> UpdateTriColorLightsAsync (并行写3个灯)
       └─> UpdateButtonLightsAsync (并行写2个灯)
```

### 6. **停止流程的复杂性**

#### 位置
文件：`ZakYip.Singulation.Infrastructure/Safety/SafetyPipeline.cs`
方法：`HandleCommandAsync` - Stop命令处理 (第377-420行)

#### 停止流程步骤（正常停止，非急停）
```
1. 设置所有轴速度为0
   └─> 调用 IAxisController.WriteSpeedAllAsync(0)
       └─> 每个轴：WriteSpeed(0)操作 + 2ms延迟
       
2. 禁用所有轴使能
   └─> 调用 IAxisController.DisableAllAsync()
       └─> 每个轴：Disable操作 + 2ms延迟
       
3. 更新指示灯状态
   └─> 调用 IndicatorLightService.UpdateStateAsync(Stopped)
       └─> UpdateTriColorLightsAsync (并行写3个灯)
       └─> UpdateButtonLightsAsync (并行写2个灯)
```

### 7. **复位流程的复杂性**

#### 位置
文件：`ZakYip.Singulation.Infrastructure/Safety/SafetyPipeline.cs`
方法：`HandleCommandAsync` - Reset命令处理 (第421-457行)

#### 复位流程步骤
```
1. 设置所有轴速度为0
   └─> 调用 IAxisController.WriteSpeedAllAsync(0)
       
2. 禁用所有轴使能
   └─> 调用 IAxisController.DisableAllAsync()
       
3. 清除控制器错误
   └─> 调用 IAxisController.Bus.ResetAsync()
   
4. 重置安全隔离状态
   └─> ISafetyIsolator.TryResetIsolation() 或 TryRecoverFromDegraded()
   
5. 更新指示灯状态
   └─> 调用 IndicatorLightService.UpdateStateAsync(Ready)
```

## 性能瓶颈总结

### 主要瓶颈（按影响程度排序）

1. **串行执行所有轴操作** - 最严重
   - 影响：启动、停止、写速度等所有批量操作
   - 延迟：N个轴 × (单轴操作时间 + 2ms)
   - 示例：10个轴的启动 = 10×(Enable时间 + 2ms) + 10×(WriteSpeed时间 + 2ms)

2. **Enable/Disable操作内部的多次延迟** - 严重
   - 每个轴的Enable操作包含多个延迟步骤
   - 每个轴的Disable操作包含2次延迟
   - 这些延迟会随着串行执行而累加

3. **ThrottleAsync节流机制** - 中等
   - 根据 MinWriteInterval 配置限制操作频率
   - 在快速连续操作时会引入额外等待

4. **IO轮询间隔** - 较轻
   - LeadshineSafetyIoModule的轮询间隔默认50ms
   - 影响IO按钮响应延迟，但不影响流程执行速度

## 性能优化建议

### 高优先级优化

1. **并行化轴操作**
   - 修改 `ForEachDriveAsync` 为并行执行
   - 使用 `Task.WhenAll` 同时操作所有轴
   - 可以保留轻微的随机延迟避免总线拥塞

2. **优化Enable/Disable的延迟策略**
   - 检查 LeadshineProtocolMap 中的延迟常量
   - 评估是否可以减少延迟时间
   - 考虑使用动态延迟（根据实际响应调整）

### 中优先级优化

3. **优化ThrottleAsync策略**
   - 批量操作时可以豁免或减少节流
   - 为批量操作和单个操作使用不同的节流策略

4. **异步化配置读取**
   - 启动流程中的 `_controllerOptionsStore.GetAsync` 可以提前缓存
   - 避免在关键路径上进行IO操作

### 低优先级优化

5. **调整IO轮询间隔**
   - 如果需要更快的响应，可以降低PollingIntervalMs（当前50ms）
   - 但这主要影响响应时间，不影响流程执行时间

## 具体延迟时间估算

### 延迟常量（来自 LeadshineProtocolMap.DelayMs）
```csharp
AfterFaultReset = 10ms      // Enable流程步骤0.1
AfterClear = 5ms            // Enable流程步骤0.2
AfterSetMode = 5ms          // Enable流程步骤1
BetweenStateCmds = 10ms     // Enable流程步骤2（3次） + Disable流程（2次）
```

### 其他配置（来自 DriverOptions）
```csharp
MinWriteInterval = 5ms      // 每次写操作的最小间隔（节流）
```

### 单个轴的操作时间估算

#### Enable操作
```
ThrottleAsync: 0-5ms (取决于上次操作时间)
+ FaultReset写入 + 10ms延迟
+ Clear写入 + 5ms延迟
+ SetMode写入 + 5ms延迟
+ Shutdown写入 + 10ms延迟
+ SwitchOn写入 + 10ms延迟
+ EnableOperation写入 + 10ms延迟
+ ReadAxisPulsesPerRevAsync (2次OD读取)
≈ 55-60ms + 网络/硬件响应时间
```

#### Disable操作
```
ThrottleAsync: 0-5ms
+ TargetVelocity写入(0)
+ ControlWord写入(0x0002) + 10ms延迟
+ ControlWord写入(Shutdown) + 10ms延迟
≈ 20-25ms + 网络/硬件响应时间
```

#### WriteSpeed操作
```
ThrottleAsync: 0-5ms
+ TargetVelocity写入
≈ 5ms + 网络/硬件响应时间
```

### 完整流程时间估算（假设10个轴）

#### 启动流程（本地模式）
```
步骤1: EnableAllAsync
  = 10个轴 × (55-60ms + 2ms ForEachDrive延迟)
  = 10个轴 × 57-62ms
  = 570-620ms

步骤2: WriteSpeedAllAsync(固定速度)
  = 10个轴 × (5ms + 2ms ForEachDrive延迟)
  = 10个轴 × 7ms
  = 70ms

步骤3: UpdateStateAsync(Running)
  = 并行写5个灯 (红黄绿 + 启动停止按钮灯)
  ≈ 10-20ms

总计: 650-710ms
```

#### 停止流程（正常停止）
```
步骤1: WriteSpeedAllAsync(0)
  = 10个轴 × (5ms + 2ms ForEachDrive延迟)
  = 70ms

步骤2: DisableAllAsync
  = 10个轴 × (20-25ms + 2ms ForEachDrive延迟)
  = 10个轴 × 22-27ms
  = 220-270ms

步骤3: UpdateStateAsync(Stopped)
  = 并行写5个灯
  ≈ 10-20ms

总计: 300-360ms
```

#### 复位流程
```
步骤1: WriteSpeedAllAsync(0) = 70ms
步骤2: DisableAllAsync = 220-270ms
步骤3: Bus.ResetAsync() ≈ 10-50ms
步骤4: ISafetyIsolator操作 ≈ 1-5ms
步骤5: UpdateStateAsync(Ready) = 10-20ms

总计: 311-415ms
```

### 性能影响结论

对于10个轴的系统：
- **启动操作需要约 650-710ms**
- **停止操作需要约 300-360ms**
- **复位操作需要约 311-415ms**

这些时间中，大部分来自于：
1. 串行执行导致的时间累加
2. Enable操作内部的多次延迟（每个轴55-60ms）
3. ForEachDrive中的2ms间隔延迟（每个轴）

**如果并行化所有轴操作，理论上可以将时间缩短到单个轴的操作时间：**
- 启动时间：从 650-710ms → 60-80ms (约**节省90%**)
- 停止时间：从 300-360ms → 30-50ms (约**节省90%**)
- 复位时间：从 311-415ms → 50-100ms (约**节省75-85%**)

## 建议的后续步骤

1. **测量实际延迟**
   - 在关键路径添加计时日志
   - 测量10个轴的实际启动/停止时间

2. **查看延迟常量**
   - 检查 LeadshineProtocolMap 中的延迟配置
   - 确认这些延迟是否必要

3. **评估并行化可行性**
   - 确认EtherCAT总线是否支持并行写入
   - 评估并行操作的风险

4. **性能测试**
   - 对比串行和并行实现的性能差异
   - 验证并行化不会导致总线错误或设备故障

## 结论

**主要原因：所有轴操作都是串行执行的，每个轴之间还有2ms的人为延迟，加上每个轴Enable操作内部包含55-60ms的延迟。**

### 具体性能数据（10个轴的系统）

1. **启动流程耗时：650-710ms**
   - EnableAllAsync: 570-620ms (主要瓶颈)
   - WriteSpeedAllAsync: 70ms
   - UpdateStateAsync: 10-20ms

2. **停止流程耗时：300-360ms**
   - WriteSpeedAllAsync(0): 70ms
   - DisableAllAsync: 220-270ms (主要瓶颈)
   - UpdateStateAsync: 10-20ms

3. **复位流程耗时：311-415ms**

### 性能瓶颈的根本原因

1. **串行执行模式**（AxisController.ForEachDriveAsync）
   - 每个轴必须等待前一个轴完成
   - 时间随轴数线性增长
   - 每个轴之间强制延迟2ms

2. **Enable操作的固定延迟**（每个轴55-60ms）
   - FaultReset + 10ms
   - Clear + 5ms
   - SetMode + 5ms
   - 状态机3步，每步 + 10ms (共30ms)
   - PPR读取操作

3. **ThrottleAsync节流机制**（每次写操作5ms）
   - 防止命令过快的保护机制
   - 在批量操作时累加效果明显

### 优化潜力

**如果实现并行化，可以获得约90%的性能提升：**
- 启动时间：650-710ms → 60-80ms
- 停止时间：300-360ms → 30-50ms
- 复位时间：311-415ms → 50-100ms

**最有效的优化方案是将轴操作并行化，同时适当调整延迟策略。**
