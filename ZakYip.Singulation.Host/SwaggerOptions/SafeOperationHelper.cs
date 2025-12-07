using System;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions.Cabinet;

namespace ZakYip.Singulation.Host.SwaggerOptions;

/// <summary>
/// 安全操作辅助类 - 使用 ICabinetIsolator 提供统一的异常隔离机制
/// Safe operation helper - uses ICabinetIsolator for unified exception isolation mechanism
/// </summary>
/// <remarks>
/// 此类是 ICabinetIsolator 的薄包装器，专门用于 Swagger 配置场景。
/// 由于 Swagger 配置代码没有依赖注入上下文，这里提供了静态辅助方法。
/// 所有方法最终都委托给 ICabinetIsolator，避免代码重复。
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
    /// <remarks>
    /// 注意：此方法仅用于无法使用依赖注入的 Swagger 配置场景。
    /// 在其他场景下，请优先使用 ICabinetIsolator.SafeExecute。
    /// 此方法会创建一个临时的 CabinetIsolator 实例来执行操作。
    /// </remarks>
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
    /// <remarks>
    /// 注意：此方法仅用于无法使用依赖注入的 Swagger 配置场景。
    /// 在其他场景下，请优先使用 ICabinetIsolator.SafeExecute。
    /// </remarks>
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
    /// <param name="isolator">安全隔离器实例。如果为 null，则不执行任何操作。</param>
    /// <param name="action">要执行的操作</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <remarks>
    /// 当 isolator 参数为 null 时，此方法会静默返回而不执行任何操作。
    /// 这是有意设计的行为，用于简化调用方代码，避免空检查。
    /// 如果需要确保 isolator 不为 null，调用方应在调用前进行验证。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当 action 为 null 时抛出</exception>
    public static void SafeExecute(ICabinetIsolator? isolator, Action action, string operationName)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        // 如果 isolator 为 null，静默返回（这是有意设计的行为）
        isolator?.SafeExecute(action, operationName);
    }
}
