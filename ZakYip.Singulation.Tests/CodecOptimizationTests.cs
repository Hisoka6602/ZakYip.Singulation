using System;
using System.Linq;
using ZakYip.Singulation.Core.Utils;
using ZakYip.Singulation.Protocol.Vendors.Guiwei;
using ZakYip.Singulation.Protocol.Vendors.Huarary;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// 测试编解码器优化（ArrayPool 使用）的正确性
/// </summary>
internal sealed class CodecOptimizationTests
{
    [MiniFact]
    public void HuararyCodec_TryDecodeSpeed_WithArrayPool_DecodesCorrectly()
    {
        // Arrange
        var codec = new HuararyCodec(mainCount: 2, ejectCount: 1);
        
        // 构造一个有效的速度帧: Start(1) Ctrl(1) Len(2) Payload(8 bytes=2 int32) XOR(1) End(1)
        // Payload: speed1=100 mm/s, speed2=200 mm/s
        var frame = new byte[14];
        frame[0] = 0x2A; // Start
        frame[1] = 0x81; // CtrlSpeed (corrected from 0x82)
        frame[2] = 14; frame[3] = 0; // Length = 14 (little endian)
        
        // Payload: 2 speeds
        WriteInt32LE(frame, 4, 100);  // speed1
        WriteInt32LE(frame, 8, 200);  // speed2
        
        frame[12] = ComputeXor(frame, 12); // XOR
        frame[13] = 0x3B; // End
        
        // Act
        var success = codec.TryDecodeSpeed(frame, DateTime.UtcNow, out var speedSet);
        
        // Assert
        MiniAssert.True(success, "解码应该成功");
        MiniAssert.Equal(2, speedSet.MainMmps.Count, "MainMmps 计数应为 2");
        MiniAssert.Equal(100, speedSet.MainMmps[0], "第一个速度应为 100");
        MiniAssert.Equal(200, speedSet.MainMmps[1], "第二个速度应为 200");
    }

    [MiniFact]
    public void GuiweiCodec_TryDecodeSpeed_WithArrayPool_DecodesCorrectly()
    {
        // Arrange
        var codec = new GuiweiCodec(mainCount: 3, ejectCount: 1);
        
        // 构造一个有效的归位协议速度帧: Start(1) Payload(12 bytes=3 int32) End(1)
        var frame = new byte[14];
        frame[0] = 0x2A; // Start
        
        // Payload: 3 speeds
        WriteInt32LE(frame, 1, 150);  // speed1
        WriteInt32LE(frame, 5, 250);  // speed2
        WriteInt32LE(frame, 9, 350);  // speed3
        
        frame[13] = 0x3B; // End
        
        // Act
        var success = codec.TryDecodeSpeed(frame, DateTime.UtcNow, out var speedSet);
        
        // Assert
        MiniAssert.True(success, "解码应该成功");
        MiniAssert.Equal(3, speedSet.MainMmps.Count, "MainMmps 计数应为 3");
        MiniAssert.Equal(150, speedSet.MainMmps[0], "第一个速度应为 150");
        MiniAssert.Equal(250, speedSet.MainMmps[1], "第二个速度应为 250");
        MiniAssert.Equal(350, speedSet.MainMmps[2], "第三个速度应为 350");
    }

    [MiniFact]
    public void HuararyCodec_SetGridLayout_DisabledReturnsOriginal()
    {
        // Arrange
        var codec = new HuararyCodec(mainCount: 10, ejectCount: 2);
        var originalList = new int[] { 1, 2, 3, 4, 5 };
        
        // Act
        var result = codec.SetGridLayout(originalList, xCount: 3, enabled: false);
        
        // Assert - 应该返回原始列表引用，而不是副本
        MiniAssert.True(ReferenceEquals(originalList, result), "应该返回原始列表的引用（零拷贝优化）");
    }

    [MiniFact]
    public void HuararyCodec_SetGridLayout_EnabledTransposesCorrectly()
    {
        // Arrange
        var codec = new HuararyCodec(mainCount: 10, ejectCount: 2);
        var source = new int[] { 1, 2, 3, 4, 5, 6 };
        
        // 原始布局 (2行3列):
        // 1 2 3
        // 4 5 6
        
        // 转置后（列优先）:
        // 1 4 2 5 3 6
        
        // Act
        var result = codec.SetGridLayout(source, xCount: 3, enabled: true);
        
        // Assert
        MiniAssert.Equal(6, result.Count, "结果应包含 6 个元素");
        MiniAssert.SequenceEqual(new int[] { 1, 4, 2, 5, 3, 6 }, result.ToArray(), "转置后的顺序应正确");
    }

    [MiniFact]
    public void ArrayPool_MultipleDecodesWork()
    {
        // 验证 ArrayPool 可以被多次使用而不会产生冲突
        var codec = new HuararyCodec(mainCount: 2, ejectCount: 0);
        
        for (int i = 0; i < 10; i++)
        {
            var frame = new byte[14];
            frame[0] = 0x2A; // Start
            frame[1] = 0x81; // CtrlSpeed (corrected)
            frame[2] = 14; frame[3] = 0; // Length = 14
            
            WriteInt32LE(frame, 4, i * 10);
            WriteInt32LE(frame, 8, i * 20);
            
            frame[12] = ComputeXor(frame, 12);
            frame[13] = 0x3B; // End
            
            var success = codec.TryDecodeSpeed(frame, DateTime.UtcNow, out var speedSet);
            MiniAssert.True(success, $"第 {i} 次解码应该成功");
            MiniAssert.Equal(i * 10, speedSet.MainMmps[0], $"第 {i} 次第一个速度应正确");
            MiniAssert.Equal(i * 20, speedSet.MainMmps[1], $"第 {i} 次第二个速度应正确");
        }
    }

    [MiniFact]
    public void ByteUtils_ToHexString_SupportsMultipleFormats()
    {
        // 验证 ByteUtils.ToHexString 的多种用法（可复用性）
        var testData = new byte[] { 0x2A, 0x3B, 0x4C, 0xFF };
        
        // 默认格式：空格分隔，大写
        var result1 = ByteUtils.ToHexString(testData);
        MiniAssert.Equal("2A 3B 4C FF", result1, "默认格式应为空格分隔的大写");
        
        // 无分隔符，小写
        var result2 = ByteUtils.ToHexString(testData, "", false);
        MiniAssert.Equal("2a3b4cff", result2, "无分隔符小写格式应正确");
        
        // 冒号分隔，大写
        var result3 = ByteUtils.ToHexString(testData, ":");
        MiniAssert.Equal("2A:3B:4C:FF", result3, "冒号分隔格式应正确");
        
        // 短横线分隔，小写
        var result4 = ByteUtils.ToHexString(testData, "-", false);
        MiniAssert.Equal("2a-3b-4c-ff", result4, "短横线分隔小写格式应正确");
        
        // 空数组
        var empty = ByteUtils.ToHexString(Array.Empty<byte>());
        MiniAssert.Equal("", empty, "空数组应返回空字符串");
    }

    // Helper methods
    private static void WriteInt32LE(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static byte ComputeXor(byte[] data, int length)
    {
        byte xor = 0;
        for (int i = 0; i < length; i++)
        {
            xor ^= data[i];
        }
        return xor;
    }
}
