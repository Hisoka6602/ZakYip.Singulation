# 异常聚合和日志优化实施总结 / Exception Aggregation and Logging Optimization Implementation Summary

## 概述 / Overview

本次更新实现了完整的异常聚合和日志记录优化系统，符合所有需求规范。

This update implements a complete exception aggregation and logging optimization system that meets all requirement specifications.

## 实施的功能 / Implemented Features

### 1. 异常聚合和上报机制 / Exception Aggregation and Reporting Mechanism

#### 核心组件 / Core Components

**ExceptionAggregationService** (`ZakYip.Singulation.Infrastructure/Services/ExceptionAggregationService.cs`)

- 作为后台服务运行，持续收集异常
- 每 5 分钟聚合一次异常数据
- 每 15 分钟生成统计报告
- 自动清理超过 1 小时且发生次数少于 5 次的旧数据
- 支持可重试异常的分类跟踪

**使用方法 / Usage**:

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

**统计报告内容**:
- 异常类型和上下文
- 发生次数
- 首次和最近发生时间
- 是否可重试
- 高频异常告警（超过 100 次）

### 2. 日志记录规范优化 / Logging Specification Optimization

#### 2.1 统一日志级别使用标准 / Unified Log Level Standards

已在 `docs/logging-best-practices.md` 中完整定义：

- **Debug**: 详细诊断信息，仅用于开发和故障排查
- **Information**: 系统正常运行信息和重要业务事件
- **Warning**: 潜在问题，但不影响系统继续运行
- **Error**: 错误情况，影响特定功能但系统可继续运行
- **Critical**: 严重错误，可能导致系统不稳定或停止

#### 2.2 高频操作日志采样策略 / Log Sampling for High-Frequency Operations

**LogSampler** (`ZakYip.Singulation.Infrastructure/Logging/LogSampler.cs`)

支持两种采样策略：

1. **基于计数的采样** / Count-based sampling:
```csharp
private readonly LogSampler _logSampler = new();

// 每 100 次记录一次
if (_logSampler.ShouldLog("OperationKey", 100)) {
    var count = _logSampler.GetCount("OperationKey");
    _logger.LogDebug("操作详情 (总次数: {Count})", count);
}
```

2. **基于时间的采样** / Time-based sampling:
```csharp
// 每分钟记录一次
if (_logSampler.ShouldLogByTime("OperationKey", TimeSpan.FromMinutes(1))) {
    _logger.LogInformation("定期状态更新");
}
```

**推荐采样率**:
- 实时数据推送 (5Hz+): 每 100-500 次
- 传感器读取 (1Hz+): 每 50-100 次
- 心跳检查: 每 10-20 次
- 配置查询: 每 1 分钟

#### 2.3 结构化日志最佳实践 / Structured Logging Best Practices

**使用 LoggerMessage Source Generator** (`LogMessages.cs`)

优势：
- 零内存分配
- 编译时类型检查
- 高性能（比传统方法快 2-10 倍）
- 统一的日志格式

**示例**:
```csharp
[LoggerMessage(
    EventId = 10001,
    Level = LogLevel.Information,
    Message = "轴操作开始: 轴={AxisId}, 操作={Operation}, 目标值={TargetValue}")]
public static partial void AxisOperationStarted(
    this ILogger logger,
    int axisId,
    string operation,
    decimal targetValue);

// 使用
_logger.AxisOperationStarted(axisId, "SetSpeed", targetSpeed);
```

#### 2.4 关键业务指标日志 / Critical Business Metrics Logging

**轴操作日志** (EventId 10001-10007):
```csharp
_logger.AxisOperationStarted(axisId, operation, targetValue);
_logger.AxisOperationSucceeded(axisId, operation, durationMs);
_logger.AxisOperationFailed(axisId, operation, errorCode);
_logger.AxisEmergencyStop(axisId, reason);
_logger.AxisEnableStateChanged(axisId, enabled);
_logger.AxisLimitTriggered(axisId, limitType);
```

**安全事件日志** (EventId 11001-11005):
```csharp
_logger.SafetyEventTriggered(eventType, ruleName, affectedAxes);
_logger.SafetyViolationDetected(ruleName, systemState);
_logger.SafetyStateChanged(oldState, newState);
_logger.SafetySystemReset(resetBy);
_logger.SafetySystemResetFailed(reason);
```

**IO 联动日志** (EventId 9001-9005):
```csharp
_logger.IoLinkageTriggered(oldState, newState, count);
_logger.IoLinkageSuccess(bitNumber, level);
_logger.IoLinkageFailed(bitNumber, reason);
_logger.IoLinkageException(ex, bitNumber);
_logger.IoLinkageCompleted(state, successCount, failCount);
```

## EventId 分配方案 / EventId Allocation Scheme

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
| 14000-14999 | 系统健康监控 | System health monitoring |

## 已更新的服务 / Updated Services

### IoLinkageService
- ✅ 使用结构化日志记录 IO 联动触发、成功、失败和完成
- ✅ 集成异常聚合服务
- ✅ 使用 LoggerMessage Source Generator

### RealtimeAxisDataService
- ✅ 实施日志采样（每 100 次记录一次）
- ✅ 集成异常聚合服务
- ✅ 避免高频错误日志泛滥

### SystemHealthMonitorService
- ✅ 使用结构化日志记录健康状态
- ✅ 根据健康等级使用不同的日志级别
- ✅ 实施时间采样（优秀状态每 5 分钟记录一次）
- ✅ 集成异常聚合服务

### ExceptionAggregationService
- ✅ 全面使用结构化日志
- ✅ 所有日志消息都使用 LoggerMessage Source Generator
- ✅ 符合代码审查要求

## 单元测试 / Unit Tests

### ExceptionAggregationServiceTests (6 tests)
- ✅ RecordException_ShouldStoreException
- ✅ RecordException_ShouldAggregateMultipleExceptions
- ✅ RecordException_ShouldHandleNullException
- ✅ RecordException_ShouldTrackSingulationExceptions
- ✅ GetStatistics_ShouldReturnEmptyWhenNoExceptions
- ✅ ExecuteAsync_ShouldStartAndStopGracefully

### LogSamplerTests (11 tests)
- ✅ ShouldLog_WithSamplingRate1_AlwaysReturnsTrue
- ✅ ShouldLog_WithSamplingRate100_ReturnsEvery100th
- ✅ ShouldLog_WithDifferentKeys_MaintainsSeparateCounters
- ✅ ShouldLogByTime_WithMinInterval_RespectsTimeConstraint
- ✅ ShouldLogByTime_AfterInterval_ReturnsTrue
- ✅ GetCount_ReturnsAccurateCount
- ✅ GetCount_ForNonExistentKey_ReturnsZero
- ✅ Reset_ClearsCounterAndTime
- ✅ ShouldLog_WithNegativeSamplingRate_ThrowsException
- ✅ ShouldLog_WithZeroSamplingRate_ThrowsException
- ✅ ShouldLog_ConcurrentAccess_MaintainsAccuracy

**所有测试通过** ✅

## 文档 / Documentation

### logging-best-practices.md

完整的日志记录最佳实践文档，包括：
- 日志级别标准
- 结构化日志使用指南
- 高频操作采样策略
- 关键业务指标日志
- EventId 分配规范
- 监控和告警建议
- 性能优化提示

## 性能特性 / Performance Characteristics

1. **零分配日志**: 使用 LoggerMessage Source Generator
2. **线程安全**: 使用 ConcurrentQueue 和 ConcurrentDictionary
3. **内存控制**: 异常队列限制 10,000 条，自动清理旧数据
4. **采样优化**: 减少高频操作的日志开销

## 使用建议 / Usage Recommendations

### 在新服务中集成

1. **注入依赖**:
```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly ExceptionAggregationService? _exceptionAggregation;
    private readonly LogSampler _logSampler = new();

    public MyService(
        ILogger<MyService> logger,
        ExceptionAggregationService? exceptionAggregation = null)
    {
        _logger = logger;
        _exceptionAggregation = exceptionAggregation;
    }
}
```

2. **记录异常**:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "操作失败");
    _exceptionAggregation?.RecordException(ex, "MyService:Operation");
}
```

3. **使用日志采样**:
```csharp
if (_logSampler.ShouldLog("HighFreqOp", 100))
{
    _logger.LogDebug("高频操作详情");
}
```

4. **使用结构化日志**:
```csharp
_logger.AxisOperationStarted(axisId, operation, targetValue);
```

## 监控建议 / Monitoring Recommendations

### Critical 级别日志
- 立即发送告警
- 记录到专门的安全日志
- 触发事件响应流程

### 异常聚合报告
- 监控高频异常（>100 次）
- 追踪异常趋势
- 识别系统性问题

### 性能指标
- 监控日志采样率
- 追踪异常聚合队列大小
- 优化日志级别配置

## 总结 / Summary

本次实施完全满足所有需求：

✅ **异常聚合和上报机制**: ExceptionAggregationService 提供完整的异常收集、聚合和报告功能

✅ **统一日志级别标准**: 明确的 Debug/Info/Warning/Error/Critical 使用规范

✅ **日志采样策略**: LogSampler 提供灵活的计数和时间采样

✅ **结构化日志最佳实践**: 全面使用 LoggerMessage Source Generator

✅ **关键业务指标日志**: 轴操作、安全事件、IO 联动的完整日志记录

所有更改都经过：
- ✅ 单元测试验证
- ✅ 代码审查并已修正
- ✅ 构建成功
- ✅ 文档完整
