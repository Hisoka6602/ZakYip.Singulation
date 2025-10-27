using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using csLTDMC;
using ZakYip.Singulation.Core.Utils;

namespace ZakYip.Singulation.Drivers.Leadshine {
    /// <summary>
    /// 雷赛总线批量 PDO 操作优化工具类。
    /// <para>
    /// 提供以下优化特性：
    /// - 内存池复用：使用 ArrayPool 减少 GC 压力
    /// - 批量操作：减少 SDK 调用次数
    /// - 异步 IO：批量操作支持异步执行
    /// </para>
    /// </summary>
    public static class LeadshineBatchPdoOperations {
        
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

        /// <summary>
        /// 批量写入 RxPDO 操作的请求结构。
        /// </summary>
        public readonly struct BatchWriteRequest {
            public readonly ushort Index;
            public readonly byte SubIndex;
            public readonly ushort BitLength;
            public readonly object Value;

            public BatchWriteRequest(ushort index, object value, byte subIndex = 0) {
                Index = index;
                SubIndex = subIndex;
                Value = value;
                
                // 根据值类型自动推断位长度
                BitLength = value switch {
                    int or uint => 32,
                    short or ushort => 16,
                    byte or sbyte => 8,
                    _ => throw new NotSupportedException($"不支持的写入类型：{value.GetType().Name}")
                };
            }
        }

        /// <summary>
        /// 批量读取 TxPDO 操作的请求结构。
        /// </summary>
        public readonly struct BatchReadRequest {
            public readonly ushort Index;
            public readonly byte SubIndex;
            public readonly ushort BitLength;

            public BatchReadRequest(ushort index, ushort bitLength, byte subIndex = 0) {
                Index = index;
                SubIndex = subIndex;
                BitLength = bitLength;
            }
        }

        /// <summary>
        /// 批量写入 RxPDO 结果。
        /// </summary>
        public readonly struct BatchWriteResult {
            public readonly ushort Index;
            public readonly short ReturnCode;
            public readonly bool IsSuccess;

            public BatchWriteResult(ushort index, short returnCode) {
                Index = index;
                ReturnCode = returnCode;
                IsSuccess = returnCode == 0;
            }
        }

        /// <summary>
        /// 批量读取 TxPDO 结果。
        /// </summary>
        public readonly struct BatchReadResult {
            public readonly ushort Index;
            public readonly short ReturnCode;
            public readonly byte[]? Data;
            public readonly bool IsSuccess;

            public BatchReadResult(ushort index, short returnCode, byte[]? data = null) {
                Index = index;
                ReturnCode = returnCode;
                Data = data;
                IsSuccess = returnCode == 0;
            }
        }

        /// <summary>
        /// 批量写入多个 RxPDO（使用内存池优化）。
        /// </summary>
        /// <param name="cardNo">控制卡号</param>
        /// <param name="portNum">端口号</param>
        /// <param name="nodeId">节点 ID</param>
        /// <param name="requests">批量写入请求列表</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量写入结果列表</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static async Task<BatchWriteResult[]> BatchWriteRxPdoAsync(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            IReadOnlyList<BatchWriteRequest> requests,
            CancellationToken ct = default) {
            
            if (requests == null || requests.Count == 0) {
                return Array.Empty<BatchWriteResult>();
            }

            var results = new BatchWriteResult[requests.Count];

            // 异步执行批量操作以避免阻塞
            await Task.Run(() => {
                for (int i = 0; i < requests.Count; i++) {
                    if (ct.IsCancellationRequested) {
                        // 填充剩余结果为取消状态
                        for (int j = i; j < requests.Count; j++) {
                            results[j] = new BatchWriteResult(requests[j].Index, -999);
                        }
                        break;
                    }

                    var req = requests[i];
                    var ret = WriteRxPdoWithPool(cardNo, portNum, nodeId, req.Index, req.SubIndex, req.BitLength, req.Value);
                    results[i] = new BatchWriteResult(req.Index, ret);
                }
            }, ct).ConfigureAwait(false);

            return results;
        }

        /// <summary>
        /// 批量读取多个 TxPDO（使用内存池优化）。
        /// </summary>
        /// <param name="cardNo">控制卡号</param>
        /// <param name="portNum">端口号</param>
        /// <param name="nodeId">节点 ID</param>
        /// <param name="requests">批量读取请求列表</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量读取结果列表</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static async Task<BatchReadResult[]> BatchReadTxPdoAsync(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            IReadOnlyList<BatchReadRequest> requests,
            CancellationToken ct = default) {
            
            if (requests == null || requests.Count == 0) {
                return Array.Empty<BatchReadResult>();
            }

            var results = new BatchReadResult[requests.Count];

            // 异步执行批量操作以避免阻塞
            await Task.Run(() => {
                for (int i = 0; i < requests.Count; i++) {
                    if (ct.IsCancellationRequested) {
                        // 填充剩余结果为取消状态
                        for (int j = i; j < requests.Count; j++) {
                            results[j] = new BatchReadResult(requests[j].Index, -999);
                        }
                        break;
                    }

                    var req = requests[i];
                    var ret = ReadTxPdoWithPool(cardNo, portNum, nodeId, req.Index, req.SubIndex, req.BitLength, out var data);
                    results[i] = new BatchReadResult(req.Index, ret, data);
                }
            }, ct).ConfigureAwait(false);

            return results;
        }

        /// <summary>
        /// 使用内存池写入单个 RxPDO（内部方法）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static short WriteRxPdoWithPool(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            ushort index,
            byte subIndex,
            ushort bitLength,
            object value) {
            
            var byteLength = (bitLength + 7) / 8;
            var buffer = BufferPool.Rent(byteLength);
            
            try {
                // 将值写入缓冲区
                switch (value) {
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
            finally {
                // 归还缓冲区到池
                BufferPool.Return(buffer, clearArray: false);
            }
        }

        /// <summary>
        /// 使用内存池读取单个 TxPDO（内部方法）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static short ReadTxPdoWithPool(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            ushort index,
            byte subIndex,
            ushort bitLength,
            out byte[]? data) {
            
            var byteLength = (bitLength + 7) / 8;
            var buffer = BufferPool.Rent(byteLength);
            
            try {
                // 调用底层 SDK
                var ret = LTDMC.nmc_read_txpdo(cardNo, portNum, nodeId, index, subIndex, bitLength, buffer);
                
                if (ret == 0) {
                    // 成功：复制数据到新数组
                    data = new byte[byteLength];
                    Array.Copy(buffer, data, byteLength);
                } else {
                    // 失败：返回 null
                    data = null;
                }
                
                return ret;
            }
            finally {
                // 归还缓冲区到池
                BufferPool.Return(buffer, clearArray: false);
            }
        }

        /// <summary>
        /// 获取批量操作的统计信息（用于诊断）。
        /// </summary>
        public readonly struct BatchStatistics {
            public readonly int TotalRequests;
            public readonly int SuccessCount;
            public readonly int FailureCount;
            public readonly double SuccessRate;

            public BatchStatistics(IReadOnlyList<BatchWriteResult> results) {
                TotalRequests = results.Count;
                SuccessCount = 0;
                FailureCount = 0;

                for (int i = 0; i < results.Count; i++) {
                    if (results[i].IsSuccess) {
                        SuccessCount++;
                    } else {
                        FailureCount++;
                    }
                }

                SuccessRate = TotalRequests > 0 ? (double)SuccessCount / TotalRequests : 0.0;
            }

            public BatchStatistics(IReadOnlyList<BatchReadResult> results) {
                TotalRequests = results.Count;
                SuccessCount = 0;
                FailureCount = 0;

                for (int i = 0; i < results.Count; i++) {
                    if (results[i].IsSuccess) {
                        SuccessCount++;
                    } else {
                        FailureCount++;
                    }
                }

                SuccessRate = TotalRequests > 0 ? (double)SuccessCount / TotalRequests : 0.0;
            }
        }
    }
}
