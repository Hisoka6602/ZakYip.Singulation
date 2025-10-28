using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Exceptions;
using ZakYip.Singulation.Host.Middleware;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// 测试全局异常处理中间件
/// Tests for GlobalExceptionHandlerMiddleware
/// </summary>
internal sealed class GlobalExceptionHandlerMiddlewareTests
{
    [MiniFact]
    public async Task InvokeAsync_WithNoException_PassesThrough()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: async (ctx) => {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("OK");
            },
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(200, context.Response.StatusCode, "正常请求应返回 200");
    }

    [MiniFact]
    public async Task InvokeAsync_WithValidationException_Returns400()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new ValidationException("无效的轴ID", "axisId"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(400, context.Response.StatusCode, "ValidationException 应返回 400");
        MiniAssert.Equal("application/json", context.Response.ContentType, "Content-Type 应为 application/json");

        // Verify response content
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string responseText;
        using (var reader = new StreamReader(context.Response.Body))
        {
            responseText = await reader.ReadToEndAsync();
        }
        MiniAssert.True(responseText.Contains("VALIDATION_ERROR"), "响应应包含错误代码");
        MiniAssert.True(responseText.Contains("无效的轴ID"), "响应应包含错误消息");
    }

    [MiniFact]
    public async Task InvokeAsync_WithConfigurationException_Returns500()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new ConfigurationException("配置文件缺失"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(500, context.Response.StatusCode, "ConfigurationException 应返回 500");
    }

    [MiniFact]
    public async Task InvokeAsync_WithHardwareCommunicationException_Returns503()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new HardwareCommunicationException("控制器连接失败"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(503, context.Response.StatusCode, "HardwareCommunicationException 应返回 503");
        
        // Verify isRetryable flag in response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string responseText;
        using (var reader = new StreamReader(context.Response.Body))
        {
            responseText = await reader.ReadToEndAsync();
        }
        MiniAssert.True(responseText.Contains("isRetryable"), "响应应包含 isRetryable 标志");
    }

    [MiniFact]
    public async Task InvokeAsync_WithTransportException_Returns503()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new TransportException("TCP 连接断开"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(503, context.Response.StatusCode, "TransportException 应返回 503");
    }

    [MiniFact]
    public async Task InvokeAsync_WithCodecException_Returns400()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new CodecException("CRC 校验失败"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(400, context.Response.StatusCode, "CodecException 应返回 400");
    }

    [MiniFact]
    public async Task InvokeAsync_WithAxisControlException_Returns500()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new AxisControlException("轴运动失败", axisId: 1),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(500, context.Response.StatusCode, "AxisControlException 应返回 500");
        
        // Verify axisId in response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        MiniAssert.True(responseText.Contains("AxisId"), "响应应包含轴ID");
    }

    [MiniFact]
    public async Task InvokeAsync_WithSafetyException_Returns500()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new SafetyException("紧急停止触发"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(500, context.Response.StatusCode, "SafetyException 应返回 500");
    }

    [MiniFact]
    public async Task InvokeAsync_WithGenericException_Returns500()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new InvalidOperationException("未预期的错误"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        MiniAssert.Equal(500, context.Response.StatusCode, "未知异常应返回 500");
        
        // Verify error code
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        MiniAssert.True(responseText.Contains("INTERNAL_ERROR"), "响应应包含通用错误代码");
    }

    [MiniFact]
    public async Task InvokeAsync_ResponseIsValidJson()
    {
        // Arrange
        var middleware = new GlobalExceptionHandlerMiddleware(
            next: (ctx) => throw new ValidationException("测试异常"),
            logger: NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        
        // Try to deserialize - should not throw
        var doc = JsonDocument.Parse(responseText);
        MiniAssert.True(doc.RootElement.TryGetProperty("result", out _), "响应应包含 result 字段");
        MiniAssert.True(doc.RootElement.TryGetProperty("msg", out _), "响应应包含 msg 字段");
        MiniAssert.True(doc.RootElement.TryGetProperty("data", out _), "响应应包含 data 字段");
    }
}
