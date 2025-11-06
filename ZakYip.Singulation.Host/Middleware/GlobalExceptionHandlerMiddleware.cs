using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Exceptions;
using ZakYip.Singulation.Host.Dto;

namespace ZakYip.Singulation.Host.Middleware;

/// <summary>
/// 全局异常处理中间件
/// Global exception handling middleware
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // 重新抛出不应处理的关键异常
            if (ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException)
            {
                throw;
            }
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 记录异常
        LogException(exception);

        // 设置响应
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ValidationException validationEx => CreateResponse(
                HttpStatusCode.BadRequest,
                validationEx.ErrorCode,
                validationEx.Message,
                new { validationEx.PropertyName }),

            ConfigurationException configEx => CreateResponse(
                HttpStatusCode.InternalServerError,
                configEx.ErrorCode,
                configEx.Message),

            HardwareCommunicationException hardwareEx => CreateResponse(
                HttpStatusCode.ServiceUnavailable,
                hardwareEx.ErrorCode,
                hardwareEx.Message,
                new { isRetryable = hardwareEx.IsRetryable }),

            TransportException transportEx => CreateResponse(
                HttpStatusCode.ServiceUnavailable,
                transportEx.ErrorCode,
                transportEx.Message,
                new { isRetryable = transportEx.IsRetryable }),

            CodecException codecEx => CreateResponse(
                HttpStatusCode.BadRequest,
                codecEx.ErrorCode,
                codecEx.Message),

            AxisControlException axisEx => CreateResponse(
                HttpStatusCode.InternalServerError,
                axisEx.ErrorCode,
                axisEx.Message,
                new { axisEx.AxisId }),

            SafetyException safetyEx => CreateResponse(
                HttpStatusCode.InternalServerError,
                safetyEx.ErrorCode,
                safetyEx.Message),

            SingulationException singulationEx => CreateResponse(
                HttpStatusCode.InternalServerError,
                singulationEx.ErrorCode,
                singulationEx.Message),

            _ => CreateResponse(
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "服务器内部错误，请稍后重试")
        };

        context.Response.StatusCode = (int)response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response.ApiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private void LogException(Exception exception)
    {
        var logLevel = exception switch
        {
            ValidationException => LogLevel.Warning,
            SingulationException singulationEx when singulationEx.IsRetryable => LogLevel.Warning,
            SingulationException => LogLevel.Error,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel, exception, "异常被全局异常处理器捕获: {ExceptionType}", exception.GetType().Name);
    }

    private static (HttpStatusCode StatusCode, object ApiResponse) CreateResponse(
        HttpStatusCode statusCode,
        string errorCode,
        string message,
        object? data = null)
    {
        var enrichedData = new { errorCode, details = data };
        var apiResponse = ApiResponse<object>.Fail(message, enrichedData);
        return (statusCode, apiResponse);
    }
}

/// <summary>
/// 全局异常处理中间件扩展方法
/// Extension methods for GlobalExceptionHandlerMiddleware
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    /// <summary>
    /// 使用全局异常处理中间件
    /// Use global exception handler middleware
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
