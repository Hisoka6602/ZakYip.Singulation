using System;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Infrastructure.Logging;

/// <summary>
/// 高性能结构化日志消息定义
/// 使用 LoggerMessage 源生成器实现零分配日志记录
/// High-performance structured logging messages using source generation
/// </summary>
public static partial class LogMessages
{
    // ==================== Transport Layer Logs ====================

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "传输层已启动: {TransportType}, 端口: {Port}")]
    public static partial void TransportStarted(
        this ILogger logger,
        string transportType,
        int port);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "传输层已停止: {TransportType}")]
    public static partial void TransportStopped(
        this ILogger logger,
        string transportType);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "传输层连接失败: {TransportType}, 原因: {Reason}")]
    public static partial void TransportConnectionFailed(
        this ILogger logger,
        string transportType,
        string reason);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "传输层发生错误: {TransportType}")]
    public static partial void TransportError(
        this ILogger logger,
        Exception exception,
        string transportType);

    // ==================== Axis Control Logs ====================

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "轴控制器已初始化: 厂商={Vendor}, 轴数={AxisCount}")]
    public static partial void AxisControllerInitialized(
        this ILogger logger,
        string vendor,
        int axisCount);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "轴 {AxisId} 运动完成: 类型={MotionType}, 目标={Target}, 耗时={ElapsedMs}ms")]
    public static partial void AxisMotionCompleted(
        this ILogger logger,
        int axisId,
        string motionType,
        double target,
        long elapsedMs);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "轴 {AxisId} 运动失败: 类型={MotionType}, 错误码={ErrorCode}")]
    public static partial void AxisMotionFailed(
        this ILogger logger,
        int axisId,
        string motionType,
        int errorCode);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Error,
        Message = "轴 {AxisId} 发生严重错误: {ErrorMessage}")]
    public static partial void AxisCriticalError(
        this ILogger logger,
        Exception exception,
        int axisId,
        string errorMessage);

    // ==================== Protocol Codec Logs ====================

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Debug,
        Message = "帧解码成功: 类型={FrameType}, 长度={Length}字节")]
    public static partial void FrameDecoded(
        this ILogger logger,
        string frameType,
        int length);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "帧解码失败: CRC校验错误, 预期={Expected}, 实际={Actual}")]
    public static partial void FrameCrcError(
        this ILogger logger,
        string expected,
        string actual);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Error,
        Message = "协议解析异常: {Protocol}")]
    public static partial void ProtocolParseError(
        this ILogger logger,
        Exception exception,
        string protocol);

    // ==================== Safety System Logs ====================

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Warning,
        Message = "安全系统触发: 原因={Reason}, 受影响轴={AffectedAxes}")]
    public static partial void SafetyTriggered(
        this ILogger logger,
        string reason,
        string affectedAxes);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "安全系统已重置")]
    public static partial void SafetyReset(this ILogger logger);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Critical,
        Message = "紧急停止触发: {Reason}")]
    public static partial void EmergencyStopTriggered(
        this ILogger logger,
        string reason);

    // ==================== Configuration Logs ====================

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "配置已加载: {ConfigType}")]
    public static partial void ConfigurationLoaded(
        this ILogger logger,
        string configType);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Information,
        Message = "配置已更新: {ConfigType}, 用户={User}")]
    public static partial void ConfigurationUpdated(
        this ILogger logger,
        string configType,
        string user);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Error,
        Message = "配置加载失败: {ConfigType}")]
    public static partial void ConfigurationLoadFailed(
        this ILogger logger,
        Exception exception,
        string configType);

    // ==================== Performance Logs ====================

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Debug,
        Message = "操作性能: {Operation} 耗时 {ElapsedMs}ms")]
    public static partial void OperationPerformance(
        this ILogger logger,
        string operation,
        long elapsedMs);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Warning,
        Message = "性能警告: {Operation} 耗时 {ElapsedMs}ms, 超过阈值 {ThresholdMs}ms")]
    public static partial void PerformanceWarning(
        this ILogger logger,
        string operation,
        long elapsedMs,
        long thresholdMs);

    // ==================== Database Logs ====================

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Debug,
        Message = "数据库操作: {Operation}, 表={Table}, 耗时={ElapsedMs}ms")]
    public static partial void DatabaseOperation(
        this ILogger logger,
        string operation,
        string table,
        long elapsedMs);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Error,
        Message = "数据库操作失败: {Operation}, 表={Table}")]
    public static partial void DatabaseOperationFailed(
        this ILogger logger,
        Exception exception,
        string operation,
        string table);
}
