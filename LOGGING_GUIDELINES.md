# 日志记录规范和指南
# Logging Guidelines and Best Practices

本文档定义了 ZakYip.Singulation 项目的日志记录标准和最佳实践。

## 1. 日志级别使用指南

### Trace (追踪)
- **用途**: 最详细的日志，用于诊断复杂问题
- **示例**: 函数进入/退出、循环迭代、详细的变量值
- **不应记录**: 生产环境默认不启用

### Debug (调试)
- **用途**: 调试信息，对开发和问题诊断有帮助
- **示例**: 
  - 协议帧解析详情
  - 数据库查询细节
  - 配置参数值
- **性能考虑**: 使用 `LoggerMessage` 源生成器以实现零分配

```csharp
// 推荐：使用 LoggerMessage 源生成器
_logger.FrameDecoded(frameType: "Speed", length: 128);

// 不推荐：字符串插值（会造成分配）
_logger.LogDebug($"帧解码成功: 类型={frameType}, 长度={length}字节");
```

### Information (信息)
- **用途**: 一般信息性消息，记录正常业务流程
- **示例**:
  - 服务启动/停止
  - 配置加载/更新
  - 重要操作完成（如轴初始化、传输层连接建立）

```csharp
_logger.TransportStarted(transportType: "TCP", port: 5000);
_logger.AxisControllerInitialized(vendor: "Leadshine", axisCount: 16);
```

### Warning (警告)
- **用途**: 异常情况，但不影响主要功能
- **示例**:
  - 重试操作
  - 使用默认值代替缺失配置
  - 性能降级
  - 安全系统触发

```csharp
_logger.AxisMotionFailed(axisId: 1, motionType: "Absolute", errorCode: -1);
_logger.SafetyTriggered(reason: "急停按钮", affectedAxes: "1,2,3");
```

### Error (错误)
- **用途**: 错误情况，功能无法正常执行
- **示例**:
  - 未处理的异常
  - 操作失败
  - 数据库错误
  - 硬件通信失败

```csharp
_logger.TransportError(exception, transportType: "TCP");
_logger.ConfigurationLoadFailed(exception, configType: "DriverOptions");
```

### Critical (严重)
- **用途**: 严重错误，可能导致系统崩溃或数据丢失
- **示例**:
  - 紧急停止触发
  - 硬件致命故障
  - 数据完整性问题

```csharp
_logger.EmergencyStopTriggered(reason: "安全光栅触发");
```

## 2. 结构化日志

### 使用 LoggerMessage 源生成器

为了获得最佳性能，所有高频日志应使用 `LoggerMessage` 源生成器：

```csharp
// 在 LogMessages.cs 中定义
public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "传输层已启动: {TransportType}, 端口: {Port}")]
    public static partial void TransportStarted(
        this ILogger logger,
        string transportType,
        int port);
}

// 使用
_logger.TransportStarted("TCP", 5000);
```

### 优势
- 零分配（避免装箱、字符串插值）
- 编译时验证
- 更好的性能（比传统日志快 2-10 倍）

## 3. EventId 规范

事件 ID 按模块分配范围：

| 模块 | EventId 范围 |
|-----|-------------|
| Transport | 1001-1999 |
| Axis Control | 2001-2999 |
| Protocol/Codec | 3001-3999 |
| Safety System | 4001-4999 |
| Configuration | 5001-5999 |
| Performance | 6001-6999 |
| Database | 7001-7999 |
| 未分类 | 9001-9999 |

## 4. 性能日志记录

### 操作耗时监控

```csharp
using var operation = new OperationTimer("AxisMove", _logger);
// 执行操作
await axis.MoveAsync(...);
// 自动记录耗时
```

### 性能阈值警告

```csharp
var sw = Stopwatch.StartNew();
await PerformOperationAsync();
sw.Stop();

if (sw.ElapsedMilliseconds > 100) // 阈值
{
    _logger.PerformanceWarning(
        operation: "AxisMove",
        elapsedMs: sw.ElapsedMilliseconds,
        thresholdMs: 100);
}
```

## 5. 异常日志记录

### 标准模式

```csharp
try
{
    // 操作
}
catch (SingulationException ex)
{
    // 业务异常：使用 Warning 级别
    _logger.LogWarning(ex, "业务异常: {ErrorCode}", ex.ErrorCode);
    throw; // 重新抛出，让全局异常处理器处理
}
catch (Exception ex)
{
    // 未预期异常：使用 Error 级别
    _logger.LogError(ex, "未预期异常发生在 {Operation}", "OperationName");
    throw;
}
```

### 全局异常处理器

不需要在控制器中记录异常，全局异常处理器会自动处理：

```csharp
// 不需要
[HttpGet]
public async Task<IActionResult> GetData()
{
    try
    {
        return Ok(await _service.GetDataAsync());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error"); // 多余
        return StatusCode(500);
    }
}

// 推荐
[HttpGet]
public async Task<IActionResult> GetData()
{
    return Ok(await _service.GetDataAsync());
    // 全局异常处理器会捕获并记录异常
}
```

## 6. 敏感信息保护

### 不要记录的内容
- ❌ 密码、密钥、令牌
- ❌ 个人身份信息（PII）
- ❌ 完整的数据库连接字符串
- ❌ API 密钥

### 安全日志示例

```csharp
// 错误示例
_logger.LogDebug("连接字符串: {ConnectionString}", connectionString);

// 正确示例
_logger.LogDebug("数据库连接: {Database}", 
    new Uri(connectionString).GetLeftPart(UriPartial.Path));
```

## 7. 日志聚合和监控

### 建议的日志输出

生产环境推荐配置：
- Console（控制台）：用于容器化部署
- File（文件）：用于历史审计
- Serilog/Seq：用于结构化日志分析
- Application Insights：用于云端监控

### appsettings.json 配置示例

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "ZakYip.Singulation": "Debug"
    }
  }
}
```

## 8. 日志检查清单

在提交代码前，确认：

- [ ] 使用了正确的日志级别
- [ ] 高频日志使用了 `LoggerMessage` 源生成器
- [ ] 没有记录敏感信息
- [ ] 异常包含足够的上下文信息
- [ ] 日志消息清晰、可理解
- [ ] 使用了结构化日志参数（不是字符串插值）
- [ ] EventId 在正确的范围内

## 9. 示例代码对比

### ❌ 不推荐

```csharp
_logger.LogInformation($"轴 {axisId} 移动到 {position}");
_logger.LogError("发生错误");
_logger.LogDebug(string.Format("CRC={0}", crc));
```

### ✅ 推荐

```csharp
_logger.AxisMotionCompleted(axisId, "Absolute", position, elapsedMs);
_logger.LogError(exception, "轴控制失败: AxisId={AxisId}", axisId);
_logger.FrameDecoded(frameType, length);
```

## 10. 工具和资源

- [Microsoft.Extensions.Logging 文档](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging)
- [LoggerMessage 源生成器](https://docs.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
- [Serilog 最佳实践](https://github.com/serilog/serilog/wiki/Configuration-Basics)

## 11. 持续改进

定期审查日志：
- 确保日志级别合适
- 检查是否有过多或过少的日志
- 优化高频日志的性能
- 更新日志消息使其更有用
