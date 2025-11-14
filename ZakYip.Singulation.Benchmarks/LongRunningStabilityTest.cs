using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Benchmarks {

    /// <summary>
    /// 长时间运行稳定性测试（24小时+）
    /// 监控内存泄漏、性能退化、错误累积等问题
    /// </summary>
    public class LongRunningStabilityTest {

        private readonly TimeSpan _testDuration;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<PerformanceSnapshot> _snapshots = new();
        private readonly object _lock = new();

        public LongRunningStabilityTest(TimeSpan? duration = null) {
            _testDuration = duration ?? TimeSpan.FromHours(24);
        }

        public async Task RunAsync() {
            Console.WriteLine($"开始长时间稳定性测试，持续时间: {_testDuration.TotalHours:F1} 小时");
            Console.WriteLine($"开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine(new string('-', 80));

            var startTime = DateTime.Now;
            var stopwatch = Stopwatch.StartNew();

            // 启动监控任务
            var monitorTask = Task.Run(() => MonitorSystemHealthAsync(_cts.Token));
            
            // 启动工作负载任务
            var workloadTask = Task.Run(() => RunWorkloadAsync(_cts.Token));

            // 等待测试持续时间
            await Task.Delay(_testDuration);

            // 停止所有任务
            _cts.Cancel();

            try {
                await Task.WhenAll(monitorTask, workloadTask);
            }
            catch (OperationCanceledException) {
                // 预期的取消异常
            }

            stopwatch.Stop();

            // 生成测试报告
            GenerateReport(startTime, stopwatch.Elapsed);
        }

        private async Task MonitorSystemHealthAsync(CancellationToken ct) {
            var interval = TimeSpan.FromMinutes(5); // 每5分钟采样一次
            var iteration = 0;

            while (!ct.IsCancellationRequested) {
                try {
                    var snapshot = CapturePerformanceSnapshot(iteration);
                    
                    lock (_lock) {
                        _snapshots.Add(snapshot);
                    }

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] " +
                        $"迭代: {iteration}, " +
                        $"内存: {snapshot.MemoryMB:F1} MB, " +
                        $"GC0: {snapshot.Gen0Collections}, " +
                        $"GC1: {snapshot.Gen1Collections}, " +
                        $"GC2: {snapshot.Gen2Collections}, " +
                        $"线程: {snapshot.ThreadCount}");

                    iteration++;
                    await Task.Delay(interval, ct);
                }
                catch (OperationCanceledException) {
                    break;
                }
#pragma warning disable CA1031 // 监控循环捕获所有异常以保证测试持续运行
                catch (Exception ex) {
#pragma warning restore CA1031
                    Console.WriteLine($"监控错误: {ex.Message}");
                }
            }
        }

        private async Task RunWorkloadAsync(CancellationToken ct) {
            var random = new Random();
            var operationCount = 0;
            var errorCount = 0;

            while (!ct.IsCancellationRequested) {
                try {
                    // 模拟各种操作
                    await SimulateBatchOperationAsync(random.Next(10, 100));
                    await SimulateIoOperationAsync(random.Next(10, 50));
                    await SimulateConcurrentOperationAsync(random.Next(5, 20));

                    operationCount += 3;

                    // 短暂休息以避免 CPU 100%
                    await Task.Delay(100, ct);
                }
                catch (OperationCanceledException) {
                    break;
                }
#pragma warning disable CA1031 // 工作负载循环捕获所有异常以统计错误率
                catch (Exception ex) {
#pragma warning restore CA1031
                    errorCount++;
                    Console.WriteLine($"工作负载错误 #{errorCount}: {ex.Message}");
                }
            }

            Console.WriteLine($"工作负载完成: {operationCount} 次操作, {errorCount} 次错误");
        }

        private PerformanceSnapshot CapturePerformanceSnapshot(int iteration) {
            var process = Process.GetCurrentProcess();
            
            return new PerformanceSnapshot {
                Iteration = iteration,
                Timestamp = DateTime.Now,
                MemoryMB = process.WorkingSet64 / 1024.0 / 1024.0,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                ThreadCount = process.Threads.Count
            };
        }

        private async Task SimulateBatchOperationAsync(int count) {
            var tasks = Enumerable.Range(0, count)
                .Select(_ => Task.Run(async () => {
                    await Task.Delay(10);
                    var result = 0.0;
                    for (int i = 0; i < 1000; i++) {
                        result += Math.Sqrt(i + 1);
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        private async Task SimulateIoOperationAsync(int count) {
            for (int i = 0; i < count; i++) {
                await Task.Delay(5);
            }
        }

        private async Task SimulateConcurrentOperationAsync(int count) {
            var tasks = Enumerable.Range(0, count)
                .Select(_ => Task.Delay(20))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        private void GenerateReport(DateTime startTime, TimeSpan actualDuration) {
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("稳定性测试报告");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"开始时间: {startTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"结束时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"实际持续时间: {actualDuration.TotalHours:F2} 小时");
            Console.WriteLine();

            if (_snapshots.Count == 0) {
                Console.WriteLine("警告: 没有收集到性能快照数据");
                return;
            }

            // 内存分析
            var memoryStats = AnalyzeMemory();
            Console.WriteLine("内存统计:");
            Console.WriteLine($"  初始内存: {memoryStats.Initial:F1} MB");
            Console.WriteLine($"  最终内存: {memoryStats.Final:F1} MB");
            Console.WriteLine($"  峰值内存: {memoryStats.Peak:F1} MB");
            Console.WriteLine($"  平均内存: {memoryStats.Average:F1} MB");
            Console.WriteLine($"  内存增长: {memoryStats.Growth:F1} MB ({memoryStats.GrowthPercent:F1}%)");
            Console.WriteLine();

            // GC 分析
            var gcStats = AnalyzeGC();
            Console.WriteLine("垃圾回收统计:");
            Console.WriteLine($"  Gen0 回收次数: {gcStats.Gen0Count}");
            Console.WriteLine($"  Gen1 回收次数: {gcStats.Gen1Count}");
            Console.WriteLine($"  Gen2 回收次数: {gcStats.Gen2Count}");
            Console.WriteLine($"  平均 Gen0 间隔: {gcStats.AvgGen0Interval:F1} 分钟");
            Console.WriteLine();

            // 线程分析
            var threadStats = AnalyzeThreads();
            Console.WriteLine("线程统计:");
            Console.WriteLine($"  初始线程数: {threadStats.Initial}");
            Console.WriteLine($"  最终线程数: {threadStats.Final}");
            Console.WriteLine($"  峰值线程数: {threadStats.Peak}");
            Console.WriteLine($"  平均线程数: {threadStats.Average:F1}");
            Console.WriteLine();

            // 稳定性评估
            var stability = EvaluateStability(memoryStats, gcStats, threadStats);
            Console.WriteLine($"稳定性评估: {stability.Level}");
            Console.WriteLine($"评分: {stability.Score}/100");
            if (stability.Issues.Count > 0) {
                Console.WriteLine("发现的问题:");
                foreach (var issue in stability.Issues) {
                    Console.WriteLine($"  - {issue}");
                }
            }
            else {
                Console.WriteLine("未发现明显问题。");
            }

            Console.WriteLine(new string('=', 80));
        }

        private MemoryStats AnalyzeMemory() {
            lock (_lock) {
                var memories = _snapshots.Select(s => s.MemoryMB).ToList();
                var initial = memories.First();
                var final = memories.Last();
                
                return new MemoryStats {
                    Initial = initial,
                    Final = final,
                    Peak = memories.Max(),
                    Average = memories.Average(),
                    Growth = final - initial,
                    GrowthPercent = (final - initial) / initial * 100
                };
            }
        }

        private GCStats AnalyzeGC() {
            lock (_lock) {
                var first = _snapshots.First();
                var last = _snapshots.Last();
                var duration = (last.Timestamp - first.Timestamp).TotalMinutes;
                
                return new GCStats {
                    Gen0Count = last.Gen0Collections - first.Gen0Collections,
                    Gen1Count = last.Gen1Collections - first.Gen1Collections,
                    Gen2Count = last.Gen2Collections - first.Gen2Collections,
                    AvgGen0Interval = duration / Math.Max(1, last.Gen0Collections - first.Gen0Collections)
                };
            }
        }

        private ThreadStats AnalyzeThreads() {
            lock (_lock) {
                var threads = _snapshots.Select(s => s.ThreadCount).ToList();
                
                return new ThreadStats {
                    Initial = threads.First(),
                    Final = threads.Last(),
                    Peak = threads.Max(),
                    Average = threads.Average()
                };
            }
        }

        private StabilityAssessment EvaluateStability(MemoryStats memory, GCStats gc, ThreadStats threads) {
            var issues = new List<string>();
            var score = 100;

            // 检查内存泄漏
            if (memory.GrowthPercent > 50) {
                issues.Add($"可能存在内存泄漏：内存增长 {memory.GrowthPercent:F1}%");
                score -= 30;
            }
            else if (memory.GrowthPercent > 20) {
                issues.Add($"内存增长较大：{memory.GrowthPercent:F1}%");
                score -= 15;
            }

            // 检查 Gen2 GC 频率
            if (gc.Gen2Count > 100) {
                issues.Add($"Gen2 GC 次数过多：{gc.Gen2Count} 次");
                score -= 20;
            }

            // 检查线程泄漏
            if (threads.Final > threads.Initial * 1.5) {
                issues.Add($"可能存在线程泄漏：线程数从 {threads.Initial} 增长到 {threads.Final}");
                score -= 25;
            }

            var level = score >= 90 ? "优秀" :
                       score >= 70 ? "良好" :
                       score >= 50 ? "一般" : "差";

            return new StabilityAssessment {
                Score = score,
                Level = level,
                Issues = issues
            };
        }

        private class PerformanceSnapshot {
            public int Iteration { get; set; }
            public DateTime Timestamp { get; set; }
            public double MemoryMB { get; set; }
            public int Gen0Collections { get; set; }
            public int Gen1Collections { get; set; }
            public int Gen2Collections { get; set; }
            public int ThreadCount { get; set; }
        }

        private class MemoryStats {
            public double Initial { get; set; }
            public double Final { get; set; }
            public double Peak { get; set; }
            public double Average { get; set; }
            public double Growth { get; set; }
            public double GrowthPercent { get; set; }
        }

        private class GCStats {
            public int Gen0Count { get; set; }
            public int Gen1Count { get; set; }
            public int Gen2Count { get; set; }
            public double AvgGen0Interval { get; set; }
        }

        private class ThreadStats {
            public int Initial { get; set; }
            public int Final { get; set; }
            public int Peak { get; set; }
            public double Average { get; set; }
        }

        private class StabilityAssessment {
            public int Score { get; set; }
            public string Level { get; set; } = "";
            public List<string> Issues { get; set; } = new();
        }
    }

    /// <summary>
    /// 长时间稳定性测试的运行器
    /// 使用方法: dotnet run --project ZakYip.Singulation.Benchmarks stability [hours]
    /// </summary>
    public static class StabilityTestRunner {
        public static async Task RunAsync(string[] args) {
            TimeSpan duration;

            if (args.Length > 1 && double.TryParse(args[1], out var hours)) {
                duration = TimeSpan.FromHours(hours);
            }
            else {
                duration = TimeSpan.FromHours(24); // 默认24小时
            }

            var test = new LongRunningStabilityTest(duration);
            await test.RunAsync();
        }

        public static void PrintHelp() {
            Console.WriteLine("长时间稳定性测试工具");
            Console.WriteLine("使用方法:");
            Console.WriteLine("  dotnet run --project ZakYip.Singulation.Benchmarks stability [小时数]");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  dotnet run --project ZakYip.Singulation.Benchmarks stability 1    # 1小时测试");
            Console.WriteLine("  dotnet run --project ZakYip.Singulation.Benchmarks stability 24   # 24小时测试");
            Console.WriteLine("  dotnet run --project ZakYip.Singulation.Benchmarks stability 0.1  # 6分钟测试（用于快速验证）");
        }
    }
}
