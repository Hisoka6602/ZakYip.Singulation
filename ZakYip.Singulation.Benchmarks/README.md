# ZakYip.Singulation 性能基准测试

本项目包含 ZakYip.Singulation 的性能基准测试。

## 测试内容

### 1. 协议编解码性能测试 (ProtocolBenchmarks)
- **字节数组复制性能**：测试传统数组复制的性能
- **Span<byte> 切片性能**：测试使用 Span 的零拷贝性能
- **整数解析性能**：测试大端序整数解析的性能

### 2. LINQ vs 循环性能对比 (LinqVsLoopBenchmarks)
- **LINQ Where + Sum**：测试 LINQ 链式调用的性能
- **传统 foreach 循环**：测试传统循环的性能
- **Span 循环优化**：测试使用 Span 的高性能循环

## 运行测试

### 运行所有测试
```bash
cd ZakYip.Singulation.Benchmarks
dotnet run -c Release
```

### 运行特定测试
```bash
# 只运行协议测试
dotnet run -c Release --filter "*ProtocolBenchmarks*"

# 只运行 LINQ 测试
dotnet run -c Release --filter "*LinqVsLoopBenchmarks*"
```

## 性能指标说明

- **Mean (平均值)**：操作的平均执行时间
- **StdDev (标准差)**：执行时间的标准偏差，越小说明越稳定
- **Min/Max**：最快和最慢的执行时间
- **Median (中位数)**：50% 的操作执行时间
- **P95 (95分位)**：95% 的操作执行时间不超过此值
- **Allocated (分配内存)**：每次操作分配的内存大小

## 性能优化建议

基于测试结果，以下是推荐的性能优化实践：

1. **使用 Span\<T\> 和 ReadOnlySpan\<T\>**
   - 减少内存分配
   - 避免不必要的数组复制
   - 适用于协议解析、缓冲区处理

2. **选择合适的循环方式**
   - 简单筛选：LINQ 可读性好，性能略逊
   - 高频操作：使用传统循环或 Span
   - 内存敏感：优先使用 Span

3. **避免装箱拆箱**
   - 使用泛型避免 object 类型
   - 值类型尽量保持在栈上

4. **使用 ArrayPool 复用数组**
   - 减少 GC 压力
   - 适用于临时缓冲区

## 扩展测试

要添加新的性能测试，在 `Program.cs` 中创建新的类：

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class MyBenchmarks {
    [Benchmark]
    public void MyTest() {
        // 测试代码
    }
}
```

然后在 `Main` 方法中运行：
```csharp
BenchmarkRunner.Run<MyBenchmarks>(config, args);
```
