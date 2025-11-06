# 日志记录最佳实践 / Logging Best Practices

## 概述 / Overview

本文档定义了 ZakYip.Singulation 项目中日志记录的标准、最佳实践和性能优化策略。
This document defines the standards, best practices, and performance optimization strategies for logging in the ZakYip.Singulation project.

## 日志级别标准 / Log Level Standards

### 日志级别定义和使用场景 / Log Level Definitions and Use Cases

#### 1. Debug (调试)

**使用场景 / Use Cases:**
- 详细的流程跟踪 / Detailed flow tracking
- 变量值记录 / Variable value recording
- 算法步骤跟踪 / Algorithm step tracking
- 开发和调试期间的诊断信息 / Diagnostic information during development

**示例 / Examples:**
```csharp
_logger.LogDebug("帧解码成功: 类型={FrameType}, 长度={Length}字节", frameType, length);
_logger.LogDebug("数据库操作: {Operation}, 表={Table}, 耗时={ElapsedMs}ms", operation, table, elapsedMs);
_logger.LogDebug("缓存命中: Key={CacheKey}, TTL={TTL}秒", cacheKey, ttl);
```

**性能影响 / Performance Impact:**
- 生产环境默认关闭 / Disabled by default in production
- 开发环境可启用 / Can be enabled in development
- 不应在热路径中使用昂贵的操作 / Should not use expensive operations in hot paths

#### 2. Information (信息)

**使用场景 / Use Cases:**
- 应用程序启动和停止 / Application startup and shutdown
- 配置加载 / Configuration loading
- 重要业务流程的完成 / Completion of important business processes
- 状态变化 / State changes
- 用户操作审计 / User operation auditing

**示例 / Examples:**
```csharp
_logger.LogInformation("传输层已启动: {TransportType}, 端口: {Port}", transportType, port);
_logger.LogInformation("轴控制器已初始化: 厂商={Vendor}, 轴数={AxisCount}", vendor, axisCount);
_logger.LogInformation("配置已加载: {ConfigType}", configType);
_logger.LogInformation("轴 {AxisId} 运动完成: 类型={MotionType}, 耗时={ElapsedMs}ms", axisId, motionType, elapsedMs);
```

**性能影响 / Performance Impact:**
- 生产环境默认级别 / Default level in production
- 应控制频率，避免高频日志 / Should control frequency, avoid high-frequency logs
- 适用于关键路径的里程碑记录 / Suitable for milestone recording in critical paths

#### 3. Warning (警告)

**使用场景 / Use Cases:**
- 非致命错误 / Non-fatal errors
- 可恢复的异常情况 / Recoverable exceptional conditions
- 性能降级 / Performance degradation
- 资源使用接近阈值 / Resource usage approaching threshold
- 重试操作 / Retry operations
- 客户端错误（如验证失败）/ Client errors (e.g., validation failures)

**示例 / Examples:**
```csharp
_logger.LogWarning("传输层连接失败: {TransportType}, 原因: {Reason}", transportType, reason);
_logger.LogWarning("轴 {AxisId} 运动失败: 类型={MotionType}, 错误码={ErrorCode}", axisId, motionType, errorCode);
_logger.LogWarning("性能警告: {Operation} 耗时 {ElapsedMs}ms, 超过阈值 {ThresholdMs}ms", operation, elapsedMs, thresholdMs);
_logger.LogWarning("安全系统触发: 原因={Reason}, 受影响轴={AffectedAxes}", reason, affectedAxes);
_logger.LogWarning(ex, "操作失败，正在重试 ({RetryCount}/{MaxRetries})", retryCount, maxRetries);
```

**性能影响 / Performance Impact:**
- 应该引起关注但不需要立即处理 / Should get attention but doesn't require immediate action
- 频率应低于 Information / Frequency should be lower than Information

#### 4. Error (错误)

**使用场景 / Use Cases:**
- 操作失败 / Operation failures
- 未能完成请求 / Failed to complete request
- 需要人工干预的问题 / Issues requiring manual intervention
- 数据完整性问题 / Data integrity issues
- 外部依赖失败 / External dependency failures

**示例 / Examples:**
```csharp
_logger.LogError(ex, "传输层发生错误: {TransportType}", transportType);
_logger.LogError(ex, "轴 {AxisId} 发生严重错误: {ErrorMessage}", axisId, errorMessage);
_logger.LogError(ex, "配置加载失败: {ConfigType}", configType);
_logger.LogError(ex, "数据库操作失败: {Operation}, 表={Table}", operation, table);
_logger.LogError(ex, "协议解析异常: {Protocol}", protocol);
```

**性能影响 / Performance Impact:**
- 应立即调查 / Should be investigated immediately
- 表示功能性问题 / Indicates functional issues
- 可能影响用户体验 / May affect user experience

#### 5. Critical (严重)

**使用场景 / Use Cases:**
- 应用程序崩溃 / Application crash
- 数据丢失风险 / Data loss risk
- 安全问题 / Security issues
- 系统不可用 / System unavailability
- 需要立即响应的紧急情况 / Emergency situations requiring immediate response

**示例 / Examples:**
```csharp
_logger.LogCritical(ex, "紧急停止触发: {Reason}", reason);
_logger.LogCritical(ex, "数据库连接完全失败，系统无法运行");
_logger.LogCritical(ex, "安全系统故障，所有轴已停止");
_logger.LogCritical("内存不足，应用程序即将崩溃");
```

**性能影响 / Performance Impact:**
- 需要立即通知和响应 / Requires immediate notification and response
- 可能触发告警系统 / May trigger alerting systems
- 应该非常罕见 / Should be very rare

## 结构化日志最佳实践 / Structured Logging Best Practices

### 1. 使用结构化参数 / Use Structured Parameters

```csharp
// ✅ 正确 / Correct - 使用结构化参数
_logger.LogInformation(
    "轴 {AxisId} 移动到位置 {Position}, 速度 {Speed}",
    axisId,
    position,
    speed);

// ❌ 错误 / Wrong - 使用字符串插值
_logger.LogInformation($"轴 {axisId} 移动到位置 {position}, 速度 {speed}");
```

**优势 / Benefits:**
- 可搜索和查询 / Searchable and queryable
- 更好的性能 / Better performance
- 支持日志聚合工具 / Supports log aggregation tools
- 类型安全 / Type safe

### 2. 使用 LoggerMessage 源生成器 / Use LoggerMessage Source Generator

```csharp
// 在 LogMessages.cs 中定义 / Define in LogMessages.cs
public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "轴控制器已初始化: 厂商={Vendor}, 轴数={AxisCount}")]
    public static partial void AxisControllerInitialized(
        this ILogger logger,
        string vendor,
        int axisCount);
}

// 使用 / Usage
_logger.AxisControllerInitialized(vendor, axisCount);
```

**优势 / Benefits:**
- 零分配 / Zero allocation
- 编译时检查 / Compile-time checking
- 更好的性能 / Better performance
- 统一的日志定义 / Unified log definitions

### 3. 避免敏感信息泄露 / Avoid Sensitive Information Leakage

```csharp
// ✅ 正确 / Correct
_logger.LogInformation("用户 {UserId} 登录成功", userId);

// ❌ 错误 / Wrong - 记录密码
_logger.LogInformation("用户登录: {Username}/{Password}", username, password);
```

**需要保护的信息 / Information to Protect:**
- 密码和密钥 / Passwords and keys
- 个人身份信息 (PII) / Personal Identifiable Information
- 信用卡信息 / Credit card information
- API 密钥和令牌 / API keys and tokens

## 日志采样策略 / Log Sampling Strategy

### 高频日志处理 / High-Frequency Log Handling

对于高频操作（如每秒数百次的传感器读取），应实施采样策略：
For high-frequency operations (e.g., hundreds of sensor readings per second), implement sampling:

```csharp
public class SampledLogger
{
    private readonly ILogger _logger;
    private readonly TimeSpan _samplingInterval;
    private DateTime _lastLogTime = DateTime.MinValue;
    private long _eventCount;

    public SampledLogger(ILogger logger, TimeSpan samplingInterval)
    {
        _logger = logger;
        _samplingInterval = samplingInterval;
    }

    public void LogDebugSampled(string message, params object[] args)
    {
        var now = DateTime.UtcNow;
        Interlocked.Increment(ref _eventCount);

        if (now - _lastLogTime >= _samplingInterval)
        {
            _logger.LogDebug(
                message + " [采样: {EventCount} 次事件]",
                args.Append(_eventCount).ToArray());
            
            _lastLogTime = now;
            Interlocked.Exchange(ref _eventCount, 0);
        }
    }
}
```

**使用场景 / Use Cases:**
- 传感器数据读取 / Sensor data reading
- 网络数据包处理 / Network packet processing
- 性能监控指标 / Performance monitoring metrics

### 配置采样率 / Configure Sampling Rate

在 `appsettings.json` 中配置：
Configure in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "ZakYip.Singulation.Transport": "Debug",
      "ZakYip.Singulation.Protocol": "Debug"
    },
    "Sampling": {
      "Enabled": true,
      "HighFrequencyInterval": "00:00:10",  // 每10秒采样一次
      "Categories": [
        "ZakYip.Singulation.Transport.FrameDecoder",
        "ZakYip.Singulation.Drivers.AxisFeedback"
      ]
    }
  }
}
```

## 性能优化指南 / Performance Optimization Guidelines

### 1. 条件日志记录 / Conditional Logging

```csharp
// ✅ 正确 / Correct - 使用 IsEnabled 检查
if (_logger.IsEnabled(LogLevel.Debug))
{
    var expensiveData = GenerateExpensiveDebugData();
    _logger.LogDebug("调试数据: {Data}", expensiveData);
}

// ❌ 错误 / Wrong - 总是计算昂贵的数据
_logger.LogDebug("调试数据: {Data}", GenerateExpensiveDebugData());
```

### 2. 使用日志作用域 / Use Log Scopes

```csharp
using (_logger.BeginScope("Request {RequestId}", requestId))
{
    _logger.LogInformation("处理开始");
    // ... 处理逻辑
    _logger.LogInformation("处理完成");
}
// 所有日志都会包含 RequestId
```

### 3. 批量日志记录 / Batch Logging

对于大量相似事件，考虑批量记录：
For large numbers of similar events, consider batch logging:

```csharp
public class BatchLogger
{
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<LogEntry> _queue = new();
    private readonly Timer _flushTimer;

    public BatchLogger(ILogger logger)
    {
        _logger = logger;
        _flushTimer = new Timer(Flush, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void EnqueueLog(LogLevel level, string message, params object[] args)
    {
        _queue.Enqueue(new LogEntry(level, message, args));
    }

    private void Flush(object? state)
    {
        var batch = new List<LogEntry>();
        while (_queue.TryDequeue(out var entry))
        {
            batch.Add(entry);
        }

        if (batch.Count > 0)
        {
            _logger.LogInformation("批量日志 ({Count} 条)", batch.Count);
            foreach (var entry in batch.Take(10)) // 只记录前10条详细
            {
                _logger.Log(entry.Level, entry.Message, entry.Args);
            }
        }
    }
}
```

## 日志配置示例 / Logging Configuration Examples

### 开发环境 / Development Environment

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "SingleLine": false,
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff ",
        "UseUtcTimestamp": false
      }
    }
  }
}
```

### 生产环境 / Production Environment

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "System": "Warning"
    },
    "Console": {
      "FormatterName": "json",
      "FormatterOptions": {
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-ddTHH:mm:ss.fffZ",
        "UseUtcTimestamp": true
      }
    },
    "File": {
      "Path": "/var/log/singulation/app.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  }
}
```

## 日志监控和告警 / Log Monitoring and Alerting

### 关键指标 / Key Metrics

1. **错误率** / Error Rate
   - Warning 级别日志频率 / Warning level log frequency
   - Error 级别日志频率 / Error level log frequency
   - Critical 级别日志（应立即告警）/ Critical level logs (should alert immediately)

2. **性能指标** / Performance Metrics
   - 操作耗时超过阈值 / Operations exceeding time threshold
   - 资源使用率 / Resource utilization
   - 重试次数 / Retry count

3. **业务指标** / Business Metrics
   - 请求成功率 / Request success rate
   - 用户操作完成率 / User operation completion rate
   - 系统可用性 / System availability

### 告警规则示例 / Alert Rules Examples

```
- Critical 级别日志：立即发送告警
- Error 级别日志：5分钟内超过10次发送告警
- Warning 级别日志：1小时内超过100次发送告警
- 性能警告：连续3次超过阈值发送告警
```

## 日志清理策略 / Log Cleanup Strategy

### 日志保留策略 / Log Retention Policy

```csharp
public class LogCleanupOptions
{
    /// <summary>
    /// Debug 级别日志保留天数 / Debug log retention days
    /// </summary>
    public int DebugRetentionDays { get; set; } = 7;

    /// <summary>
    /// Information 级别日志保留天数 / Information log retention days
    /// </summary>
    public int InformationRetentionDays { get; set; } = 30;

    /// <summary>
    /// Warning 级别日志保留天数 / Warning log retention days
    /// </summary>
    public int WarningRetentionDays { get; set; } = 90;

    /// <summary>
    /// Error 和 Critical 级别日志保留天数 / Error and Critical log retention days
    /// </summary>
    public int ErrorRetentionDays { get; set; } = 365;
}
```

## 日志审计 / Log Auditing

### 审计日志要求 / Audit Log Requirements

对于关键操作，需要记录审计日志：
For critical operations, audit logs are required:

```csharp
_logger.LogInformation(
    "配置已更新: {ConfigType}, 用户={User}, IP={IpAddress}, 时间={Timestamp}",
    configType,
    user,
    ipAddress,
    DateTime.UtcNow);

_logger.LogInformation(
    "安全操作: 类型={OperationType}, 用户={User}, 结果={Result}",
    operationType,
    user,
    result);
```

**审计日志应包含 / Audit Logs Should Include:**
- 操作类型 / Operation type
- 操作者 / Operator
- 时间戳 / Timestamp
- IP 地址 / IP address
- 操作结果 / Operation result
- 相关实体 ID / Related entity IDs

## 总结 / Summary

遵循这些日志记录最佳实践可以确保：
Following these logging best practices ensures:

- ✅ 统一的日志级别使用 / Consistent log level usage
- ✅ 高性能的日志记录 / High-performance logging
- ✅ 结构化和可查询的日志 / Structured and queryable logs
- ✅ 合理的日志采样策略 / Reasonable log sampling strategy
- ✅ 有效的问题诊断和监控 / Effective issue diagnosis and monitoring
- ✅ 符合安全和合规要求 / Meets security and compliance requirements
