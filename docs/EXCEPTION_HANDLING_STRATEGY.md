# 异常处理策略 (Exception Handling Strategy)

## 核心原则 (Core Principle)

**任何异常都不能使程序崩溃 (No exception should cause the program to crash)**

这是一个关键的生产环境要求。系统必须能够在遇到异常时保持运行，只记录错误而不中断服务。

## 异常处理层次 (Exception Handling Hierarchy)

### 1. 顶层处理 (Top-Level Handlers)

**位置**: `Program.cs`, `Main` methods
- **必须** 捕获所有异常 (`catch (Exception)`)
- 记录致命错误
- 优雅关闭
- 防止进程崩溃

```csharp
try {
    host.Run();
}
#pragma warning disable CA1031 // 顶层异常处理器 - 必须捕获所有异常防止进程崩溃
catch (Exception e) {
    logger.Error(e, "运行异常");
}
#pragma warning restore CA1031
```

### 2. 中间件层 (Middleware Layer)

**位置**: `GlobalExceptionHandlerMiddleware`
- 捕获所有HTTP请求中的异常
- 排除关键异常 (`OutOfMemoryException`, `StackOverflowException`)
- 转换为适当的HTTP响应
- 记录异常到聚合服务

### 3. 业务逻辑层 (Business Logic Layer)

**推荐**: 使用领域特定异常
- `AxisControlException` - 轴控制错误
- `HardwareCommunicationException` - 硬件通信错误
- `TransportException` - 传输层错误
- `ConfigurationException` - 配置错误

**容错处理**: 在无法恢复的情况下，记录并返回错误状态，不抛出异常

### 4. 基础设施层 (Infrastructure Layer)

#### 事件处理器 (Event Handlers)
```csharp
// 事件触发必须捕获所有异常，防止破坏事件订阅者
try {
    handler?.Invoke(args);
} catch (Exception ex) {
    _logger.LogError(ex, "事件处理器异常");
}
```

#### 后台工作器 (Background Workers)
```csharp
// 后台服务必须持续运行
while (!cancellationToken.IsCancellationRequested) {
    try {
        await DoWorkAsync(cancellationToken);
    } catch (Exception ex) {
        _logger.LogError(ex, "后台工作器异常");
        await Task.Delay(1000, cancellationToken); // 短暂延迟后重试
    }
}
```

#### 清理/释放代码 (Cleanup/Disposal)
```csharp
// 清理代码必须不抛出异常，避免掩盖原始异常
finally {
    try { await resource.DisposeAsync(); } 
    catch { /* 静默失败，避免掩盖原始异常 */ }
}
```

### 5. 重试策略 (Retry Policies)

使用 Polly 时，必须捕获 `Exception` 以决定重试行为：

```csharp
var retry = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        // ...
    })
    .Build();
```

## CA1031 抑制指南 (CA1031 Suppression Guidelines)

### 何时使用 pragma 抑制

1. **顶层入口点**: `#pragma warning disable CA1031`
2. **全局异常处理器**: 已知需要捕获所有异常
3. **重试循环**: Polly 或自定义重试逻辑
4. **事件触发**: 防止破坏订阅者

### 何时使用 GlobalSuppressions.cs

1. **整个命名空间**: Controllers, ViewModels
2. **测试辅助代码**: TestHelpers
3. **UI 代码**: MAUI Services

### 何时使用 .editorconfig

1. **目录级别抑制**: `Controllers/**.cs`
2. **项目范围配置**

## 日志记录要求 (Logging Requirements)

所有 `catch (Exception)` 块必须：
1. 使用适当的日志级别 (Error/Warning)
2. 包含异常对象
3. 包含上下文信息
4. 使用结构化日志

```csharp
catch (Exception ex) {
    _logger.LogError(ex, "操作失败: {Operation}, 参数: {Params}", 
        operationName, parameters);
}
```

## 不应捕获的异常 (Exceptions That Should Not Be Caught)

这些异常表示严重的运行时问题，应该让进程崩溃：
- `OutOfMemoryException`
- `StackOverflowException`
- `ThreadAbortException`
- `AccessViolationException`

在全局异常处理器中应重新抛出这些异常。

## 测试策略 (Testing Strategy)

1. 单元测试应验证异常不会导致崩溃
2. 集成测试应验证错误被正确记录
3. 混沌测试应验证系统在异常情况下的韧性

## 代码审查检查点 (Code Review Checklist)

- [ ] 异常是否被正确记录？
- [ ] 是否有适当的抑制注释？
- [ ] 用户是否得到有意义的错误消息？
- [ ] 资源是否被正确清理？
- [ ] 是否使用了领域特定异常（在适用的情况下）？
