# 厂商驱动和协议目录结构

本文档说明 ZakYip.Singulation 项目中厂商驱动和协议的组织结构，便于对接不同厂商的硬件和通信协议。

## 目录结构

```
ZakYip.Singulation/
├── ZakYip.Singulation.Drivers/          # 硬件驱动层
│   ├── Abstractions/                     # 驱动抽象接口
│   │   ├── IAxisDrive.cs                 # 轴驱动接口
│   │   ├── IBusAdapter.cs                # 总线适配器接口
│   │   └── ...
│   ├── Common/                           # 驱动通用组件
│   ├── Enums/                            # 驱动枚举定义
│   ├── Health/                           # 健康监测组件
│   ├── Resilience/                       # 弹性和容错组件
│   ├── Registry/                         # 驱动注册表
│   └── Leadshine/                        # 雷赛厂商驱动 ⭐
│       ├── README.md                     # 雷赛驱动文档
│       ├── LTDMC.cs                      # 雷赛 SDK P/Invoke 定义
│       ├── LTDMC.dll                     # 雷赛官方 SDK（Windows）
│       ├── LeadshineLtdmcBusAdapter.cs   # 雷赛总线适配器
│       ├── LeadshineLtdmcAxisDrive.cs    # 雷赛轴驱动
│       └── LeadshineProtocolMap.cs       # CiA 402 对象字典映射
│
└── ZakYip.Singulation.Protocol/         # 上游通信协议层
    ├── Abstractions/                     # 协议抽象接口
    │   ├── IUpstreamCodec.cs             # 上游编解码器接口
    │   └── ...
    ├── Enums/                            # 协议枚举定义
    └── Vendors/                          # 厂商协议实现 ⭐
        ├── Guiwei/                       # 归位厂商协议
        │   ├── README.md                 # 归位协议文档
        │   ├── GuiweiCodec.cs            # 归位编解码器
        │   ├── GuiweiControl.cs          # 归位控制字符定义
        │   └── homing_only_tcp.md        # 归位测试报文
        │
        └── Huarary/                      # 华雷厂商协议
            ├── README.md                 # 华雷协议文档
            ├── HuararyCodec.cs           # 华雷编解码器
            ├── HuararyControl.cs         # 华雷控制字符定义
            └── vision_mock_packets.md    # 华雷测试报文
```

## 厂商对接指南

### 1. 添加新的硬件驱动厂商

如需对接新的运动控制器厂商（如西门子、三菱、台达等），请按以下步骤进行：

#### 步骤 1：创建厂商目录
在 `ZakYip.Singulation.Drivers/` 下创建新的厂商目录，例如 `Siemens/`：

```
ZakYip.Singulation.Drivers/
└── Siemens/
    ├── README.md                    # 必需：厂商驱动文档
    ├── SiemensBusAdapter.cs         # 总线适配器实现
    ├── SiemensAxisDrive.cs          # 轴驱动实现
    ├── SiemensProtocolMap.cs        # 协议映射（如需要）
    └── [第三方 SDK 文件]             # 厂商 SDK（如有）
```

#### 步骤 2：实现核心接口
实现以下核心接口：

**IBusAdapter**（总线适配器）：
```csharp
public class SiemensBusAdapter : IBusAdapter {
    public Task<(bool success, string message)> InitializeAsync(CancellationToken ct);
    public Task<int> GetAxisCountAsync(CancellationToken ct);
    public Task<int> GetErrorCodeAsync(CancellationToken ct);
    public Task ResetAsync(CancellationToken ct);
    public Task CloseAsync(CancellationToken ct);
    // ... 其他接口成员
}
```

**IAxisDrive**（轴驱动）：
```csharp
public class SiemensAxisDrive : IAxisDrive {
    public Task WriteSpeedAsync(decimal mmPerSec, CancellationToken ct);
    public Task SetAccelDecelAsync(decimal accel, decimal decel, CancellationToken ct);
    public Task EnableAsync(CancellationToken ct);
    public Task<bool> PingAsync(CancellationToken ct);
    // ... 其他接口成员
}
```

#### 步骤 3：编写 README.md
在厂商目录下创建 `README.md`，参考 [Leadshine/README.md](../../ZakYip.Singulation.Drivers/Leadshine/README.md) 的结构，包含：
- 概述
- 硬件支持（型号、接口、协议）
- 核心组件说明
- 驱动配置参数
- 通信协议详解
- 错误处理和故障排查
- 性能优化建议
- 示例代码

#### 步骤 4：注册驱动
在驱动注册表中注册新厂商：
```csharp
// ZakYip.Singulation.Drivers/Registry/DriveRegistry.cs
public void RegisterVendor(string vendorName, Func<DriverOptions, IAxisDrive> factory) {
    // 注册驱动工厂
}
```

### 2. 添加新的上游协议厂商

如需对接新的视觉系统或上游设备协议（如基恩士、康耐视等），请按以下步骤进行：

#### 步骤 1：创建厂商目录
在 `ZakYip.Singulation.Protocol/Vendors/` 下创建新的厂商目录，例如 `Keyence/`：

```
ZakYip.Singulation.Protocol/Vendors/
└── Keyence/
    ├── README.md                    # 必需：协议文档
    ├── KeyenceCodec.cs              # 编解码器实现
    ├── KeyenceControl.cs            # 控制字符/常量定义
    └── test_packets.md              # 测试报文（可选）
```

#### 步骤 2：实现编解码器
实现 `IUpstreamCodec` 接口：

```csharp
public class KeyenceCodec : IUpstreamCodec {
    // 解码速度帧
    public bool TryDecodeSpeed(ReadOnlySpan<byte> frame, out SpeedSet set);
    
    // 解码位置帧
    public bool TryDecodePositions(ReadOnlySpan<byte> frame, out IReadOnlyList<ParcelPose> poses);
    
    // 编码启动/停止命令
    public int EncodeStartStop(IBufferWriter<byte> writer, bool start);
    
    // ... 其他协议方法
}
```

#### 步骤 3：定义控制常量
创建控制字符常量类：

```csharp
public static class KeyenceControl {
    public const byte Start = 0x02;      // STX
    public const byte End = 0x03;        // ETX
    public const byte CtrlSpeed = 0x30;  // 速度帧
    public const byte CtrlPos = 0x31;    // 位置帧
    // ... 其他控制码
}
```

#### 步骤 4：编写 README.md
在厂商目录下创建 `README.md`，参考 [Huarary/README.md](../../ZakYip.Singulation.Protocol/Vendors/Huarary/README.md) 的结构，包含：
- 协议概述和特点
- 帧结构详解
- 控制码定义
- 编解码实现
- 配置示例
- 测试报文集
- 调试技巧
- 协议对比

#### 步骤 5：添加测试报文
创建 `test_packets.md`，提供测试用的报文示例：
- 各类帧的十六进制示例
- 字段解析说明
- 边界情况示例

### 3. 文档要求

每个厂商目录必须包含 `README.md`，内容应涵盖：

#### 硬件驱动文档（Drivers）
1. **概述**：厂商简介、支持的硬件型号
2. **硬件支持**：控制器型号、通信接口、协议标准
3. **核心组件**：驱动类、适配器类的功能说明
4. **配置参数**：所有配置选项的详细说明和示例
5. **通信协议**：底层通信机制（如 EtherCAT、Modbus）
6. **错误处理**：错误码对照表、故障排查流程
7. **性能优化**：优化建议和最佳实践
8. **示例代码**：完整的使用示例

#### 上游协议文档（Protocol）
1. **协议概述**：协议类型、传输方向、适用场景
2. **协议特点**：帧结构、校验方式、编码方式
3. **帧结构**：各类帧的详细格式和字段说明
4. **控制码**：所有控制码的定义和用途
5. **编解码实现**：解析和编码的代码示例
6. **配置示例**：典型配置和参数说明
7. **测试报文**：十六进制测试数据
8. **调试技巧**：常见问题和解决方案

## 现有厂商支持

### 硬件驱动

| 厂商 | 目录 | 文档 | 支持状态 |
|------|------|------|---------|
| 雷赛（Leadshine） | `Drivers/Leadshine/` | [README.md](../../ZakYip.Singulation.Drivers/Leadshine/README.md) | ✅ 完全支持 |

**雷赛驱动特性**：
- LTDMC 系列控制器
- EtherCAT 通信
- CiA 402 状态机
- 速度模式控制
- 完整的故障恢复机制

### 上游协议

| 厂商 | 目录 | 文档 | 支持状态 |
|------|------|------|---------|
| 归位（Guiwei） | `Protocol/Vendors/Guiwei/` | [README.md](../../ZakYip.Singulation.Protocol/Vendors/Guiwei/README.md) | ✅ 完全支持 |
| 华雷（Huarary） | `Protocol/Vendors/Huarary/` | [README.md](../../ZakYip.Singulation.Protocol/Vendors/Huarary/README.md) | ✅ 完全支持 |

**协议特性对比**：

| 特性 | 归位（Guiwei） | 华雷（Huarary） |
|------|---------------|----------------|
| 帧结构 | 简化（仅起止符） | 完整（控制码+长度+校验） |
| 校验 | 无 | XOR |
| 帧类型 | 单一（速度） | 多种（速度/位置/命令） |
| 双向通信 | 单向 | 双向 |
| 适用场景 | 简单速度控制 | 复杂视觉引导 |

## 命名规范

### 驱动类命名
- 总线适配器：`{Vendor}BusAdapter`（如 `LeadshineLtdmcBusAdapter`）
- 轴驱动：`{Vendor}AxisDrive`（如 `LeadshineLtdmcAxisDrive`）
- 协议映射：`{Vendor}ProtocolMap`（如 `LeadshineProtocolMap`）

### 协议类命名
- 编解码器：`{Vendor}Codec`（如 `GuiweiCodec`）
- 控制常量：`{Vendor}Control`（如 `HuararyControl`）

### 文件命名
- 文档：`README.md`（必需）
- 测试报文：`test_packets.md` 或 `{protocol}_packets.md`

## 依赖管理

### 第三方 SDK
如果厂商驱动依赖第三方 SDK（如 DLL、SO 文件），请：
1. 将 SDK 文件放在厂商目录下
2. 在 README.md 中说明 SDK 的来源和版本
3. 在 `.csproj` 中配置 SDK 文件的复制规则
4. 提供 SDK 的下载链接或安装说明

### NuGet 包
优先使用官方 NuGet 包，在项目文件中声明依赖：
```xml
<ItemGroup>
  <PackageReference Include="VendorSdk" Version="1.0.0" />
</ItemGroup>
```

## 测试规范

### 单元测试
为每个厂商驱动/协议创建对应的单元测试：
```
ZakYip.Singulation.Tests/
├── Drivers/
│   └── LeadshineTests/
│       ├── LeadshineBusAdapterTests.cs
│       └── LeadshineAxisDriveTests.cs
└── Protocol/
    ├── GuiweiCodecTests.cs
    └── HuararyCodecTests.cs
```

### 集成测试
在真实硬件上进行集成测试，验证：
- 连接建立
- 速度控制
- 状态反馈
- 故障恢复
- 性能指标

## 最佳实践

1. **接口优先**：始终通过接口编程，避免直接依赖具体实现
2. **错误隔离**：驱动层异常不应向上传播，通过事件通知
3. **异步操作**：所有 I/O 操作使用异步方法，支持取消令牌
4. **资源释放**：实现 `IDisposable` 或 `IAsyncDisposable`，确保资源正确释放
5. **日志记录**：记录关键操作和错误信息，便于故障排查
6. **参数验证**：使用 `Guard` 类进行参数验证，统一异常处理
7. **配置管理**：通过配置文件管理厂商参数，避免硬编码
8. **文档完善**：为每个厂商提供详细文档，包括示例和故障排查

## 版本兼容性

### 驱动版本
在 README.md 中明确说明：
- 支持的控制器固件版本
- SDK 版本要求
- 已知兼容性问题

### 协议版本
如协议有多个版本，应：
- 在编解码器中支持版本检测
- 提供版本迁移指南
- 标注已废弃的功能

## 贡献指南

添加新厂商支持时，请：
1. 按照本文档的目录结构和命名规范创建文件
2. 实现所有必需的接口
3. 编写详细的 README.md 文档
4. 添加单元测试（覆盖率 > 80%）
5. 提供完整的配置示例
6. 在真实硬件上验证功能
7. 提交 Pull Request 并附上测试报告

## 相关文档

- [项目架构文档](../docs/ARCHITECTURE.md)
- [开发者指南](../docs/DEVELOPER_GUIDE.md)
- [API 文档](../docs/API.md)
- [故障排查手册](../docs/TROUBLESHOOTING.md)

## 联系方式

如有问题或建议，请通过以下方式联系：
- 提交 Issue：[GitHub Issues](https://github.com/Hisoka6602/ZakYip.Singulation/issues)
- 讨论区：[GitHub Discussions](https://github.com/Hisoka6602/ZakYip.Singulation/discussions)
