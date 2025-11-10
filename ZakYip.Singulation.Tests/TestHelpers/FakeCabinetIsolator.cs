using System;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的简单 CabinetIsolator 模拟实现。
    /// </summary>
    internal sealed class FakeCabinetIsolator : ICabinetIsolator {
        public CabinetIsolationState State => CabinetIsolationState.Normal;
        public bool IsDegraded => false;
        public bool IsIsolated => false;
        public CabinetTriggerKind LastTriggerKind { get; private set; }
        public string? LastTriggerReason { get; private set; }
        public int WriteCount { get; private set; }

        public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

        public bool TryTrip(CabinetTriggerKind kind, string reason) {
            LastTriggerKind = kind;
            LastTriggerReason = reason;
            WriteCount++;
            return true;
        }

        public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) {
            LastTriggerKind = kind;
            LastTriggerReason = reason;
            WriteCount++;
            return true;
        }


        public bool TryRecoverFromDegraded(string reason) {
            WriteCount++;
            return true;
        }

        public bool TryResetIsolation(string reason, CancellationToken ct = default) {
            WriteCount++;
            return true;
        }

        // 实现新的安全执行方法
        public bool SafeExecute(Action action, string operationName, Action<Exception>? onError = null) {
            try {
                action();
                return true;
            }
            catch (Exception ex) {
                onError?.Invoke(ex);
                return false;
            }
        }

        public T SafeExecute<T>(Func<T> func, string operationName, T defaultValue, Action<Exception>? onError = null) {
            try {
                return func();
            }
            catch (Exception ex) {
                onError?.Invoke(ex);
                return defaultValue;
            }
        }

        public T? SafeExecuteNullable<T>(Func<T> func, string operationName, Action<Exception>? onError = null) where T : class {
            try {
                return func();
            }
            catch (Exception ex) {
                onError?.Invoke(ex);
                return null;
            }
        }

        public int SafeExecuteBatch(Action[] actions, string operationName, bool stopOnFirstError = false) {
            int successCount = 0;
            foreach (var action in actions) {
                try {
                    action();
                    successCount++;
                }
                catch {
                    if (stopOnFirstError) break;
                }
            }
            return successCount;
        }

        public async Task<bool> SafeExecuteAsync(Func<Task> action, string operationName, Action<Exception>? onError = null) {
            try {
                await action();
                return true;
            }
            catch (Exception ex) {
                onError?.Invoke(ex);
                return false;
            }
        }

        public async Task<T> SafeExecuteAsync<T>(Func<Task<T>> func, string operationName, T defaultValue, Action<Exception>? onError = null) {
            try {
                return await func();
            }
            catch (Exception ex) {
                onError?.Invoke(ex);
                return defaultValue;
            }
        }
    }
}
