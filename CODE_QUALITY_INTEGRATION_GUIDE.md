# 代码质量改进集成指南
# Code Quality Improvements Integration Guide

本文档说明如何集成新的代码质量改进功能到现有系统中。

## 1. 集成全局异常处理中间件

### 1.1 替换现有异常处理器

在 `ZakYip.Singulation.Host/Program.cs` 中，找到当前的异常处理配置（约第 286 行）：

**当前代码**:
```csharp
// ---------- 全局异常处理 ----------
app.UseExceptionHandler(errorApp => {
    errorApp.Run(async httpContext => {
        httpContext.Response.StatusCode = 500;
        httpContext.Response.ContentType = "application/json";
        var ex = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        NLog.LogManager.GetCurrentClassLogger().Error($"系统异常 {ex}");
        await httpContext.Response.WriteAsJsonAsync(new {
            Result = false,
            Msg = "系统异常"
        });
    });
});
```

**替换为**:
```csharp
// ---------- 全局异常处理 ----------
// 使用新的全局异常处理中间件，提供统一的异常处理策略
app.UseGlobalExceptionHandler();
```

### 1.2 添加必要的 using 语句

在 `Program.cs` 文件顶部添加：

```csharp
using ZakYip.Singulation.Host.Middleware;
```

### 1.3 优势

新的全局异常处理中间件提供：
- 统一的异常响应格式
- 自动异常分类（ValidationException → 400, TransportException → 503 等）
- 结构化的错误信息（包含 errorCode 和详细信息）
- 智能日志记录（根据异常类型选择日志级别）
- 可重试错误标识

## 2. 使用新的自定义异常

### 2.1 在服务层使用

**示例 1: 验证异常**

在 `ZakYip.Singulation.Infrastructure` 或业务逻辑中：

```csharp
using ZakYip.Singulation.Core.Exceptions;

public async Task MoveAxisAsync(int axisId, double position, CancellationToken ct)
{
    // 验证输入
    if (axisId < 0 || axisId >= _maxAxes)
    {
        throw new ValidationException(
            $"轴ID {axisId} 超出有效范围 [0, {_maxAxes})",
            propertyName: nameof(axisId));
    }

    if (position < _minPosition || position > _maxPosition)
    {
        throw new ValidationException(
            $"位置 {position} 超出允许范围 [{_minPosition}, {_maxPosition}]",
            propertyName: nameof(position));
    }

    // 执行操作...
}
```

**示例 2: 硬件通信异常**

```csharp
public async Task InitializeAsync(string vendor, DriverOptions options, CancellationToken ct)
{
    try
    {
        await _bus.InitAsync(vendor, options);
    }
    catch (DllNotFoundException ex)
    {
        throw new ConfigurationException(
            $"未找到 {vendor} 驱动库，请确认已正确安装驱动程序", ex);
    }
    catch (IOException ex)
    {
        throw new HardwareCommunicationException(
            "与控制器通信失败，请检查硬件连接", ex);
    }
}
```

**示例 3: 传输层异常**

```csharp
public async Task SendFrameAsync(byte[] frame, CancellationToken ct)
{
    if (!_tcpClient.Connected)
    {
        throw new TransportException(
            "TCP连接已断开，请检查网络连接");
    }

    try
    {
        await _stream.WriteAsync(frame, 0, frame.Length, ct);
    }
    catch (SocketException ex)
    {
        throw new TransportException(
            "发送数据失败，网络可能不稳定", ex);
    }
}
```

### 2.2 在控制器层使用

**重要**: 控制器层**不应该**捕获异常，让全局异常处理器处理：

```csharp
// ✅ 推荐：直接抛出，让全局处理器处理
[HttpPost("axes/{axisId}/move")]
public async Task<IActionResult> MoveAxis(int axisId, [FromBody] MoveRequest request)
{
    // 直接调用，异常会被全局处理器捕获
    await _axisController.MoveAsync(axisId, request.Position);
    return Ok(ApiResponse<object>.Success(new { }, "运动命令已发送"));
}

// ❌ 不推荐：在控制器中捕获
[HttpPost("axes/{axisId}/move")]
public async Task<IActionResult> MoveAxis(int axisId, [FromBody] MoveRequest request)
{
    try
    {
        await _axisController.MoveAsync(axisId, request.Position);
        return Ok(ApiResponse<object>.Success(new { }, "运动命令已发送"));
    }
    catch (Exception ex) // 不要这样做
    {
        return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
    }
}
```

## 3. 使用高性能日志记录

### 3.1 在现有代码中添加高性能日志

**示例：在传输层添加日志**

在 `ZakYip.Singulation.Transport` 或相关代码中：

```csharp
using ZakYip.Singulation.Infrastructure.Logging;

public class TcpTransport
{
    private readonly ILogger<TcpTransport> _logger;

    public async Task StartAsync(int port)
    {
        // 使用高性能日志方法（零分配）
        _logger.TransportStarted("TCP", port);
        
        // ... 启动逻辑
    }

    public async Task StopAsync()
    {
        _logger.TransportStopped("TCP");
        
        // ... 停止逻辑
    }

    private void OnConnectionFailed(string reason)
    {
        _logger.TransportConnectionFailed("TCP", reason);
    }

    private void OnError(Exception ex)
    {
        _logger.TransportError(ex, "TCP");
    }
}
```

**示例：在轴控制器添加日志**

```csharp
public async Task MoveAsync(int axisId, double target)
{
    var sw = Stopwatch.StartNew();
    
    try
    {
        await _drive.MoveToPositionAsync(axisId, target);
        sw.Stop();
        
        // 记录成功的运动操作
        _logger.AxisMotionCompleted(
            axisId, 
            "Absolute", 
            target, 
            sw.ElapsedMilliseconds);
    }
    catch (Exception)
    {
        var errorCode = await _drive.GetErrorCodeAsync(axisId);
        
        // 记录失败的运动操作
        _logger.AxisMotionFailed(axisId, "Absolute", errorCode);
        throw;
    }
}
```

### 3.2 添加新的日志消息

如果需要添加新的日志消息，在 `ZakYip.Singulation.Infrastructure/Logging/LogMessages.cs` 中添加：

```csharp
[LoggerMessage(
    EventId = 2005,  // 选择合适的 EventId
    Level = LogLevel.Information,
    Message = "自定义操作完成: {OperationName}, 耗时={Duration}ms")]
public static partial void CustomOperationCompleted(
    this ILogger logger,
    string operationName,
    long duration);
```

## 4. 运行 SonarQube 代码分析

### 4.1 安装 SonarQube Scanner

```bash
dotnet tool install --global dotnet-sonarscanner
```

### 4.2 配置 SonarQube 服务器

设置环境变量：

```bash
# Linux/Mac
export SONAR_HOST_URL="http://localhost:9000"
export SONAR_TOKEN="your-sonar-token"

# Windows PowerShell
$env:SONAR_HOST_URL="http://localhost:9000"
$env:SONAR_TOKEN="your-sonar-token"
```

### 4.3 运行分析

使用提供的脚本：

```bash
chmod +x sonar-scan.sh
./sonar-scan.sh
```

或手动运行：

```bash
# 1. 开始扫描
dotnet sonarscanner begin \
    /k:"ZakYip.Singulation" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.login="$SONAR_TOKEN"

# 2. 构建项目
dotnet build --no-incremental

# 3. 运行测试并生成覆盖率
dotnet test \
    --no-build \
    --collect:"XPlat Code Coverage" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# 4. 结束扫描并上传结果
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
```

## 5. 应用 .editorconfig 规则

### 5.1 在 Visual Studio 中

.editorconfig 文件会自动被识别，无需额外配置。

### 5.2 在 VS Code 中

安装 EditorConfig 扩展：
```
ext install EditorConfig.EditorConfig
```

### 5.3 在命令行中

使用 `dotnet format` 工具：

```bash
# 安装工具
dotnet tool install -g dotnet-format

# 格式化代码
dotnet format ZakYip.Singulation.sln

# 仅检查不修改
dotnet format ZakYip.Singulation.sln --verify-no-changes
```

## 6. 性能优化实施

### 6.1 识别高频日志

使用性能分析工具找到高频日志调用：

```bash
dotnet trace collect --process-id <pid> --providers Microsoft-Extensions-Logging
```

### 6.2 迁移到 LoggerMessage

将高频日志迁移到 `LogMessages.cs` 中的源生成方法。

**迁移前**:
```csharp
_logger.LogDebug($"帧解码: 类型={frameType}, 长度={length}");
```

**迁移后**:
```csharp
_logger.FrameDecoded(frameType, length);
```

### 6.3 使用 ArrayPool

对于临时缓冲区：

```csharp
// 替换
byte[] buffer = new byte[1024];
// ... 使用 buffer

// 为
byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
try
{
    // ... 使用 buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

## 7. 测试集成

### 7.1 测试异常处理

创建测试验证异常处理：

```csharp
[Fact]
public async Task MoveAxis_WithInvalidAxisId_Returns400()
{
    // Arrange
    var client = _factory.CreateClient();
    var invalidAxisId = -1;

    // Act
    var response = await client.PostAsync(
        $"/api/axes/{invalidAxisId}/move",
        JsonContent.Create(new { position = 100 }));

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    
    var content = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content);
    
    Assert.False(apiResponse.Result);
    Assert.Contains("VALIDATION_ERROR", content);
}
```

### 7.2 测试日志记录

验证日志正确记录：

```csharp
[Fact]
public void FrameDecoded_LogsCorrectly()
{
    // Arrange
    var loggerFactory = LoggerFactory.Create(builder => 
        builder.AddDebug());
    var logger = loggerFactory.CreateLogger<TestClass>();

    // Act
    logger.FrameDecoded("Speed", 128);

    // Assert
    // 验证日志输出（使用测试日志提供程序）
}
```

## 8. 持续集成

### 8.1 添加到 CI/CD 流程

在 GitHub Actions 或其他 CI 中：

```yaml
- name: Code Quality Analysis
  run: |
    dotnet format --verify-no-changes
    ./sonar-scan.sh
  env:
    SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

### 8.2 质量门控

配置 SonarQube Quality Gate，确保：
- 代码覆盖率 > 70%
- 无严重或阻塞问题
- 技术债务 < 5%

## 9. 渐进式迁移策略

### 9.1 阶段 1（立即）
- ✅ 集成全局异常处理中间件
- ✅ 应用 .editorconfig 规则
- ✅ 设置 SonarQube 分析

### 9.2 阶段 2（1-2周）
- 🔄 新代码使用新的异常类型
- 🔄 新代码使用 LoggerMessage 源生成
- 🔄 修复 SonarQube 识别的高优先级问题

### 9.3 阶段 3（1-2月）
- 📋 逐步迁移现有代码到新异常处理模式
- 📋 迁移高频日志到 LoggerMessage
- 📋 应用性能优化建议

## 10. 监控和度量

### 10.1 关键指标

追踪以下指标的改进：

| 指标 | 当前 | 目标 |
|-----|------|------|
| SonarQube 技术债务 | TBD | < 5% |
| 代码覆盖率 | TBD | > 70% |
| API 响应时间 (P95) | TBD | < 100ms |
| 日志分配开销 | TBD | 减少 50% |

### 10.2 定期审查

- 每周审查 SonarQube 报告
- 每月评估性能指标
- 每季度更新优化策略

## 11. 常见问题

### Q1: 是否需要立即迁移所有异常处理？

**A**: 不需要。使用渐进式策略：
- 新代码使用新的异常类型
- 修改现有代码时顺便更新
- 高频路径优先迁移

### Q2: LoggerMessage 源生成器性能提升多少？

**A**: 根据日志频率，通常有 2-10 倍的性能提升，主要体现在：
- 零内存分配
- 避免装箱
- 消除字符串插值开销

### Q3: 全局异常处理器会影响现有错误处理吗？

**A**: 不会。现有的 try-catch 仍然有效。全局处理器只处理未捕获的异常。

### Q4: 如何处理不在 LogMessages.cs 中的日志？

**A**: 仍然可以使用传统的 ILogger 方法（LogInformation、LogError 等）。建议高频日志使用 LoggerMessage 源生成器。

## 12. 参考文档

- [异常处理指南](./EXCEPTION_HANDLING_GUIDELINES.md)
- [日志记录规范](./LOGGING_GUIDELINES.md)
- [性能优化指南](./PERFORMANCE_OPTIMIZATION_GUIDE.md)
- [SonarQube 配置](./sonar-project.properties)

## 13. 支持

如有问题或建议，请：
1. 查看相关文档
2. 在团队中讨论
3. 创建 Issue 或 PR

---

**最后更新**: 2025-10-28
**维护者**: 开发团队
