# 归位（Guiwei）上游通信协议

## 概述

归位（Guiwei）协议是一种简化的 TCP 通信协议，专为单件分离系统的速度控制设计。该协议使用起止符包裹的帧结构，载荷为多路电机速度值（mm/s），采用小端序（Little Endian）编码。

## 协议特点

- **协议类型**：TCP 流式协议
- **传输方向**：上游设备（视觉系统） → 控制器
- **帧结构**：简化帧格式，无控制码和长度字段
- **校验方式**：无 XOR 校验，依赖 TCP 层保证可靠性
- **数据编码**：int32 小端序
- **速度单位**：mm/s（毫米每秒）

## 核心组件

### 1. GuiweiCodec
**文件**：`GuiweiCodec.cs`

**功能**：归位协议编解码器，实现 `IUpstreamCodec` 接口。

**主要方法**：
- `TryDecodeSpeed()`：解析速度帧，提取分离段和扩散段速度
- `Encode()`：编码速度帧（可选，用于测试）

**构造参数**：
```csharp
public GuiweiCodec(int mainCount, int ejectCount)
```
- **mainCount**：分离段电机数量（例如：28）
- **ejectCount**：扩散段电机数量（例如：1）

### 2. GuiweiControl
**文件**：`GuiweiControl.cs`

**功能**：协议控制字符常量定义。

**常量定义**：
```csharp
public static class GuiweiControl {
    public const byte Start = 0x2A;  // 起始符：'*'
    public const byte End = 0x3B;    // 结束符：';'
}
```

## 帧结构

### 速度帧格式

```
┌────────┬────────────────────────┬────────┐
│ 起始符  │       速度载荷          │ 结束符  │
│  0x2A  │  N × int32 (小端序)     │  0x3B  │
└────────┴────────────────────────┴────────┘
   1字节      N × 4字节              1字节
```

**字段说明**：
- **起始符（Start）**：固定为 `0x2A`（ASCII：`*`）
- **速度载荷（Payload）**：N 个 int32 值（4字节/值，小端序）
  - 前 `mainCount` 个值：分离段电机速度（mm/s）
  - 后 `ejectCount` 个值：扩散段电机速度（mm/s）
- **结束符（End）**：固定为 `0x3B`（ASCII：`;`）

**帧长度**：
```
总长度 = 1 (起始符) + N × 4 (速度) + 1 (结束符)
       = 2 + N × 4 字节
```

### 速度值编码

**数据类型**：int32（32位有符号整数）  
**字节序**：小端序（Little Endian）  
**速度单位**：mm/s（毫米每秒）  
**有效范围**：-2,147,483,648 ~ 2,147,483,647 mm/s

**示例**：速度值 1000 mm/s 的编码
```
十进制：1000
十六进制：0x000003E8
小端序字节：E8 03 00 00
```

## 通信序列

### 正常归位序列

以 28 路分离段为例，归位过程分多个阶段递减速度：

**阶段 1：减速至 1000 mm/s**
```
2A E8 03 00 00 E8 03 00 00 E8 03 00 00 ... (28个) ... 3B
```
- 所有轴速度：1000 mm/s
- 载荷：28 × 4 = 112 字节
- 总帧长：1 + 112 + 1 = 114 字节

**阶段 2：减速至 700 mm/s**
```
2A BC 02 00 00 BC 02 00 00 BC 02 00 00 ... (28个) ... 3B
```
- 所有轴速度：700 mm/s (0x02BC)

**阶段 3：减速至 400 mm/s**
```
2A 90 01 00 00 90 01 00 00 90 01 00 00 ... (28个) ... 3B
```
- 所有轴速度：400 mm/s (0x0190)

**阶段 4：减速至 200 mm/s**
```
2A C8 00 00 00 C8 00 00 00 C8 00 00 00 ... (28个) ... 3B
```
- 所有轴速度：200 mm/s (0x00C8)

**阶段 5-N：完全停止**
```
2A 00 00 00 00 00 00 00 00 00 00 00 00 ... (28个) ... 3B
```
- 所有轴速度：0 mm/s
- 进入禁止状态（EnableFlag = 0）

### 归位带反转清线序列

在正常归位基础上，最后阶段进行短暂反转以清除残留物料：

**反转阶段（R06）**：
```
2A 00 00 00 00 00 00 00 00 00 00 00 00 ... FF FF FF FF 3B
```
- 前 27 轴：0 mm/s
- 第 28 轴：-1 mm/s (0xFFFFFFFF，小端序)
- 作用：轻微反转清线

**完全停止（R07-R08）**：
```
2A 00 00 00 00 00 00 00 00 00 00 00 00 ... (28个) ... 3B
```
- 所有轴速度：0 mm/s

## 解码实现

### 解码流程

```csharp
public bool TryDecodeSpeed(ReadOnlySpan<byte> frame, out SpeedSet set) {
    set = default;
    
    // 1. 检查最小长度（至少包含起止符）
    if (frame.Length < 2) return false;
    
    // 2. 验证起止符
    if (frame[0] != GuiweiControl.Start || 
        frame[^1] != GuiweiControl.End) 
        return false;
    
    // 3. 提取载荷（去掉起止符）
    var payload = frame.Slice(1, frame.Length - 2);
    
    // 4. 检查载荷长度（必须是 4 的倍数）
    if (payload.Length % 4 != 0) return false;
    
    // 5. 解析速度值（小端序 int32）
    int n = payload.Length / 4;
    var all = new int[n];
    for (int i = 0, off = 0; i < n; i++, off += 4) {
        all[i] = BinaryPrimitives.ReadInt32LittleEndian(
            payload.Slice(off, 4)
        );
    }
    
    // 6. 分离主段和扩散段
    var main = new int[Math.Min(_mainCount, n)];
    var eject = new int[Math.Min(_ejectCount, Math.Max(0, n - _mainCount))];
    
    Array.Copy(all, 0, main, 0, main.Length);
    if (eject.Length > 0) {
        Array.Copy(all, _mainCount, eject, 0, eject.Length);
    }
    
    // 7. 构造 SpeedSet
    set = new SpeedSet(
        Main: main.Select(v => new Speed(v, SpeedUnit.MmPerSec)).ToList(),
        Eject: eject.Select(v => new Speed(v, SpeedUnit.MmPerSec)).ToList()
    );
    
    return true;
}
```

### 错误处理

解码失败的情况：
1. **帧长度不足**：`frame.Length < 2`
2. **起止符不匹配**：`frame[0] != 0x2A` 或 `frame[^1] != 0x3B`
3. **载荷长度错误**：`payload.Length % 4 != 0`

**解码失败时**：
- 返回 `false`
- `set` 输出参数为 `default`
- 上层应丢弃该帧并等待下一帧

## 配置示例

### 典型配置（28 分离 + 1 扩散）

```csharp
// 创建编解码器
var codec = new GuiweiCodec(
    mainCount: 28,    // 28路分离段
    ejectCount: 1     // 1路扩散段
);

// 解码速度帧
if (codec.TryDecodeSpeed(frameBytes, out var speedSet)) {
    // 分离段速度
    foreach (var speed in speedSet.Main) {
        Console.WriteLine($"Main: {speed.Value} mm/s");
    }
    
    // 扩散段速度
    foreach (var speed in speedSet.Eject) {
        Console.WriteLine($"Eject: {speed.Value} mm/s");
    }
}
```

### 配置文件（appsettings.json）

```json
{
  "Upstream": {
    "Vendor": "Guiwei",
    "Port": 8001,
    "Codec": {
      "MainCount": 28,
      "EjectCount": 1
    }
  }
}
```

## 测试报文示例

### 示例 1：全速运行（1000 mm/s）

**十六进制**（29轴）：
```
2A 
E8 03 00 00 E8 03 00 00 E8 03 00 00 E8 03 00 00 
E8 03 00 00 E8 03 00 00 E8 03 00 00 E8 03 00 00 
E8 03 00 00 E8 03 00 00 E8 03 00 00 E8 03 00 00 
E8 03 00 00 E8 03 00 00 E8 03 00 00 E8 03 00 00 
E8 03 00 00 E8 03 00 00 E8 03 00 00 E8 03 00 00 
E8 03 00 00 E8 03 00 00 E8 03 00 00 E8 03 00 00 
E8 03 00 00 E8 03 00 00 E8 03 00 00 E8 03 00 00 
E8 03 00 00 
3B
```

**解析结果**：
- 前28轴（Main）：1000 mm/s
- 第29轴（Eject）：1000 mm/s

### 示例 2：梯度速度

**十六进制**（5轴示例）：
```
2A 
64 00 00 00   # 100 mm/s
C8 00 00 00   # 200 mm/s
2C 01 00 00   # 300 mm/s
90 01 00 00   # 400 mm/s
F4 01 00 00   # 500 mm/s
3B
```

**解析结果**：
- Main[0]: 100 mm/s
- Main[1]: 200 mm/s
- Main[2]: 300 mm/s
- Main[3]: 400 mm/s
- Main[4]: 500 mm/s

### 示例 3：反向速度（清线）

**十六进制**（2轴示例）：
```
2A 
00 00 00 00   # 0 mm/s
FF FF FF FF   # -1 mm/s (0xFFFFFFFF)
3B
```

**解析结果**：
- Main[0]: 0 mm/s
- Main[1]: -1 mm/s（反向）

## 性能优化

### 1. 零分配解析
使用 `ReadOnlySpan<byte>` 进行零拷贝解析：
```csharp
// 避免分配新数组
var payload = frame.Slice(1, frame.Length - 2);

// 直接在 span 上读取
int value = BinaryPrimitives.ReadInt32LittleEndian(payload);
```

### 2. 批量处理
对于高频数据流，可批量处理多个帧：
```csharp
while (stream.DataAvailable) {
    var frame = await ReadFrameAsync();
    if (codec.TryDecodeSpeed(frame, out var set)) {
        // 处理速度集
    }
}
```

### 3. 缓冲区复用
使用 `ArrayPool<byte>` 复用缓冲区：
```csharp
var buffer = ArrayPool<byte>.Shared.Rent(1024);
try {
    // 读取数据到 buffer
}
finally {
    ArrayPool<byte>.Shared.Return(buffer);
}
```

## 调试技巧

### 十六进制查看器
使用十六进制工具查看原始字节：
```bash
# Linux/Mac
xxd capture.bin

# Windows PowerShell
Format-Hex capture.bin
```

### 速度值计算
```python
# Python 脚本快速验证
import struct

# 小端序解码
speed = struct.unpack('<i', bytes.fromhex('E8030000'))[0]
print(f"Speed: {speed} mm/s")  # 输出：Speed: 1000 mm/s
```

### 常见问题诊断

**问题 1：解码始终失败**
- 检查起止符是否正确（0x2A 和 0x3B）
- 检查帧长度是否满足 `(n × 4) + 2`
- 使用十六进制工具检查原始字节

**问题 2：速度值异常**
- 确认字节序为小端序
- 检查是否误用大端序解析
- 验证速度范围是否合理（通常 0~3000 mm/s）

**问题 3：部分轴速度丢失**
- 检查 `mainCount` 和 `ejectCount` 配置
- 确认帧中速度值数量 `n ≥ mainCount + ejectCount`
- 查看日志中是否有截断警告

## 协议扩展建议

虽然当前协议简单高效，但可考虑以下扩展：

1. **版本字段**：在起始符后增加版本号字节
2. **帧序号**：添加序列号以检测丢包
3. **时间戳**：添加时间戳字段同步时序
4. **CRC 校验**：增加 CRC16/CRC32 校验提高可靠性
5. **控制帧**：增加控制帧类型（启动/停止/配置）

**注意**：任何扩展应保持向后兼容或通过版本字段区分。

## 与华雷协议对比

| 特性 | 归位协议（Guiwei） | 华雷协议（Huarary） |
|------|-------------------|-------------------|
| 帧结构 | 简化（仅起止符） | 完整（控制码+长度+校验） |
| 校验方式 | 无 | XOR 校验 |
| 长度字段 | 无 | 有（2字节小端） |
| 控制码 | 无 | 有（0x81=速度，0x82=位置） |
| 适用场景 | 简单速度控制 | 复杂多帧混合 |
| 可靠性 | 依赖TCP | TCP + 应用层校验 |
| 解析复杂度 | 低 | 中等 |

**选择建议**：
- **归位协议**：适用于简单速度控制场景，追求低延迟
- **华雷协议**：适用于需要位置、状态等多类型数据的复杂场景

## 相关文档

- [华雷协议文档](../Huarary/README.md)
- [上游传输层文档](../../../ZakYip.Singulation.Transport/README.md)
- [协议抽象接口](../../Abstractions/IUpstreamCodec.cs)
- [速度单位转换](../../../ZakYip.Singulation.Core/Utils/SpeedConverter.cs)

## 附录：速度值对照表

| mm/s | 十六进制（小端） | 字节序列 |
|------|----------------|---------|
| 0 | 0x00000000 | 00 00 00 00 |
| 100 | 0x00000064 | 64 00 00 00 |
| 200 | 0x000000C8 | C8 00 00 00 |
| 500 | 0x000001F4 | F4 01 00 00 |
| 1000 | 0x000003E8 | E8 03 00 00 |
| 2000 | 0x000007D0 | D0 07 00 00 |
| 3000 | 0x00000BB8 | B8 0B 00 00 |
| -1 | 0xFFFFFFFF | FF FF FF FF |
| -100 | 0xFFFFFF9C | 9C FF FF FF |

**计算公式**：
```csharp
// mm/s 转字节
byte[] ToBytes(int mmps) {
    return BitConverter.GetBytes(mmps);  // 自动小端序（x86/x64）
}

// 字节转 mm/s
int FromBytes(byte[] bytes, int offset) {
    return BitConverter.ToInt32(bytes, offset);
}
```
