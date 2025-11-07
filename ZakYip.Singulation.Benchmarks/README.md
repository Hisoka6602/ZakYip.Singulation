# ZakYip.Singulation 性能基准测试

本项目包含 ZakYip.Singulation 的性能基准测试和长时间稳定性测试工具。

## 测试内容

### 1. 批量操作性能测试 (BatchOperationBenchmarks)
测试不同规模轴的批量操作性能：
- **10轴并行操作** - 基线测试
- **50轴并行操作** - 中等规模
- **100轴并行操作** - 大规模测试
- **顺序操作对比** - 并行 vs 顺序性能对比

### 2. 内存分配和GC压力测试 (MemoryAllocationBenchmarks)
监控内存分配和垃圾回收压力：
- **小对象分配** - 1000/10000次分配测试
- **数组分配** - 小数组(1KB)/中数组(10KB)分配
- **ArrayPool使用** - 对象池 vs 直接分配性能对比

### 3. IO操作性能测试 (IoOperationBenchmarks)
测试IO操作的性能特征：
- **顺序IO写入** - 100端口顺序写入
- **并行IO写入** - 100端口并行写入
- **顺序IO读取** - 100端口顺序读取
- **并行IO读取** - 100端口并行读取

### 4. 并发操作性能测试 (ConcurrencyBenchmarks)
测试不同并发度的性能：
- **10并发任务** - 轻度并发
- **50并发任务** - 中度并发
- **100并发任务** - 高度并发

### 5. 协议编解码性能测试 (ProtocolBenchmarks)
- **字节数组复制性能**：测试传统数组复制的性能
- **Span<byte> 切片性能**：测试使用 Span 的零拷贝性能
- **整数解析性能**：测试大端序整数解析的性能

### 6. LINQ vs 循环性能对比 (LinqVsLoopBenchmarks)
- **LINQ Where + Sum**：测试 LINQ 链式调用的性能
- **传统 foreach 循环**：测试传统循环的性能
- **Span 循环优化**：测试使用 Span 的高性能循环

### 7. 长时间稳定性测试 (LongRunningStabilityTest)
测试系统长时间运行的稳定性：
- **内存泄漏检测** - 监控内存增长趋势
- **GC压力监控** - Gen0/1/2回收统计
- **线程泄漏检测** - 监控线程数变化
- **性能退化监控** - 每5分钟采样
- **稳定性评分** - 0-100分综合评分

## 运行测试

### 运行默认测试（批量操作）
```bash
cd ZakYip.Singulation.Benchmarks
dotnet run -c Release
```

### 运行特定测试套件
```bash
# 批量操作性能测试
dotnet run -c Release -- batch

# 内存分配和GC测试
dotnet run -c Release -- memory

# IO操作性能测试
dotnet run -c Release -- io

# 并发操作性能测试
dotnet run -c Release -- concurrency

# 协议编解码测试
dotnet run -c Release -- protocol

# LINQ vs 循环对比
dotnet run -c Release -- linq

# 运行所有基准测试
dotnet run -c Release -- all
```

### 运行长时间稳定性测试
```bash
# 1小时稳定性测试
dotnet run -c Release -- stability 1

# 24小时稳定性测试
dotnet run -c Release -- stability 24

# 6分钟快速验证测试
dotnet run -c Release -- stability 0.1
```

## 性能指标说明

### 基准测试指标

- **Mean (平均值)**：操作的平均执行时间
- **StdDev (标准差)**：执行时间的标准偏差，越小说明越稳定
- **Min/Max**：最快和最慢的执行时间
- **Median (中位数)**：50% 的操作执行时间
- **P95 (95分位)**：95% 的操作执行时间不超过此值
- **Allocated (分配内存)**：每次操作分配的内存大小
- **Ratio (比率)**：相对于基线测试的性能比率

### 稳定性测试指标

- **内存增长率** - 测试期间内存增长百分比
- **GC频率** - Gen0/1/2垃圾回收次数和间隔
- **线程变化** - 线程数的初始值、峰值和最终值
- **稳定性评分** - 综合评分（0-100分）
  - 90-100: 优秀
  - 70-89: 良好
  - 50-69: 一般
  - <50: 差

## 示例输出

### 基准测试输出
```
|                Method |      Mean |    StdDev |       Min |       Max |    Median |       P95 | Ratio |
|---------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|
| BatchOperation_10Axes |  52.34 ms |  1.234 ms |  50.12 ms |  55.67 ms |  52.11 ms |  54.89 ms |  1.00 |
| BatchOperation_50Axes | 245.67 ms |  5.678 ms | 238.90 ms | 256.78 ms | 244.56 ms | 253.45 ms |  4.69 |
|BatchOperation_100Axes | 487.89 ms | 10.234 ms | 475.12 ms | 505.67 ms | 486.34 ms | 502.34 ms |  9.32 |
```

### 稳定性测试输出
```
===================================================
稳定性测试报告
===================================================
开始时间: 2025-11-07 10:00:00
结束时间: 2025-11-07 11:00:00
实际持续时间: 1.00 小时

内存统计:
  初始内存: 45.2 MB
  最终内存: 47.8 MB
  峰值内存: 52.1 MB
  平均内存: 48.5 MB
  内存增长: 2.6 MB (5.8%)

垃圾回收统计:
  Gen0 回收次数: 125
  Gen1 回收次数: 12
  Gen2 回收次数: 2
  平均 Gen0 间隔: 0.5 分钟

线程统计:
  初始线程数: 15
  最终线程数: 16
  峰值线程数: 18
  平均线程数: 16.2

稳定性评估: 优秀
评分: 95/100
未发现明显问题。
===================================================
```

## 性能优化建议

基于测试结果，以下是推荐的性能优化实践：

### 1. 批量操作优化
- ✅ **使用并行操作** - 相比顺序操作，性能提升明显
- ✅ **控制并发度** - 避免过度并发导致资源竞争
- ⚠️ **注意上下文切换开销** - 轻量级操作可能不适合并行

### 2. 内存管理优化
- ✅ **使用 ArrayPool** - 减少GC压力，提升性能
- ✅ **使用 Span\<T\> 和 ReadOnlySpan\<T\>** - 减少内存分配
- ✅ **避免装箱拆箱** - 使用泛型避免 object 类型
- ⚠️ **控制对象生命周期** - 及时释放大对象

### 3. IO操作优化
- ✅ **并行IO操作** - 显著提升吞吐量
- ✅ **批量提交** - 减少网络往返次数
- ⚠️ **控制并发度** - 根据硬件能力调整

### 4. 选择合适的循环方式
- **简单筛选**：LINQ 可读性好，性能略逊
- **高频操作**：使用传统循环或 Span
- **内存敏感**：优先使用 Span

### 5. 长时间运行优化
- ✅ **定期监控内存** - 检测潜在内存泄漏
- ✅ **监控GC压力** - Gen2次数过多需优化
- ✅ **监控线程数** - 避免线程泄漏
- ⚠️ **建立性能基线** - 便于检测性能退化

## 扩展测试

要添加新的性能测试，在 `PerformanceBenchmarks.cs` 中创建新的类：

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class MyBenchmarks {
    [GlobalSetup]
    public void Setup() {
        // 初始化测试数据
    }

    [Benchmark(Baseline = true)]
    public void MyBaselineTest() {
        // 基线测试代码
    }

    [Benchmark]
    public void MyOptimizedTest() {
        // 优化版本测试代码
    }
}
```

然后在 `Program.cs` 的 switch 语句中添加：
```csharp
case "my":
    BenchmarkRunner.Run<MyBenchmarks>(config);
    break;
```

## CI/CD 集成

可以将基准测试集成到 CI/CD 流程中：

```yaml
# GitHub Actions 示例
- name: Run Benchmarks
  run: |
    dotnet run -c Release --project ZakYip.Singulation.Benchmarks -- batch
    
- name: Run Stability Test
  run: |
    dotnet run -c Release --project ZakYip.Singulation.Benchmarks -- stability 0.1
```

## 注意事项

1. **运行环境**：
   - 基准测试应在生产环境相似的硬件上运行
   - 关闭其他应用以减少干扰
   - 使用 Release 配置以获得真实性能数据

2. **测试时间**：
   - 基准测试通常需要几分钟到几十分钟
   - 长时间稳定性测试需要数小时甚至数天
   - 合理安排测试时间

3. **结果解读**：
   - 关注相对性能（Ratio）而非绝对值
   - 多次运行以确保结果稳定
   - 结合业务需求评估性能是否满足要求
