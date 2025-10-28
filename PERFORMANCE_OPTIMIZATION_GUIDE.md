# 性能优化指南和分析
# Performance Optimization Guide and Analysis

本文档提供性能分析、优化建议和最佳实践。

## 1. 性能分析工具

### 1.1 BenchmarkDotNet

项目已包含 `ZakYip.Singulation.Benchmarks` 项目，用于性能基准测试。

运行基准测试：
```bash
cd ZakYip.Singulation.Benchmarks
dotnet run -c Release
```

### 1.2 dotnet-counters

实时监控性能计数器：
```bash
# 安装工具
dotnet tool install --global dotnet-counters

# 监控运行中的应用
dotnet-counters monitor --process-id <pid> System.Runtime Microsoft.AspNetCore.Hosting
```

### 1.3 dotnet-trace

收集性能追踪数据：
```bash
# 安装工具
dotnet tool install --global dotnet-trace

# 收集追踪
dotnet-trace collect --process-id <pid>
```

### 1.4 Visual Studio Profiler

使用 Visual Studio 的性能分析器：
- CPU 使用率分析
- 内存分配分析
- 异步操作分析

## 2. 已识别的性能瓶颈

### 2.1 日志记录

**问题**: 传统日志方法会导致字符串分配和装箱

**解决方案**: 使用 LoggerMessage 源生成器

```csharp
// ❌ 低性能：字符串插值和装箱
_logger.LogInformation($"轴 {axisId} 移动到 {position}");

// ✅ 高性能：LoggerMessage 源生成器（零分配）
_logger.AxisMotionCompleted(axisId, "Absolute", position, elapsedMs);
```

**性能提升**: 2-10倍（取决于日志频率）

### 2.2 异步操作

**问题**: 不必要的 async/await 增加开销

**优化建议**:
```csharp
// ❌ 不必要的 async/await
public async Task<int> GetValueAsync()
{
    return await _repository.GetValueAsync();
}

// ✅ 直接返回 Task
public Task<int> GetValueAsync()
{
    return _repository.GetValueAsync();
}
```

### 2.3 集合操作

**问题**: LINQ 链式调用可能创建多个中间集合

**优化建议**:
```csharp
// ❌ 多次迭代
var result = list
    .Where(x => x.IsActive)
    .Select(x => x.Id)
    .ToList();

// ✅ 单次迭代
var result = new List<int>(list.Count);
foreach (var item in list)
{
    if (item.IsActive)
        result.Add(item.Id);
}
```

### 2.4 字符串操作

**问题**: 频繁的字符串拼接导致大量分配

**优化建议**:
```csharp
// ❌ 字符串拼接
string result = "";
foreach (var item in items)
{
    result += item.ToString(); // 每次都创建新字符串
}

// ✅ StringBuilder
var sb = new StringBuilder(items.Count * 10);
foreach (var item in items)
{
    sb.Append(item.ToString());
}
string result = sb.ToString();
```

## 3. 内存优化

### 3.1 使用 ArrayPool

对于临时缓冲区，使用 `ArrayPool` 减少分配：

```csharp
// ❌ 每次分配新数组
byte[] buffer = new byte[1024];
try
{
    // 使用 buffer
}
finally
{
    // buffer 被 GC 回收
}

// ✅ 使用 ArrayPool
byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
try
{
    // 使用 buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

### 3.2 使用 Span<T> 和 Memory<T>

避免不必要的数组拷贝：

```csharp
// ❌ 创建子数组（分配）
byte[] subArray = new byte[length];
Array.Copy(sourceArray, offset, subArray, 0, length);
ProcessData(subArray);

// ✅ 使用 Span（零分配）
Span<byte> slice = sourceArray.AsSpan(offset, length);
ProcessData(slice);
```

### 3.3 对象池化

对于频繁创建/销毁的对象，使用对象池：

```csharp
public class ConnectionPool
{
    private readonly ObjectPool<TcpClient> _pool;

    public ConnectionPool()
    {
        _pool = new DefaultObjectPool<TcpClient>(
            new TcpClientPooledObjectPolicy());
    }

    public TcpClient Rent() => _pool.Get();
    public void Return(TcpClient client) => _pool.Return(client);
}
```

## 4. 异步性能优化

### 4.1 ValueTask 使用

对于可能同步完成的操作，使用 `ValueTask`:

```csharp
// ❌ 总是分配 Task（即使同步完成）
public async Task<int> GetCachedValueAsync(string key)
{
    if (_cache.TryGetValue(key, out var value))
        return value; // 仍然分配 Task
    
    return await FetchFromDatabaseAsync(key);
}

// ✅ ValueTask 避免分配
public ValueTask<int> GetCachedValueAsync(string key)
{
    if (_cache.TryGetValue(key, out var value))
        return new ValueTask<int>(value); // 不分配
    
    return new ValueTask<int>(FetchFromDatabaseAsync(key));
}
```

### 4.2 ConfigureAwait(false)

在库代码中使用 `ConfigureAwait(false)` 避免上下文切换：

```csharp
// 在非 UI 库代码中
public async Task ProcessAsync()
{
    var data = await FetchDataAsync().ConfigureAwait(false);
    await SaveDataAsync(data).ConfigureAwait(false);
}

// 注意：ASP.NET Core 中不需要 ConfigureAwait(false)
```

### 4.3 并行异步操作

使用 `Task.WhenAll` 并行执行独立操作：

```csharp
// ❌ 串行执行（慢）
var data1 = await FetchData1Async();
var data2 = await FetchData2Async();
var data3 = await FetchData3Async();

// ✅ 并行执行（快）
var tasks = new[]
{
    FetchData1Async(),
    FetchData2Async(),
    FetchData3Async()
};
var results = await Task.WhenAll(tasks);
```

## 5. 数据库性能优化

### 5.1 使用索引

确保频繁查询的字段有索引：

```csharp
// LiteDB 示例
collection.EnsureIndex(x => x.AxisId);
collection.EnsureIndex(x => x.Timestamp);
```

### 5.2 批量操作

使用批量操作而不是单个操作：

```csharp
// ❌ 单个插入
foreach (var item in items)
{
    collection.Insert(item);
}

// ✅ 批量插入
collection.InsertBulk(items);
```

### 5.3 连接池

使用连接池管理数据库连接：

```csharp
// LiteDB 已内置连接池
var db = new LiteDatabase(connectionString);
```

## 6. 网络性能优化

### 6.1 使用 SocketAsyncEventArgs

对于高频网络操作，使用 `SocketAsyncEventArgs` 减少分配：

```csharp
public class HighPerformanceSocket
{
    private readonly SocketAsyncEventArgs _receiveArgs;

    public HighPerformanceSocket()
    {
        _receiveArgs = new SocketAsyncEventArgs();
        _receiveArgs.Completed += OnReceiveCompleted;
        _receiveArgs.SetBuffer(new byte[8192], 0, 8192);
    }

    public void ReceiveAsync()
    {
        if (!_socket.ReceiveAsync(_receiveArgs))
        {
            OnReceiveCompleted(this, _receiveArgs);
        }
    }
}
```

### 6.2 TCP 选项优化

```csharp
tcpClient.NoDelay = true; // 禁用 Nagle 算法（低延迟）
tcpClient.ReceiveBufferSize = 65536; // 增大接收缓冲区
tcpClient.SendBufferSize = 65536; // 增大发送缓冲区
```

### 6.3 使用 Channel 进行生产者-消费者模式

```csharp
var channel = Channel.CreateBounded<Frame>(new BoundedChannelOptions(1000)
{
    FullMode = BoundedChannelFullMode.DropOldest
});

// 生产者
await channel.Writer.WriteAsync(frame);

// 消费者
await foreach (var frame in channel.Reader.ReadAllAsync())
{
    await ProcessFrameAsync(frame);
}
```

## 7. 缓存策略

### 7.1 内存缓存

使用 `IMemoryCache` 缓存频繁访问的数据：

```csharp
public class CachedConfigService
{
    private readonly IMemoryCache _cache;

    public async Task<Config> GetConfigAsync()
    {
        return await _cache.GetOrCreateAsync("config", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _configStore.LoadAsync();
        });
    }
}
```

### 7.2 分布式缓存

对于多实例部署，使用分布式缓存（如 Redis）：

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

## 8. 编译优化

### 8.1 Release 模式

始终在 Release 模式下进行性能测试：

```bash
dotnet build -c Release
dotnet run -c Release
```

### 8.2 启用编译器优化

在 .csproj 中：

```xml
<PropertyGroup>
  <Optimize>true</Optimize>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

### 8.3 使用 ReadyToRun (R2R)

减少启动时间：

```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```

## 9. 性能监控指标

### 9.1 关键指标

监控以下性能指标：

| 指标 | 目标 | 说明 |
|-----|------|------|
| API 响应时间 (P95) | < 100ms | 95% 的请求应在 100ms 内完成 |
| 轴运动延迟 | < 10ms | 从接收命令到开始执行 |
| 帧解析吞吐量 | > 10000 帧/秒 | 协议解析性能 |
| GC 停顿时间 | < 5ms | 垃圾回收暂停时间 |
| 内存使用 | < 500MB | 稳定状态下的内存占用 |
| CPU 使用率 | < 50% | 正常负载下的 CPU 使用 |

### 9.2 性能测试

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ProtocolBenchmark
{
    private byte[] _frameData;
    private ICodec _codec;

    [GlobalSetup]
    public void Setup()
    {
        _frameData = GenerateTestFrame();
        _codec = new GuiweiCodec();
    }

    [Benchmark]
    public void DecodeFrame()
    {
        _codec.Decode(_frameData);
    }
}
```

## 10. 性能优化检查清单

在开发新功能时，检查：

- [ ] 高频日志使用了 LoggerMessage 源生成器
- [ ] 避免了不必要的 async/await
- [ ] 使用了合适的集合类型和容量
- [ ] 临时缓冲区使用了 ArrayPool 或 stackalloc
- [ ] 异步操作使用了 ValueTask（如果合适）
- [ ] 数据库查询使用了索引和批量操作
- [ ] 缓存了频繁访问的数据
- [ ] 避免了字符串拼接（使用 StringBuilder）
- [ ] 使用了 Span<T> 避免数组拷贝
- [ ] 在 Release 模式下测试了性能

## 11. 性能回归测试

### 11.1 持续性能监控

在 CI/CD 中集成性能测试：

```yaml
# .github/workflows/performance.yml
- name: Run Benchmarks
  run: |
    cd ZakYip.Singulation.Benchmarks
    dotnet run -c Release --exporters json
    
- name: Compare Results
  run: |
    # 与基准版本比较，检测性能回归
    dotnet tool run BenchmarkDotNet.ResultsComparer
```

### 11.2 性能预算

设置性能预算，超出时触发警告：

```json
{
  "performanceBudgets": {
    "apiResponseTime": { "p95": 100, "unit": "ms" },
    "frameDecoding": { "throughput": 10000, "unit": "ops/s" },
    "memoryUsage": { "max": 500, "unit": "MB" }
  }
}
```

## 12. 进一步优化建议

### 12.1 短期（1-2周）

1. **日志优化**: 将所有高频日志迁移到 LoggerMessage 源生成器
2. **缓存优化**: 为配置和静态数据添加内存缓存
3. **集合优化**: 使用预分配容量的集合

### 12.2 中期（1-2个月）

1. **协议优化**: 使用 Span<T> 和 Memory<T> 优化协议解析
2. **异步优化**: 引入 ValueTask 减少分配
3. **数据库优化**: 添加索引，使用批量操作

### 12.3 长期（3-6个月）

1. **架构优化**: 考虑引入消息队列处理高吞吐场景
2. **分布式缓存**: 对于多实例部署，引入 Redis
3. **原生 AOT**: 评估 .NET Native AOT 的适用性

## 13. 参考资源

- [.NET 性能最佳实践](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [BenchmarkDotNet 文档](https://benchmarkdotnet.org/)
- [ASP.NET Core 性能最佳实践](https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)
- [高性能 .NET](https://github.com/dotnet/performance)
