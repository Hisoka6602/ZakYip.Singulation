# 日志记录最佳实践 / Logging Best Practices

本文档定义了 ZakYip.Singulation 系统的日志记录标准和最佳实践。

This document defines logging standards and best practices for the ZakYip.Singulation system.

## 日志级别标准 / Log Level Standards

### Debug (调试)
- **用途**: 详细的诊断信息，仅在开发和故障排查时使用
- **示例**: 
  - 详细的数据流追踪
  - 配置值的详细输出
  - 内部状态变化
- **Production 环境**: 通常禁用

```csharp
_logger.LogDebug("系统状态未变化，当前状态：{State}，跳过 IO 联动", newState);
```

### Information (信息)
- **用途**: 记录系统的正常运行信息和重要的业务事件
- **示例**:
  - 服务启动/停止
  - 配置加载成功
  - 业务流程完成
- **Production 环境**: 启用

```csharp
_logger.LogInformation("轴实时数据广播服务启动，更新频率: {Frequency}Hz", frequency);
```

### Warning (警告)
- **用途**: 潜在问题或异常情况，但不影响系统继续运行
- **示例**:
  - 性能下降
  - 重试操作
  - 资源使用率高
  - 配置不推荐但可接受
- **Production 环境**: 启用

```csharp
_logger.LogWarning("系统健康度: {Score}, 等级: {Level}", health.Score, health.Level);
```

### Error (错误)
- **用途**: 错误情况，影响特定功能但系统可继续运行
- **示例**:
  - 操作失败但可重试
  - 资源访问失败
  - 数据验证错误
- **Production 环境**: 启用

```csharp
_logger.LogError(ex, "广播轴数据失败");
```

### Critical (严重)
- **用途**: 严重错误，可能导致系统不稳定或停止
- **示例**:
  - 安全系统触发
  - 紧急停止
  - 关键资源不可用
  - 数据损坏
- **Production 环境**: 启用并告警

```csharp
_logger.LogCritical("紧急停止触发: {Reason}", reason);
```

## 结构化日志 / Structured Logging

### 使用 LoggerMessage Source Generator

推荐使用 `LoggerMessage` 特性定义结构化日志消息，以获得最佳性能和类型安全。

```csharp
// 在 LogMessages.cs 中定义
[LoggerMessage(
    EventId = 10001,
    Level = LogLevel.Information,
    Message = "轴操作开始: 轴={AxisId}, 操作={Operation}, 目标值={TargetValue}")]
public static partial void AxisOperationStarted(
    this ILogger logger,
    int axisId,
    string operation,
    decimal targetValue);

// 在服务中使用
_logger.AxisOperationStarted(axisId, "SetSpeed", targetSpeed);
```

### 优势
- **零分配**: 避免装箱和字符串格式化的内存分配
- **类型安全**: 编译时检查参数类型
- **性能**: 比传统方法快 2-10 倍
- **一致性**: 统一的日志格式和事件ID

## 高频操作日志采样 / Log Sampling for High-Frequency Operations

对于高频操作（如实时数据推送、传感器读取等），使用 `LogSampler` 进行采样以避免日志泛滥。

```csharp
private readonly LogSampler _logSampler = new();

// 每100次记录一次
if (_logSampler.ShouldLog("OperationKey", 100)) {
    var count = _logSampler.GetCount("OperationKey");
    _logger.HighFrequencyOperationSampled("Operation", count, 100);
    _logger.LogDebug("操作详情 (总次数: {Count})", count);
}

// 或基于时间间隔采样
if (_logSampler.ShouldLogByTime("OperationKey", TimeSpan.FromMinutes(1))) {
    _logger.LogDebug("定期状态更新");
}
```

### 采样策略建议

| 操作类型 | 推荐采样率 | 说明 |
|---------|-----------|------|
| 实时数据推送 (5Hz+) | 每 100-500 次 | 避免日志泛滥 |
| 传感器读取 (1Hz+) | 每 50-100 次 | 保留足够的诊断信息 |
| 心跳检查 | 每 10-20 次 | 定期确认运行状态 |
| 配置查询 | 每 1 分钟 | 基于时间的采样 |

## 关键业务指标日志 / Critical Business Metrics Logging

### 轴操作日志

所有轴操作都应记录以下关键指标：

```csharp
// 操作开始
_logger.AxisOperationStarted(axisId, "SetSpeed", targetSpeed);

// 操作成功
_logger.AxisOperationSucceeded(axisId, "SetSpeed", durationMs);

// 操作失败
_logger.AxisOperationFailed(axisId, "SetSpeed", errorCode);

// 紧急停止
_logger.AxisEmergencyStop(axisId, reason);
```

### 安全事件日志

所有安全相关事件必须使用 Critical 或 Warning 级别：

```csharp
// 安全事件触发
_logger.SafetyEventTriggered(eventType, ruleName, affectedAxes);

// 安全违规
_logger.SafetyViolationDetected(ruleName, systemState);

// 安全系统重置
_logger.SafetySystemReset(resetBy);
```

### IO 联动日志

IO 联动操作应记录触发、执行和完成：

```csharp
// 联动触发
_logger.IoLinkageTriggered(oldState, newState, count);

// 单个IO操作
_logger.IoLinkageSuccess(bitNumber, level);
_logger.IoLinkageFailed(bitNumber, reason);

// 联动完成
_logger.IoLinkageCompleted(state, successCount, failCount);
```

## 异常聚合和上报 / Exception Aggregation and Reporting

使用 `ExceptionAggregationService` 收集和聚合异常信息：

```csharp
public class MyService
{
    private readonly ExceptionAggregationService? _exceptionAggregation;

    public async Task DoWorkAsync()
    {
        try
        {
            // 业务逻辑
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "操作失败");
            // 记录异常用于聚合分析
            _exceptionAggregation?.RecordException(ex, "MyService:DoWork");
            throw;
        }
    }
}
```

### 上下文命名规范

使用分层的上下文命名，便于分析：

- 格式: `{ServiceName}:{Operation}:{Detail}`
- 示例:
  - `IoLinkage:Execute`
  - `IoLinkage:Bit12`
  - `AxisControl:SetSpeed:Axis3`
  - `Transport:TcpConnect`

## EventId 分配规范 / EventId Allocation

EventId 用于唯一标识日志消息类型，便于监控和告警。

| EventId 范围 | 用途 | 说明 |
|-------------|------|------|
| 1000-1999 | 传输层 | Transport layer |
| 2000-2999 | 轴控制 | Axis control |
| 3000-3999 | 协议编解码 | Protocol codec |
| 4000-4999 | 安全系统 | Safety system |
| 5000-5999 | 配置管理 | Configuration |
| 6000-6999 | 性能监控 | Performance |
| 7000-7999 | 数据库操作 | Database operations |
| 8000-8999 | 事件泵 | Event pump |
| 9000-9999 | IO 联动 | IO linkage |
| 10000-10999 | 轴操作业务指标 | Axis operation metrics |
| 11000-11999 | 安全事件业务指标 | Safety event metrics |
| 12000-12999 | 异常聚合 | Exception aggregation |
| 13000-13999 | 高频操作采样 | High-frequency sampling |

## 日志格式示例 / Log Format Examples

### 良好的日志格式
```
[2024-11-07 10:30:15.123] [Information] [EventId: 10001] 轴操作开始: 轴=3, 操作=SetSpeed, 目标值=1500.5
[2024-11-07 10:30:15.456] [Information] [EventId: 10002] 轴操作成功: 轴=3, 操作=SetSpeed, 耗时=333ms
[2024-11-07 10:30:20.789] [Warning] [EventId: 11002] 安全违规检测: 规则=SpeedLimit, 系统状态=Running
```

### 避免的日志格式
```
// 缺少上下文
[2024-11-07 10:30:15.123] [Information] 操作完成

// 没有结构化数据
[2024-11-07 10:30:15.123] [Information] Axis 3 set to 1500.5 mm/s in 333 milliseconds

// 过于详细的Debug信息在Info级别
[2024-11-07 10:30:15.123] [Information] Frame[0x12, 0x34, 0x56, ...]
```

## 监控和告警建议 / Monitoring and Alerting Recommendations

### Critical 级别日志
- 立即发送告警
- 记录到专门的安全日志
- 触发事件响应流程

### Error 级别日志
- 每小时汇总告警
- 当错误率超过阈值时告警
- 异常聚合报告

### Warning 级别日志
- 每日汇总报告
- 趋势分析
- 性能优化参考

## 性能优化提示 / Performance Optimization Tips

1. **使用 LoggerMessage Source Generator**: 避免字符串格式化和装箱
2. **条件日志**: 在生产环境禁用 Debug 级别
3. **日志采样**: 对高频操作使用采样
4. **异步日志**: 使用异步日志提供程序避免阻塞
5. **结构化数据**: 避免大对象序列化到日志中

## 更新历史 / Change History

- 2024-11-07: 初始版本，添加日志级别标准、结构化日志、采样策略和异常聚合
