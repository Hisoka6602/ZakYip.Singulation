using System;
using System.Threading;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;

namespace ZakYip.Singulation.Core.Abstractions.Cabinet {

    /// <summary>
    /// 安全隔离器：负责集中管理降级/隔离状态、对外广播状态变化，并提供安全操作执行能力。
    /// </summary>
    public interface ICabinetIsolator {
        /// <summary>当前隔离状态。</summary>
        CabinetIsolationState State { get; }

        /// <summary>当前是否处于降级。</summary>
        bool IsDegraded { get; }

        /// <summary>当前是否处于隔离。</summary>
        bool IsIsolated { get; }

        /// <summary>最近一次触发的来源。</summary>
        CabinetTriggerKind LastTriggerKind { get; }

        /// <summary>最近一次触发的描述。</summary>
        string? LastTriggerReason { get; }

        /// <summary>状态变化事件。</summary>
        event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

        /// <summary>触发隔离。</summary>
        bool TryTrip(CabinetTriggerKind kind, string reason);

        /// <summary>进入降级运行。</summary>
        bool TryEnterDegraded(CabinetTriggerKind kind, string reason);

        /// <summary>从降级恢复。</summary>
        bool TryRecoverFromDegraded(string reason);

        /// <summary>从隔离状态复位。</summary>
        bool TryResetIsolation(string reason, CancellationToken ct = default);

        /// <summary>
        /// 安全执行操作（无返回值）。
        /// 捕获异常、记录日志，防止单个操作失败影响整体系统。
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作是否成功</returns>
        bool SafeExecute(Action action, string operationName, Action<Exception>? onError = null);

        /// <summary>
        /// 安全执行操作（有返回值）。
        /// 捕获异常、记录日志，失败时返回默认值。
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">失败时的默认返回值</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作结果或默认值</returns>
        T SafeExecute<T>(Func<T> func, string operationName, T defaultValue, Action<Exception>? onError = null);

        /// <summary>
        /// 安全执行操作（可选返回值）。
        /// 捕获异常、记录日志，失败时返回 null。
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作结果，失败时返回 null</returns>
        T? SafeExecuteNullable<T>(Func<T> func, string operationName, Action<Exception>? onError = null) where T : class;

        /// <summary>
        /// 批量安全执行操作。
        /// </summary>
        /// <param name="actions">要执行的操作列表</param>
        /// <param name="operationName">操作名称前缀（用于日志）</param>
        /// <param name="stopOnFirstError">是否在第一个错误时停止</param>
        /// <returns>成功执行的操作数量</returns>
        int SafeExecuteBatch(Action[] actions, string operationName, bool stopOnFirstError = false);

        /// <summary>
        /// 异步安全执行操作（无返回值）。
        /// 捕获异常、记录日志，防止单个操作失败影响整体系统。
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作是否成功</returns>
        Task<bool> SafeExecuteAsync(Func<Task> action, string operationName, Action<Exception>? onError = null);

        /// <summary>
        /// 异步安全执行操作（有返回值）。
        /// 捕获异常、记录日志，失败时返回默认值。
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">失败时的默认返回值</param>
        /// <param name="onError">可选的错误处理回调</param>
        /// <returns>操作结果或默认值</returns>
        Task<T> SafeExecuteAsync<T>(Func<Task<T>> func, string operationName, T defaultValue, Action<Exception>? onError = null);
    }
}
