using System;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions.Cabinet;

namespace ZakYip.Singulation.Host.SwaggerOptions;

/// <summary>
/// 安全操作辅助类 - 使用 ICabinetIsolator 提供统一的异常隔离机制
/// Safe operation helper - uses ICabinetIsolator for unified exception isolation mechanism
/// </summary>
/// <remarks>
/// 此类现在是 ICabinetIsolator 的薄包装器，专门用于 Swagger 配置场景。
/// 由于 Swagger 配置代码没有依赖注入上下文，这里提供了一个静态辅助方法。
/// </remarks>
public static class SafeOperationHelper
{
    /// <summary>
    /// 安全执行操作，捕获并记录异常，防止操作失败阻塞主流程
    /// Safely execute operation, catch and log exceptions to prevent blocking main flow
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    public static void SafeExecute(Action action, ILogger? logger, string operationName)
    {
        try
        {
            action();
        }
        // 故意捕获所有异常，这是安全隔离器的设计目的
        // Intentionally catching all exceptions - this is the purpose of safe operation isolation
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // 记录异常但不抛出，确保不阻塞主流程
            logger?.LogWarning(ex, "安全隔离器捕获异常 - 操作: {OperationName}. 已忽略异常以确保服务继续运行。", operationName);
        }
    }

    /// <summary>
    /// 安全执行操作，捕获并记录异常，返回操作是否成功
    /// Safely execute operation, catch and log exceptions, return whether operation succeeded
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>操作是否成功</returns>
    public static bool TrySafeExecute(Action action, ILogger? logger, string operationName)
    {
        try
        {
            action();
            return true;
        }
        // 故意捕获所有异常，这是安全隔离器的设计目的
        // Intentionally catching all exceptions - this is the purpose of safe operation isolation
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger?.LogWarning(ex, "安全隔离器捕获异常 - 操作: {OperationName}. 已忽略异常以确保服务继续运行。", operationName);
            return false;
        }
    }

    /// <summary>
    /// 当有 ICabinetIsolator 实例时使用的安全执行方法
    /// Safe execute method when ICabinetIsolator instance is available
    /// </summary>
    /// <param name="isolator">安全隔离器实例</param>
    /// <param name="action">要执行的操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    public static void SafeExecute(ICabinetIsolator isolator, Action action, string operationName)
    {
        isolator?.SafeExecute(action, operationName);
    }
}
