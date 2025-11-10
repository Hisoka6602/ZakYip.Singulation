using System;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    /// <summary>
    /// 安全操作隔离器：为有风险的操作提供隔离保护
    /// 捕获异常、记录日志，防止单个操作失败影响整体系统
    /// </summary>
    /// <remarks>
    /// [已弃用] 此类已被统一到 ICabinetIsolator/CabinetIsolator 中。
    /// 请使用 ICabinetIsolator 的 SafeExecute/SafeExecuteAsync 方法代替。
    /// </remarks>
    [Obsolete("此类已被弃用，请使用 ICabinetIsolator 的 SafeExecute/SafeExecuteAsync 方法代替", false)]
    public class SafeOperationIsolator {
        private readonly ILogger _logger;

        public SafeOperationIsolator(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 安全执行操作（无返回值）
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作是否成功</returns>
        public bool SafeExecute(Action action, string operationName, Action<Exception>? onError = null) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }

            try {
                _logger.LogDebug("开始执行操作: {OperationName}", operationName);
                action();
                _logger.LogDebug("操作执行成功: {OperationName}", operationName);
                return true;
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "操作执行失败: {OperationName}", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) {
                    _logger.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return false;
            }
        }

        /// <summary>
        /// 安全执行操作（有返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">失败时的默认返回值</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作结果或默认值</returns>
        public T SafeExecute<T>(Func<T> func, string operationName, T defaultValue, Action<Exception>? onError = null) {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            try {
                _logger.LogDebug("开始执行操作: {OperationName}", operationName);
                var result = func();
                _logger.LogDebug("操作执行成功: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "操作执行失败: {OperationName}，返回默认值", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) {
                    _logger.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全执行操作（可选返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作结果，失败时返回 null</returns>
        public T? SafeExecuteNullable<T>(Func<T> func, string operationName, Action<Exception>? onError = null) where T : class {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            try {
                _logger.LogDebug("开始执行操作: {OperationName}", operationName);
                var result = func();
                _logger.LogDebug("操作执行成功: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "操作执行失败: {OperationName}，返回 null", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) {
                    _logger.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return null;
            }
        }

        /// <summary>
        /// 批量安全执行操作
        /// </summary>
        /// <param name="actions">要执行的操作列表</param>
        /// <param name="operationName">操作名称前缀（用于日志）</param>
        /// <param name="stopOnFirstError">是否在第一个错误时停止</param>
        /// <returns>成功执行的操作数量</returns>
        public int SafeExecuteBatch(Action[] actions, string operationName, bool stopOnFirstError = false) {
            if (actions == null) {
                throw new ArgumentNullException(nameof(actions));
            }

            int successCount = 0;
            for (int i = 0; i < actions.Length; i++) {
                bool success = SafeExecute(
                    actions[i],
                    $"{operationName}[{i}]"
                );

                if (success) {
                    successCount++;
                }
                else if (stopOnFirstError) {
                    _logger.LogWarning("批量操作在第 {Index} 个操作失败后停止", i);
                    break;
                }
            }

            _logger.LogInformation(
                "批量操作完成: {SuccessCount}/{TotalCount} 成功",
                successCount,
                actions.Length
            );

            return successCount;
        }
    }
}
