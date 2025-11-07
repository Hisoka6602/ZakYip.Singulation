using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ZakYip.Singulation.Benchmarks {

    /// <summary>
    /// 批量操作性能基准测试
    /// 测试 10/50/100 轴同时操作的性能
    /// </summary>
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, warmupCount: 3, iterationCount: 10)]
    public class BatchOperationBenchmarks {

        private readonly List<FakeAxisOperation> _10Axes = new();
        private readonly List<FakeAxisOperation> _50Axes = new();
        private readonly List<FakeAxisOperation> _100Axes = new();

        [GlobalSetup]
        public void Setup() {
            // 初始化测试数据
            for (int i = 0; i < 10; i++) {
                _10Axes.Add(new FakeAxisOperation(i));
            }
            for (int i = 0; i < 50; i++) {
                _50Axes.Add(new FakeAxisOperation(i));
            }
            for (int i = 0; i < 100; i++) {
                _100Axes.Add(new FakeAxisOperation(i));
            }
        }

        [Benchmark(Baseline = true)]
        public async Task<int> BatchOperation_10Axes() {
            return await ExecuteBatchOperationAsync(_10Axes);
        }

        [Benchmark]
        public async Task<int> BatchOperation_50Axes() {
            return await ExecuteBatchOperationAsync(_50Axes);
        }

        [Benchmark]
        public async Task<int> BatchOperation_100Axes() {
            return await ExecuteBatchOperationAsync(_100Axes);
        }

        [Benchmark]
        public async Task<int> BatchOperation_10Axes_Sequential() {
            return await ExecuteSequentialOperationAsync(_10Axes);
        }

        [Benchmark]
        public async Task<int> BatchOperation_50Axes_Sequential() {
            return await ExecuteSequentialOperationAsync(_50Axes);
        }

        [Benchmark]
        public async Task<int> BatchOperation_100Axes_Sequential() {
            return await ExecuteSequentialOperationAsync(_100Axes);
        }

        private async Task<int> ExecuteBatchOperationAsync(List<FakeAxisOperation> axes) {
            // 模拟并行批量操作
            var tasks = axes.Select(axis => axis.ExecuteAsync()).ToArray();
            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        private async Task<int> ExecuteSequentialOperationAsync(List<FakeAxisOperation> axes) {
            // 模拟顺序操作
            int count = 0;
            foreach (var axis in axes) {
                await axis.ExecuteAsync();
                count++;
            }
            return count;
        }

        /// <summary>
        /// 模拟轴操作（避免实际硬件依赖）
        /// </summary>
        private class FakeAxisOperation {
            private readonly int _axisId;
            private readonly Random _random = new();

            public FakeAxisOperation(int axisId) {
                _axisId = axisId;
            }

            public async Task ExecuteAsync() {
                // 模拟轴操作的计算和 I/O 延迟
                await Task.Delay(_random.Next(1, 5)); // 1-5ms 延迟
                
                // 模拟一些计算工作
                var result = 0.0;
                for (int i = 0; i < 100; i++) {
                    result += Math.Sqrt(i * _axisId + 1);
                }
            }
        }
    }

    /// <summary>
    /// 内存分配和 GC 压力监控基准测试
    /// </summary>
    [MemoryDiagnoser]
    [GcServer(true)]
    [GcConcurrent(true)]
    public class MemoryAllocationBenchmarks {

        [Benchmark]
        public void SmallObjectAllocation_1000() {
            for (int i = 0; i < 1000; i++) {
                var obj = new SmallObject { Id = i, Value = i * 2.0 };
            }
        }

        [Benchmark]
        public void SmallObjectAllocation_10000() {
            for (int i = 0; i < 10000; i++) {
                var obj = new SmallObject { Id = i, Value = i * 2.0 };
            }
        }

        [Benchmark]
        public void ArrayAllocation_Small() {
            var arrays = new byte[100][];
            for (int i = 0; i < 100; i++) {
                arrays[i] = new byte[1024]; // 1KB
            }
        }

        [Benchmark]
        public void ArrayAllocation_Medium() {
            var arrays = new byte[100][];
            for (int i = 0; i < 100; i++) {
                arrays[i] = new byte[10240]; // 10KB
            }
        }

        [Benchmark]
        public void ArrayPoolUsage() {
            var pool = System.Buffers.ArrayPool<byte>.Shared;
            var arrays = new byte[100][];
            
            for (int i = 0; i < 100; i++) {
                arrays[i] = pool.Rent(1024);
            }

            for (int i = 0; i < 100; i++) {
                pool.Return(arrays[i]);
            }
        }

        private class SmallObject {
            public int Id { get; set; }
            public double Value { get; set; }
        }
    }

    /// <summary>
    /// IO 操作性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    public class IoOperationBenchmarks {

        private readonly List<IoPort> _ports = new();

        [GlobalSetup]
        public void Setup() {
            for (int i = 0; i < 100; i++) {
                _ports.Add(new IoPort(i));
            }
        }

        [Benchmark]
        public async Task<int> IO_Write_Sequential_100Ports() {
            int count = 0;
            foreach (var port in _ports) {
                await port.WriteAsync(true);
                count++;
            }
            return count;
        }

        [Benchmark]
        public async Task<int> IO_Write_Parallel_100Ports() {
            var tasks = _ports.Select(p => p.WriteAsync(true)).ToArray();
            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        [Benchmark]
        public async Task<int> IO_Read_Sequential_100Ports() {
            int count = 0;
            foreach (var port in _ports) {
                await port.ReadAsync();
                count++;
            }
            return count;
        }

        [Benchmark]
        public async Task<int> IO_Read_Parallel_100Ports() {
            var tasks = _ports.Select(p => p.ReadAsync()).ToArray();
            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        private class IoPort {
            private readonly int _portNumber;
            private bool _state;

            public IoPort(int portNumber) {
                _portNumber = portNumber;
            }

            public async Task WriteAsync(bool value) {
                await Task.Delay(1); // 模拟 IO 延迟
                _state = value;
            }

            public async Task<bool> ReadAsync() {
                await Task.Delay(1); // 模拟 IO 延迟
                return _state;
            }
        }
    }

    /// <summary>
    /// 并发操作性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    public class ConcurrencyBenchmarks {

        [Benchmark]
        public async Task<int> ConcurrentTasks_10() {
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => SimulateWorkAsync())
                .ToArray();
            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        [Benchmark]
        public async Task<int> ConcurrentTasks_50() {
            var tasks = Enumerable.Range(0, 50)
                .Select(_ => SimulateWorkAsync())
                .ToArray();
            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        [Benchmark]
        public async Task<int> ConcurrentTasks_100() {
            var tasks = Enumerable.Range(0, 100)
                .Select(_ => SimulateWorkAsync())
                .ToArray();
            await Task.WhenAll(tasks);
            return tasks.Length;
        }

        private async Task SimulateWorkAsync() {
            await Task.Delay(10);
            var result = 0.0;
            for (int i = 0; i < 1000; i++) {
                result += Math.Sqrt(i + 1);
            }
        }
    }
}
