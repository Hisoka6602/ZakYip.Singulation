using System;
using System.Collections.Generic;
using System.Threading;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Infrastructure.Cabinet {

    /// <summary>
    /// 默认安全隔离器实现：集中管理隔离/降级状态、对外广播，并提供安全操作执行能力。
    /// 统一了 SafeOperationIsolator 的功能，成为唯一的安全隔离器。
    /// </summary>
    public sealed class CabinetIsolator : ICabinetIsolator {
        private readonly ILogger<CabinetIsolator> _log;
        private readonly IRealtimeNotifier _realtime;
        private readonly object _gate = new();

        private int _state = (int)CabinetIsolationState.Normal;
        private int _lastTriggerKind = (int)CabinetTriggerKind.Unknown;
        private string? _lastTriggerReason;

        public CabinetIsolator(ILogger<CabinetIsolator> log, IRealtimeNotifier realtime) {
            _log = log;
            _realtime = realtime;
        }

        public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

        public CabinetIsolationState State => (CabinetIsolationState)Volatile.Read(ref _state);

        public bool IsDegraded => State == CabinetIsolationState.Degraded;

        public bool IsIsolated => State == CabinetIsolationState.Isolated;

        public CabinetTriggerKind LastTriggerKind => (CabinetTriggerKind)Volatile.Read(ref _lastTriggerKind);

        public string? LastTriggerReason => Volatile.Read(ref _lastTriggerReason);

        public bool TryTrip(CabinetTriggerKind kind, string reason) {
            if (string.IsNullOrWhiteSpace(reason)) reason = "unknown";
            return Transition(kind, reason, CabinetIsolationState.Isolated);
        }

        public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) {
            if (string.IsNullOrWhiteSpace(reason)) reason = "degraded";
            return Transition(kind, reason, CabinetIsolationState.Degraded);
        }

        public bool TryRecoverFromDegraded(string reason) {
            if (State != CabinetIsolationState.Degraded) return false;
            return Transition(CabinetTriggerKind.HealthRecovered, string.IsNullOrWhiteSpace(reason) ? "recovered" : reason, CabinetIsolationState.Normal);
        }

        public bool TryResetIsolation(string reason, CancellationToken ct = default) {
            if (State != CabinetIsolationState.Isolated) return false;
            ct.ThrowIfCancellationRequested();
            return Transition(CabinetTriggerKind.ResetButton, string.IsNullOrWhiteSpace(reason) ? "reset" : reason, CabinetIsolationState.Normal);
        }

        private bool Transition(CabinetTriggerKind kind, string reason, CabinetIsolationState target) {
            CabinetStateChangedEventArgs? ev = null;
            lock (_gate) {
                var current = (CabinetIsolationState)_state;
                if (current == target) {
                    if (target == CabinetIsolationState.Degraded) {
                        _lastTriggerKind = (int)kind;
                        _lastTriggerReason = reason;
                    }
                    return false;
                }

                if (target == CabinetIsolationState.Degraded && current == CabinetIsolationState.Isolated)
                    return false;

                if (target == CabinetIsolationState.Isolated && current == CabinetIsolationState.Isolated)
                    return false;

                _state = (int)target;
                _lastTriggerKind = (int)kind;
                _lastTriggerReason = reason;
                ev = new CabinetStateChangedEventArgs {
                    Previous = current,
                    Current = target,
                    ReasonKind = kind,
                    ReasonText = reason
                };
            }

            if (ev is not null) {
                try {
                    LogStateChange(ev);
                    StateChanged?.Invoke(this, ev);
                }
                catch (Exception ex) // Intentional: Prevent event subscriber exceptions from affecting state transition
                {
                    _log.LogError(ex, "Safety isolator state change notification failed.");
                }
            }
            return ev is not null;
        }

        private void LogStateChange(CabinetStateChangedEventArgs ev) {
            var payload = new {
                kind = ev.ReasonKind.ToString(),
                state = ev.Current.ToString(),
                prev = ev.Previous.ToString(),
                reason = ev.ReasonText
            };

            switch (ev.Current) {
                case CabinetIsolationState.Isolated:
                    _log.LogWarning("Safety isolated due to {Kind}: {Reason}", ev.ReasonKind, ev.ReasonText);
                    SingulationMetrics.Instance.DegradeCounter.Add(1,
                        new KeyValuePair<string, object?>("state", "isolated"));
                    break;
                case CabinetIsolationState.Degraded:
                    _log.LogWarning("Safety degraded due to {Kind}: {Reason}", ev.ReasonKind, ev.ReasonText);
                    SingulationMetrics.Instance.DegradeCounter.Add(1,
                        new KeyValuePair<string, object?>("state", "degraded"));
                    break;
                default:
                    _log.LogInformation("Safety state recovered: {Reason}", ev.ReasonText);
                    break;
            }

            _ = _realtime.PublishDeviceAsync(new {
                kind = "safety.state",
                triggerKind = payload.kind,
                payload.state,
                payload.prev,
                payload.reason
            });
        }

        // ========== 安全操作执行方法（统一 SafeOperationIsolator 功能） ==========

        /// <summary>
        /// 安全执行操作（无返回值）
        /// </summary>
        /// <remarks>
        /// 此方法故意捕获所有异常类型（catch Exception）以提供安全包装器功能。
        /// 目的是防止任何未预料的异常导致系统崩溃，所有异常都会被记录并通过返回值表示失败。
        /// This method intentionally catches all exception types to provide a safety wrapper.
        /// </remarks>
        public bool SafeExecute(Action action, string operationName, Action<Exception>? onError = null) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }

            try {
                _log.LogDebug("开始执行操作: {OperationName}", operationName);
                action();
                _log.LogDebug("操作执行成功: {OperationName}", operationName);
                return true;
            }
            catch (Exception ex) // Intentional: Safety wrapper must catch all exceptions
            {
                _log.LogWarning(ex, "操作执行失败: {OperationName}", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) // Intentional: Prevent callback failures from affecting main flow
                {
                    _log.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return false;
            }
        }

        /// <summary>
        /// 安全执行操作（有返回值）
        /// </summary>
        /// <remarks>
        /// 此方法故意捕获所有异常类型（catch Exception）以提供安全包装器功能。
        /// This method intentionally catches all exception types to provide a safety wrapper.
        /// </remarks>
        public T SafeExecute<T>(Func<T> func, string operationName, T defaultValue, Action<Exception>? onError = null) {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            try {
                _log.LogDebug("开始执行操作: {OperationName}", operationName);
                var result = func();
                _log.LogDebug("操作执行成功: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex) // Intentional: Safety wrapper must catch all exceptions
            {
                _log.LogWarning(ex, "操作执行失败: {OperationName}，返回默认值", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) // Intentional: Prevent callback failures from affecting main flow
                {
                    _log.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全执行操作（可选返回值）
        /// </summary>
        /// <remarks>
        /// 此方法故意捕获所有异常类型（catch Exception）以提供安全包装器功能。
        /// This method intentionally catches all exception types to provide a safety wrapper.
        /// </remarks>
        public T? SafeExecuteNullable<T>(Func<T> func, string operationName, Action<Exception>? onError = null) where T : class {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            try {
                _log.LogDebug("开始执行操作: {OperationName}", operationName);
                var result = func();
                _log.LogDebug("操作执行成功: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex) // Intentional: Safety wrapper must catch all exceptions
            {
                _log.LogWarning(ex, "操作执行失败: {OperationName}，返回 null", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) // Intentional: Prevent callback failures from affecting main flow
                {
                    _log.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return null;
            }
        }

        /// <summary>
        /// 批量安全执行操作
        /// </summary>
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
                    _log.LogWarning("批量操作在第 {Index} 个操作失败后停止", i);
                    break;
                }
            }

            _log.LogInformation(
                "批量操作完成: {SuccessCount}/{TotalCount} 成功",
                successCount,
                actions.Length
            );

            return successCount;
        }

        /// <summary>
        /// 异步安全执行操作（无返回值）
        /// </summary>
        public async Task<bool> SafeExecuteAsync(Func<Task> action, string operationName, Action<Exception>? onError = null) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }

            try {
                _log.LogDebug("开始执行异步操作: {OperationName}", operationName);
                await action().ConfigureAwait(false);
                _log.LogDebug("异步操作执行成功: {OperationName}", operationName);
                return true;
            }
            catch (Exception ex) {
                _log.LogWarning(ex, "异步操作执行失败: {OperationName}", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) {
                    _log.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return false;
            }
        }

        /// <summary>
        /// 异步安全执行操作（有返回值）
        /// </summary>
        public async Task<T> SafeExecuteAsync<T>(Func<Task<T>> func, string operationName, T defaultValue, Action<Exception>? onError = null) {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            try {
                _log.LogDebug("开始执行异步操作: {OperationName}", operationName);
                var result = await func().ConfigureAwait(false);
                _log.LogDebug("异步操作执行成功: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex) {
                _log.LogWarning(ex, "异步操作执行失败: {OperationName}，返回默认值", operationName);
                
                try {
                    onError?.Invoke(ex);
                }
                catch (Exception callbackEx) {
                    _log.LogError(callbackEx, "错误处理回调执行失败: {OperationName}", operationName);
                }
                
                return defaultValue;
            }
        }
    }
}
