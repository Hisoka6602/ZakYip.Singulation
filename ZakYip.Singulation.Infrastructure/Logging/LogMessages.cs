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

    // ==================== Vendor SDK Call Logs ====================

    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Warning,
        Message = "厂商SDK调用失败: 轴={AxisId}, 方法={MethodName}, 返回值={ReturnCode}")]
    public static partial void VendorSdkCallFailed(
        this ILogger logger,
        int axisId,
        string methodName,
        int returnCode);

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "厂商SDK调用非预期结果: 轴={AxisId}, 调用={Invocation}, 返回值={ReturnCode}")]
    public static partial void VendorSdkUnexpectedResult(
        this ILogger logger,
        int axisId,
        string invocation,
        int returnCode);

    [LoggerMessage(
        EventId = 2102,
        Level = LogLevel.Error,
        Message = "厂商SDK调用异常: 轴={AxisId}, 方法={MethodName}, 错误消息={ErrorMessage}")]
    public static partial void VendorSdkException(
        this ILogger logger,
        Exception exception,
        int axisId,
        string methodName,
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

    // ==================== Event Pump Logs ====================

    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Information,
        Message = "[TransportEventPump] UpstreamTransportManager initialized")]
    public static partial void TransportManagerInitialized(this ILogger logger);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Error,
        Message = "[TransportEventPump] Failed to initialize UpstreamTransportManager")]
    public static partial void TransportManagerInitializationFailed(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Warning,
        Message = "[TransportEventPump] Failed to resolve transport '{Key}' - initialization may have failed")]
    public static partial void TransportResolveFailed(
        this ILogger logger,
        Exception exception,
        string key);

    [LoggerMessage(
        EventId = 8004,
        Level = LogLevel.Error,
        Message = "[TransportEventPump] Essential transport '{Essential}' is missing after initialization")]
    public static partial void EssentialTransportMissing(
        this ILogger logger,
        string essential);

    [LoggerMessage(
        EventId = 8005,
        Level = LogLevel.Information,
        Message = "[transport:{Name}] started, status={Status}")]
    public static partial void TransportStartedWithStatus(
        this ILogger logger,
        string name,
        string status);

    [LoggerMessage(
        EventId = 8006,
        Level = LogLevel.Error,
        Message = "[transport:{Name}] start failed")]
    public static partial void TransportStartFailed(
        this ILogger logger,
        Exception exception,
        string name);

    [LoggerMessage(
        EventId = 8007,
        Level = LogLevel.Error,
        Message = "[event-pump] pipeline error")]
    public static partial void EventPumpPipelineError(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 8008,
        Level = LogLevel.Debug,
        Message = "[transport:{Name}] stop ignored")]
    public static partial void TransportStopIgnored(
        this ILogger logger,
        Exception exception,
        string name);

    [LoggerMessage(
        EventId = 8009,
        Level = LogLevel.Warning,
        Message = "[transport:{Name}] dropped={Dropped} written={Written}")]
    public static partial void TransportStatsWithDrops(
        this ILogger logger,
        string name,
        long dropped,
        long written);

    [LoggerMessage(
        EventId = 8010,
        Level = LogLevel.Information,
        Message = "[transport:{Name}] written={Written}")]
    public static partial void TransportStatsNoDrops(
        this ILogger logger,
        string name,
        long written);

    [LoggerMessage(
        EventId = 8011,
        Level = LogLevel.Warning,
        Message = "[axis] dropped={Dropped} written={Written}")]
    public static partial void AxisStatsWithDrops(
        this ILogger logger,
        long dropped,
        long written);

    [LoggerMessage(
        EventId = 8012,
        Level = LogLevel.Information,
        Message = "[axis] written={Written}")]
    public static partial void AxisStatsNoDrops(
        this ILogger logger,
        long written);

    [LoggerMessage(
        EventId = 8013,
        Level = LogLevel.Information,
        Message = "[transport:{Source}] state={State}")]
    public static partial void TransportStateChanged(
        this ILogger logger,
        string source,
        string state);

    [LoggerMessage(
        EventId = 8014,
        Level = LogLevel.Error,
        Message = "[transport:{Source}] error")]
    public static partial void TransportErrorOccurred(
        this ILogger logger,
        Exception exception,
        string source);

    // ==================== IO Linkage Logs ====================

    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Information,
        Message = "IO联动触发: 状态变更 {OldState} → {NewState}, 联动点数={Count}")]
    public static partial void IoLinkageTriggered(
        this ILogger logger,
        string oldState,
        string newState,
        int count);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Debug,
        Message = "IO联动成功: 位={BitNumber}, 电平={Level}")]
    public static partial void IoLinkageSuccess(
        this ILogger logger,
        int bitNumber,
        string level);

    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Warning,
        Message = "IO联动失败: 位={BitNumber}, 原因={Reason}")]
    public static partial void IoLinkageFailed(
        this ILogger logger,
        int bitNumber,
        string reason);

    [LoggerMessage(
        EventId = 9004,
        Level = LogLevel.Error,
        Message = "IO联动异常: 位={BitNumber}")]
    public static partial void IoLinkageException(
        this ILogger logger,
        Exception exception,
        int bitNumber);

    [LoggerMessage(
        EventId = 9005,
        Level = LogLevel.Information,
        Message = "IO联动完成: 状态={State}, 成功={Success}, 失败={Failed}")]
    public static partial void IoLinkageCompleted(
        this ILogger logger,
        string state,
        int success,
        int failed);

    // ==================== Axis Operation Business Metrics ====================

    [LoggerMessage(
        EventId = 10001,
        Level = LogLevel.Information,
        Message = "轴操作开始: 轴={AxisId}, 操作={Operation}, 目标值={TargetValue}")]
    public static partial void AxisOperationStarted(
        this ILogger logger,
        int axisId,
        string operation,
        decimal targetValue);

    [LoggerMessage(
        EventId = 10002,
        Level = LogLevel.Information,
        Message = "轴操作成功: 轴={AxisId}, 操作={Operation}, 耗时={DurationMs}ms")]
    public static partial void AxisOperationSucceeded(
        this ILogger logger,
        int axisId,
        string operation,
        long durationMs);

    [LoggerMessage(
        EventId = 10003,
        Level = LogLevel.Warning,
        Message = "轴操作重试: 轴={AxisId}, 操作={Operation}, 重试次数={RetryCount}")]
    public static partial void AxisOperationRetry(
        this ILogger logger,
        int axisId,
        string operation,
        int retryCount);

    [LoggerMessage(
        EventId = 10004,
        Level = LogLevel.Error,
        Message = "轴操作失败: 轴={AxisId}, 操作={Operation}, 错误码={ErrorCode}")]
    public static partial void AxisOperationFailed(
        this ILogger logger,
        int axisId,
        string operation,
        string errorCode);

    [LoggerMessage(
        EventId = 10005,
        Level = LogLevel.Critical,
        Message = "轴紧急停止: 轴={AxisId}, 原因={Reason}")]
    public static partial void AxisEmergencyStop(
        this ILogger logger,
        int axisId,
        string reason);

    [LoggerMessage(
        EventId = 10006,
        Level = LogLevel.Information,
        Message = "轴使能状态变更: 轴={AxisId}, 使能={Enabled}")]
    public static partial void AxisEnableStateChanged(
        this ILogger logger,
        int axisId,
        bool enabled);

    [LoggerMessage(
        EventId = 10007,
        Level = LogLevel.Warning,
        Message = "轴限位触发: 轴={AxisId}, 限位类型={LimitType}")]
    public static partial void AxisLimitTriggered(
        this ILogger logger,
        int axisId,
        string limitType);

    // ==================== Safety Event Business Metrics ====================

    [LoggerMessage(
        EventId = 11001,
        Level = LogLevel.Critical,
        Message = "安全事件触发: 类型={EventType}, 规则={RuleName}, 受影响轴={AffectedAxes}")]
    public static partial void SafetyEventTriggered(
        this ILogger logger,
        string eventType,
        string ruleName,
        string affectedAxes);

    [LoggerMessage(
        EventId = 11002,
        Level = LogLevel.Warning,
        Message = "安全违规检测: 规则={RuleName}, 系统状态={SystemState}")]
    public static partial void SafetyViolationDetected(
        this ILogger logger,
        string ruleName,
        string systemState);

    [LoggerMessage(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "安全系统状态变更: 旧状态={OldState}, 新状态={NewState}")]
    public static partial void SafetyStateChanged(
        this ILogger logger,
        string oldState,
        string newState);

    [LoggerMessage(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "安全系统重置成功: 重置者={ResetBy}")]
    public static partial void SafetySystemReset(
        this ILogger logger,
        string resetBy);

    [LoggerMessage(
        EventId = 11005,
        Level = LogLevel.Error,
        Message = "安全系统重置失败: 原因={Reason}")]
    public static partial void SafetySystemResetFailed(
        this ILogger logger,
        string reason);

    // ==================== Exception Aggregation Logs ====================

    [LoggerMessage(
        EventId = 12001,
        Level = LogLevel.Information,
        Message = "异常统计: 类型={ExceptionType}, 上下文={Context}, 次数={Count}, 可重试={IsRetryable}")]
    public static partial void ExceptionStatistics(
        this ILogger logger,
        string exceptionType,
        string context,
        long count,
        bool isRetryable);

    [LoggerMessage(
        EventId = 12002,
        Level = LogLevel.Warning,
        Message = "异常聚合报告: 总类型数={TotalTypes}, 总次数={TotalCount}")]
    public static partial void ExceptionAggregationReport(
        this ILogger logger,
        int totalTypes,
        long totalCount);

    [LoggerMessage(
        EventId = 12003,
        Level = LogLevel.Information,
        Message = "异常聚合服务已启动，聚合间隔: {AggregationMinutes}分钟")]
    public static partial void ExceptionAggregationServiceStarted(
        this ILogger logger,
        double aggregationMinutes);

    [LoggerMessage(
        EventId = 12004,
        Level = LogLevel.Information,
        Message = "异常聚合服务已取消")]
    public static partial void ExceptionAggregationServiceCancelled(
        this ILogger logger);

    [LoggerMessage(
        EventId = 12005,
        Level = LogLevel.Debug,
        Message = "聚合了 {Count} 个异常记录")]
    public static partial void ExceptionRecordsAggregated(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 12006,
        Level = LogLevel.Information,
        Message = "异常统计报告：无异常记录")]
    public static partial void ExceptionReportEmpty(
        this ILogger logger);

    [LoggerMessage(
        EventId = 12007,
        Level = LogLevel.Critical,
        Message = "高频异常: 类型={Type}, 上下文={Context}, 次数={Count}")]
    public static partial void HighFrequencyException(
        this ILogger logger,
        string type,
        string context,
        long count);

    // ==================== High-Frequency Operation Sampled Logs ====================

    [LoggerMessage(
        EventId = 13001,
        Level = LogLevel.Debug,
        Message = "高频操作采样: 操作={Operation}, 累计次数={TotalCount}, 采样率={SamplingRate}")]
    public static partial void HighFrequencyOperationSampled(
        this ILogger logger,
        string operation,
        long totalCount,
        int samplingRate);

    // ==================== System Health Monitoring Logs ====================

    [LoggerMessage(
        EventId = 14001,
        Level = LogLevel.Warning,
        Message = "系统健康度下降: 评分={Score}, 等级={Level}, 在线轴={OnlineCount}/{TotalCount}")]
    public static partial void SystemHealthDegraded(
        this ILogger logger,
        double score,
        string level,
        int onlineCount,
        int totalCount);

    [LoggerMessage(
        EventId = 14002,
        Level = LogLevel.Critical,
        Message = "系统健康严重告警: 评分={Score}, 故障轴={FaultedCount}, 错误率={ErrorRate:P2}")]
    public static partial void SystemHealthCritical(
        this ILogger logger,
        double score,
        int faultedCount,
        double errorRate);

    [LoggerMessage(
        EventId = 14003,
        Level = LogLevel.Information,
        Message = "系统健康度优秀: 评分={Score}, 所有轴正常")]
    public static partial void SystemHealthExcellent(
        this ILogger logger,
        double score);

    // ==================== Cabinet IO Module Logs ====================

    [LoggerMessage(
        EventId = 15001,
        Level = LogLevel.Information,
        Message = "控制面板IO模块启动: 轮询间隔={PollingIntervalMs}ms")]
    public static partial void CabinetIoModuleStarted(
        this ILogger logger,
        int pollingIntervalMs);

    [LoggerMessage(
        EventId = 15002,
        Level = LogLevel.Information,
        Message = "控制面板IO模块已停止")]
    public static partial void CabinetIoModuleStopped(
        this ILogger logger);

    [LoggerMessage(
        EventId = 15003,
        Level = LogLevel.Warning,
        Message = "控制面板IO模块已在运行中")]
    public static partial void CabinetIoModuleAlreadyRunning(
        this ILogger logger);

    [LoggerMessage(
        EventId = 15004,
        Level = LogLevel.Warning,
        Message = "检测到急停按键按下 - IO端口: IN{Port}")]
    public static partial void EmergencyStopButtonPressed(
        this ILogger logger,
        int port);

    [LoggerMessage(
        EventId = 15005,
        Level = LogLevel.Information,
        Message = "检测到停止按键按下 - IO端口: IN{Port}")]
    public static partial void StopButtonPressed(
        this ILogger logger,
        int port);

    [LoggerMessage(
        EventId = 15006,
        Level = LogLevel.Information,
        Message = "检测到启动按键按下 - IO端口: IN{Port}")]
    public static partial void StartButtonPressed(
        this ILogger logger,
        int port);

    [LoggerMessage(
        EventId = 15007,
        Level = LogLevel.Information,
        Message = "检测到复位按键按下 - IO端口: IN{Port}")]
    public static partial void ResetButtonPressed(
        this ILogger logger,
        int port);

    [LoggerMessage(
        EventId = 15008,
        Level = LogLevel.Information,
        Message = "检测到远程/本地模式切换: {Mode}")]
    public static partial void RemoteLocalModeChanged(
        this ILogger logger,
        string mode);

    [LoggerMessage(
        EventId = 15009,
        Level = LogLevel.Information,
        Message = "启动时读取远程/本地模式IO状态: {Mode}")]
    public static partial void InitialRemoteLocalModeDetected(
        this ILogger logger,
        string mode);

    [LoggerMessage(
        EventId = 15010,
        Level = LogLevel.Information,
        Message = "远程/本地模式IO未配置，默认为本地模式")]
    public static partial void RemoteLocalModeNotConfigured(
        this ILogger logger);

    [LoggerMessage(
        EventId = 15011,
        Level = LogLevel.Warning,
        Message = "启动时读取远程/本地模式IO超时（控制器可能未初始化），默认为本地模式")]
    public static partial void InitialRemoteLocalModeTimeout(
        this ILogger logger);

    [LoggerMessage(
        EventId = 15012,
        Level = LogLevel.Information,
        Message = "输出位状态设置: 位={BitNo}, 状态={Status}")]
    public static partial void OutputBitSet(
        this ILogger logger,
        int bitNo,
        string status);

    [LoggerMessage(
        EventId = 15013,
        Level = LogLevel.Warning,
        Message = "读取输入位失败: 位={BitNo}, 错误码={ErrorCode}")]
    public static partial void ReadInputBitFailed(
        this ILogger logger,
        int bitNo,
        int errorCode);

    [LoggerMessage(
        EventId = 15014,
        Level = LogLevel.Error,
        Message = "读取输入位时发生硬件库异常: 位={BitNo}")]
    public static partial void ReadInputBitException(
        this ILogger logger,
        Exception exception,
        int bitNo);

    [LoggerMessage(
        EventId = 15015,
        Level = LogLevel.Error,
        Message = "读取控制面板IO时发生异常")]
    public static partial void CabinetIoReadException(
        this ILogger logger,
        Exception exception);

    // ==================== UDP Discovery Service Logs ====================

    [LoggerMessage(
        EventId = 16001,
        Level = LogLevel.Information,
        Message = "UDP服务发现已禁用")]
    public static partial void UdpDiscoveryDisabled(
        this ILogger logger);

    [LoggerMessage(
        EventId = 16002,
        Level = LogLevel.Information,
        Message = "UDP服务发现服务启动，端口: {Port}, 间隔: {Interval}秒")]
    public static partial void UdpDiscoveryServiceStarted(
        this ILogger logger,
        int port,
        int interval);

    [LoggerMessage(
        EventId = 16003,
        Level = LogLevel.Information,
        Message = "UDP服务发现服务已停止")]
    public static partial void UdpDiscoveryServiceStopped(
        this ILogger logger);

    [LoggerMessage(
        EventId = 16004,
        Level = LogLevel.Debug,
        Message = "已发送UDP广播: 序号={SequenceNumber}")]
    public static partial void UdpBroadcastSent(
        this ILogger logger,
        long sequenceNumber);

    [LoggerMessage(
        EventId = 16005,
        Level = LogLevel.Error,
        Message = "发送UDP广播时发生错误")]
    public static partial void UdpBroadcastError(
        this ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 16006,
        Level = LogLevel.Error,
        Message = "UDP服务发现服务异常")]
    public static partial void UdpDiscoveryServiceException(
        this ILogger logger,
        Exception exception);

    // ==================== Heartbeat Worker Logs ====================

    [LoggerMessage(
        EventId = 17001,
        Level = LogLevel.Debug,
        Message = "心跳接收: 长度={Length}字节, 序号={SequenceNumber}")]
    public static partial void HeartbeatReceived(
        this ILogger logger,
        int length,
        long sequenceNumber);

    [LoggerMessage(
        EventId = 17002,
        Level = LogLevel.Warning,
        Message = "心跳包处理失败: 长度={Length}字节")]
    public static partial void HeartbeatProcessingFailed(
        this ILogger logger,
        Exception exception,
        int length);

    [LoggerMessage(
        EventId = 17003,
        Level = LogLevel.Error,
        Message = "心跳监听发生异常")]
    public static partial void HeartbeatListenerException(
        this ILogger logger,
        Exception exception);

}
