# ZakYip.Singulation 开发者指南

## 1. 开发环境搭建

### 1.1 必需工具

**开发工具**：
- **Visual Studio 2022** (17.8+) 或 **Visual Studio Code**
- **.NET 8.0 SDK**
- **Git** 版本控制

**可选工具**：
- **Postman** 或 **REST Client** - API 测试
- **Redis Desktop Manager** - Redis 调试（如使用）
- **Docker Desktop** - 容器化开发

### 1.2 Visual Studio 配置

**安装工作负载**：
1. ASP.NET 和 Web 开发
2. .NET 桌面开发
3. .NET MAUI (移动开发)

**推荐扩展**：
- ReSharper 或 IntelliCode
- EditorConfig
- GitLens (VS Code)
- C# Dev Kit (VS Code)

### 1.3 克隆项目

```bash
# 克隆仓库
git clone https://github.com/Hisoka6602/ZakYip.Singulation.git
cd ZakYip.Singulation

# 恢复 NuGet 包
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test
```

### 1.4 配置开发环境

**appsettings.Development.json**：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ZakYip.Singulation": "Trace"
    }
  },
  "KestrelUrl": "http://localhost:5005",
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3
  }
}
```

## 2. 项目结构详解

### 2.1 解决方案组织

```
ZakYip.Singulation.sln
├── src/
│   ├── ZakYip.Singulation.Core          # 核心领域层
│   ├── ZakYip.Singulation.Drivers       # 驱动层
│   ├── ZakYip.Singulation.Infrastructure # 基础设施层
│   ├── ZakYip.Singulation.Protocol      # 协议层
│   ├── ZakYip.Singulation.Transport     # 传输层
│   └── ZakYip.Singulation.Host          # 主机服务
├── apps/
│   ├── ZakYip.Singulation.MauiApp       # MAUI 客户端
│   └── ZakYip.Singulation.ConsoleDemo   # 控制台演示
├── tests/
│   └── ZakYip.Singulation.Tests         # 单元测试
└── docs/                                # 文档
```

### 2.2 核心概念

**轴 (Axis)**：
- 表示单个伺服电机控制单元
- 实现 `IAxisDrive` 接口
- 包含状态、速度、使能等属性

**控制器 (Controller)**：
- 管理多个轴的聚合根
- 实现 `IAxisController` 接口
- 负责轴的生命周期管理

**安全管线 (Safety Pipeline)**：
- 执行安全命令的状态机
- 确保安全操作顺序正确
- 提供故障恢复机制

**事件聚合器 (Event Aggregator)**：
- 发布/订阅模式实现
- 解耦组件间通信
- 支持异步事件处理

## 3. 编码规范

### 3.1 命名约定

**C# 命名风格**：
```csharp
// 类名：PascalCase
public class AxisController { }

// 接口：I + PascalCase
public interface IAxisDrive { }

// 方法：PascalCase + Async 后缀（异步方法）
public async Task<int> GetAxisCountAsync() { }

// 属性：PascalCase
public string AxisId { get; set; }

// 私有字段：_camelCase
private readonly HttpClient _httpClient;

// 常量：UPPER_SNAKE_CASE
private const int MAX_RETRY_COUNT = 3;

// 局部变量：camelCase
var axisCount = 10;
```

**文件命名**：
- 一个类一个文件
- 文件名与类名一致
- 接口文件名以 I 开头

### 3.2 代码风格

**缩进和格式**：
```csharp
// 使用 4 空格缩进，不使用 Tab
// 左大括号另起一行
public class ExampleClass
{
    public void ExampleMethod()
    {
        if (condition)
        {
            // 代码块
        }
    }
}

// 链式调用每行一个方法
var result = builder
    .WithUrl(url)
    .WithTimeout(timeout)
    .Build();
```

**注释规范**：
```csharp
/// <summary>
/// 获取指定轴的状态信息
/// </summary>
/// <param name="axisId">轴 ID</param>
/// <returns>轴状态响应 DTO</returns>
/// <exception cref="ArgumentNullException">当 axisId 为空时抛出</exception>
public async Task<AxisResponseDto> GetAxisAsync(string axisId)
{
    // 单行注释用于解释复杂逻辑
    if (string.IsNullOrEmpty(axisId))
    {
        throw new ArgumentNullException(nameof(axisId));
    }
    
    // ...
}
```

### 3.3 异步编程

**推荐实践**：
```csharp
// ✅ 好的做法
public async Task<int> GetCountAsync()
{
    var result = await _repository.CountAsync().ConfigureAwait(false);
    return result;
}

// ✅ 使用 ValueTask 减少分配
public async ValueTask<bool> ExistsAsync(string id)
{
    return await _cache.ContainsKeyAsync(id).ConfigureAwait(false);
}

// ❌ 避免：同步阻塞
public int GetCount()
{
    return GetCountAsync().GetAwaiter().GetResult(); // 可能死锁
}

// ❌ 避免：async void（除事件处理器）
public async void DoWork() // 不推荐
{
    await Task.Delay(1000);
}
```

### 3.4 错误处理

```csharp
// ✅ 具体异常类型
public async Task<AxisResponseDto> GetAxisAsync(string axisId)
{
    if (string.IsNullOrEmpty(axisId))
    {
        throw new ArgumentNullException(nameof(axisId));
    }
    
    try
    {
        var axis = await _controller.GetAxisAsync(axisId);
        if (axis == null)
        {
            throw new AxisNotFoundException(axisId);
        }
        return MapToDto(axis);
    }
    catch (Exception ex) when (ex is not ArgumentNullException and not AxisNotFoundException)
    {
        _logger.LogError(ex, "Failed to get axis {AxisId}", axisId);
        throw new AxisOperationException($"Failed to get axis {axisId}", ex);
    }
}

// ✅ 使用 ApiResponse 包装 API 返回
public async Task<ActionResult<ApiResponse<AxisResponseDto>>> GetAxis(string axisId)
{
    try
    {
        var dto = await _service.GetAxisAsync(axisId);
        return Ok(new ApiResponse<AxisResponseDto>
        {
            Result = true,
            Data = dto
        });
    }
    catch (AxisNotFoundException ex)
    {
        return NotFound(new ApiResponse<AxisResponseDto>
        {
            Result = false,
            Msg = ex.Message
        });
    }
}
```

### 3.5 LINQ 和集合

```csharp
// ✅ 使用 LINQ 简化查询
var enabledAxes = axes
    .Where(a => a.Enabled == true)
    .OrderBy(a => a.AxisId)
    .ToList();

// ✅ 避免多次枚举
var activeAxes = axes.Where(a => a.Status == AxisStatus.Active).ToList();
Console.WriteLine($"Count: {activeAxes.Count}");
foreach (var axis in activeAxes) { /* ... */ }

// ❌ 避免：重复枚举
var query = axes.Where(a => a.Status == AxisStatus.Active);
Console.WriteLine($"Count: {query.Count()}"); // 枚举一次
foreach (var axis in query) { /* ... */ }      // 再次枚举
```

## 4. 添加新功能

### 4.1 添加新的 API 端点

**步骤 1：创建 DTO**：
```csharp
// ZakYip.Singulation.Core/Contracts/Dto/MyFeatureDto.cs
namespace ZakYip.Singulation.Core.Contracts.Dto;

public record MyFeatureRequestDto
{
    public string Name { get; init; } = string.Empty;
    public int Value { get; init; }
}

public record MyFeatureResponseDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
```

**步骤 2：创建控制器**：
```csharp
// ZakYip.Singulation.Host/Controllers/MyFeatureController.cs
using Microsoft.AspNetCore.Mvc;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyFeatureController : ControllerBase
{
    private readonly IMyFeatureService _service;
    private readonly ILogger<MyFeatureController> _logger;

    public MyFeatureController(IMyFeatureService service, ILogger<MyFeatureController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 创建新功能实例
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MyFeatureResponseDto>>> Create(
        [FromBody] MyFeatureRequestDto request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return Ok(new ApiResponse<MyFeatureResponseDto>
            {
                Result = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create feature");
            return StatusCode(500, new ApiResponse<MyFeatureResponseDto>
            {
                Result = false,
                Msg = "Internal server error"
            });
        }
    }

    /// <summary>
    /// 获取功能列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MyFeatureResponseDto>>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(new ApiResponse<List<MyFeatureResponseDto>>
        {
            Result = true,
            Data = items
        });
    }
}
```

**步骤 3：注册服务**：
```csharp
// ZakYip.Singulation.Host/Program.cs
services.AddScoped<IMyFeatureService, MyFeatureService>();
```

### 4.2 添加新的驱动支持

**步骤 1：实现驱动接口**：
```csharp
// ZakYip.Singulation.Drivers/MyVendor/MyVendorAxisDrive.cs
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Drivers.MyVendor;

public class MyVendorAxisDrive : IAxisDrive
{
    private readonly AxisOptions _options;
    
    public MyVendorAxisDrive(AxisOptions options)
    {
        _options = options;
    }
    
    public async Task<DriverStatus> GetStatusAsync()
    {
        // 实现状态读取
        return DriverStatus.Ready;
    }
    
    public async Task EnableAsync()
    {
        // 实现使能逻辑
    }
    
    public async Task DisableAsync()
    {
        // 实现禁用逻辑
    }
    
    // 实现其他接口方法...
}
```

**步骤 2：注册驱动**：
```csharp
// ZakYip.Singulation.Host/Program.cs
services.AddSingleton<IDriveRegistry>(sp => {
    var registry = new DefaultDriveRegistry();
    registry.Register("leadshine", (axisId, port, opts) => new LeadshineLtdmcAxisDrive(opts));
    registry.Register("myvendor", (axisId, port, opts) => new MyVendorAxisDrive(opts));
    return registry;
});
```

### 4.3 添加 SignalR 事件

**步骤 1：扩展 Hub**：
```csharp
// ZakYip.Singulation.Host/SignalR/Hubs/EventsHub.cs
public async Task BroadcastCustomEvent(string eventName, object data)
{
    await Clients.All.SendAsync("CustomEvent", eventName, data);
}
```

**步骤 2：发布事件**：
```csharp
// 在服务中注入 IHubContext
public class MyService
{
    private readonly IHubContext<EventsHub> _hubContext;
    
    public async Task DoSomethingAsync()
    {
        // 业务逻辑...
        
        // 发送事件到所有客户端
        await _hubContext.Clients.All.SendAsync("CustomEvent", "MyEvent", new
        {
            Message = "Something happened",
            Timestamp = DateTime.Now
        });
    }
}
```

**步骤 3：客户端订阅**：
```csharp
// MAUI 客户端
_hubConnection.On<string, object>("CustomEvent", (eventName, data) =>
{
    Debug.WriteLine($"Custom event: {eventName}, Data: {data}");
});
```

## 5. 测试指南

### 5.1 单元测试

**使用 xUnit + Moq**：
```csharp
using Xunit;
using Moq;
using ZakYip.Singulation.Core.Axis;

namespace ZakYip.Singulation.Tests.Core;

public class AxisControllerTests
{
    [Fact]
    public async Task GetAxisAsync_ExistingAxis_ReturnsAxis()
    {
        // Arrange
        var mockDrive = new Mock<IAxisDrive>();
        mockDrive.Setup(d => d.GetStatusAsync())
            .ReturnsAsync(DriverStatus.Ready);
        
        var controller = new AxisController(/* 依赖注入 */);
        
        // Act
        var result = await controller.GetAxisAsync("axis1");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("axis1", result.AxisId);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetAxisAsync_InvalidId_ThrowsArgumentException(string axisId)
    {
        // Arrange
        var controller = new AxisController(/* 依赖注入 */);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => controller.GetAxisAsync(axisId));
    }
}
```

**运行测试**：
```bash
# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~AxisControllerTests"

# 生成覆盖率报告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 5.2 集成测试

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ZakYip.Singulation.Tests.Integration;

public class AxesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public AxesApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetAxes_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/axes/axes");
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }
}
```

### 5.3 性能测试

**使用 BenchmarkDotNet**：
```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
public class AxisControllerBenchmarks
{
    private AxisController _controller;
    
    [GlobalSetup]
    public void Setup()
    {
        _controller = new AxisController(/* 初始化 */);
    }
    
    [Benchmark]
    public async Task GetAxisAsync()
    {
        await _controller.GetAxisAsync("axis1");
    }
    
    [Benchmark]
    public async Task GetAllAxesAsync()
    {
        await _controller.GetAllAxesAsync();
    }
}

// 运行基准测试
BenchmarkRunner.Run<AxisControllerBenchmarks>();
```

## 6. 调试技巧

### 6.1 Visual Studio 调试

**断点条件**：
```csharp
// 右键断点 -> 条件
// 条件：axisId == "axis1"
// 命中次数：命中次数等于 5
```

**数据断点**：
```csharp
// 监视特定变量的值变化
// 右键变量 -> 设置数据断点
```

**追踪点**：
```csharp
// 不中断执行，只输出日志
// 右键断点 -> 操作 -> 记录消息到输出窗口
// 消息：Axis {axisId} speed changed to {speed}
```

### 6.2 日志调试

**结构化日志**：
```csharp
_logger.LogInformation("Axis {AxisId} enabled by {User} at {Time}", 
    axisId, userName, DateTime.Now);

_logger.LogError(ex, "Failed to enable axis {AxisId}", axisId);
```

**日志过滤**：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "ZakYip.Singulation.Core.Axis": "Trace"
    }
  }
}
```

### 6.3 SignalR 调试

**客户端日志**：
```csharp
_hubConnection = new HubConnectionBuilder()
    .WithUrl($"{_baseUrl}/hubs/events")
    .ConfigureLogging(logging => 
    {
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .Build();
```

**监控消息**：
```csharp
_hubConnection.On<string, object>("ReceiveEvent", (eventName, data) =>
{
    Debug.WriteLine($"[SignalR] {eventName}: {JsonSerializer.Serialize(data)}");
});
```

## 7. Git 工作流

### 7.1 分支策略

```
main (生产)
  ↓
develop (开发)
  ↓
feature/xxx (功能分支)
hotfix/xxx (紧急修复)
```

### 7.2 提交规范

**Commit Message 格式**：
```
<type>(<scope>): <subject>

<body>

<footer>
```

**类型 (type)**：
- `feat`: 新功能
- `fix`: 修复 Bug
- `docs`: 文档更新
- `style`: 代码格式调整
- `refactor`: 重构
- `perf`: 性能优化
- `test`: 测试相关
- `chore`: 构建/工具链更新

**示例**：
```
feat(axis): add batch enable/disable support

- Added EnableAxesAsync method
- Added DisableAxesAsync method
- Updated API controller endpoints

Closes #123
```

### 7.3 Pull Request 流程

1. 创建功能分支：`git checkout -b feature/my-feature`
2. 开发和提交：`git commit -m "feat: add new feature"`
3. 推送分支：`git push origin feature/my-feature`
4. 创建 PR，填写描述和关联 Issue
5. 代码评审，修改反馈
6. 合并到 develop 分支

## 8. 性能优化建议

### 8.1 异步最佳实践

```csharp
// ✅ 并行执行独立任务
var task1 = GetAxisAsync("axis1");
var task2 = GetAxisAsync("axis2");
await Task.WhenAll(task1, task2);

// ✅ 使用 ConfigureAwait(false)
await _repository.SaveAsync().ConfigureAwait(false);

// ✅ 避免不必要的 Task.Run
public async Task DoWorkAsync()
{
    // ❌ 不要这样
    await Task.Run(async () => await _service.ProcessAsync());
    
    // ✅ 直接调用
    await _service.ProcessAsync();
}
```

### 8.2 内存优化

```csharp
// ✅ 使用 ArrayPool
var buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    // 使用 buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// ✅ 使用 Span<T> 避免分配
public void ProcessData(ReadOnlySpan<byte> data)
{
    // 处理数据，无堆分配
}

// ✅ 使用 struct 代替 class (值类型)
public readonly struct AxisState
{
    public int AxisId { get; }
    public double Speed { get; }
}
```

### 8.3 缓存策略

```csharp
// ✅ 使用 MemoryCache
private readonly IMemoryCache _cache;

public async Task<AxisInfo> GetAxisCachedAsync(string axisId)
{
    return await _cache.GetOrCreateAsync($"axis:{axisId}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await _repository.GetAxisAsync(axisId);
    });
}
```

## 9. 常见问题

### Q1: 如何添加新的控制器厂商支持？

实现 `IAxisDrive` 接口，并在 `DriveRegistry` 中注册。参考 [4.2 添加新的驱动支持](#42-添加新的驱动支持)。

### Q2: 如何调试 SignalR 连接问题？

启用详细日志，检查网络连接，验证端点路径。参考 [6.3 SignalR 调试](#63-signalr-调试)。

### Q3: 如何提高 API 响应速度？

使用异步编程、添加缓存、启用响应压缩、优化数据库查询。

### Q4: 单元测试如何 Mock 依赖？

使用 Moq 库创建 Mock 对象。参考 [5.1 单元测试](#51-单元测试)。

## 10. 参考资源

- [C# 编码规范](https://docs.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ASP.NET Core 最佳实践](https://docs.microsoft.com/aspnet/core/fundamentals/best-practices)
- [异步编程模式](https://docs.microsoft.com/dotnet/standard/asynchronous-programming-patterns/)
- [单元测试最佳实践](https://docs.microsoft.com/dotnet/core/testing/unit-testing-best-practices)

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 开发团队
