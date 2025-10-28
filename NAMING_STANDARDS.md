# 快递分拣系统命名规范标准

## 文档说明

本文档为 ZakYip.Singulation 项目的官方命名规范，适用于快递分拣（Parcel Singulation）和机械控制（Mechanical Control）领域的软件开发。

**版本**：v1.0  
**生效日期**：2025-10-28  
**适用范围**：所有 .NET C# 代码

---

## 一、通用命名原则

### 1.1 基本原则

#### ✅ DO（应当遵循）

1. **使用清晰的英文命名**
   - 使用完整的英文单词
   - 避免拼音和中英文混合
   - 优先使用行业标准术语

2. **保持命名一致性**
   - 相同概念使用相同的术语
   - 遵循项目既有的命名风格
   - 在整个代码库中保持统一

3. **见名知意**
   - 名称应清楚表达其用途
   - 避免过度缩写
   - 提供足够的上下文信息

4. **遵循 C# 命名约定**
   - PascalCase：类型、方法、属性、事件
   - camelCase：参数、局部变量、私有字段
   - 接口使用 `I` 前缀
   - 常量全大写（可选用下划线分隔）

#### ❌ DON'T（应当避免）

1. **暴露实现细节**
   ```csharp
   // ❌ 不推荐
   public class TouchClientByteTransport { }  // 暴露第三方库名称
   public class SqlServerRepository { }        // 暴露数据库类型
   
   // ✅ 推荐
   public class TcpClientByteTransport { }
   public class ParcelRepository { }
   ```

2. **使用非标准缩写**
   ```csharp
   // ❌ 不推荐
   var msg = "Hello";
   var cfg = new Config();
   var btn = GetButton();
   
   // ✅ 推荐
   var message = "Hello";
   var config = new Config();
   var button = GetButton();
   ```

3. **使用模糊的名称**
   ```csharp
   // ❌ 不推荐
   public class Data { }
   public class Info { }
   public class Manager { }
   
   // ✅ 推荐
   public class ParcelData { }
   public class SystemInfo { }
   public class AxisManager { }
   ```

---

## 二、行业术语标准

### 2.1 快递分拣领域术语

#### 核心概念

| 中文 | 英文 | 说明 | 示例代码 |
|-----|------|------|---------|
| 包裹 | `Parcel` | 快递包裹（推荐）| `public class Parcel { }` |
| 包裹 | `Package` | 可选，北美常用 | `public class Package { }` |
| 分拣 | `Sorting` | 分拣操作 | `public class SortingZone { }` |
| 分拣 | `Sortation` | 物流行业专业术语 | `public class SortationSystem { }` |
| 单件化 | `Singulation` | 将堆叠包裹分散成单件 | `public class SingulationZone { }` |
| 输送机 | `Conveyor` | 传送带 | `public class Conveyor { }` |
| 输送段 | `ConveyorSection` | 传送带的一段 | `public class ConveyorSection { }` |
| 分流器 | `Diverter` | 改变包裹方向的设备 | `public class Diverter { }` |
| 合流 | `Merge` | 多条线汇合 | `public class MergePoint { }` |
| 分流 | `Diverge` | 一条线分成多条 | `public class DivergePoint { }` |

#### 区域概念

| 中文 | 英文 | 说明 | 示例代码 |
|-----|------|------|---------|
| 进料区 | `Infeed` / `InfeedZone` | 包裹进入系统 | `public int InfeedSpeed { get; }` |
| 出料区 | `Outfeed` / `OutfeedZone` | 包裹离开系统 | `public int OutfeedSpeed { get; }` |
| 缓冲区 | `Buffer` / `BufferZone` | 临时存储区域 | `public class BufferZone { }` |
| 卸料区 | `Discharge` | 卸料/排出区域 | `public int DischargeSpeed { get; }` |
| 分拣口 | `Outlet` / `Chute` | 分拣出口 | `public class Outlet { }` |

#### 操作和状态

| 中文 | 英文 | 说明 | 示例代码 |
|-----|------|------|---------|
| 进料 | `Loading` | 包裹上料 | `public void StartLoading() { }` |
| 出料 | `Unloading` | 包裹卸料 | `public void StartUnloading() { }` |
| 堵塞 | `Blockage` / `Jam` | 包裹堵塞 | `public event BlockageDetected;` |
| 满载 | `Full` | 区域已满 | `public bool IsFull { get; }` |
| 空载 | `Empty` | 区域为空 | `public bool IsEmpty { get; }` |

---

### 2.2 机械控制领域术语

#### 运动控制

| 中文 | 英文 | 说明 | 示例代码 |
|-----|------|------|---------|
| 轴 | `Axis` | 运动控制轴 | `public interface IAxis { }` |
| 驱动器 | `Drive` | 电机驱动器 | `public interface IAxisDrive { }` |
| 伺服驱动 | `ServoDrive` | 伺服电机驱动 | `public class ServoDrive { }` |
| 编码器 | `Encoder` | 位置反馈 | `public int EncoderPosition { get; }` |
| 控制器 | `Controller` | 运动控制器 | `public interface IAxisController { }` |
| 总线 | `Bus` | 通信总线 | `public interface IBusAdapter { }` |

#### 运动参数

| 中文 | 英文 | 说明 | 单位 | 示例代码 |
|-----|------|------|-----|---------|
| 速度 | `Velocity` | 线速度（推荐）| mm/s | `public int VelocityMmps { get; }` |
| 速度 | `Speed` | 可选 | mm/s | `public int SpeedMmps { get; }` |
| 位置 | `Position` | 绝对位置 | mm | `public double PositionMm { get; }` |
| 加速度 | `Acceleration` | 加速度 | mm/s² | `public int AccelerationMmps2 { get; }` |
| 减速度 | `Deceleration` | 减速度 | mm/s² | `public int DecelerationMmps2 { get; }` |
| 距离 | `Distance` | 移动距离 | mm | `public double DistanceMm { get; }` |

#### 控制指令

| 中文 | 英文 | 说明 | 示例代码 |
|-----|------|------|---------|
| 使能 | `Enable` | 驱动器使能 | `public void Enable() { }` |
| 禁用 | `Disable` | 驱动器禁用 | `public void Disable() { }` |
| 回零 | `Home` / `Homing` | 返回原点 | `public Task HomeAsync() { }` |
| 点动 | `Jog` | 手动点动 | `public void Jog(int velocity) { }` |
| 定位 | `MoveTo` / `Position` | 移动到位置 | `public Task MoveToAsync(double pos) { }` |
| 停止 | `Stop` | 正常停止 | `public void Stop() { }` |
| 急停 | `EmergencyStop` / `EStop` | 紧急停止 | `public void EmergencyStop() { }` |

#### 状态和标志

| 中文 | 英文 | 说明 | 示例代码 |
|-----|------|------|---------|
| 就绪 | `Ready` | 设备就绪 | `public bool IsReady { get; }` |
| 运行中 | `Running` | 正在运行 | `public bool IsRunning { get; }` |
| 故障 | `Fault` / `Faulted` | 设备故障 | `public bool IsFaulted { get; }` |
| 报警 | `Alarm` | 报警状态 | `public bool HasAlarm { get; }` |
| 使能 | `Enabled` | 已使能 | `public bool IsEnabled { get; }` |
| 在位 | `InPosition` | 位置到达 | `public bool IsInPosition { get; }` |
| 限位 | `Limit` | 触发限位 | `public bool IsAtLimit { get; }` |

---

### 2.3 通信和协议术语

| 中文 | 英文 | 说明 | 示例代码 |
|-----|------|------|---------|
| 上游 | `Upstream` | 数据来源方向 | `public interface IUpstreamCodec { }` |
| 下游 | `Downstream` | 数据去向方向 | `public interface IDownstreamCodec { }` |
| 编解码器 | `Codec` | 编码/解码器 | `public class GuiweiCodec : ICodec { }` |
| 帧 | `Frame` | 数据帧 | `public byte[] EncodeFrame() { }` |
| 传输 | `Transport` | 传输层 | `public interface IByteTransport { }` |
| 协议 | `Protocol` | 通信协议 | `public class ProtocolHandler { }` |

---

## 三、命名模式和约定

### 3.1 类命名

#### 业务实体类
```csharp
// 使用名词或名词短语
public class Parcel { }
public class Conveyor { }
public class SortingZone { }
```

#### 服务类
```csharp
// 使用名词 + Service 后缀
public class ParcelTrackingService { }
public class SortingService { }
public class LoggingService { }
```

#### 管理类
```csharp
// 使用名词 + Manager 后缀（适度使用）
public class ConnectionManager { }
public class ResourceManager { }

// 或使用更具体的名称
public class AxisController { }  // 优于 AxisManager
public class TransportCoordinator { }  // 优于 TransportManager
```

#### 工厂类
```csharp
// 使用名词 + Factory 后缀
public class AxisDriveFactory { }
public class CodecFactory { }
```

#### 适配器类
```csharp
// 使用名词 + Adapter 后缀
public class LeadshineBusAdapter : IBusAdapter { }
public class TcpTransportAdapter { }
```

---

### 3.2 接口命名

#### 基本接口
```csharp
// 使用 I 前缀 + 形容词/名词
public interface IAxisController { }
public interface IByteTransport { }
public interface IDisposable { }  // .NET 标准
```

#### 能力接口
```csharp
// 使用 I + -able 后缀表示能力
public interface IStartable { }
public interface IConfigurable { }
public interface IResettable { }
```

---

### 3.3 枚举命名

```csharp
// 枚举名称：单数名词
public enum AxisEventType { }
public enum DriveStatus { }
public enum SortingMode { }

// 枚举值：PascalCase，描述性名词
public enum TransportStatus {
    Disconnected,    // 不使用 NotConnected
    Connecting,
    Connected,
    Faulted         // 不使用 Error
}

// 标志枚举：复数名词 + [Flags]
[Flags]
public enum AxisCapabilities {
    None = 0,
    Homing = 1,
    Positioning = 2,
    Velocity = 4
}
```

---

### 3.4 方法命名

#### 查询方法（不改变状态）
```csharp
// 使用 Get/Is/Has/Can 前缀
public int GetAxisCount() { }
public bool IsReady() { }
public bool HasFault() { }
public bool CanMove() { }
```

#### 命令方法（改变状态）
```csharp
// 使用动词
public void Enable() { }
public void Start() { }
public void Stop() { }
public void Reset() { }

// 或 动词 + 名词
public void StartConveyor() { }
public void StopAxis() { }
public void ResetController() { }
```

#### 异步方法
```csharp
// 使用 Async 后缀
public async Task InitializeAsync() { }
public async Task<bool> MoveToAsync(double position) { }
public async Task<ParcelData> GetParcelDataAsync(string id) { }
```

---

### 3.5 属性命名

```csharp
// 使用名词或名词短语（PascalCase）
public string Name { get; set; }
public int AxisCount { get; }
public bool IsEnabled { get; private set; }

// 布尔属性：使用 Is/Has/Can 前缀
public bool IsReady { get; }
public bool HasError { get; }
public bool CanExecute { get; }

// 集合属性：使用复数
public IReadOnlyList<Axis> Axes { get; }
public List<Parcel> Parcels { get; }
```

---

### 3.6 事件命名

```csharp
// 使用动词的过去式/完成时
public event EventHandler Started;
public event EventHandler Stopped;
public event EventHandler<ErrorEventArgs> ErrorOccurred;

// 或使用 -ing 形式表示进行中
public event EventHandler Starting;
public event EventHandler Stopping;

// 事件参数类：名词 + EventArgs
public class AxisFaultedEventArgs : EventArgs {
    public string Reason { get; init; }
}
```

---

### 3.7 常量和字段命名

```csharp
// 公共常量：PascalCase
public const int MaxVelocity = 3000;
public const string DefaultHost = "localhost";

// 私有字段：_camelCase
private readonly ILogger _logger;
private int _retryCount;

// 静态只读字段：PascalCase
public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
```

---

## 四、单位标识规范

### 4.1 速度单位

```csharp
// 推荐：使用 Velocity + 单位后缀
public int VelocityMmps { get; }     // 毫米/秒
public double VelocityMps { get; }    // 米/秒
public int VelocityRpm { get; }       // 转/分

// 可选：使用 Speed（不太精确）
public int SpeedMmps { get; }
```

### 4.2 位置单位

```csharp
// 使用 Position + 单位后缀
public double PositionMm { get; }     // 毫米
public double PositionM { get; }      // 米
public int PositionPulses { get; }    // 脉冲
```

### 4.3 时间单位

```csharp
// 推荐：使用 .NET TimeSpan 类型
public TimeSpan Timeout { get; }
public TimeSpan Duration { get; }

// 如果必须使用原始类型，添加单位后缀
public int TimeoutMs { get; }         // 毫秒
public double DurationSec { get; }    // 秒
```

### 4.4 加速度单位

```csharp
public int AccelerationMmps2 { get; }  // 毫米/秒²
public int DecelerationMmps2 { get; }
```

---

## 五、特定场景命名

### 5.1 DTO (Data Transfer Object)

```csharp
// 使用名词 + Dto 后缀（或 Request/Response）
public class ParcelPoseDto { }
public class SpeedSetDto { }

// API 请求/响应
public class CreateParcelRequest { }
public class GetParcelResponse { }
```

### 5.2 值对象 (Value Object)

```csharp
// 使用简洁的名词
public record AxisId(int Value);
public record Velocity(int Mmps);
public record Position(double Mm);

// 或使用 struct
public readonly record struct ParcelPose {
    public double X { get; init; }
    public double Y { get; init; }
}
```

### 5.3 配置类

```csharp
// 使用名词 + Options/Settings/Configuration
public class DriverOptions { }
public class TcpClientOptions { }
public class SystemSettings { }
public class DatabaseConfiguration { }
```

### 5.4 扩展方法

```csharp
// 放在 Extensions 命名空间
namespace ZakYip.Singulation.Extensions {
    public static class StringExtensions {
        public static bool IsNullOrEmpty(this string value) { }
    }
}
```

---

## 六、命名审查清单

在提交代码前，请确认：

- [ ] 类名使用名词或名词短语
- [ ] 方法名使用动词或动词短语
- [ ] 接口使用 `I` 前缀
- [ ] 异步方法使用 `Async` 后缀
- [ ] 布尔属性/变量使用 `Is/Has/Can` 前缀
- [ ] 没有暴露第三方库名称（如 Touch, Sql, Redis）
- [ ] 使用行业标准术语而非自创术语
- [ ] 保持与现有代码风格一致
- [ ] 单位明确标识（Mmps, Mm, Ms）
- [ ] 避免不必要的缩写
- [ ] 名称具有描述性，见名知意
- [ ] XML 文档注释完整（公共 API）

---

## 七、不推荐的命名模式

### ❌ 避免使用

```csharp
// 过于通用的名称
public class Data { }
public class Info { }
public class Helper { }
public class Util { }
public class Common { }

// 匈牙利命名法
public string strName;
public int iCount;
public bool bEnabled;

// 类型后缀（除非是设计模式）
public class ParcelClass { }
public class SpeedVariable { }

// 带数字的名称（除非有明确含义）
public class Data1 { }
public void Process2() { }

// 过度缩写
public class PrcSvc { }  // ParcelService
public void PrcPkt() { }  // ProcessPacket
```

---

## 八、命名示例对照表

### 好的命名 vs 差的命名

| ❌ 不推荐 | ✅ 推荐 | 说明 |
|---------|--------|------|
| `TouchClientByteTransport` | `TcpClientByteTransport` | 移除第三方库名称 |
| `EjectMmps` | `DischargeVelocityMmps` | 使用更精确的行业术语 |
| `MainMmps` | `ConveyorVelocityMmps` | 更明确的业务语义 |
| `Data` | `ParcelData` | 增加上下文 |
| `Manager` | `AxisController` | 更具体的职责描述 |
| `Process()` | `ProcessParcel()` | 明确处理对象 |
| `Get()` | `GetAxisCount()` | 明确返回内容 |
| `IsOk` | `IsReady` | 更专业的术语 |
| `HasErr` | `HasFault` | 完整单词 + 行业术语 |
| `Cfg` | `Configuration` | 避免缩写 |

---

## 九、参考资料

### 行业标准
- IEC 61131-3: 工业控制编程语言
- ISO 13849: 机械安全标准
- VDI 2193: 物流系统术语

### .NET 命名规范
- [Microsoft C# Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [.NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)

### 领域驱动设计
- Domain-Driven Design (Eric Evans)
- Implementing Domain-Driven Design (Vaughn Vernon)

---

**文档版本**：v1.0  
**最后更新**：2025-10-28  
**维护者**：开发团队  
**审核状态**：待审核
