# ZakYip.Singulation 命名规范分析与优化建议

## 一、项目背景

本项目是一个**快递包裹分拣系统**（Parcel Singulation System），属于**物流自动化**和**机械控制**领域。项目涉及：
- 快递包裹的自动分拣
- 机械轴控制和驱动
- 视觉系统通信
- 运动规划和速度控制

## 二、当前命名问题分析

### 2.1 不符合行业规范的命名

#### 问题1：第三方库名称泄露到业务代码

**位置**：`ZakYip.Singulation.Transport/Tcp/`

**问题类名**：
- `TouchClientByteTransport` 
- `TouchServerByteTransport`

**问题描述**：
- 类名中使用了第三方库 `TouchSocket` 的名称作为前缀
- 违反了**依赖倒置原则**和**领域驱动设计**原则
- 第三方库的实现细节泄露到了业务代码命名中
- 如果将来更换网络库（如从 TouchSocket 换成 System.Net.Sockets），类名将失去意义

**行业影响**：
- 在工业控制和机械行业中，代码应当反映业务概念，而非技术实现细节
- 降低了代码的可维护性和可读性

**建议修改**：
```csharp
// 当前命名（❌ 不推荐）
TouchClientByteTransport
TouchServerByteTransport

// 建议命名（✅ 推荐）
TcpClientByteTransport
TcpServerByteTransport
```

**修改理由**：
- `Tcp` 明确表示传输协议，是行业标准术语
- 移除了第三方库名称，提高了抽象层次
- 符合机械控制行业的命名习惯

---

#### 问题2：术语不够精确

**位置**：多处使用 `Eject` 术语

**当前使用**：
- `EjectMmps` - 疏散/扩散段速度
- `EjectUnitCount` - 疏散单元数量
- `ejectCount` - 参数命名

**问题描述**：
- `Eject` 在英文中主要表示"弹出、喷射"，常用于打印机、光驱等场景
- 在快递分拣行业，更准确的术语应该是：
  - **Discharge**（卸料、排出）- 用于物料从传送带排出
  - **Diverter**（分流器）- 用于包裹分拣的分流设备
  - **Outlet**（出口）- 用于分拣出口

**建议根据具体场景选择**：
```csharp
// 如果是指分拣出口/分流段
EjectMmps → DischargeSpeed 或 DiverterSpeed 或 OutletSpeed

// 如果是指疏散段（分散包裹间距）
EjectMmps → SpacingConveyorSpeed 或 SeparationSpeed

// 如果是指扩散段（将包裹分散到不同通道）
EjectMmps → DistributionSpeed
```

---

#### 问题3：领域概念不够明确

**位置**：`SpeedSet` 数据结构

**当前定义**：
```csharp
public readonly record struct SpeedSet {
    public IReadOnlyList<int> MainMmps { get; init; }
    public IReadOnlyList<int> EjectMmps { get; init; }
}
```

**问题描述**：
- `Main` 过于通用，没有体现快递分拣业务
- 在快递分拣系统中，通常分为：
  - **Singulation Zone**（分拣区/单件化区）- 将堆叠的包裹分散成单个包裹
  - **Conveyor Sections**（输送段）- 多段传送带
  - **Sortation Zone**（分拣区）- 最终分拣到不同出口

**建议命名**：
```csharp
// 方案1：更明确的业务术语
MainMmps → SingulationSpeed 或 ConveyorSpeed
EjectMmps → DischargeSpeed 或 OutletSpeed

// 方案2：按功能区域命名
MainMmps → InfeedSpeed（进料区速度）
EjectMmps → OutfeedSpeed（出料区速度）
```

---

### 2.2 命名一致性问题

#### 问题4：速度单位标识不统一

**当前使用**：
- `Mmps` - 毫米每秒 (millimeters per second)
- 部分注释中使用 `mm/s`

**问题描述**：
- 缩写 `Mmps` 可能引起混淆（大写M通常表示百万）
- 行业中更常用 `MmPerSec`、`VelocityMmps` 或直接在类型中表达单位

**建议**：
```csharp
// 当前命名
public IReadOnlyList<int> MainMmps { get; init; }

// 建议命名（方案1：更清晰的缩写）
public IReadOnlyList<int> MainVelocityMmps { get; init; }

// 建议命名（方案2：使用值对象）
public IReadOnlyList<VelocityMmps> MainVelocities { get; init; }

// 建议命名（方案3：使用单位库如UnitsNet）
public IReadOnlyList<Speed> MainSpeeds { get; init; }
```

---

#### 问题5：枚举命名不够描述性

**位置**：`ZakYip.Singulation.Core.Enums`

**部分枚举分析**：
```csharp
public enum AxisEventType {
    Faulted,           // ✅ 清晰
    Disconnected,      // ✅ 清晰
    DriverNotLoaded,   // ✅ 清晰
    ControllerFaulted  // ⚠️ 与 Faulted 语义重复
}
```

**建议**：
- 保持现有清晰的枚举值
- `ControllerFaulted` 可以考虑重命名为 `ControllerLevelFault` 以区分层次

---

### 2.3 符合规范的优秀命名示例

#### ✅ 优秀示例1：轴控制相关命名

```csharp
IAxisController    // ✅ 清晰的接口命名
IAxisDrive        // ✅ 符合机械行业术语
IBusAdapter       // ✅ 适配器模式命名标准
AxisEventAggregator // ✅ 清晰的聚合器命名
```

**优点**：
- 使用行业标准术语（Axis、Drive、Bus）
- 遵循设计模式命名约定（Adapter、Aggregator）
- 接口使用 `I` 前缀符合 C# 规范

---

#### ✅ 优秀示例2：协议相关命名

```csharp
IUpstreamCodec     // ✅ 明确的上游编解码器
GuiweiCodec        // ✅ 按厂商命名
HuararyCodec       // ✅ 按厂商命名
```

**优点**：
- 清晰区分上游/下游
- 按厂商隔离不同协议实现
- 使用 Codec（编解码器）是行业标准术语

---

#### ✅ 优秀示例3：安全相关命名

```csharp
SafetyCommand
SafetyIsolationState
SafetyTriggerKind
FrameGuard
PowerGuard
```

**优点**：
- 使用 `Safety` 前缀清晰标识安全相关功能
- 符合机械安全标准（如 ISO 13849）中的术语
- `Guard` 后缀符合安全防护装置的命名习惯

---

## 三、命名规范建议

### 3.1 快递分拣行业标准术语

#### 核心业务术语

| 中文术语 | 推荐英文术语 | 说明 |
|---------|------------|------|
| 包裹 | Parcel / Package | 快递包裹 |
| 分拣 | Sorting / Sortation | 分拣操作 |
| 单件化 | Singulation | 将堆叠包裹分散成单件 |
| 输送段 | Conveyor Section | 传送带的一段 |
| 分流器 | Diverter | 用于改变包裹方向的设备 |
| 卸料口 | Discharge Point / Outlet | 包裹离开系统的位置 |
| 进料区 | Infeed Zone | 包裹进入系统的区域 |
| 出料区 | Outfeed Zone | 包裹离开系统的区域 |
| 缓冲区 | Buffer Zone | 临时存储包裹的区域 |
| 合流 | Merge | 多条线汇合 |
| 分流 | Diverge | 一条线分成多条 |

#### 机械控制术语

| 中文术语 | 推荐英文术语 | 说明 |
|---------|------------|------|
| 轴 | Axis | 运动控制轴 |
| 驱动器 | Drive / Servo Drive | 电机驱动器 |
| 编码器 | Encoder | 位置反馈设备 |
| 限位开关 | Limit Switch | 行程限位 |
| 急停 | Emergency Stop / E-Stop | 紧急停止 |
| 使能 | Enable | 驱动器使能 |
| 回零 | Homing | 返回原点 |
| 点动 | Jog | 手动点动 |
| 定位 | Positioning | 位置控制 |
| 速度环 | Velocity Loop | 速度控制环 |

---

### 3.2 命名规范准则

#### 准则1：业务优先于技术

```csharp
// ❌ 不推荐：暴露技术实现
TouchClientByteTransport
SqlDatabaseRepository

// ✅ 推荐：关注业务概念
TcpClientByteTransport
ParcelRepository
```

#### 准则2：使用行业标准术语

```csharp
// ❌ 不推荐：自创术语
public class FastMovingPart { }
public class SlowMovingPart { }

// ✅ 推荐：行业术语
public class HighSpeedConveyor { }
public class BufferConveyor { }
```

#### 准则3：保持一致性

```csharp
// ❌ 不推荐：同一概念不同命名
MainMmps  // 使用缩写
EjectSpeedMillimetersPerSecond  // 全称

// ✅ 推荐：统一风格
MainVelocityMmps
EjectVelocityMmps
```

#### 准则4：避免缩写，除非是行业通用

```csharp
// ✅ 可以使用的缩写
TCP, UDP, IO, PLC, HMI, PID, PWM

// ⚠️ 应避免的缩写
Msg → Message
Cfg → Configuration
Btn → Button
```

#### 准则5：明确单位和精度

```csharp
// ❌ 不推荐：单位不明确
public int Speed { get; set; }
public double Position { get; set; }

// ✅ 推荐：单位明确
public int SpeedMmps { get; set; }  // 毫米/秒
public double PositionMm { get; set; }  // 毫米
public TimeSpan Duration { get; set; }  // 使用强类型
```

---

### 3.3 分层命名规范

#### Core 层（核心领域层）
- 使用纯业务术语
- 不依赖任何技术框架
- 示例：`ParcelPose`, `SpeedSet`, `AxisId`

#### Infrastructure 层（基础设施层）
- 可以包含技术术语
- 但应通过接口抽象技术细节
- 示例：`LiteDbRepository`, `TcpTransport`

#### Drivers 层（驱动层）
- 使用厂商和设备名称
- 遵循硬件文档术语
- 示例：`LeadshineLtdmcAxisDrive`, `LeadshineProtocolMap`

#### Protocol 层（协议层）
- 使用协议标准术语
- 按厂商分离实现
- 示例：`GuiweiCodec`, `HuararyControl`

---

## 四、优先优化建议（按优先级排序）

### 🔴 高优先级（强烈建议立即修改）

#### 1. 重命名 Touch 前缀的类（影响范围：2个文件）

**文件位置**：
- `ZakYip.Singulation.Transport/Tcp/TcpClientByteTransport/TouchClientByteTransport.cs`
- `ZakYip.Singulation.Transport/Tcp/TcpServerByteTransport/TouchServerByteTransport.cs`

**修改方案**：
```csharp
// 重命名类名和文件名
TouchClientByteTransport → TcpClientByteTransport
TouchServerByteTransport → TcpServerByteTransport
```

**影响评估**：
- 需要修改的文件：约 5-10 个文件（包括使用方）
- 风险：低（内部实现类，接口未变）
- 收益：显著提高代码质量和可维护性

---

### 🟡 中优先级（建议在下一版本优化）

#### 2. 优化 Eject 相关命名

**影响范围**：约 15-20 处代码

**建议**：
- 统一使用更精确的行业术语
- 根据实际业务场景选择：Discharge、Diverter、Outlet 等

**修改示例**：
```csharp
// SpeedSet.cs
EjectMmps → DischargeVelocityMmps

// VisionParams.cs
EjectUnitCount → DischargeUnitCount
EjectDefaultMmps → DischargeDefaultVelocityMmps

// Codec 相关
ejectCount → dischargeCount
```

---

#### 3. 统一速度相关命名

**建议**：
- 统一使用 `VelocityMmps` 后缀
- 或引入值对象 `Velocity` 类型

**修改示例**：
```csharp
// 方案1：统一后缀
MainMmps → MainVelocityMmps
EjectMmps → DischargeVelocityMmps

// 方案2：值对象（更好的类型安全）
public readonly record struct Velocity {
    public int Mmps { get; init; }
    public static Velocity FromMmps(int mmps) => new() { Mmps = mmps };
}
```

---

### 🟢 低优先级（可选优化）

#### 4. 添加更多领域值对象

**建议**：
- 为关键领域概念创建值对象
- 提高类型安全性

**示例**：
```csharp
// 当前使用原始类型
public int SpeedPort { get; set; }
public int PositionPort { get; set; }

// 建议使用值对象
public record TcpPort {
    public ushort Value { get; init; }
    public static TcpPort Create(int value) {
        if (value < 1 || value > 65535)
            throw new ArgumentOutOfRangeException();
        return new TcpPort { Value = (ushort)value };
    }
}
```

---

#### 5. 完善注释和文档

**建议**：
- 为所有公共 API 添加 XML 文档注释
- 说明单位、精度、取值范围
- 提供使用示例

**示例**：
```csharp
/// <summary>
/// 分拣段的线速度（毫米/秒）。
/// </summary>
/// <remarks>
/// 取值范围：0 ~ 3000 mm/s
/// 精度：1 mm/s
/// 默认值：500 mm/s
/// </remarks>
public IReadOnlyList<int> MainVelocityMmps { get; init; }
```

---

## 五、具体实施计划

### 阶段一：高优先级修改（预计 2-4 小时）

1. **重命名 Touch 前缀类**
   - 修改类名和文件名
   - 更新所有引用
   - 运行测试确保无破坏性变更

2. **更新相关文档**
   - 更新 README
   - 更新 API 文档

### 阶段二：中优先级优化（预计 1-2 天）

1. **优化 Eject 相关命名**
   - 确认业务语义
   - 批量重命名
   - 更新注释

2. **统一速度命名**
   - 引入 Velocity 值对象
   - 迁移现有代码
   - 更新测试

### 阶段三：长期优化（预计 1 周）

1. **建立命名规范文档**
   - 编写团队命名规范
   - 提供代码示例
   - Code Review 检查清单

2. **完善代码文档**
   - 添加 XML 注释
   - 生成 API 文档
   - 编写使用指南

---

## 六、总结

### 主要发现

1. ✅ **优点**：
   - 项目整体架构清晰，分层合理
   - 核心机械控制术语使用规范（Axis、Drive、Controller）
   - 安全相关命名符合工业标准
   - 接口和抽象设计良好

2. ⚠️ **待改进**：
   - 存在技术实现泄露到命名中（Touch 前缀）
   - 部分快递分拣业务术语不够精确（Eject）
   - 单位标识不够统一（Mmps）
   - 缺少部分领域值对象

### 关键建议

1. **立即修改**：移除 Touch 前缀，改用 Tcp
2. **短期优化**：统一 Eject 为更精确的行业术语
3. **长期规划**：建立完善的命名规范文档和代码审查流程

### 预期收益

- 📈 提高代码可读性和可维护性
- 🔧 降低新成员学习成本
- 🏭 更好地符合快递分拣和机械控制行业标准
- 🛡️ 提高类型安全性，减少运行时错误
- 📚 改善代码文档质量

---

**文档版本**：v1.0  
**创建日期**：2025-10-28  
**作者**：GitHub Copilot Coding Agent  
**状态**：待审核
