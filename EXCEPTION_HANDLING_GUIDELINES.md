# 异常处理规范和指南
# Exception Handling Guidelines

本文档定义了 ZakYip.Singulation 项目的异常处理标准和最佳实践。

## 1. 异常层次结构

项目中的所有自定义异常都继承自 `SingulationException` 基类：

```
SingulationException (基类)
├── ConfigurationException (配置异常)
├── ValidationException (验证异常)
├── HardwareCommunicationException (硬件通信异常)
├── TransportException (传输层异常)
├── CodecException (协议编解码异常)
├── AxisControlException (轴控制异常)
└── SafetyException (安全系统异常)
```

### 异常属性

每个异常包含：
- `ErrorCode`: 错误代码，用于客户端识别错误类型
- `IsRetryable`: 指示是否可重试
- `Message`: 人类可读的错误消息

## 2. 异常使用指南

### ConfigurationException
**用途**: 配置相关问题

```csharp
if (string.IsNullOrEmpty(config.Vendor))
{
    throw new ConfigurationException("控制器厂商配置不能为空");
}
```

### ValidationException
**用途**: 输入验证失败

```csharp
if (axisId < 0 || axisId >= maxAxes)
{
    throw new ValidationException(
        $"轴ID {axisId} 超出范围 [0, {maxAxes})",
        propertyName: nameof(axisId));
}
```

### HardwareCommunicationException
**用途**: 硬件通信失败（可重试）

```csharp
try
{
    await bus.SendCommandAsync(command);
}
catch (IOException ex)
{
    throw new HardwareCommunicationException(
        "与控制器通信失败，请检查连接", ex);
}
```

### TransportException
**用途**: 网络传输层错误（可重试）

```csharp
if (!tcpClient.Connected)
{
    throw new TransportException(
        "TCP连接已断开，正在尝试重连");
}
```

### CodecException
**用途**: 协议编解码错误

```csharp
if (crcCalculated != crcReceived)
{
    throw new CodecException(
        $"CRC校验失败: 期望={crcCalculated:X4}, 实际={crcReceived:X4}");
}
```

### AxisControlException
**用途**: 轴控制操作失败

```csharp
if (errorCode != 0)
{
    throw new AxisControlException(
        $"轴运动失败，错误码: {errorCode}",
        axisId: axisId);
}
```

### SafetyException
**用途**: 安全系统相关错误

```csharp
if (emergencyStopTriggered)
{
    throw new SafetyException("紧急停止被触发，所有运动已停止");
}
```

## 3. 异常处理模式

### 3.1 控制器层（不捕获）

控制器层应该让异常向上传播，由全局异常处理器处理：

```csharp
// ✅ 推荐：让异常传播
[HttpPost("axes/{axisId}/move")]
public async Task<IActionResult> MoveAxis(int axisId, MoveRequest request)
{
    await _axisController.MoveAsync(axisId, request.Position);
    return Ok(ApiResponse<object>.Success(new { }, "运动命令已发送"));
}

// ❌ 不推荐：在控制器中捕获和处理
[HttpPost("axes/{axisId}/move")]
public async Task<IActionResult> MoveAxis(int axisId, MoveRequest request)
{
    try
    {
        await _axisController.MoveAsync(axisId, request.Position);
        return Ok(ApiResponse<object>.Success(new { }, "运动命令已发送"));
    }
    catch (Exception ex) // 不要在控制器中做这个
    {
        _logger.LogError(ex, "Error");
        return StatusCode(500, ApiResponse<object>.Fail("ERROR", ex.Message));
    }
}
```

### 3.2 服务层（转换异常）

服务层应该捕获底层异常并转换为业务异常：

```csharp
public async Task InitializeAsync(string vendor, DriverOptions options)
{
    try
    {
        await _bus.InitAsync(vendor, options);
    }
    catch (DllNotFoundException ex)
    {
        throw new ConfigurationException(
            $"未找到 {vendor} 驱动库，请检查安装", ex);
    }
    catch (Exception ex)
    {
        throw new HardwareCommunicationException(
            "控制器初始化失败", ex);
    }
}
```

### 3.3 基础设施层（特定处理）

基础设施层可以捕获特定异常进行重试或恢复：

```csharp
public async Task<bool> TryReconnectAsync(int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await ConnectAsync();
            return true;
        }
        catch (TransportException ex) when (ex.IsRetryable)
        {
            _logger.LogWarning(ex, "连接失败，重试 {Attempt}/{MaxRetries}", 
                i + 1, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // 指数退避
        }
    }
    return false;
}
```

### 3.4 全局异常处理器

全局异常处理器负责：
- 将异常转换为统一的 API 响应格式
- 记录异常日志
- 设置合适的 HTTP 状态码

```csharp
// 在 GlobalExceptionHandlerMiddleware.cs 中已实现
// 不同异常类型映射到不同的 HTTP 状态码：
// - ValidationException -> 400 Bad Request
// - ConfigurationException -> 500 Internal Server Error
// - HardwareCommunicationException -> 503 Service Unavailable
// - TransportException -> 503 Service Unavailable
// - CodecException -> 400 Bad Request
// - AxisControlException -> 500 Internal Server Error
// - SafetyException -> 500 Internal Server Error
```

## 4. 异常处理最佳实践

### 4.1 不要吞掉异常

```csharp
// ❌ 错误：吞掉异常
try
{
    await DangerousOperation();
}
catch
{
    // 什么都不做 - 错误被隐藏！
}

// ✅ 正确：至少记录日志
try
{
    await DangerousOperation();
}
catch (Exception ex)
{
    _logger.LogError(ex, "操作失败");
    throw; // 重新抛出
}
```

### 4.2 使用 when 子句过滤异常

```csharp
// ✅ 推荐：只捕获特定条件的异常
try
{
    await operation();
}
catch (IOException ex) when (ex.Message.Contains("timeout"))
{
    // 只处理超时相关的 IOException
    await RetryWithBackoff();
}
```

### 4.3 不要捕获太宽泛的异常

```csharp
// ❌ 错误：捕获所有异常
try
{
    await operation();
}
catch (Exception ex) // 太宽泛
{
    // 可能掩盖意外错误
}

// ✅ 正确：捕获特定异常
try
{
    await operation();
}
catch (HardwareCommunicationException ex)
{
    // 处理已知的硬件通信问题
}
catch (TransportException ex)
{
    // 处理已知的传输问题
}
```

### 4.4 保留异常堆栈

```csharp
// ❌ 错误：丢失原始堆栈跟踪
try
{
    await operation();
}
catch (Exception ex)
{
    throw ex; // 重置堆栈跟踪！
}

// ✅ 正确：保留堆栈跟踪
try
{
    await operation();
}
catch (Exception ex)
{
    throw; // 或者包装：throw new WrapperException("...", ex);
}
```

### 4.5 提供有意义的错误消息

```csharp
// ❌ 错误：消息不明确
throw new ValidationException("Invalid input");

// ✅ 正确：清晰的消息
throw new ValidationException(
    $"轴速度 {speed} mm/s 超出允许范围 [{minSpeed}, {maxSpeed}] mm/s",
    propertyName: nameof(speed));
```

## 5. 异步异常处理

### 5.1 使用 try-catch 包围 await

```csharp
// ✅ 正确
try
{
    await SomeAsyncOperation();
}
catch (Exception ex)
{
    // 异常会被正确捕获
}
```

### 5.2 不要在 Task 后面捕获异常

```csharp
// ❌ 错误：异常不会被捕获
try
{
    Task.Run(() => throw new Exception()); // 火并忘
}
catch (Exception ex)
{
    // 永远不会执行
}

// ✅ 正确：等待任务
try
{
    await Task.Run(() => throw new Exception());
}
catch (Exception ex)
{
    // 正确捕获
}
```

## 6. 性能考虑

### 6.1 异常不应用于控制流

```csharp
// ❌ 错误：使用异常控制流程
public bool TryParse(string input)
{
    try
    {
        int.Parse(input);
        return true;
    }
    catch
    {
        return false; // 性能差
    }
}

// ✅ 正确：使用 TryXxx 模式
public bool TryParse(string input)
{
    return int.TryParse(input, out _);
}
```

### 6.2 避免频繁抛出异常

```csharp
// ❌ 错误：高频路径中抛出异常
foreach (var item in hugeList)
{
    if (item == null)
        throw new ValidationException("Item cannot be null");
}

// ✅ 正确：提前验证
if (hugeList.Any(x => x == null))
    throw new ValidationException("列表包含空项");
```

## 7. 单元测试异常

### 7.1 测试异常抛出

```csharp
[Fact]
public async Task MoveAsync_WithInvalidAxisId_ThrowsValidationException()
{
    // Arrange
    var invalidAxisId = -1;

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => _controller.MoveAsync(invalidAxisId, 100));
    
    Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
    Assert.Contains("轴ID", exception.Message);
}
```

### 7.2 测试异常消息

```csharp
[Fact]
public void Constructor_WithNullConfig_ThrowsConfigurationException()
{
    var exception = Assert.Throws<ConfigurationException>(
        () => new Service(null));
    
    Assert.Equal("CONFIG_ERROR", exception.ErrorCode);
    Assert.NotNull(exception.Message);
}
```

## 8. 检查清单

在编写异常处理代码时，确认：

- [ ] 使用了正确的自定义异常类型
- [ ] 提供了清晰、有帮助的错误消息
- [ ] 包含了足够的上下文信息（如 AxisId、PropertyName）
- [ ] 保留了内部异常（使用 innerException 参数）
- [ ] 不在正常控制流中使用异常
- [ ] 不捕获过于宽泛的异常
- [ ] 控制器层不捕获异常（让全局处理器处理）
- [ ] 服务层将底层异常转换为业务异常
- [ ] 异步代码正确使用 try-catch-await

## 9. 监控和告警

### 9.1 异常监控

生产环境应监控：
- 异常频率和趋势
- 特定异常类型的发生率
- 异常与业务指标的关联

### 9.2 告警规则示例

- 任何 `SafetyException` 立即告警
- `HardwareCommunicationException` 连续 5 次触发告警
- 1 分钟内超过 100 个 `ValidationException` 触发告警

## 10. 参考资源

- [.NET 异常处理最佳实践](https://docs.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [ASP.NET Core 错误处理](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [设计异常](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/exceptions)
