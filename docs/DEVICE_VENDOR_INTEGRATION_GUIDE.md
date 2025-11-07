# 设备厂商集成指南

## 概述

本文档说明如何将新的设备厂商（如倍福 Beckhoff、西门子 Siemens、联诚 Liancheng 等）集成到 ZakYip.Singulation 系统中。

## 架构概述

系统采用分层架构和接口抽象，实现设备驱动的解耦：

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│              (Host, Controllers, Services)                  │
└────────────────────────┬────────────────────────────────────┘
                         │ 依赖接口，不依赖具体实现
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  Abstraction Layer                          │
│  IAxisDrive, IAxisController, IBusAdapter, IDriveRegistry  │
└────────────────────────┬────────────────────────────────────┘
                         │ 由各厂商实现
                         ↓
┌─────────────────────────────────────────────────────────────┐
│              Vendor-Specific Implementations                │
│   ┌──────────┐  ┌───────────┐  ┌────────┐  ┌──────────┐   │
│   │Leadshine │  │  Beckhoff │  │Siemens │  │Liancheng │   │
│   │  (雷赛)  │  │  (倍福)   │  │(西门子)│  │ (联诚)   │   │
│   └──────────┘  └───────────┘  └────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 核心接口说明

### 1. IAxisDrive - 单轴驱动接口

**位置**: `ZakYip.Singulation.Drivers/Abstractions/IAxisDrive.cs`

**职责**: 定义单个轴的控制接口，包括速度设置、使能/禁用、加减速设置等。

**核心方法**:
- `WriteSpeedAsync(decimal mmPerSec)` - 设置目标速度
- `SetAccelDecelByLinearAsync(decimal accel, decimal decel)` - 设置加减速度
- `EnableAsync()` / `DisableAsync()` - 使能/禁用轴
- `StopAsync()` - 停止轴运动
- `UpdateLinearLimitsAsync()` - 更新速度限制
- `UpdateMechanicsAsync()` - 更新机械参数（PPR、齿轮比、滚筒直径）

**核心属性**:
- `AxisId Axis` - 轴标识
- `DriverStatus Status` - 驱动状态
- `decimal? LastTargetMmps` - 最近目标速度
- `decimal? LastFeedbackMmps` - 最近反馈速度
- `bool IsEnabled` - 是否使能
- `int LastErrorCode` - 最近错误码
- `int? Ppr` - 每转脉冲数

**核心事件**:
- `AxisFaulted` - 轴故障事件
- `AxisDisconnected` - 轴断线事件
- `SpeedFeedback` - 速度反馈事件
- `CommandIssued` - 命令下发事件

### 2. IAxisController - 轴群控制器接口

**位置**: `ZakYip.Singulation.Drivers/Abstractions/IAxisController.cs`

**职责**: 管理多个轴的批量操作。

**核心方法**:
- `InitializeAsync(vendor, template, overrideAxisCount)` - 初始化总线和创建轴
- `EnableAllAsync()` / `DisableAllAsync()` - 批量使能/禁用
- `WriteSpeedAllAsync(decimal mmPerSec)` - 批量设置速度
- `SetAccelDecelAllAsync(accel, decel)` - 批量设置加减速度
- `StopAllAsync()` - 批量停止
- `ApplySpeedSetAsync(SpeedSet set)` - 应用速度集
- `DisposeAllAsync()` - 释放所有资源

**核心属性**:
- `IBusAdapter Bus` - 总线适配器
- `IReadOnlyList<IAxisDrive> Drives` - 轴驱动列表
- `IReadOnlyList<decimal?> TargetSpeedsMmps` - 目标速度数组
- `IReadOnlyList<decimal?> RealtimeSpeedsMmps` - 实时速度数组

### 3. IBusAdapter - 总线适配器接口

**位置**: `ZakYip.Singulation.Drivers/Abstractions/IBusAdapter.cs`

**职责**: 封装总线通信细节（如 EtherCAT、Profinet、CANopen 等）。

**核心方法**:
- `ConnectAsync()` - 连接总线
- `DisconnectAsync()` - 断开总线
- `ScanNodesAsync()` - 扫描总线节点
- `ReadSdoAsync()` / `WriteSdoAsync()` - 读写 SDO（服务数据对象）
- `ReadPdoAsync()` / `WritePdoAsync()` - 读写 PDO（过程数据对象）

### 4. IDriveRegistry - 驱动注册表接口

**位置**: `ZakYip.Singulation.Drivers/Abstractions/IDriveRegistry.cs`

**职责**: 根据厂商标识创建对应的轴驱动实例（工厂模式）。

**核心方法**:
- `CreateDrive(vendor, nodeId, template)` - 创建指定厂商的轴驱动

## 集成新厂商的步骤

### 步骤 1: 创建厂商目录

在 `ZakYip.Singulation.Drivers` 项目下创建新厂商目录：

```
ZakYip.Singulation.Drivers/
├── Leadshine/           # 现有：雷赛
├── Beckhoff/            # 新增：倍福
├── Siemens/             # 新增：西门子
└── Liancheng/           # 新增：联诚
```

### 步骤 2: 实现 IBusAdapter

创建厂商特定的总线适配器，例如 `BeckhoffEthercatBusAdapter.cs`:

```csharp
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Drivers.Beckhoff {
    /// <summary>
    /// 倍福 EtherCAT 总线适配器
    /// </summary>
    public sealed class BeckhoffEthercatBusAdapter : IBusAdapter {
        public string Vendor => "Beckhoff";
        public BusType Type => BusType.Ethercat;
        public BusStatus Status { get; private set; }

        public async Task<bool> ConnectAsync(CancellationToken ct = default) {
            // 实现倍福 EtherCAT 连接逻辑
            // 例如：初始化 TwinCAT ADS 连接
            try {
                // TODO: 连接到 TwinCAT runtime
                Status = BusStatus.Connected;
                return true;
            }
            catch (Exception ex) {
                throw new HardwareCommunicationException(
                    "倍福总线连接失败", ex);
            }
        }

        public async Task<bool> DisconnectAsync(CancellationToken ct = default) {
            // 实现断开逻辑
            Status = BusStatus.Disconnected;
            return true;
        }

        public async Task<int> ScanNodesAsync(CancellationToken ct = default) {
            // 扫描 EtherCAT 从站
            // TODO: 通过 TwinCAT System Manager 获取从站列表
            return 0;
        }

        public async Task<byte[]?> ReadSdoAsync(
            int nodeId, ushort index, byte subIndex, 
            CancellationToken ct = default) {
            // 实现 SDO 读取（通过 ADS 命令）
            throw new NotImplementedException();
        }

        public async Task<bool> WriteSdoAsync(
            int nodeId, ushort index, byte subIndex, byte[] data,
            CancellationToken ct = default) {
            // 实现 SDO 写入
            throw new NotImplementedException();
        }

        public async Task<byte[]?> ReadPdoAsync(
            int nodeId, PdoType type, CancellationToken ct = default) {
            // 实现 PDO 读取（从过程映像）
            throw new NotImplementedException();
        }

        public async Task<bool> WritePdoAsync(
            int nodeId, PdoType type, byte[] data,
            CancellationToken ct = default) {
            // 实现 PDO 写入（到过程映像）
            throw new NotImplementedException();
        }
    }
}
```

### 步骤 3: 实现 IAxisDrive

创建厂商特定的轴驱动，例如 `BeckhoffAxisDrive.cs`:

```csharp
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Drivers.Beckhoff {
    /// <summary>
    /// 倍福伺服轴驱动
    /// </summary>
    public sealed class BeckhoffAxisDrive : IAxisDrive {
        private readonly IBusAdapter _bus;
        private readonly int _nodeId;
        private readonly DriverOptions _options;

        public BeckhoffAxisDrive(
            IBusAdapter bus,
            int nodeId,
            DriverOptions options) {
            _bus = bus;
            _nodeId = nodeId;
            _options = options;
            Axis = new AxisId(options.AxisId);
        }

        // 实现 IAxisDrive 接口
        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;
        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;
        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;
        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;
        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        public AxisId Axis { get; }
        public DriverStatus Status { get; private set; }
        public decimal? LastTargetMmps { get; private set; }
        public decimal? LastFeedbackMmps { get; private set; }
        public bool IsEnabled { get; private set; }
        public int LastErrorCode { get; private set; }
        public string? LastErrorMessage { get; private set; }
        public decimal? MaxLinearMmps { get; private set; }
        public decimal? MaxAccelMmps2 { get; private set; }
        public decimal? MaxDecelMmps2 { get; private set; }
        public AxisType AxisType { get; set; }
        public int? Ppr => _options.Ppr;

        public async Task WriteSpeedAsync(
            decimal mmPerSec, 
            CancellationToken ct = default) {
            try {
                // 1. 转换单位：mm/s → 倍福内部单位
                var beckhoffUnits = ConvertMmpsToInternalUnits(mmPerSec);

                // 2. 通过 PDO 或 SDO 写入目标速度
                // 例如：写入 0x6042 (Target Velocity)
                await _bus.WriteSdoAsync(
                    _nodeId,
                    index: 0x6042,
                    subIndex: 0,
                    BitConverter.GetBytes(beckhoffUnits),
                    ct);

                LastTargetMmps = mmPerSec;
                
                CommandIssued?.Invoke(this, new AxisCommandIssuedEventArgs {
                    AxisId = Axis,
                    Command = "WriteSpeed",
                    Parameters = new Dictionary<string, object> {
                        ["TargetSpeed"] = mmPerSec
                    }
                });
            }
            catch (Exception ex) {
                throw new AxisOperationException(
                    "倍福轴速度设置失败",
                    ex,
                    axisId: Axis.Value,
                    operation: "WriteSpeed",
                    attemptedValue: mmPerSec);
            }
        }

        public async Task SetAccelDecelByLinearAsync(
            decimal accelMmPerSec, 
            decimal decelMmPerSec,
            CancellationToken ct = default) {
            // 实现加减速度设置逻辑
            // 例如：写入 0x6083 (Profile Acceleration), 0x6084 (Profile Deceleration)
        }

        public async Task EnableAsync(CancellationToken ct = default) {
            // 实现轴使能逻辑
            // 例如：发送 CANopen 状态机命令 (Shutdown → Switch On → Enable Operation)
            IsEnabled = true;
        }

        public async ValueTask DisableAsync(CancellationToken ct = default) {
            // 实现轴禁用逻辑
            IsEnabled = false;
        }

        public async ValueTask StopAsync(CancellationToken ct = default) {
            // 实现停止逻辑
            // 例如：写入 0 速度或发送 Quick Stop 命令
            await WriteSpeedAsync(0, ct);
        }

        public async Task UpdateLinearLimitsAsync(
            decimal maxLinearMmps, 
            decimal maxAccelMmps2, 
            decimal maxDecelMmps2,
            CancellationToken ct = default) {
            MaxLinearMmps = maxLinearMmps;
            MaxAccelMmps2 = maxAccelMmps2;
            MaxDecelMmps2 = maxDecelMmps2;
        }

        public async Task UpdateMechanicsAsync(
            decimal rollerDiameterMm, 
            decimal gearRatio, 
            int ppr,
            CancellationToken ct = default) {
            // 更新机械参数并重新计算换算系数
            _options.RollerDiameterMm = rollerDiameterMm;
            _options.GearRatio = gearRatio;
            _options.Ppr = ppr;
        }

        public async ValueTask DisposeAsync() {
            await DisableAsync();
            Status = DriverStatus.Disposed;
        }

        private int ConvertMmpsToInternalUnits(decimal mmPerSec) {
            // 实现单位转换逻辑
            // 倍福可能使用不同的内部单位（如 encoder counts/sec）
            var circumference = (double)_options.RollerDiameterMm * Math.PI;
            var rps = (double)mmPerSec / circumference;
            var encoderCountsPerSec = rps * (_options.Ppr ?? 1000);
            return (int)encoderCountsPerSec;
        }
    }
}
```

### 步骤 4: 注册到 DriveRegistry

在 `DriveRegistry.cs` 中添加新厂商的创建逻辑：

```csharp
public IAxisDrive CreateDrive(
    string vendor, 
    int nodeId, 
    DriverOptions template) {
    
    return vendor.ToLowerInvariant() switch {
        "leadshine" => new LeadshineLtdmcAxisDrive(
            _bus, nodeId, template, _eventAgg, _logger),
        
        "beckhoff" => new BeckhoffAxisDrive(
            _bus, nodeId, template),
        
        "siemens" => new SiemensAxisDrive(
            _bus, nodeId, template),
        
        "liancheng" => new LianchengAxisDrive(
            _bus, nodeId, template),
        
        _ => throw new ConfigurationException(
            $"不支持的驱动厂商: {vendor}")
    };
}
```

### 步骤 5: 配置文件

在 `appsettings.json` 中配置新厂商：

```json
{
  "ControllerOptions": {
    "Vendor": "Beckhoff",  // 或 "Siemens", "Liancheng"
    "ConnectionString": "192.168.1.100:851",
    "BusType": "Ethercat",
    "DriverParametersTemplate": {
      "RollerDiameterMm": 50.0,
      "GearRatio": 1.0,
      "Ppr": 2048,
      "AxisType": 0,
      "MaxLinearMmps": 2000.0,
      "MaxAccelMmps2": 5000.0,
      "MaxDecelMmps2": 5000.0
    }
  }
}
```

## 单位转换和协议映射

不同厂商可能使用不同的内部单位和对象字典，需要实现转换逻辑。

### 雷赛 (Leadshine)
- 总线：EtherCAT (LTDMC SDK)
- 速度单位：pps (pulses per second)
- 位置单位：pulses
- 转换公式：`pps = (mm/s ÷ 周长) × PPR`

### 倍福 (Beckhoff)
- 总线：EtherCAT (TwinCAT ADS)
- 速度单位：encoder counts/sec
- 位置单位：encoder counts
- 对象字典：CANopen CiA402

### 西门子 (Siemens)
- 总线：Profinet
- 速度单位：rpm 或内部单位
- 位置单位：encoder pulses
- 对象字典：厂商特定

### 联诚 (Liancheng)
- 总线：待定（可能是 Modbus TCP 或 EtherCAT）
- 速度单位：待确认
- 位置单位：待确认

## 测试策略

### 1. 单元测试

为新厂商的驱动创建单元测试：

```csharp
public class BeckhoffAxisDriveTests {
    [Fact]
    public async Task WriteSpeedAsync_ValidSpeed_SetsTargetSpeed() {
        // Arrange
        var mockBus = new Mock<IBusAdapter>();
        var drive = new BeckhoffAxisDrive(
            mockBus.Object, 
            nodeId: 1, 
            TestDriverOptions);

        // Act
        await drive.WriteSpeedAsync(1000);

        // Assert
        Assert.Equal(1000, drive.LastTargetMmps);
        mockBus.Verify(b => b.WriteSdoAsync(
            It.IsAny<int>(),
            0x6042,
            0,
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 2. 集成测试

使用模拟总线适配器测试完整流程：

```csharp
public class BeckhoffIntegrationTests {
    [Fact]
    public async Task AxisController_InitializeWithBeckhoff_CreatesAllAxes() {
        // 使用模拟 Bus 或实际硬件进行测试
    }
}
```

## 最佳实践

1. **遵循接口契约**: 严格实现所有接口方法和属性
2. **异常处理**: 使用 `AxisOperationException`, `HardwareCommunicationException` 等标准异常
3. **日志记录**: 使用 `ILogger<T>` 记录关键操作和错误
4. **线程安全**: 确保并发访问时的线程安全
5. **资源释放**: 正确实现 `IAsyncDisposable`
6. **事件触发**: 及时触发 `AxisFaulted`, `SpeedFeedback` 等事件
7. **单位转换**: 提供清晰的单位转换工具类（参考 `LeadshineConversions.cs`）
8. **性能优化**: 避免在关键路径上的不必要分配和阻塞

## 参考实现

- **完整示例**: 参考 `ZakYip.Singulation.Drivers/Leadshine/` 目录下的雷赛实现
- **总线适配器**: `LeadshineLtdmcBusAdapter.cs`
- **轴驱动**: `LeadshineLtdmcAxisDrive.cs`
- **协议映射**: `LeadshineProtocolMap.cs`
- **单位转换**: `LeadshineConversions.cs`
- **批量操作**: `LeadshineBatchPdoOperations.cs`

## 常见问题

### Q: 如何选择使用 SDO 还是 PDO？
A: 
- PDO（Process Data Object）：用于周期性、实时的数据交换（如速度、位置反馈）
- SDO（Service Data Object）：用于非周期性的配置和诊断（如参数设置、错误读取）

### Q: 如何处理不同厂商的错误码？
A: 创建错误码映射表，将厂商特定错误码转换为标准错误描述。参考 `FaultDiagnosisService` 中的知识库。

### Q: 如何支持不同的总线类型？
A: 实现对应的 `IBusAdapter`，例如：
- EtherCAT → 使用 EtherCAT Master SDK
- Profinet → 使用 Profinet Stack
- CANopen → 使用 CANopen 库
- Modbus TCP → 使用 Modbus 客户端库

## 总结

通过遵循本指南，可以轻松集成新的设备厂商，同时保持系统的架构清晰和可维护性。关键是：

1. ✅ 实现标准接口 (`IAxisDrive`, `IBusAdapter`)
2. ✅ 处理厂商特定的协议和单位转换
3. ✅ 注册到 `DriveRegistry`
4. ✅ 编写完整的测试
5. ✅ 更新配置文件

如有疑问，请参考雷赛的完整实现或联系架构团队。
