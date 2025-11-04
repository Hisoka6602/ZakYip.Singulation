using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using csLTDMC;
using ZakYip.Singulation.Core.Utils;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// 雷赛（Leadshine）PDO 操作辅助工具类。
    /// <para>
    /// 提供使用内存池优化的 RxPDO/TxPDO 读写方法，
    /// 避免在批量操作中重复代码。
    /// </para>
    /// </summary>
    public static class LeadshinePdoHelpers
    {
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

        /// <summary>
        /// 将位长度转换为字节长度。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetByteLength(ushort bitLength) => (bitLength + 7) / 8;

        /// <summary>
        /// 使用内存池写入单个 RxPDO（内部方法）。
        /// </summary>
        /// <param name="cardNo">控制卡号</param>
        /// <param name="portNum">端口号</param>
        /// <param name="nodeId">节点 ID</param>
        /// <param name="index">对象字典索引</param>
        /// <param name="subIndex">子索引</param>
        /// <param name="bitLength">位长度</param>
        /// <param name="value">要写入的值（支持 int, uint, short, ushort, byte, sbyte）</param>
        /// <returns>SDK 返回码（0 表示成功，-2 表示不支持的类型）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static short WriteRxPdoWithPool(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            ushort index,
            byte subIndex,
            ushort bitLength,
            object value)
        {
            var byteLength = GetByteLength(bitLength);
            var buffer = BufferPool.Rent(byteLength);

            try
            {
                // 将值写入缓冲区
                switch (value)
                {
                    case int i32:
                        ByteUtils.WriteInt32LittleEndian(buffer.AsSpan(0, 4), i32);
                        break;
                    case uint u32:
                        ByteUtils.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), u32);
                        break;
                    case short i16:
                        ByteUtils.WriteInt16LittleEndian(buffer.AsSpan(0, 2), i16);
                        break;
                    case ushort u16:
                        ByteUtils.WriteUInt16LittleEndian(buffer.AsSpan(0, 2), u16);
                        break;
                    case byte b8:
                        buffer[0] = b8;
                        break;
                    case sbyte s8:
                        buffer[0] = unchecked((byte)s8);
                        break;
                    default:
                        return -2; // 不支持的类型
                }

                // 调用底层 SDK
                return LTDMC.nmc_write_rxpdo(cardNo, portNum, nodeId, index, subIndex, bitLength, buffer);
            }
            finally
            {
                // 归还缓冲区到池
                BufferPool.Return(buffer, clearArray: false);
            }
        }

        /// <summary>
        /// 使用内存池读取单个 TxPDO（内部方法）。
        /// </summary>
        /// <param name="cardNo">控制卡号</param>
        /// <param name="portNum">端口号</param>
        /// <param name="nodeId">节点 ID</param>
        /// <param name="index">对象字典索引</param>
        /// <param name="subIndex">子索引</param>
        /// <param name="bitLength">位长度</param>
        /// <param name="data">读取到的数据（成功时返回字节数组，失败时返回 null）</param>
        /// <returns>SDK 返回码（0 表示成功）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static short ReadTxPdoWithPool(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            ushort index,
            byte subIndex,
            ushort bitLength,
            out byte[]? data)
        {
            var byteLength = GetByteLength(bitLength);
            var buffer = BufferPool.Rent(byteLength);

            try
            {
                // 调用底层 SDK
                var ret = LTDMC.nmc_read_txpdo(cardNo, portNum, nodeId, index, subIndex, bitLength, buffer);

                if (ret == 0)
                {
                    // 成功：复制数据到新数组
                    data = new byte[byteLength];
                    Array.Copy(buffer, data, byteLength);
                }
                else
                {
                    // 失败：返回 null
                    data = null;
                }

                return ret;
            }
            finally
            {
                // 归还缓冲区到池
                BufferPool.Return(buffer, clearArray: false);
            }
        }
    }
}
