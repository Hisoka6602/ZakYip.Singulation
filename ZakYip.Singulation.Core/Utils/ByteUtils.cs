using System;
using System.Buffers;
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

    /// <summary>
    /// 高效地将字节数组转换为十六进制字符串。
    /// 对于小数据（≤256字符）使用 stackalloc，大数据使用 ArrayPool 以减少 GC 压力。
    /// </summary>
    /// <param name="bytes">要转换的字节数据。</param>
    /// <param name="separator">字节之间的分隔符，默认为空格。传入 null 或空字符串表示无分隔符。</param>
    /// <param name="uppercase">是否使用大写字母（A-F），默认为 true。设为 false 使用小写字母（a-f）。</param>
    /// <returns>格式化的十六进制字符串。</returns>
    /// <remarks>
    /// 此方法针对性能优化，避免了多次内存分配。相比 BitConverter.ToString().Replace()，
    /// 减少了 3 次分配（ToArray、ToString、Replace）。
    /// <para>示例用法：</para>
    /// <code>
    /// ToHexString(bytes)                    // "2A 3B 4C" (默认空格分隔，大写)
    /// ToHexString(bytes, "-")               // "2A-3B-4C" (短横线分隔)
    /// ToHexString(bytes, "", false)         // "2a3b4c" (无分隔，小写)
    /// ToHexString(bytes, ":", true)         // "2A:3B:4C" (冒号分隔)
    /// </code>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string ToHexString(ReadOnlyMemory<byte> bytes, string? separator = " ", bool uppercase = true) {
        var span = bytes.Span;
        var length = span.Length;
        
        if (length == 0) return string.Empty;
        
        var useSeparator = !string.IsNullOrEmpty(separator);
        var separatorLength = useSeparator ? separator!.Length : 0;
        
        // 计算所需字符数：每个字节2个十六进制字符 + 分隔符（除了最后一个字节）
        var charCount = length * 2 + (useSeparator ? (length - 1) * separatorLength : 0);
        
        // 对于小数据使用 stackalloc，大数据使用 ArrayPool
        char[]? rentedArray = null;
        Span<char> chars = charCount <= 512
            ? stackalloc char[charCount]
            : (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount);
        
        try {
            var hexChars = uppercase ? "0123456789ABCDEF" : "0123456789abcdef";
            var charIndex = 0;
            
            for (int i = 0; i < length; i++) {
                var b = span[i];
                chars[charIndex++] = hexChars[b >> 4];
                chars[charIndex++] = hexChars[b & 0xF];
                
                // 添加分隔符（最后一个字节除外）
                if (useSeparator && i < length - 1) {
                    for (int j = 0; j < separatorLength; j++) {
                        chars[charIndex++] = separator![j];
                    }
                }
            }
            
            return new string(chars);
        }
        finally {
            if (rentedArray != null) {
                ArrayPool<char>.Shared.Return(rentedArray);
            }
        }
    }

    /// <summary>
    /// 高效地将字节 Span 转换为十六进制字符串。
    /// </summary>
    /// <param name="bytes">要转换的字节数据。</param>
    /// <param name="separator">字节之间的分隔符，默认为空格。</param>
    /// <param name="uppercase">是否使用大写字母（A-F），默认为 true。</param>
    /// <returns>格式化的十六进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string ToHexString(ReadOnlySpan<byte> bytes, string? separator = " ", bool uppercase = true) {
        return ToHexString(new ReadOnlyMemory<byte>(bytes.ToArray()), separator, uppercase);
    }

    /// <summary>
    /// 高效地将字节数组转换为十六进制字符串。
    /// </summary>
    /// <param name="bytes">要转换的字节数组。</param>
    /// <param name="separator">字节之间的分隔符，默认为空格。</param>
    /// <param name="uppercase">是否使用大写字母（A-F），默认为 true。</param>
    /// <returns>格式化的十六进制字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string ToHexString(byte[] bytes, string? separator = " ", bool uppercase = true) {
        return ToHexString(new ReadOnlyMemory<byte>(bytes), separator, uppercase);
    }

}
