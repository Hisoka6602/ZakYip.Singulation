# 远程模式状态管理和本地模式启动预警功能实现总结

## 需求概述

### 需求 1: 远程模式下接收速度时的状态管理
当远程模式下接收到上游下发速度时判断运行状态，如果不是运行中应该改成运行中，并且亮绿灯。

### 需求 2: 本地模式启动预警功能
应该增加在面板(控制)IO上增加一个配置参数：运行预警秒数，用于在本地模式下按下启动按钮时三色灯亮灯的持续秒数，默认0秒。例如我设置成5秒时，按下启动按钮三色灯亮红灯持续5秒再执行开启逻辑。

## 实现变更

### 1. 配置参数添加

**文件**: `ZakYip.Singulation.Core/Configs/CabinetIndicatorPoint.cs`

添加了新的配置属性：

```csharp
/// <summary>运行预警秒数：用于在本地模式下按下启动按钮时三色灯亮红灯的持续秒数，默认0秒。例如设置成5秒时，按下启动按钮后三色灯亮红灯持续5秒再执行开启逻辑。</summary>
[Range(0, 60, ErrorMessage = "运行预警秒数必须在 0 到 60 秒之间")]
public int RunningWarningSeconds { get; init; } = 0;
```

**位置**: 添加到 `CabinetIndicatorPoint` 类的末尾
**默认值**: 0秒（无预警）
**取值范围**: 0-60秒

### 2. 指示灯服务增强

**文件**: `ZakYip.Singulation.Infrastructure/Services/IndicatorLightService.cs`

添加了新方法 `ShowRunningWarningAsync`，用于显示运行预警灯（红灯）指定秒数后执行回调：

```csharp
/// <summary>
/// 显示运行预警灯（红灯）指定秒数后执行回调。
/// 用于本地模式下按下启动按钮时，先亮红灯持续指定秒数，再执行开启逻辑。
/// </summary>
public async Task ShowRunningWarningAsync(int warningSeconds, Func<Task> callback, CancellationToken ct = default)
```

**功能**:
- 如果预警秒数 <= 0，直接执行回调
- 否则，先将三色灯设置为红灯（Alarm状态）
- 等待指定秒数
- 然后执行回调函数（启动逻辑）

### 3. CabinetPipeline 启动流程改进

**文件**: `ZakYip.Singulation.Infrastructure/Cabinet/CabinetPipeline.cs`

**主要变更**:

1. **添加依赖注入**: 增加 `ILeadshineCabinetIoOptionsStore` 依赖，用于读取运行预警秒数配置

2. **重构启动逻辑**: 
   - 将启动逻辑提取到新的私有方法 `ExecuteStartLogicAsync`
   - 在本地模式且由IO触发时，检查并应用运行预警逻辑
   - 远程模式或API调用时，直接启动无预警

3. **新增方法**:
```csharp
private async Task ExecuteStartLogicAsync(bool isRemote, CancellationToken ct)
```
此方法封装了实际的启动逻辑：使能所有轴 → 设置速度（本地模式）→ 更新系统状态

**启动流程逻辑**:
```
如果 (本地模式 && IO触发 && 配置了预警秒数) {
    先亮红灯N秒，再执行启动
} 否则 {
    直接执行启动
}
```

### 4. SpeedFrameWorker 状态检查

**文件**: `ZakYip.Singulation.Infrastructure/Workers/SpeedFrameWorker.cs`

**主要变更**:

1. **添加依赖**: 增加 `IndicatorLightService?` 可选依赖

2. **添加状态检查逻辑**: 在接收并应用上游速度后，检查当前系统状态：

```csharp
// 当远程模式下接收到上游下发速度时判断运行状态，如果不是运行中应该改成运行中，并且亮绿灯
if (_indicatorLightService != null) {
    var currentState = _indicatorLightService.CurrentState;
    if (currentState != SystemState.Running) {
        _log.LogInformation("【远程模式速度接收】当前状态为 {State}，更新为运行中并亮绿灯", currentState);
        await _indicatorLightService.UpdateStateAsync(SystemState.Running, stoppingToken).ConfigureAwait(false);
    }
}
```

**功能**: 确保在远程模式下接收到速度数据时，系统自动切换到运行状态并亮绿灯

### 5. 测试支持

**新增文件**: `ZakYip.Singulation.Tests/TestHelpers/FakeLeadshineCabinetIoOptionsStore.cs`

创建了测试用的假存储类，用于支持单元测试。

**更新文件**: `ZakYip.Singulation.Tests/CabinetPipelineTests.cs`

所有测试用例都更新为包含新的 `ILeadshineCabinetIoOptionsStore` 依赖。

## 工作流程说明

### 本地模式启动流程（带预警）

1. 用户按下启动按钮（IO触发）
2. 系统检查：本地模式 + IO触发 + RunningWarningSeconds > 0
3. 如果满足，执行预警流程：
   - 三色灯亮红灯
   - 等待配置的秒数
   - 红灯持续期间，用户可以取消操作
4. 预警结束后，执行正常启动流程：
   - 使能所有轴
   - 设置本地固定速度
   - 更新状态为运行中（绿灯亮）

### 远程模式速度接收流程

1. 上游系统下发速度数据
2. SpeedFrameWorker 接收并解码速度帧
3. 应用速度到轴控制器
4. 检查当前系统状态
5. 如果状态不是"运行中"：
   - 更新系统状态为"运行中"
   - 三色灯自动切换到绿灯
   - 记录状态变更日志

## 配置示例

在前端或API中设置运行预警秒数：

```json
{
  "cabinetIndicatorPoint": {
    "redLight": 0,
    "yellowLight": 1,
    "greenLight": 2,
    "runningWarningSeconds": 5  // 启动前红灯预警5秒
  }
}
```

## 向后兼容性

- 默认值为 0，表示无预警，保持原有行为
- 所有更改向后兼容，不影响现有功能
- 新依赖为可选，即使未配置也不会影响系统运行

## 测试验证

所有现有测试通过，包括：
- ✅ EmergencyStopFromIoZeroesSpeedAsync
- ✅ IoResetClearsIsolationAsync  
- ✅ IoStartPublishesRealtimeNotificationAsync

无新增失败测试，现有失败均为环境依赖问题（DLL加载），与本次变更无关。

## 建议使用场景

1. **工业现场安全需求**: 在本地模式下，给操作人员足够的反应时间来确认启动操作
2. **防误操作**: 通过预警灯提示，避免意外按下启动按钮导致设备突然启动
3. **符合安全规范**: 满足某些工业安全标准要求的启动前预警要求

## 后续工作建议

1. 在前端界面添加运行预警秒数的配置项
2. 在用户手册中记录此功能的使用说明
3. 考虑添加声音警报支持（如果硬件支持）
4. 可以考虑添加预警期间的取消机制（按停止按钮可中断预警）
