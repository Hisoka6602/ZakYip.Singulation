using System.Diagnostics;

namespace ZakYip.Singulation.MauiApp.Helpers;

/// <summary>
/// 安全执行器 - 提供异常隔离和安全执行功能
/// </summary>
public static class SafeExecutor
{
    /// <summary>
    /// 安全执行异步操作，捕获并处理异常
    /// </summary>
    /// <param name="action">要执行的异步操作</param>
    /// <param name="onError">错误处理回调（可选）</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <param name="timeout">超时时间（毫秒），默认30秒</param>
    /// <returns>成功返回true，失败返回false</returns>
    public static async Task<bool> ExecuteAsync(
        Func<Task> action,
        Action<Exception>? onError = null,
        string operationName = "Unknown",
        int timeout = 30000)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        try
        {
            using var cts = new CancellationTokenSource(timeout);
            var task = action();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            
            if (completedTask != task)
            {
                var timeoutException = new TimeoutException($"Operation '{operationName}' timed out after {timeout}ms");
                Debug.WriteLine($"[SafeExecutor] Timeout: {operationName}");
                onError?.Invoke(timeoutException);
                return false;
            }

            await task; // Re-await to get any exceptions
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SafeExecutor] Exception in {operationName}: {ex.Message}");
            onError?.Invoke(ex);
            return false;
        }
    }

    /// <summary>
    /// 安全执行异步操作并返回结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的异步函数</param>
    /// <param name="defaultValue">发生错误时的默认返回值</param>
    /// <param name="onError">错误处理回调（可选）</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <param name="timeout">超时时间（毫秒），默认30秒</param>
    /// <returns>成功返回函数结果，失败返回默认值</returns>
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> func,
        T defaultValue,
        Action<Exception>? onError = null,
        string operationName = "Unknown",
        int timeout = 30000)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        try
        {
            using var cts = new CancellationTokenSource(timeout);
            var task = func();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            
            if (completedTask != task)
            {
                var timeoutException = new TimeoutException($"Operation '{operationName}' timed out after {timeout}ms");
                Debug.WriteLine($"[SafeExecutor] Timeout: {operationName}");
                onError?.Invoke(timeoutException);
                return defaultValue;
            }

            return await task; // Re-await to get the result or exception
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SafeExecutor] Exception in {operationName}: {ex.Message}");
            onError?.Invoke(ex);
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全执行同步操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="onError">错误处理回调（可选）</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>成功返回true，失败返回false</returns>
    public static bool Execute(
        Action action,
        Action<Exception>? onError = null,
        string operationName = "Unknown")
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SafeExecutor] Exception in {operationName}: {ex.Message}");
            onError?.Invoke(ex);
            return false;
        }
    }

    /// <summary>
    /// 安全执行同步操作并返回结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <param name="defaultValue">发生错误时的默认返回值</param>
    /// <param name="onError">错误处理回调（可选）</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>成功返回函数结果，失败返回默认值</returns>
    public static T Execute<T>(
        Func<T> func,
        T defaultValue,
        Action<Exception>? onError = null,
        string operationName = "Unknown")
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SafeExecutor] Exception in {operationName}: {ex.Message}");
            onError?.Invoke(ex);
            return defaultValue;
        }
    }

    /// <summary>
    /// 断路器模式 - 防止重复调用失败的操作
    /// </summary>
    public class CircuitBreaker
    {
        private int _failureCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly int _failureThreshold;
        private readonly TimeSpan _resetTimeout;
        private bool _isOpen = false;

        public CircuitBreaker(int failureThreshold = 3, int resetTimeoutSeconds = 60)
        {
            _failureThreshold = failureThreshold;
            _resetTimeout = TimeSpan.FromSeconds(resetTimeoutSeconds);
        }

        public bool IsOpen => _isOpen;

        /// <summary>
        /// 执行操作，如果断路器打开则直接返回失败
        /// </summary>
        public async Task<bool> ExecuteAsync(
            Func<Task> action,
            Action<Exception>? onError = null,
            string operationName = "Unknown")
        {
            // 检查是否需要重置断路器
            if (_isOpen && DateTime.UtcNow - _lastFailureTime > _resetTimeout)
            {
                Debug.WriteLine($"[CircuitBreaker] Attempting to reset for {operationName}");
                _failureCount = 0;
                _isOpen = false;
            }

            // 如果断路器打开，直接返回失败
            if (_isOpen)
            {
                Debug.WriteLine($"[CircuitBreaker] Circuit is open for {operationName}, rejecting call");
                onError?.Invoke(new InvalidOperationException("Circuit breaker is open"));
                return false;
            }

            // 执行操作
            var success = await SafeExecutor.ExecuteAsync(action, onError, operationName);
            
            if (!success)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                // 检查是否达到失败阈值
                if (_failureCount >= _failureThreshold)
                {
                    _isOpen = true;
                    Debug.WriteLine($"[CircuitBreaker] Circuit opened for {operationName} after {_failureCount} failures");
                }
            }
            else
            {
                // 成功后重置失败计数
                _failureCount = 0;
            }

            return success;
        }

        /// <summary>
        /// 手动重置断路器
        /// </summary>
        public void Reset()
        {
            _failureCount = 0;
            _isOpen = false;
            _lastFailureTime = DateTime.MinValue;
        }
    }
}
