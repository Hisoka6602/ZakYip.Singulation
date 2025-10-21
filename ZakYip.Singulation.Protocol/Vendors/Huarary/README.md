# 华雷（Huarary）视觉通信协议

## 概述

华雷（Huarary）协议是一种完整的帧式 TCP 通信协议，专为单件分离系统的视觉引导设计。该协议支持多种帧类型（速度、位置、指令、状态），采用控制码区分帧类型，使用 XOR 校验保证数据完整性。

## 协议特点

- **协议类型**：TCP 帧式协议
- **传输方向**：双向（视觉系统 ↔ 控制器）
- **帧结构**：完整帧格式（起始符 + 控制码 + 长度 + 载荷 + 校验 + 结束符）
- **校验方式**：XOR 校验（从起始符到载荷末尾）
- **数据编码**：小端序（Little Endian）
- **速度单位**：mm/s（毫米每秒）
- **位置单位**：mm（毫米）和度（角度）

## 核心组件

### 1. HuararyCodec
**文件**：`HuararyCodec.cs`

**功能**：华雷协议编解码器，实现 `IUpstreamCodec` 接口。

**主要方法**：
- `TryDecodeSpeed()`：解析速度帧（0x81）
- `TryDecodePositions()`：解析位置帧（0x82）
- `EncodeStartCommand()`：编码启动命令（0x89）
- `EncodeStopCommand()`：编码停止命令（0x8A）
- `EncodeModeCommand()`：编码模式设置命令（0x84）
- `EncodeDistanceCommand()`：编码分离距离设置命令（0x86）
- `EncodePauseCommand()`：编码暂停命令（0x85）
- `EncodeStatusRequest()`：编码状态查询命令（0x5A）
- `EncodeParamsRequest()`：编码参数查询命令（0x5C）

**构造参数**：
```csharp
public HuararyCodec(int mainCount, int ejectCount)
```
- **mainCount**：分离段电机数量（例如：28）
- **ejectCount**：扩散段电机数量（例如：3）

### 2. HuararyControl
**文件**：`HuararyControl.cs`

**功能**：协议控制字符和控制码常量定义。

**控制字符常量**：
```csharp
public static class HuararyControl {
    public const byte Start = 0x2A;      // 起始符：'*'
    public const byte End = 0x3B;        // 结束符：';'
    
    // 下行帧（视觉 → 控制器）
    public const byte CtrlSpeed = 0x81;  // 速度帧
    public const byte CtrlPos = 0x82;    // 位置帧
    
    // 上行帧（控制器 → 视觉）
    public const byte CmdStart = 0x89;   // 启动命令
    public const byte CmdStop = 0x8A;    // 停止命令
    public const byte CmdMode = 0x84;    // 模式设置
    public const byte CmdDist = 0x86;    // 分离距离设置
    public const byte CmdPause = 0x85;   // 暂停命令
    
    // 查询与响应
    public const byte ReqStatus = 0x5A;  // 状态查询
    public const byte RespStatus = 0x5B; // 状态响应
    public const byte ReqParams = 0x5C;  // 参数查询
    public const byte RespParams = 0x5C; // 参数响应
}
```

## 帧结构

### 通用帧格式

```
┌────────┬────────┬────────┬────────────┬────────┬────────┐
│ 起始符  │ 控制码  │  长度  │   载荷      │  校验  │ 结束符  │
│  0x2A  │ 1 byte │ 2 bytes│  N bytes   │ 1 byte │  0x3B  │
└────────┴────────┴────────┴────────────┴────────┴────────┘
   1字节    1字节    小端序      可变       XOR     1字节
```

**字段说明**：
- **起始符（Start）**：固定为 `0x2A`（ASCII：`*`）
- **控制码（Control）**：帧类型标识（1字节）
- **长度（Length）**：整个帧的总长度，包括起止符（2字节，小端序）
- **载荷（Payload）**：实际数据内容（N 字节，由控制码决定格式）
- **校验（Checksum）**：XOR 校验值（1字节）
- **结束符（End）**：固定为 `0x3B`（ASCII：`;`）

**校验计算**：
```csharp
byte checksum = 0;
for (int i = 0; i < checksumIndex; i++) {
    checksum ^= frame[i];  // 从起始符到载荷末尾
}
```

### 速度帧（0x81）

**控制码**：`0x81`  
**方向**：视觉系统 → 控制器  
**用途**：下发多路电机的目标速度

**帧结构**：
```
2A 81 [LEN_L LEN_H] [V0 V1 V2 V3] [V4 V5 V6 V7] ... [XOR] 3B
```

**载荷格式**：
```
┌────────────────────────────────────┐
│ 速度1 (int32) │ 速度2 (int32) │ ... │
└────────────────────────────────────┘
     4字节          4字节
```

**示例**：31路全速 1000 mm/s
```
2A 81 82 00                               # 起始 + 控制码 + 长度(130字节)
E8 03 00 00 E8 03 00 00 E8 03 00 00 ...  # 31 × int32 (1000)
C2 3B                                     # 校验 + 结束
```

### 位置帧（0x82）

**控制码**：`0x82`  
**方向**：视觉系统 → 控制器  
**用途**：下发检测到的包裹位置和姿态

**帧结构**：
```
2A 82 [LEN_L LEN_H] [COUNT] [POSE1] [POSE2] ... [XOR] 3B
```

**载荷格式**：
```
┌───────────────────────────────────────────────┐
│ Count (int32) │ Pose 1 │ Pose 2 │ ... │ Pose N │
└───────────────────────────────────────────────┘
     4字节         20字节    20字节        20字节
```

**单个 Pose 结构**：
```
┌──────────────────────────────────────────────┐
│ X (float) │ Y (float) │ L (float) │ W (float) │ A (float) │
└──────────────────────────────────────────────┘
   4字节       4字节       4字节       4字节       4字节
```
- **X, Y**：包裹中心坐标（mm）
- **L**：包裹长度（mm）
- **W**：包裹宽度（mm）
- **A**：包裹角度（度）

**示例**：1个包裹，中心(100, 200)，尺寸 150×200，角度 80°
```
2A 82 1E 00                      # 起始 + 控制码 + 长度(30字节)
01 00 00 00                      # Count = 1
64 00 00 00 C8 00 00 00          # X=100, Y=200
96 00 00 00 C8 00 00 00 50 00 00 00  # L=150, W=200, A=80
15 3B                            # 校验 + 结束
```

### 指令帧

#### 启动命令（0x89）

**控制码**：`0x89`  
**方向**：控制器 → 视觉系统  
**用途**：启动分离系统

**帧结构**：
```
2A 89 0A 00 00 00 00 01 A8 3B
```
- 长度：10 字节
- 载荷：`00 00 00 01`（int32，值为 1）

#### 停止命令（0x8A）

**控制码**：`0x8A`  
**方向**：控制器 → 视觉系统  
**用途**：停止分离系统

**帧结构**：
```
2A 8A 0A 00 00 00 00 00 A9 3B
```
- 长度：10 字节
- 载荷：`00 00 00 00`（int32，值为 0）

#### 模式设置命令（0x84）

**控制码**：`0x84`  
**方向**：控制器 → 视觉系统  
**用途**：设置分离模式、最大最小速度

**帧结构**：
```
2A 84 0B 00 [MODE] [VMAX_L VMAX_H] [VMIN_L VMIN_H] [XOR] 3B
```

**载荷格式**：
```
┌──────────────────────────────────────┐
│ Mode (byte) │ Vmax (ushort) │ Vmin (ushort) │
└──────────────────────────────────────┘
     1字节        2字节(小端)     2字节(小端)
```

**示例**：模式2，Vmax=2000，Vmin=1200
```
2A 84 0B 00 02 D0 07 B0 04 C4 3B
```

#### 分离距离设置命令（0x86）

**控制码**：`0x86`  
**方向**：控制器 → 视觉系统  
**用途**：设置分离距离

**帧结构**：
```
2A 86 0A 00 [MODE] [DIST_L DIST_H] 00 [XOR] 3B
```

**载荷格式**：
```
┌────────────────────────────────┐
│ Mode (byte) │ Distance (ushort) │ Reserved │
└────────────────────────────────┘
     1字节        2字节(小端)        1字节
```

**示例**：模式2，距离=600mm
```
2A 86 0A 00 02 58 02 00 FE 3B
```

#### 暂停命令（0x85）

**控制码**：`0x85`  
**方向**：控制器 → 视觉系统  
**用途**：暂停或恢复分离

**帧结构**：
```
2A 85 0A 00 00 00 00 [FLAG] [XOR] 3B
```
- FLAG = 1：暂停
- FLAG = 0：恢复

**示例**：暂停
```
2A 85 0A 00 00 00 00 01 A4 3B
```

### 查询与响应帧

#### 状态查询（0x5A）

**控制码**：`0x5A`  
**方向**：控制器 → 视觉系统  
**用途**：查询视觉系统状态

**帧结构**：
```
2A 5A 0A 00 00 00 00 00 70 3B
```

#### 状态响应（0x5B）

**控制码**：`0x5B`  
**方向**：视觉系统 → 控制器  
**用途**：返回系统状态信息

**帧结构**：
```
2A 5B [LEN] [STATUS_DATA] [XOR] 3B
```

**载荷示例**：
```
01 00 19 02          # 状态标志
02 41 31 00          # 版本信息 "A1"
02 42 32 02          # 型号信息 "B2"
```

#### 参数查询（0x5C）

**控制码**：`0x5C`  
**方向**：双向（查询与响应使用相同控制码）  
**用途**：查询或返回参数配置

**帧结构**：
```
2A 5C [LEN] [PARAM_DATA] [XOR] 3B
```

**载荷示例**：
```
54 1F 70 17 9A 1F    # 速度范围
03 E8 03 0A          # 其他参数
02 41 31             # 版本 "A1"
```

## 解码实现

### 速度帧解码

```csharp
public bool TryDecodeSpeed(ReadOnlySpan<byte> frame, out SpeedSet set) {
    set = default;
    
    // 1. 基本校验
    if (frame.Length < 6) return false;
    if (frame[0] != HuararyControl.Start) return false;
    if (frame[^1] != HuararyControl.End) return false;
    
    // 2. 控制码校验
    var ctrl = frame[1];
    if (ctrl != HuararyControl.CtrlSpeed) return false;
    
    // 3. 长度校验
    var len = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(2, 2));
    if (len != frame.Length) return false;
    
    // 4. XOR 校验
    var checksumIndex = frame.Length - 2;
    byte xor = 0;
    for (int i = 0; i < checksumIndex; i++) {
        xor ^= frame[i];
    }
    if (xor != frame[checksumIndex]) return false;
    
    // 5. 提取载荷
    var payload = frame.Slice(4, frame.Length - 6);
    if (payload.Length % 4 != 0) return false;
    
    // 6. 解析速度值
    var n = payload.Length / 4;
    var all = new int[n];
    for (int i = 0, off = 0; i < n; i++, off += 4) {
        all[i] = BinaryPrimitives.ReadInt32LittleEndian(
            payload.Slice(off, 4)
        );
    }
    
    // 7. 分离主段和扩散段
    var main = new int[Math.Min(_mainCount, n)];
    var eject = new int[Math.Min(_ejectCount, Math.Max(0, n - _mainCount))];
    
    Array.Copy(all, 0, main, 0, main.Length);
    if (eject.Length > 0) {
        Array.Copy(all, _mainCount, eject, 0, eject.Length);
    }
    
    set = new SpeedSet(DateTime.Now, 0, main, eject);
    return true;
}
```

### 位置帧解码

```csharp
public bool TryDecodePositions(ReadOnlySpan<byte> frame, 
    out IReadOnlyList<ParcelPose> poses) {
    poses = [];
    
    // 基本校验（同速度帧）
    // ...
    
    // 解析包裹数量
    var payload = frame.Slice(4, frame.Length - 6);
    var count = BinaryPrimitives.ReadInt32LittleEndian(payload);
    
    var list = new List<ParcelPose>(count);
    var off = 4;
    
    for (var i = 0; i < count; i++) {
        if (off + 20 > payload.Length) break;
        
        // 读取 5 个 float 值
        var x = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))
        ); off += 4;
        
        var y = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))
        ); off += 4;
        
        var l = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))
        ); off += 4;
        
        var w = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))
        ); off += 4;
        
        var a = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))
        ); off += 4;
        
        list.Add(new ParcelPose {
            CenterX = x,
            CenterY = y,
            Length = l,
            Width = w,
            Angle = a
        });
    }
    
    poses = list;
    return true;
}
```

## 编码实现

### 指令编码辅助方法

```csharp
// 通用帧编码器
private static byte[] EncodeFrame(byte ctrl, ReadOnlySpan<byte> payload) {
    var totalLen = 1 + 1 + 2 + payload.Length + 1 + 1;
    var frame = new byte[totalLen];
    
    frame[0] = HuararyControl.Start;
    frame[1] = ctrl;
    BinaryPrimitives.WriteUInt16LittleEndian(
        frame.AsSpan(2, 2), (ushort)totalLen
    );
    
    payload.CopyTo(frame.AsSpan(4));
    
    // 计算 XOR 校验
    byte xor = 0;
    for (int i = 0; i < totalLen - 2; i++) {
        xor ^= frame[i];
    }
    frame[totalLen - 2] = xor;
    frame[totalLen - 1] = HuararyControl.End;
    
    return frame;
}

// 启动命令
public byte[] EncodeStartCommand() {
    var payload = new byte[4];
    BinaryPrimitives.WriteInt32LittleEndian(payload, 1);
    return EncodeFrame(HuararyControl.CmdStart, payload);
}

// 模式设置命令
public byte[] EncodeModeCommand(byte mode, ushort vmax, ushort vmin) {
    var payload = new byte[5];
    payload[0] = mode;
    BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(1), vmax);
    BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(3), vmin);
    return EncodeFrame(HuararyControl.CmdMode, payload);
}
```

## 配置示例

### 典型配置（28 分离 + 3 扩散）

```csharp
// 创建编解码器
var codec = new HuararyCodec(
    mainCount: 28,    // 28路分离段
    ejectCount: 3     // 3路扩散段
);

// 解码速度帧
if (codec.TryDecodeSpeed(frameBytes, out var speedSet)) {
    Console.WriteLine($"Timestamp: {speedSet.Timestamp}");
    Console.WriteLine($"Main speeds: {speedSet.Main.Count}");
    Console.WriteLine($"Eject speeds: {speedSet.Eject.Count}");
}

// 解码位置帧
if (codec.TryDecodePositions(frameBytes, out var poses)) {
    foreach (var pose in poses) {
        Console.WriteLine($"Parcel at ({pose.CenterX}, {pose.CenterY})");
    }
}

// 编码启动命令
var startCmd = codec.EncodeStartCommand();
await stream.WriteAsync(startCmd);
```

### 配置文件（appsettings.json）

```json
{
  "Upstream": {
    "Vendor": "Huarary",
    "SpeedPort": 8001,
    "PositionPort": 8002,
    "HeartbeatPort": 8003,
    "Codec": {
      "MainCount": 28,
      "EjectCount": 3
    }
  }
}
```

## 测试报文集

详见 `vision_mock_packets.md` 文档，包含：
- 速度帧示例（全速、交替、斜坡）
- 位置帧示例（0包裹、1包裹、2包裹）
- 各类指令帧示例
- 状态和参数响应示例

## 性能优化

### 1. 零分配解析
```csharp
// 使用 Span<byte> 避免内存分配
public bool TryDecodeSpeed(ReadOnlySpan<byte> frame, out SpeedSet set)
```

### 2. 缓冲区池化
```csharp
var buffer = ArrayPool<byte>.Shared.Rent(4096);
try {
    // 读取和解析
}
finally {
    ArrayPool<byte>.Shared.Return(buffer);
}
```

### 3. 帧缓存
对于高频重复的指令帧，可预先编码并缓存：
```csharp
private static readonly byte[] CachedStartCmd = 
    new HuararyCodec(0, 0).EncodeStartCommand();
```

## 调试与故障排查

### 帧完整性检查工具

```csharp
public static bool ValidateFrame(ReadOnlySpan<byte> frame) {
    if (frame.Length < 6) {
        Console.WriteLine("帧太短");
        return false;
    }
    
    if (frame[0] != 0x2A) {
        Console.WriteLine("起始符错误");
        return false;
    }
    
    if (frame[^1] != 0x3B) {
        Console.WriteLine("结束符错误");
        return false;
    }
    
    var len = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(2, 2));
    if (len != frame.Length) {
        Console.WriteLine($"长度不匹配: 声明{len}, 实际{frame.Length}");
        return false;
    }
    
    byte xor = 0;
    for (int i = 0; i < frame.Length - 2; i++) {
        xor ^= frame[i];
    }
    
    if (xor != frame[frame.Length - 2]) {
        Console.WriteLine($"校验失败: 计算{xor:X2}, 实际{frame[frame.Length - 2]:X2}");
        return false;
    }
    
    Console.WriteLine("帧校验通过");
    return true;
}
```

### 常见问题

**问题 1：校验失败**
- 确认校验范围：从 frame[0] 到 frame[length-3]
- 检查是否误包含了校验字节本身
- 验证帧边界是否正确

**问题 2：长度字段不匹配**
- 检查是否使用小端序读取
- 确认长度包含起止符
- 验证载荷长度计算

**问题 3：float 解析错误**
- 确认使用 `BitConverter.Int32BitsToSingle()`
- 注意不是 `BitConverter.ToSingle()`
- 先读取 int32，再转换为 float

**问题 4：分离主段和扩散段错误**
- 检查 `mainCount` 和 `ejectCount` 配置
- 确认数组切片边界
- 验证 `Array.Copy` 的索引和长度

## 协议扩展

### 自定义帧类型
可定义新的控制码扩展协议：
```csharp
public const byte CustomCommand = 0x90;

public byte[] EncodeCustomCommand(byte[] data) {
    return EncodeFrame(CustomCommand, data);
}
```

### 版本兼容
建议在载荷中添加版本字段：
```
[VERSION(1)] [DATA...]
```

## 与归位协议对比

| 特性 | 华雷协议（Huarary） | 归位协议（Guiwei） |
|------|-------------------|-------------------|
| 帧结构 | 完整（控制码+长度+校验） | 简化（仅起止符） |
| 校验方式 | XOR 校验 | 无 |
| 帧类型 | 多种（速度/位置/命令） | 单一（速度） |
| 双向通信 | 支持 | 单向 |
| 可靠性 | TCP + 应用层校验 | 仅 TCP |
| 复杂度 | 中等 | 低 |
| 适用场景 | 复杂视觉引导系统 | 简单速度控制 |

**选择建议**：
- **华雷协议**：需要位置信息、双向通信、多种指令的复杂场景
- **归位协议**：仅需速度控制的简单场景，追求低延迟和简单性

## 相关文档

- [归位协议文档](../Guiwei/README.md)
- [上游传输层文档](../../../ZakYip.Singulation.Transport/README.md)
- [协议抽象接口](../../Abstractions/IUpstreamCodec.cs)
- [测试报文集](./vision_mock_packets.md)

## 附录：控制码速查表

| 控制码 | 名称 | 方向 | 用途 |
|--------|------|------|------|
| 0x81 | CtrlSpeed | 下行 | 速度控制 |
| 0x82 | CtrlPos | 下行 | 位置信息 |
| 0x84 | CmdMode | 上行 | 模式设置 |
| 0x85 | CmdPause | 上行 | 暂停控制 |
| 0x86 | CmdDist | 上行 | 距离设置 |
| 0x89 | CmdStart | 上行 | 启动命令 |
| 0x8A | CmdStop | 上行 | 停止命令 |
| 0x5A | ReqStatus | 上行 | 状态查询 |
| 0x5B | RespStatus | 下行 | 状态响应 |
| 0x5C | ReqParams | 双向 | 参数查询/响应 |

**注**：上行=控制器→视觉，下行=视觉→控制器
