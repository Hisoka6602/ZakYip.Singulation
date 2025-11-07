using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;

namespace ZakYip.Singulation.Benchmarks;

/// <summary>
/// ZakYip.Singulation 性能基准测试主程序
/// </summary>
class Program {
    static async Task Main(string[] args) {
        // 检查是否是稳定性测试
        if (args.Length > 0 && args[0] == "stability") {
            await StabilityTestRunner.RunAsync(args);
            return;
        }

        Console.WriteLine("===================================================");
        Console.WriteLine("ZakYip.Singulation 性能基准测试工具");
        Console.WriteLine("===================================================");
        Console.WriteLine();
        Console.WriteLine("可用的测试套件:");
        Console.WriteLine("  1. ProtocolBenchmarks - 协议编解码性能测试");
        Console.WriteLine("  2. LinqVsLoopBenchmarks - LINQ vs 循环性能对比");
        Console.WriteLine("  3. BatchOperationBenchmarks - 批量操作性能测试 (10/50/100轴)");
        Console.WriteLine("  4. MemoryAllocationBenchmarks - 内存分配和GC压力测试");
        Console.WriteLine("  5. IoOperationBenchmarks - IO操作性能测试");
        Console.WriteLine("  6. ConcurrencyBenchmarks - 并发操作性能测试");
        Console.WriteLine("  7. All - 运行所有基准测试");
        Console.WriteLine();
        Console.WriteLine("特殊命令:");
        Console.WriteLine("  stability [小时数] - 运行长时间稳定性测试");
        Console.WriteLine();
        
        var config = DefaultConfig.Instance
            .AddColumn(StatisticColumn.Mean)
            .AddColumn(StatisticColumn.StdDev)
            .AddColumn(StatisticColumn.Min)
            .AddColumn(StatisticColumn.Max)
            .AddColumn(StatisticColumn.Median)
            .AddColumn(StatisticColumn.P95)
            .AddColumn(BaselineRatioColumn.RatioMean)
            .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));
        
        // 根据命令行参数选择要运行的基准测试
        if (args.Length > 0) {
            switch (args[0].ToLower()) {
                case "protocol":
                    BenchmarkRunner.Run<ProtocolBenchmarks>(config);
                    break;
                case "linq":
                    BenchmarkRunner.Run<LinqVsLoopBenchmarks>(config);
                    break;
                case "batch":
                    BenchmarkRunner.Run<BatchOperationBenchmarks>(config);
                    break;
                case "memory":
                    BenchmarkRunner.Run<MemoryAllocationBenchmarks>(config);
                    break;
                case "io":
                    BenchmarkRunner.Run<IoOperationBenchmarks>(config);
                    break;
                case "concurrency":
                    BenchmarkRunner.Run<ConcurrencyBenchmarks>(config);
                    break;
                case "all":
                    BenchmarkRunner.Run<ProtocolBenchmarks>(config);
                    BenchmarkRunner.Run<LinqVsLoopBenchmarks>(config);
                    BenchmarkRunner.Run<BatchOperationBenchmarks>(config);
                    BenchmarkRunner.Run<MemoryAllocationBenchmarks>(config);
                    BenchmarkRunner.Run<IoOperationBenchmarks>(config);
                    BenchmarkRunner.Run<ConcurrencyBenchmarks>(config);
                    break;
                default:
                    Console.WriteLine($"未知的基准测试类型: {args[0]}");
                    Console.WriteLine("使用 'all' 运行所有测试，或指定具体的测试类型");
                    StabilityTestRunner.PrintHelp();
                    return;
            }
        }
        else {
            // 默认运行批量操作基准测试（最重要的测试）
            BenchmarkRunner.Run<BatchOperationBenchmarks>(config);
        }
        
        Console.WriteLine();
        Console.WriteLine("===================================================");
        Console.WriteLine("性能测试完成！");
        Console.WriteLine("===================================================");
    }
}

/// <summary>
/// 协议编解码性能测试
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
public class ProtocolBenchmarks {
    private byte[] _speedFrameData = null!;
    private byte[] _positionFrameData = null!;
    
    [GlobalSetup]
    public void Setup() {
        // 准备测试数据：模拟上游协议帧
        _speedFrameData = new byte[] { 
            0x81, // 速度端命令字
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F 
        };
        
        _positionFrameData = new byte[] {
            0x82, // 位置端命令字
            0x00, 0x00, 0x00, 0x01, // X1
            0x00, 0x00, 0x00, 0x02, // Y1
            0x00, 0x00, 0x00, 0x03, // X2
            0x00, 0x00, 0x00, 0x04  // Y2
        };
    }
    
    /// <summary>
    /// 基准测试：字节数组复制性能
    /// </summary>
    [Benchmark(Baseline = true)]
    public byte[] ByteArrayCopy() {
        var result = new byte[_speedFrameData.Length];
        _speedFrameData.CopyTo(result, 0);
        return result;
    }
    
    /// <summary>
    /// 基准测试：Span&lt;byte&gt; 切片性能
    /// </summary>
    [Benchmark]
    public ReadOnlySpan<byte> SpanSlice() {
        return new ReadOnlySpan<byte>(_speedFrameData);
    }
    
    /// <summary>
    /// 基准测试：整数解析性能（大端序）
    /// </summary>
    [Benchmark]
    public int ParseBigEndianInt() {
        var span = new ReadOnlySpan<byte>(_positionFrameData, 1, 4);
        return (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
    }
}

/// <summary>
/// LINQ vs 循环性能对比测试
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class LinqVsLoopBenchmarks {
    private List<int> _data = null!;
    private const int Count = 10000;
    
    [GlobalSetup]
    public void Setup() {
        _data = Enumerable.Range(1, Count).ToList();
    }
    
    /// <summary>
    /// 使用 LINQ 筛选数据
    /// </summary>
    [Benchmark(Baseline = true)]
    public int LinqWhere() {
        return _data.Where(x => x % 2 == 0).Sum();
    }
    
    /// <summary>
    /// 使用传统循环筛选数据
    /// </summary>
    [Benchmark]
    public int ForLoop() {
        var sum = 0;
        foreach (var item in _data) {
            if (item % 2 == 0) {
                sum += item;
            }
        }
        return sum;
    }
    
    /// <summary>
    /// 使用 Span&lt;T&gt; 和循环（高性能版本）
    /// </summary>
    [Benchmark]
    public int SpanLoop() {
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_data);
        var sum = 0;
        for (var i = 0; i < span.Length; i++) {
            if (span[i] % 2 == 0) {
                sum += span[i];
            }
        }
        return sum;
    }
}
