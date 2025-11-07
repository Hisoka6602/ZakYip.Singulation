# 上游数据源集成指南

## 概述

本文档说明如何将新的上游数据源（如海康威视 Hikvision、自研系统等）集成到 ZakYip.Singulation 系统中。

## 架构概述

系统通过协议抽象层实现上游数据源的解耦：

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│         (TransportStreamService, Controllers)               │
└────────────────────────┬────────────────────────────────────┘
                         │ 依赖接口，不依赖具体实现
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  Abstraction Layer                          │
│               IUpstreamCodec, ITransportData                │
└────────────────────────┬────────────────────────────────────┘
                         │ 由各厂商实现
                         ↓
┌─────────────────────────────────────────────────────────────┐
│           Vendor-Specific Protocol Implementations          │
│   ┌──────────┐  ┌───────────┐  ┌──────────┐  ┌──────────┐ │
│   │ Huarary  │  │  Guiwei   │  │Hikvision │  │  Custom  │ │
│   │ (华睿)   │  │  (归位)   │  │(海康威视)│  │ (自研)   │ │
│   └──────────┘  └───────────┘  └──────────┘  └──────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## 核心接口说明

### 1. IUpstreamCodec - 上游协议编解码接口

**位置**: `ZakYip.Singulation.Protocol/Abstractions/IUpstreamCodec.cs`

**职责**: 定义上游数据的编码和解码规则。

**核心方法**:
- `DecodeAsync(ReadOnlyMemory<byte> data)` - 解码上游数据为标准 SpeedSet
- `EncodeAsync(SpeedSet speedSet)` - 编码 SpeedSet 为上游协议格式
- `TryDecodeMessage(data, out result)` - 尝试解码消息（非阻塞）

**核心属性**:
- `string VendorName` - 厂商名称标识
- `int ProtocolVersion` - 协议版本号
- `bool SupportsEncoding` - 是否支持编码（上行）
- `bool SupportsDecoding` - 是否支持解码（下行）

### 2. ITransportData - 传输数据接口

**位置**: `ZakYip.Singulation.Core/Contracts/ITransportData.cs`

**职责**: 定义标准的数据传输对象。

**核心属性**:
- `int BatchId` - 批次 ID
- `SpeedSet SpeedSet` - 速度集合
- `DateTime ReceivedAt` - 接收时间
- `string Source` - 数据来源

## 集成新上游数据源的步骤

### 步骤 1: 创建厂商目录

在 `ZakYip.Singulation.Protocol/Vendors` 目录下创建新厂商子目录：

```
ZakYip.Singulation.Protocol/Vendors/
├── Huarary/              # 现有：华睿
├── Guiwei/               # 现有：归位
├── Hikvision/            # 新增：海康威视
└── Custom/               # 新增：自研系统
```

### 步骤 2: 定义协议格式

根据上游系统的协议文档，定义数据格式。

#### 示例 1: JSON 格式协议（海康威视）

```json
{
  "batchId": 12345,
  "timestamp": "2025-11-07T10:30:00Z",
  "speeds": {
    "main": [800, 850, 900, 920, 950],
    "eject": [1000, 1020, 1050]
  },
  "metadata": {
    "productType": "A",
    "quality": "OK"
  }
}
```

#### 示例 2: 二进制格式协议

```
帧头 (2字节): 0xAA55
批次ID (4字节): Little-Endian Int32
主段轴数 (1字节): Byte
疏散段轴数 (1字节): Byte
主段速度数组 (N×2字节): Little-Endian Int16[] (单位: mm/s)
疏散段速度数组 (M×2字节): Little-Endian Int16[] (单位: mm/s)
校验和 (2字节): CRC16
帧尾 (2字节): 0x55AA
```

### 步骤 3: 实现 IUpstreamCodec

创建协议编解码器，例如 `HikvisionCodec.cs`:

```csharp
using System.Text;
using System.Text.Json;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Protocol.Vendors.Hikvision {
    /// <summary>
    /// 海康威视上游协议编解码器
    /// </summary>
    public sealed class HikvisionCodec : IUpstreamCodec {
        private readonly ILogger<HikvisionCodec> _logger;

        public string VendorName => "Hikvision";
        public int ProtocolVersion => 1;
        public bool SupportsEncoding => false;  // 仅接收数据，不上传
        public bool SupportsDecoding => true;

        public HikvisionCodec(ILogger<HikvisionCodec> logger) {
            _logger = logger;
        }

        public async Task<SpeedSet?> DecodeAsync(
            ReadOnlyMemory<byte> data, 
            CancellationToken ct = default) {
            
            try {
                // 1. 转换为字符串
                var json = Encoding.UTF8.GetString(data.Span);

                // 2. 解析 JSON
                var dto = JsonSerializer.Deserialize<HikvisionSpeedDataDto>(json);
                if (dto == null) {
                    throw new CodecException("海康威视数据解析失败：JSON 为空");
                }

                // 3. 验证必要字段
                if (dto.Speeds == null || dto.Speeds.Main == null) {
                    throw new CodecException("海康威视数据无效：缺少速度数据");
                }

                // 4. 转换为标准 SpeedSet
                var speedSet = new SpeedSet {
                    BatchId = dto.BatchId,
                    MainSegmentSpeeds = dto.Speeds.Main
                        .Select(s => (decimal)s)
                        .ToArray(),
                    EjectSegmentSpeeds = dto.Speeds.Eject?
                        .Select(s => (decimal)s)
                        .ToArray() ?? Array.Empty<decimal>(),
                    ReceivedAt = dto.Timestamp ?? DateTime.Now,
                    Source = VendorName
                };

                _logger.LogInformation(
                    "成功解码海康威视数据: BatchId={BatchId}, MainCount={MainCount}, EjectCount={EjectCount}",
                    speedSet.BatchId,
                    speedSet.MainSegmentSpeeds.Length,
                    speedSet.EjectSegmentSpeeds.Length);

                return speedSet;
            }
            catch (JsonException ex) {
                throw new CodecException("海康威视 JSON 解析失败", ex);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "解码海康威视数据时发生错误");
                throw new CodecException("解码海康威视数据失败", ex);
            }
        }

        public async Task<byte[]?> EncodeAsync(
            SpeedSet speedSet, 
            CancellationToken ct = default) {
            // 海康威视仅作为数据源，不支持上传
            throw new NotSupportedException("海康威视协议不支持编码");
        }

        public bool TryDecodeMessage(
            ReadOnlyMemory<byte> data, 
            out SpeedSet? result) {
            
            result = null;
            try {
                result = DecodeAsync(data).GetAwaiter().GetResult();
                return result != null;
            }
            catch {
                return false;
            }
        }
    }

    /// <summary>
    /// 海康威视协议 DTO
    /// </summary>
    internal sealed record class HikvisionSpeedDataDto {
        public int BatchId { get; init; }
        public DateTime? Timestamp { get; init; }
        public required HikvisionSpeedsDto Speeds { get; init; }
        public HikvisionMetadataDto? Metadata { get; init; }
    }

    internal sealed record class HikvisionSpeedsDto {
        public required double[] Main { get; init; }
        public double[]? Eject { get; init; }
    }

    internal sealed record class HikvisionMetadataDto {
        public string? ProductType { get; init; }
        public string? Quality { get; init; }
    }
}
```

#### 二进制协议示例（自研系统）

```csharp
using System.Buffers.Binary;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Protocol.Vendors.Custom {
    /// <summary>
    /// 自研系统二进制协议编解码器
    /// </summary>
    public sealed class CustomBinaryCodec : IUpstreamCodec {
        private readonly ILogger<CustomBinaryCodec> _logger;
        
        private const ushort FRAME_HEADER = 0xAA55;
        private const ushort FRAME_FOOTER = 0x55AA;
        private const int MIN_FRAME_SIZE = 12; // 帧头+批次ID+轴数×2+校验+帧尾

        public string VendorName => "Custom";
        public int ProtocolVersion => 1;
        public bool SupportsEncoding => true;
        public bool SupportsDecoding => true;

        public CustomBinaryCodec(ILogger<CustomBinaryCodec> logger) {
            _logger = logger;
        }

        public async Task<SpeedSet?> DecodeAsync(
            ReadOnlyMemory<byte> data,
            CancellationToken ct = default) {
            
            var span = data.Span;

            // 1. 验证帧长度
            if (span.Length < MIN_FRAME_SIZE) {
                throw new CodecException($"帧长度不足: {span.Length} < {MIN_FRAME_SIZE}");
            }

            // 2. 验证帧头
            var header = BinaryPrimitives.ReadUInt16BigEndian(span);
            if (header != FRAME_HEADER) {
                throw new CodecException($"无效的帧头: 0x{header:X4}");
            }

            // 3. 读取批次 ID
            var batchId = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(2));

            // 4. 读取轴数
            byte mainCount = span[6];
            byte ejectCount = span[7];

            // 5. 验证帧长度是否匹配
            int expectedLength = 2 + 4 + 2 + (mainCount + ejectCount) * 2 + 2 + 2;
            if (span.Length != expectedLength) {
                throw new CodecException(
                    $"帧长度不匹配: 期望 {expectedLength}，实际 {span.Length}");
            }

            // 6. 读取速度数据
            int offset = 8;
            var mainSpeeds = new decimal[mainCount];
            for (int i = 0; i < mainCount; i++) {
                var speedInt = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset));
                mainSpeeds[i] = speedInt;
                offset += 2;
            }

            var ejectSpeeds = new decimal[ejectCount];
            for (int i = 0; i < ejectCount; i++) {
                var speedInt = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset));
                ejectSpeeds[i] = speedInt;
                offset += 2;
            }

            // 7. 验证校验和
            var expectedCrc = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset));
            var actualCrc = CalculateCrc16(span.Slice(0, offset));
            if (expectedCrc != actualCrc) {
                throw new CodecException(
                    $"校验和错误: 期望 0x{expectedCrc:X4}，实际 0x{actualCrc:X4}");
            }
            offset += 2;

            // 8. 验证帧尾
            var footer = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
            if (footer != FRAME_FOOTER) {
                throw new CodecException($"无效的帧尾: 0x{footer:X4}");
            }

            // 9. 构造 SpeedSet
            var speedSet = new SpeedSet {
                BatchId = batchId,
                MainSegmentSpeeds = mainSpeeds,
                EjectSegmentSpeeds = ejectSpeeds,
                ReceivedAt = DateTime.Now,
                Source = VendorName
            };

            _logger.LogInformation(
                "成功解码自研协议数据: BatchId={BatchId}, Main={MainCount}, Eject={EjectCount}",
                batchId, mainCount, ejectCount);

            return speedSet;
        }

        public async Task<byte[]?> EncodeAsync(
            SpeedSet speedSet,
            CancellationToken ct = default) {
            
            byte mainCount = (byte)speedSet.MainSegmentSpeeds.Length;
            byte ejectCount = (byte)speedSet.EjectSegmentSpeeds.Length;

            int frameSize = 2 + 4 + 2 + (mainCount + ejectCount) * 2 + 2 + 2;
            var buffer = new byte[frameSize];
            var span = buffer.AsSpan();

            int offset = 0;

            // 1. 写入帧头
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), FRAME_HEADER);
            offset += 2;

            // 2. 写入批次 ID
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset), speedSet.BatchId);
            offset += 4;

            // 3. 写入轴数
            span[offset++] = mainCount;
            span[offset++] = ejectCount;

            // 4. 写入主段速度
            foreach (var speed in speedSet.MainSegmentSpeeds) {
                BinaryPrimitives.WriteInt16LittleEndian(
                    span.Slice(offset), (short)speed);
                offset += 2;
            }

            // 5. 写入疏散段速度
            foreach (var speed in speedSet.EjectSegmentSpeeds) {
                BinaryPrimitives.WriteInt16LittleEndian(
                    span.Slice(offset), (short)speed);
                offset += 2;
            }

            // 6. 计算并写入校验和
            var crc = CalculateCrc16(span.Slice(0, offset));
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(offset), crc);
            offset += 2;

            // 7. 写入帧尾
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), FRAME_FOOTER);

            return buffer;
        }

        public bool TryDecodeMessage(
            ReadOnlyMemory<byte> data,
            out SpeedSet? result) {
            
            result = null;
            try {
                result = DecodeAsync(data).GetAwaiter().GetResult();
                return result != null;
            }
            catch {
                return false;
            }
        }

        private ushort CalculateCrc16(ReadOnlySpan<byte> data) {
            // 实现 CRC-16 校验算法（示例：CRC-16/MODBUS）
            ushort crc = 0xFFFF;
            foreach (byte b in data) {
                crc ^= b;
                for (int i = 0; i < 8; i++) {
                    if ((crc & 0x0001) != 0) {
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    } else {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }
    }
}
```

### 步骤 4: 注册编解码器

在依赖注入容器中注册新的编解码器：

```csharp
// Program.cs
services.AddSingleton<IUpstreamCodec, HuararyCodec>();  // 华睿
services.AddSingleton<IUpstreamCodec, GuiweiCodec>();   // 归位
services.AddSingleton<IUpstreamCodec, HikvisionCodec>(); // 海康威视
services.AddSingleton<IUpstreamCodec, CustomBinaryCodec>(); // 自研系统
```

### 步骤 5: 配置选择

通过配置文件指定使用的协议：

```json
{
  "UpstreamOptions": {
    "Vendor": "Hikvision",  // 或 "Huarary", "Guiwei", "Custom"
    "Endpoint": "http://192.168.1.200:8080/api/speeds",
    "PollingIntervalMs": 100,
    "Timeout": "00:00:05"
  }
}
```

### 步骤 6: 传输控制

`TransportStreamService` 会自动使用配置的编解码器：

```csharp
public class TransportStreamService : BackgroundService {
    private readonly IEnumerable<IUpstreamCodec> _codecs;
    private readonly IOptions<UpstreamOptions> _options;

    protected override async Task ExecuteAsync(CancellationToken ct) {
        // 根据配置选择编解码器
        var codec = _codecs.FirstOrDefault(c =>
            c.VendorName.Equals(_options.Value.Vendor,
                StringComparison.OrdinalIgnoreCase));

        if (codec == null) {
            throw new ConfigurationException(
                $"未找到上游协议编解码器: {_options.Value.Vendor}");
        }

        while (!ct.IsCancellationRequested) {
            try {
                // 接收数据（TCP、HTTP、UDP 等）
                var rawData = await ReceiveDataAsync(ct);

                // 解码为标准格式
                var speedSet = await codec.DecodeAsync(rawData, ct);

                if (speedSet != null) {
                    // 应用到轴控制器
                    await _axisController.ApplySpeedSetAsync(speedSet, ct);
                }
            }
            catch (CodecException ex) {
                _logger.LogError(ex, "协议解码失败");
            }

            await Task.Delay(_options.Value.PollingIntervalMs, ct);
        }
    }
}
```

## 协议兼容性矩阵

| 厂商         | 协议类型       | 编码支持 | 解码支持 | 传输方式        |
|--------------|----------------|----------|----------|-----------------|
| 华睿 Huarary | JSON           | ❌       | ✅       | HTTP/WebSocket  |
| 归位 Guiwei  | JSON           | ❌       | ✅       | HTTP            |
| 海康 Hikvision | JSON         | ❌       | ✅       | HTTP/MQTT       |
| 自研 Custom  | 二进制         | ✅       | ✅       | TCP/UDP         |

## 数据流向

```
┌──────────────┐
│  上游系统    │ (华睿/归位/海康/自研)
└──────┬───────┘
       │ HTTP/TCP/WebSocket
       ↓
┌──────────────────────────┐
│   IUpstreamCodec         │ (协议解码)
│   - HuararyCodec         │
│   - GuiweiCodec          │
│   - HikvisionCodec       │
│   - CustomBinaryCodec    │
└──────────┬───────────────┘
           │ SpeedSet (标准格式)
           ↓
┌──────────────────────────┐
│ TransportStreamService   │
└──────────┬───────────────┘
           │ SpeedSet
           ↓
┌──────────────────────────┐
│   IAxisController        │ (批量下发)
└──────────┬───────────────┘
           │ 单个轴速度
           ↓
┌──────────────────────────┐
│   IAxisDrive (N个轴)     │
└──────────────────────────┘
```

## 测试策略

### 1. 单元测试

测试编解码器的正确性：

```csharp
public class HikvisionCodecTests {
    [Fact]
    public async Task DecodeAsync_ValidJson_ReturnsSpeedSet() {
        // Arrange
        var codec = new HikvisionCodec(Mock.Of<ILogger<HikvisionCodec>>());
        var json = @"{
            ""batchId"": 123,
            ""speeds"": {
                ""main"": [800, 850, 900],
                ""eject"": [1000, 1050]
            }
        }";
        var data = Encoding.UTF8.GetBytes(json);

        // Act
        var result = await codec.DecodeAsync(data);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.BatchId);
        Assert.Equal(3, result.MainSegmentSpeeds.Length);
        Assert.Equal(2, result.EjectSegmentSpeeds.Length);
        Assert.Equal(800, result.MainSegmentSpeeds[0]);
    }

    [Fact]
    public async Task DecodeAsync_InvalidJson_ThrowsCodecException() {
        // Arrange
        var codec = new HikvisionCodec(Mock.Of<ILogger<HikvisionCodec>>());
        var invalidJson = "{ invalid json }";
        var data = Encoding.UTF8.GetBytes(invalidJson);

        // Act & Assert
        await Assert.ThrowsAsync<CodecException>(
            () => codec.DecodeAsync(data));
    }
}
```

### 2. 集成测试

测试完整的数据流：

```csharp
public class UpstreamIntegrationTests {
    [Fact]
    public async Task EndToEnd_HikvisionData_AppliedToAxes() {
        // 模拟海康威视数据源
        // 验证数据流经编解码器后正确应用到轴
    }
}
```

## 最佳实践

1. **错误处理**: 使用 `CodecException` 包装解码错误，提供清晰的错误消息
2. **验证**: 验证接收到的数据完整性（帧头尾、校验和、必要字段）
3. **日志记录**: 记录解码成功/失败的详细信息
4. **性能**: 避免不必要的内存分配，使用 `Span<T>` 和 `Memory<T>`
5. **可配置**: 支持通过配置文件调整协议参数（如超时、重试次数）
6. **向后兼容**: 在协议版本升级时保持向后兼容性
7. **文档**: 提供协议格式的详细文档和示例数据

## 常见问题

### Q: 如何处理不完整的数据包？
A: 实现缓冲区累积机制，等待完整数据包到达后再解码。

### Q: 如何支持多种数据格式（JSON + XML）？
A: 实现多个编解码器，通过配置或自动检测选择合适的编解码器。

### Q: 如何处理协议版本升级？
A: 在编解码器中检查版本号，实现不同版本的解码逻辑。

### Q: 如何优化大数据量的解码性能？
A: 
- 使用 `Span<T>` 和 `Memory<T>` 避免不必要的分配
- 实现对象池复用 DTO 对象
- 考虑使用 System.Text.Json 的源生成器

## 参考实现

- **完整示例**: 参考 `ZakYip.Singulation.Protocol/Vendors/Huarary/` 和 `Guiwei/` 目录
- **JSON 协议**: `HuararyCodec.cs`, `GuiweiCodec.cs`
- **接口定义**: `IUpstreamCodec.cs`

## 总结

通过遵循本指南，可以轻松集成新的上游数据源，同时保持系统的灵活性和可扩展性。关键是：

1. ✅ 实现 `IUpstreamCodec` 接口
2. ✅ 处理厂商特定的数据格式和协议
3. ✅ 注册到依赖注入容器
4. ✅ 编写完整的测试
5. ✅ 更新配置文件

如有疑问，请参考华睿和归位的实现或联系架构团队。
