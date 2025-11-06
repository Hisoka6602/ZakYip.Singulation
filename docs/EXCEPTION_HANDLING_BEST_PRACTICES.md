# 异常处理最佳实践 / Exception Handling Best Practices

## 概述 / Overview

本文档定义了 ZakYip.Singulation 项目中异常处理的最佳实践和统一标准。
This document defines the best practices and unified standards for exception handling in the ZakYip.Singulation project.

## 异常分类 / Exception Classification

### 1. 可重试异常 / Retryable Exceptions

这些异常通常由临时性问题引起，可以通过重试来解决：
These exceptions are usually caused by transient issues and can be resolved by retrying:

- **HardwareCommunicationException**: 硬件通信失败 / Hardware communication failures
- **TransportException**: 传输层错误 / Transport layer errors
- 网络超时 / Network timeouts
- 临时资源不可用 / Temporary resource unavailability

**重试策略 / Retry Strategy:**
- 最大重试次数: 3次 / Maximum retries: 3
- 重试间隔: 指数退避 (100ms, 200ms, 400ms) / Retry interval: Exponential backoff (100ms, 200ms, 400ms)
- 适用场景: IsRetryable = true / Applicable when: IsRetryable = true

### 2. 不可重试异常 / Non-Retryable Exceptions

这些异常由系统错误或业务逻辑错误引起，重试无效：
These exceptions are caused by system errors or business logic errors, retrying is ineffective:

- **ValidationException**: 数据验证失败 / Data validation failures
- **ConfigurationException**: 配置错误 / Configuration errors
- **CodecException**: 协议编解码错误 / Protocol codec errors
- **SafetyException**: 安全系统错误 / Safety system errors
- **AxisControlException**: 轴控制错误 / Axis control errors

**处理策略 / Handling Strategy:**
- 记录详细错误信息 / Log detailed error information
- 返回明确的错误响应 / Return clear error response
- 不进行重试 / Do not retry

### 3. 关键异常 / Critical Exceptions

这些异常表示系统级别的严重问题，应立即抛出：
These exceptions indicate system-level critical issues and should be thrown immediately:

- **OutOfMemoryException**: 内存溢出 / Out of memory
- **StackOverflowException**: 栈溢出 / Stack overflow
- **ThreadAbortException**: 线程中止 / Thread abort

**处理策略 / Handling Strategy:**
- 不捕获，直接抛出 / Do not catch, throw immediately
- 记录到关键日志级别 / Log at Critical level
- 触发应急响应 / Trigger emergency response

## 异常处理模式 / Exception Handling Patterns

### 1. 控制器层 / Controller Layer

```csharp
[HttpGet]
public async Task<IActionResult> GetData()
{
    try
    {
        var result = await _service.GetDataAsync();
        return Ok(ApiResponse<DataDto>.Success(result));
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning(ex, "数据验证失败: {PropertyName}", ex.PropertyName);
        return BadRequest(ApiResponse<object>.Fail(ex.Message));
    }
    catch (SingulationException ex) when (ex.IsRetryable)
    {
        _logger.LogWarning(ex, "可重试异常: {ErrorCode}", ex.ErrorCode);
        return StatusCode(503, ApiResponse<object>.Fail(ex.Message));
    }
    catch (SingulationException ex)
    {
        _logger.LogError(ex, "业务异常: {ErrorCode}", ex.ErrorCode);
        return StatusCode(500, ApiResponse<object>.Fail(ex.Message));
    }
}
```

### 2. 服务层 / Service Layer

```csharp
public async Task<Result> ProcessDataAsync(DataDto data)
{
    // 验证输入 / Validate input
    if (data == null)
    {
        throw new ValidationException("数据不能为空", nameof(data));
    }

    try
    {
        // 执行业务逻辑 / Execute business logic
        var result = await _repository.SaveAsync(data);
        return Result.Success(result);
    }
    catch (DbUpdateException ex)
    {
        // 转换为领域异常 / Convert to domain exception
        throw new ConfigurationException("数据保存失败", ex);
    }
}
```

### 3. 基础设施层 / Infrastructure Layer

```csharp
public async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    int maxRetries = 3)
{
    var retryCount = 0;
    var delay = TimeSpan.FromMilliseconds(100);

    while (true)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (IsRetryableException(ex) && retryCount < maxRetries)
        {
            retryCount++;
            _logger.LogWarning(
                ex,
                "操作失败，正在重试 ({RetryCount}/{MaxRetries})",
                retryCount,
                maxRetries);
            
            await Task.Delay(delay);
            delay *= 2; // 指数退避 / Exponential backoff
        }
    }
}

private bool IsRetryableException(Exception ex)
{
    return ex is SingulationException singulationEx && singulationEx.IsRetryable;
}
```

### 4. 传输层 / Transport Layer

```csharp
public async Task SendAsync(byte[] data)
{
    try
    {
        await _transport.WriteAsync(data);
        _logger.LogDebug("数据发送成功，长度: {Length} 字节", data.Length);
    }
    catch (IOException ex)
    {
        // 转换为可重试的传输异常 / Convert to retryable transport exception
        throw new TransportException("数据发送失败", ex);
    }
    catch (TimeoutException ex)
    {
        throw new TransportException("发送超时", ex);
    }
}
```

## 全局异常处理 / Global Exception Handling

全局异常处理中间件 (GlobalExceptionHandlerMiddleware) 负责：
The global exception handler middleware is responsible for:

1. **捕获未处理的异常** / Catch unhandled exceptions
2. **记录异常详情** / Log exception details
3. **返回统一的错误响应** / Return unified error response
4. **区分异常类型** / Distinguish exception types
5. **设置正确的 HTTP 状态码** / Set correct HTTP status codes

## 日志记录指南 / Logging Guidelines

### 异常日志级别映射 / Exception Log Level Mapping

| 异常类型 / Exception Type | 日志级别 / Log Level | 说明 / Description |
|---------------------------|---------------------|-------------------|
| ValidationException | Warning | 客户端输入错误 / Client input error |
| 可重试异常 (IsRetryable=true) | Warning | 临时性问题 / Transient issue |
| ConfigurationException | Error | 配置问题 / Configuration issue |
| HardwareCommunicationException | Error | 硬件通信错误 / Hardware communication error |
| SafetyException | Critical | 安全系统错误 / Safety system error |
| 关键系统异常 / Critical System Exceptions | Critical | 系统级严重问题 / System-level critical issue |

### 异常日志记录最佳实践 / Exception Logging Best Practices

```csharp
// ✅ 正确 / Correct
_logger.LogError(
    ex,
    "轴 {AxisId} 运动失败: {ErrorCode}",
    axisId,
    errorCode);

// ❌ 错误 / Wrong
_logger.LogError($"轴 {axisId} 运动失败: {ex.Message}");
```

**关键点 / Key Points:**
- 始终传递异常对象作为第一个参数 / Always pass exception object as first parameter
- 使用结构化日志参数 / Use structured logging parameters
- 包含相关上下文信息 / Include relevant context information
- 避免字符串插值 / Avoid string interpolation

## 异常设计原则 / Exception Design Principles

1. **单一职责** / Single Responsibility
   - 每个异常类型代表一种特定的错误场景 / Each exception type represents a specific error scenario

2. **信息完整** / Complete Information
   - 包含错误代码 / Include error code
   - 包含详细消息 / Include detailed message
   - 保留内部异常 / Preserve inner exception
   - 添加相关上下文数据 / Add relevant context data

3. **可操作性** / Actionability
   - 明确是否可重试 / Clearly indicate if retryable
   - 提供错误恢复建议 / Provide error recovery suggestions
   - 区分客户端错误和服务端错误 / Distinguish client vs server errors

4. **性能考虑** / Performance Considerations
   - 避免异常用于控制流 / Avoid exceptions for control flow
   - 在热路径中使用结果对象 / Use result objects in hot paths
   - 仅在真正异常情况下抛出异常 / Only throw for truly exceptional cases

## 测试建议 / Testing Recommendations

```csharp
[Fact]
public async Task Should_ThrowValidationException_When_InputIsNull()
{
    // Arrange
    var service = CreateService();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => service.ProcessDataAsync(null!));
    
    Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
    Assert.NotNull(exception.PropertyName);
}

[Fact]
public async Task Should_RetryOnTransportException()
{
    // Arrange
    var mockTransport = new Mock<ITransport>();
    mockTransport
        .SetupSequence(x => x.SendAsync(It.IsAny<byte[]>()))
        .ThrowsAsync(new TransportException("临时错误"))
        .ThrowsAsync(new TransportException("临时错误"))
        .ReturnsAsync(true);

    // Act
    var result = await ExecuteWithRetryAsync(
        () => mockTransport.Object.SendAsync(data));

    // Assert
    Assert.True(result);
    mockTransport.Verify(x => x.SendAsync(It.IsAny<byte[]>()), Times.Exactly(3));
}
```

## 总结 / Summary

遵循这些最佳实践可以确保：
Following these best practices ensures:

- ✅ 一致的异常处理模式 / Consistent exception handling patterns
- ✅ 清晰的错误分类和响应 / Clear error classification and response
- ✅ 合理的重试策略 / Reasonable retry strategies
- ✅ 完整的日志记录 / Complete logging
- ✅ 更好的可维护性和可调试性 / Better maintainability and debuggability
