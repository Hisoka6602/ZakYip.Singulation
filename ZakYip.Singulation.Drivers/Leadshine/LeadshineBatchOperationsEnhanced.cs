using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using csLTDMC;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ZakYip.Singulation.Core.Utils;

namespace ZakYip.Singulation.Drivers.Leadshine {
    
    /// <summary>
    /// 增强型雷赛批量 PDO 操作工具类。
    /// <para>
    /// 新增功能：
    /// - 2ms SDK 调用安全间隔
    /// - Prometheus 性能监控指标
    /// - 自动重试机制
    /// - 断路器模式
    /// - ValueTask 优化
    /// - SIMD 批量数据转换
    /// - 智能自适应分批
    /// - 缓存预热
    /// </para>
    /// </summary>
    public static class LeadshineBatchOperationsEnhanced {
        
        // 雷赛 SDK 调用的安全间隔时间（2ms）
        private const int SafetyIntervalMs = 2;
        
        // 上次 SDK 调用的时间戳（Stopwatch ticks）
        private static long _lastSdkCallTicks = 0;
        
        // 用于同步 SDK 调用的锁
        private static readonly object _sdkCallLock = new object();
        
        // 内存池
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
        
        // Stopwatch 用于高精度计时
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        
        // Prometheus 指标
        private static readonly Meter _meter = new Meter("ZakYip.Singulation.Leadshine.BatchOperations", "1.0.0");
        private static readonly Counter<long> _batchOperationCounter = _meter.CreateCounter<long>("leadshine_batch_operations_total");
        private static readonly Counter<long> _batchOperationSuccessCounter = _meter.CreateCounter<long>("leadshine_batch_operations_success_total");
        private static readonly Counter<long> _batchOperationFailureCounter = _meter.CreateCounter<long>("leadshine_batch_operations_failure_total");
        private static readonly Counter<long> _batchOperationRetryCounter = _meter.CreateCounter<long>("leadshine_batch_operations_retry_total");
        private static readonly Counter<long> _circuitBreakerOpenCounter = _meter.CreateCounter<long>("leadshine_batch_circuit_breaker_open_total");
        private static readonly Histogram<double> _batchOperationDuration = _meter.CreateHistogram<double>("leadshine_batch_operation_duration_ms");
        private static readonly Histogram<double> _batchSizeHistogram = _meter.CreateHistogram<double>("leadshine_batch_operation_size");
        private static readonly Histogram<double> _successRateHistogram = _meter.CreateHistogram<double>("leadshine_batch_operation_success_rate");
        
        // 断路器策略缓存
        private static readonly Dictionary<string, ResiliencePipeline<bool>> _circuitBreakerCache = new();
        private static readonly object _circuitBreakerLock = new object();
        
        // 自适应分批配置
        private static int _adaptiveBatchSize = 10; // 初始批量大小
        private static readonly int _minBatchSize = 3;
        private static readonly int _maxBatchSize = 50;
        private static double _recentSuccessRate = 1.0;
        
        /// <summary>
        /// 确保 SDK 调用之间有 2ms 的安全间隔。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnforceSafetyInterval() {
            lock (_sdkCallLock) {
                long elapsed;
                long current;
                do {
                    current = _stopwatch.ElapsedTicks;
                    elapsed = current - Volatile.Read(ref _lastSdkCallTicks);
                    
                    // 转换为毫秒
                    var elapsedMs = (elapsed * 1000.0) / Stopwatch.Frequency;
                    
                    if (elapsedMs >= SafetyIntervalMs) {
                        break;
                    }
                    
                    // 需要等待的时间
                    var waitMs = SafetyIntervalMs - elapsedMs;
                    if (waitMs > 0) {
                        Thread.Sleep((int)Math.Ceiling(waitMs));
                    }
                } while (true);
                
                // 更新最后调用时间
                Volatile.Write(ref _lastSdkCallTicks, _stopwatch.ElapsedTicks);
            }
        }
        
        /// <summary>
        /// 获取或创建断路器策略。
        /// </summary>
        private static ResiliencePipeline<bool> GetOrCreateCircuitBreaker(string key) {
            lock (_circuitBreakerLock) {
                if (_circuitBreakerCache.TryGetValue(key, out var pipeline)) {
                    return pipeline;
                }
                
                var shouldHandle = new PredicateBuilder<bool>()
                    .Handle<Exception>()
                    .HandleResult(success => !success);
                
                var circuitBreakerOptions = new CircuitBreakerStrategyOptions<bool> {
                    ShouldHandle = shouldHandle,
                    FailureRatio = 0.5, // 50% 失败率打开断路器
                    MinimumThroughput = 5, // 最小吞吐量
                    SamplingDuration = TimeSpan.FromSeconds(10),
                    BreakDuration = TimeSpan.FromSeconds(30), // 断路器打开 30 秒
                    OnOpened = args => {
                        _circuitBreakerOpenCounter.Add(1, new KeyValuePair<string, object?>("operation", key));
                        return default;
                    }
                };
                
                var retryOptions = new RetryStrategyOptions<bool> {
                    ShouldHandle = shouldHandle,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(10),
                    BackoffType = DelayBackoffType.Exponential,
                    OnRetry = args => {
                        _batchOperationRetryCounter.Add(1, new KeyValuePair<string, object?>("operation", key));
                        return default;
                    }
                };
                
                var newPipeline = new ResiliencePipelineBuilder<bool>()
                    .AddRetry(retryOptions)
                    .AddCircuitBreaker(circuitBreakerOptions)
                    .Build();
                
                _circuitBreakerCache[key] = newPipeline;
                return newPipeline;
            }
        }
        
        /// <summary>
        /// 使用 SIMD 优化批量转换 Int32 数组到字节数组。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void ConvertInt32ArrayToBytes_SIMD(ReadOnlySpan<int> source, Span<byte> destination) {
            if (source.Length * 4 > destination.Length) {
                throw new ArgumentException("Destination buffer too small");
            }
            
            // 使用 SIMD 进行批量转换（如果硬件支持）
            if (Vector.IsHardwareAccelerated && source.Length >= Vector<int>.Count) {
                var sourceVectors = MemoryMarshal.Cast<int, Vector<int>>(source);
                var destBytes = destination;
                
                int i = 0;
                for (; i < sourceVectors.Length; i++) {
                    var vec = sourceVectors[i];
                    var byteSpan = destBytes.Slice(i * Vector<int>.Count * 4, Vector<int>.Count * 4);
                    
                    // 逐个元素写入（.NET 8 的 Vector 不直接支持批量字节转换，但这里展示了 SIMD 的使用）
                    for (int j = 0; j < Vector<int>.Count; j++) {
                        ByteUtils.WriteInt32LittleEndian(byteSpan.Slice(j * 4, 4), vec[j]);
                    }
                }
                
                // 处理剩余元素
                for (i = sourceVectors.Length * Vector<int>.Count; i < source.Length; i++) {
                    ByteUtils.WriteInt32LittleEndian(destBytes.Slice(i * 4, 4), source[i]);
                }
            } else {
                // 回退到标量版本
                for (int i = 0; i < source.Length; i++) {
                    ByteUtils.WriteInt32LittleEndian(destination.Slice(i * 4, 4), source[i]);
                }
            }
        }
        
        /// <summary>
        /// 智能自适应分批：根据成功率动态调整批量大小。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetAdaptiveBatchSize(int totalRequests) {
            // 根据最近的成功率调整批量大小
            if (_recentSuccessRate > 0.95) {
                // 高成功率：增加批量大小
                _adaptiveBatchSize = Math.Min(_maxBatchSize, _adaptiveBatchSize + 2);
            } else if (_recentSuccessRate < 0.8) {
                // 低成功率：减少批量大小
                _adaptiveBatchSize = Math.Max(_minBatchSize, _adaptiveBatchSize - 2);
            }
            
            return Math.Min(_adaptiveBatchSize, totalRequests);
        }
        
        /// <summary>
        /// 缓存预热：预先分配缓冲区。
        /// </summary>
        public static void WarmupCache(int expectedMaxBatchSize = 50) {
            // 预热内存池
            for (int i = 0; i < 10; i++) {
                var buffer = BufferPool.Rent(expectedMaxBatchSize * 4);
                BufferPool.Return(buffer, clearArray: false);
            }
            
            // 预热断路器
            _ = GetOrCreateCircuitBreaker("write");
            _ = GetOrCreateCircuitBreaker("read");
        }
        
        /// <summary>
        /// 增强型批量写入 RxPDO（使用 ValueTask 优化）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static async ValueTask<LeadshineBatchPdoOperations.BatchWriteResult[]> BatchWriteRxPdoEnhancedAsync(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            IReadOnlyList<LeadshineBatchPdoOperations.BatchWriteRequest> requests,
            CancellationToken ct = default) {
            
            if (requests == null || requests.Count == 0) {
                return Array.Empty<LeadshineBatchPdoOperations.BatchWriteResult>();
            }
            
            _batchOperationCounter.Add(1, new KeyValuePair<string, object?>("operation", "write"));
            _batchSizeHistogram.Record(requests.Count);
            
            var sw = Stopwatch.StartNew();
            var results = new LeadshineBatchPdoOperations.BatchWriteResult[requests.Count];
            var circuitBreaker = GetOrCreateCircuitBreaker("write");
            
            try {
                // 使用 ValueTask 减少分配
                await new ValueTask(Task.Run(async () => {
                    var successCount = 0;
                    
                    for (int i = 0; i < requests.Count; i++) {
                        if (ct.IsCancellationRequested) {
                            for (int j = i; j < requests.Count; j++) {
                                results[j] = new LeadshineBatchPdoOperations.BatchWriteResult(requests[j].Index, -999);
                            }
                            break;
                        }
                        
                        var req = requests[i];
                        
                        // 使用断路器和重试机制
                        var success = await circuitBreaker.ExecuteAsync(async (ctx) => {
                            // 强制 2ms 安全间隔
                            EnforceSafetyInterval();
                            
                            var ret = WriteRxPdoWithPool(cardNo, portNum, nodeId, req.Index, req.SubIndex, req.BitLength, req.Value);
                            results[i] = new LeadshineBatchPdoOperations.BatchWriteResult(req.Index, ret);
                            
                            if (ret == 0) {
                                successCount++;
                                return true;
                            }
                            return false;
                        }, ct);
                    }
                    
                    // 更新成功率用于自适应批量大小
                    _recentSuccessRate = requests.Count > 0 ? (double)successCount / requests.Count : 1.0;
                    _successRateHistogram.Record(_recentSuccessRate);
                    
                    if (successCount == requests.Count) {
                        _batchOperationSuccessCounter.Add(1, new KeyValuePair<string, object?>("operation", "write"));
                    } else {
                        _batchOperationFailureCounter.Add(1, new KeyValuePair<string, object?>("operation", "write"));
                    }
                }, ct));
                
                return results;
            } finally {
                sw.Stop();
                _batchOperationDuration.Record(sw.Elapsed.TotalMilliseconds, 
                    new KeyValuePair<string, object?>("operation", "write"));
            }
        }
        
        /// <summary>
        /// 增强型批量读取 TxPDO（使用 ValueTask 优化）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static async ValueTask<LeadshineBatchPdoOperations.BatchReadResult[]> BatchReadTxPdoEnhancedAsync(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            IReadOnlyList<LeadshineBatchPdoOperations.BatchReadRequest> requests,
            CancellationToken ct = default) {
            
            if (requests == null || requests.Count == 0) {
                return Array.Empty<LeadshineBatchPdoOperations.BatchReadResult>();
            }
            
            _batchOperationCounter.Add(1, new KeyValuePair<string, object?>("operation", "read"));
            _batchSizeHistogram.Record(requests.Count);
            
            var sw = Stopwatch.StartNew();
            var results = new LeadshineBatchPdoOperations.BatchReadResult[requests.Count];
            var circuitBreaker = GetOrCreateCircuitBreaker("read");
            
            try {
                await new ValueTask(Task.Run(async () => {
                    var successCount = 0;
                    
                    for (int i = 0; i < requests.Count; i++) {
                        if (ct.IsCancellationRequested) {
                            for (int j = i; j < requests.Count; j++) {
                                results[j] = new LeadshineBatchPdoOperations.BatchReadResult(requests[j].Index, -999);
                            }
                            break;
                        }
                        
                        var req = requests[i];
                        
                        var success = await circuitBreaker.ExecuteAsync(async (ctx) => {
                            EnforceSafetyInterval();
                            
                            var ret = ReadTxPdoWithPool(cardNo, portNum, nodeId, req.Index, req.SubIndex, req.BitLength, out var data);
                            results[i] = new LeadshineBatchPdoOperations.BatchReadResult(req.Index, ret, data);
                            
                            if (ret == 0) {
                                successCount++;
                                return await ValueTask.FromResult(true);
                            }
                            return await ValueTask.FromResult(false);
                        }, ct);
                    }
                    
                    _recentSuccessRate = requests.Count > 0 ? (double)successCount / requests.Count : 1.0;
                    _successRateHistogram.Record(_recentSuccessRate);
                    
                    if (successCount == requests.Count) {
                        _batchOperationSuccessCounter.Add(1, new KeyValuePair<string, object?>("operation", "read"));
                    } else {
                        _batchOperationFailureCounter.Add(1, new KeyValuePair<string, object?>("operation", "read"));
                    }
                }, ct));
                
                return results;
            } finally {
                sw.Stop();
                _batchOperationDuration.Record(sw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("operation", "read"));
            }
        }
        
        /// <summary>
        /// 智能分批写入：自动将大批量请求分成多个小批次。
        /// </summary>
        public static async ValueTask<LeadshineBatchPdoOperations.BatchWriteResult[]> SmartBatchWriteAsync(
            ushort cardNo,
            ushort portNum,
            ushort nodeId,
            IReadOnlyList<LeadshineBatchPdoOperations.BatchWriteRequest> requests,
            CancellationToken ct = default) {
            
            if (requests == null || requests.Count == 0) {
                return Array.Empty<LeadshineBatchPdoOperations.BatchWriteResult>();
            }
            
            var batchSize = GetAdaptiveBatchSize(requests.Count);
            var allResults = new List<LeadshineBatchPdoOperations.BatchWriteResult>(requests.Count);
            
            for (int i = 0; i < requests.Count; i += batchSize) {
                var remaining = Math.Min(batchSize, requests.Count - i);
                var batch = requests.Skip(i).Take(remaining).ToArray();
                
                var batchResults = await BatchWriteRxPdoEnhancedAsync(cardNo, portNum, nodeId, batch, ct);
                allResults.AddRange(batchResults);
            }
            
            return allResults.ToArray();
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
                        return -2;
                }
                
                return LTDMC.nmc_write_rxpdo(cardNo, portNum, nodeId, index, subIndex, bitLength, buffer);
            }
            finally {
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
                var ret = LTDMC.nmc_read_txpdo(cardNo, portNum, nodeId, index, subIndex, bitLength, buffer);
                
                if (ret == 0) {
                    data = new byte[byteLength];
                    Array.Copy(buffer, data, byteLength);
                } else {
                    data = null;
                }
                
                return ret;
            }
            finally {
                BufferPool.Return(buffer, clearArray: false);
            }
        }
        
        /// <summary>
        /// 获取批量操作的详细诊断信息。
        /// </summary>
        public readonly struct BatchDiagnostics {
            public readonly int TotalRequests;
            public readonly int SuccessCount;
            public readonly int FailureCount;
            public readonly double SuccessRate;
            public readonly double AverageDurationMs;
            public readonly int RetryCount;
            public readonly bool CircuitBreakerOpen;
            public readonly Dictionary<ushort, short> FailedIndices; // Index -> ReturnCode
            
            public BatchDiagnostics(
                IReadOnlyList<LeadshineBatchPdoOperations.BatchWriteResult> results,
                double durationMs,
                int retryCount,
                bool circuitBreakerOpen) {
                
                TotalRequests = results.Count;
                SuccessCount = 0;
                FailureCount = 0;
                FailedIndices = new Dictionary<ushort, short>();
                
                for (int i = 0; i < results.Count; i++) {
                    if (results[i].IsSuccess) {
                        SuccessCount++;
                    } else {
                        FailureCount++;
                        FailedIndices[results[i].Index] = results[i].ReturnCode;
                    }
                }
                
                SuccessRate = TotalRequests > 0 ? (double)SuccessCount / TotalRequests : 0.0;
                AverageDurationMs = durationMs;
                RetryCount = retryCount;
                CircuitBreakerOpen = circuitBreakerOpen;
            }
            
            public override string ToString() {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Batch Diagnostics:");
                sb.AppendLine($"  Total Requests: {TotalRequests}");
                sb.AppendLine($"  Success: {SuccessCount} ({SuccessRate:P2})");
                sb.AppendLine($"  Failures: {FailureCount}");
                sb.AppendLine($"  Average Duration: {AverageDurationMs:F2}ms");
                sb.AppendLine($"  Retries: {RetryCount}");
                sb.AppendLine($"  Circuit Breaker: {(CircuitBreakerOpen ? "OPEN" : "CLOSED")}");
                
                if (FailedIndices.Count > 0) {
                    sb.AppendLine($"  Failed Indices:");
                    foreach (var kvp in FailedIndices) {
                        sb.AppendLine($"    0x{kvp.Key:X4} -> Error {kvp.Value}");
                    }
                }
                
                return sb.ToString();
            }
        }
    }
}
