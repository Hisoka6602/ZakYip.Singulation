using System;

namespace ZakYip.Singulation.Core.Exceptions;

/// <summary>
/// 配置相关异常
/// Configuration-related exception
/// </summary>
public class ConfigurationException : SingulationException
{
    public ConfigurationException(string message)
        : base("CONFIG_ERROR", message)
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base("CONFIG_ERROR", message, innerException)
    {
    }
}

/// <summary>
/// 验证异常
/// Validation exception
/// </summary>
public class ValidationException : SingulationException
{
    public string? PropertyName { get; }

    public ValidationException(string message, string? propertyName = null)
        : base("VALIDATION_ERROR", message)
    {
        PropertyName = propertyName;
    }
}

/// <summary>
/// 硬件通信异常
/// Hardware communication exception
/// </summary>
public class HardwareCommunicationException : SingulationException
{
    public HardwareCommunicationException(string message)
        : base("HARDWARE_COMM_ERROR", message)
    {
        IsRetryable = true;
    }

    public HardwareCommunicationException(string message, Exception innerException)
        : base("HARDWARE_COMM_ERROR", message, innerException)
    {
        IsRetryable = true;
    }
}

/// <summary>
/// 传输层异常
/// Transport layer exception
/// </summary>
public class TransportException : SingulationException
{
    public TransportException(string message)
        : base("TRANSPORT_ERROR", message)
    {
        IsRetryable = true;
    }

    public TransportException(string message, Exception innerException)
        : base("TRANSPORT_ERROR", message, innerException)
    {
        IsRetryable = true;
    }
}

/// <summary>
/// 协议编解码异常
/// Protocol codec exception
/// </summary>
public class CodecException : SingulationException
{
    public CodecException(string message)
        : base("CODEC_ERROR", message)
    {
    }

    public CodecException(string message, Exception innerException)
        : base("CODEC_ERROR", message, innerException)
    {
    }
}

/// <summary>
/// 轴控制异常（基础轴控制错误）
/// Axis control exception (basic axis control errors)
/// 用于简单的轴控制错误，如轴ID无效、轴未找到等
/// Used for simple axis control errors like invalid axis ID, axis not found, etc.
/// </summary>
public class AxisControlException : SingulationException
{
    public int? AxisId { get; }

    public AxisControlException(string message, int? axisId = null)
        : base("AXIS_CONTROL_ERROR", message)
    {
        AxisId = axisId;
    }

    public AxisControlException(string message, int? axisId, Exception innerException)
        : base("AXIS_CONTROL_ERROR", message, innerException)
    {
        AxisId = axisId;
    }
}

/// <summary>
/// 轴操作异常（更具体的轴操作错误，包含操作上下文）
/// Axis operation exception with operation context
/// 用于具体的轴操作失败，包含操作名称、尝试的值等详细上下文
/// Used for specific axis operation failures with operation name, attempted value, and other detailed context
/// 区别：AxisControlException 是基础异常，AxisOperationException 包含更丰富的操作上下文
/// Difference: AxisControlException is basic, AxisOperationException includes richer operational context
/// </summary>
public class AxisOperationException : SingulationException
{
    /// <summary>
    /// 轴标识符
    /// Axis identifier
    /// </summary>
    public int? AxisId { get; }

    /// <summary>
    /// 执行的操作名称
    /// Operation name being performed
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// 尝试设置的值
    /// Attempted value to set
    /// </summary>
    public decimal? AttemptedValue { get; }

    public AxisOperationException(
        string message,
        int? axisId = null,
        string? operation = null,
        decimal? attemptedValue = null)
        : base("AXIS_OPERATION_ERROR", message)
    {
        AxisId = axisId;
        Operation = operation;
        AttemptedValue = attemptedValue;
        // 默认为可重试，具体情况由调用者设置
        IsRetryable = true;
    }

    public AxisOperationException(
        string message,
        Exception innerException,
        int? axisId = null,
        string? operation = null,
        decimal? attemptedValue = null)
        : base("AXIS_OPERATION_ERROR", message, innerException)
    {
        AxisId = axisId;
        Operation = operation;
        AttemptedValue = attemptedValue;
        // 默认为可重试，具体情况由调用者设置
        IsRetryable = true;
    }
}

/// <summary>
/// 安全系统异常
/// Safety system exception
/// </summary>
public class SafetyException : SingulationException
{
    public SafetyException(string message)
        : base("SAFETY_ERROR", message)
    {
    }

    public SafetyException(string message, Exception innerException)
        : base("SAFETY_ERROR", message, innerException)
    {
    }
}

/// <summary>
/// 安全违规异常（更具体的安全系统违规）
/// Safety violation exception for specific safety constraint violations
/// </summary>
public class SafetyViolationException : SingulationException
{
    /// <summary>
    /// 违规的安全规则名称
    /// Name of the violated safety rule
    /// </summary>
    public string? RuleName { get; }

    /// <summary>
    /// 违规时的系统状态
    /// System state at the time of violation
    /// </summary>
    public string? SystemState { get; }

    public SafetyViolationException(
        string message,
        string? ruleName = null,
        string? systemState = null)
        : base("SAFETY_VIOLATION", message)
    {
        RuleName = ruleName;
        SystemState = systemState;
        // 安全违规不应重试
        IsRetryable = false;
    }

    public SafetyViolationException(
        string message,
        Exception innerException,
        string? ruleName = null,
        string? systemState = null)
        : base("SAFETY_VIOLATION", message, innerException)
    {
        RuleName = ruleName;
        SystemState = systemState;
        // 安全违规不应重试
        IsRetryable = false;
    }
}
