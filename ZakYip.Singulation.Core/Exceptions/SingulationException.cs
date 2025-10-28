using System;

namespace ZakYip.Singulation.Core.Exceptions;

/// <summary>
/// 基础异常类，所有自定义异常的基类
/// Base exception class for all custom exceptions in the application
/// </summary>
public abstract class SingulationException : Exception
{
    /// <summary>
    /// 错误代码，用于标识特定类型的错误
    /// Error code to identify specific types of errors
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// 是否为可重试的错误
    /// Indicates if the error is retryable
    /// </summary>
    public bool IsRetryable { get; protected set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    protected SingulationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        IsRetryable = false;
    }

    /// <summary>
    /// 构造函数（带内部异常）
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    protected SingulationException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        IsRetryable = false;
    }
}
