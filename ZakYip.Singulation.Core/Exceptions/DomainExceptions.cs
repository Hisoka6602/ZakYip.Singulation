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
/// 轴控制异常
/// Axis control exception
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
