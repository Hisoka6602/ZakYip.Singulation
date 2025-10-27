using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace ZakYip.Singulation.Core.Utils;

/// <summary>
/// 字节操作工具类，提供高性能的字节转换、校验和计算等常用功能。
/// </summary>
/// <remarks>
/// 此类提供了一组优化的静态方法用于字节数组操作，包括：
/// - 大端序/小端序转换
/// - XOR 校验和计算
/// - CRC 校验（如需要）
/// - 字节数组比较和操作
/// </remarks>
public static class ByteUtils {

    /// <summary>
    /// 计算 XOR 校验和（异或校验）。
    /// </summary>
    /// <param name="data">要计算校验和的数据。</param>
    /// <returns>XOR 校验和结果。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static byte ComputeXorChecksum(ReadOnlySpan<byte> data) {
        byte xor = 0;
        foreach (var b in data) {
            xor ^= b;
        }
        return xor;
    }

    /// <summary>
    /// 验证 XOR 校验和是否正确。
    /// </summary>
    /// <param name="data">包含数据和校验和的完整数据（校验和在最后一个字节之前）。</param>
    /// <param name="checksumIndex">校验和字节的索引位置。</param>
    /// <returns>校验和正确返回 true，否则返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool VerifyXorChecksum(ReadOnlySpan<byte> data, int checksumIndex) {
        if (checksumIndex < 0 || checksumIndex >= data.Length) {
            return false;
        }
        var calculated = ComputeXorChecksum(data[..checksumIndex]);
        return calculated == data[checksumIndex];
    }

    /// <summary>
    /// 从小端序字节读取 Int32。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ReadInt32LittleEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadInt32LittleEndian(source);
    }

    /// <summary>
    /// 从小端序字节读取 UInt32。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadUInt32LittleEndian(source);
    }

    /// <summary>
    /// 从小端序字节读取 Int16。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static short ReadInt16LittleEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadInt16LittleEndian(source);
    }

    /// <summary>
    /// 从小端序字节读取 UInt16。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadUInt16LittleEndian(source);
    }

    /// <summary>
    /// 将 Int32 写入小端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteInt32LittleEndian(Span<byte> destination, int value) {
        BinaryPrimitives.WriteInt32LittleEndian(destination, value);
    }

    /// <summary>
    /// 将 UInt32 写入小端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteUInt32LittleEndian(Span<byte> destination, uint value) {
        BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
    }

    /// <summary>
    /// 将 Int16 写入小端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteInt16LittleEndian(Span<byte> destination, short value) {
        BinaryPrimitives.WriteInt16LittleEndian(destination, value);
    }

    /// <summary>
    /// 将 UInt16 写入小端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteUInt16LittleEndian(Span<byte> destination, ushort value) {
        BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
    }

    /// <summary>
    /// 从大端序字节读取 Int32。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ReadInt32BigEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadInt32BigEndian(source);
    }

    /// <summary>
    /// 从大端序字节读取 UInt32。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint ReadUInt32BigEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadUInt32BigEndian(source);
    }

    /// <summary>
    /// 从大端序字节读取 Int16。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static short ReadInt16BigEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadInt16BigEndian(source);
    }

    /// <summary>
    /// 从大端序字节读取 UInt16。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source) {
        return BinaryPrimitives.ReadUInt16BigEndian(source);
    }

    /// <summary>
    /// 将 Int32 写入大端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteInt32BigEndian(Span<byte> destination, int value) {
        BinaryPrimitives.WriteInt32BigEndian(destination, value);
    }

    /// <summary>
    /// 将 UInt32 写入大端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteUInt32BigEndian(Span<byte> destination, uint value) {
        BinaryPrimitives.WriteUInt32BigEndian(destination, value);
    }

    /// <summary>
    /// 将 Int16 写入大端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteInt16BigEndian(Span<byte> destination, short value) {
        BinaryPrimitives.WriteInt16BigEndian(destination, value);
    }

    /// <summary>
    /// 将 UInt16 写入大端序字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void WriteUInt16BigEndian(Span<byte> destination, ushort value) {
        BinaryPrimitives.WriteUInt16BigEndian(destination, value);
    }

    /// <summary>
    /// 将 Int32 的位模式转换为 Float（单精度浮点数）。
    /// 这是一个无损转换，直接重新解释位模式。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Int32BitsToSingle(int value) {
        return BitConverter.Int32BitsToSingle(value);
    }

}
