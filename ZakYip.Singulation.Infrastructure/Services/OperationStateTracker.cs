using System;
using System.Collections.Concurrent;

namespace ZakYip.Singulation.Infrastructure.Services;

/// <summary>
/// 操作状态跟踪服务
/// 用于防止重复调用初始化、重置等长时间运行的操作
/// </summary>
public sealed class OperationStateTracker
{
    private readonly ConcurrentDictionary<string, OperationState> _operations = new();

    /// <summary>
    /// 尝试开始一个操作
    /// </summary>
    /// <param name="operationKey">操作唯一键</param>
    /// <param name="operationName">操作名称（用于显示）</param>
    /// <returns>如果操作可以开始返回true，如果已有相同操作在进行中返回false</returns>
    public bool TryBeginOperation(string operationKey, string operationName)
    {
        var state = new OperationState
        {
            Name = operationName,
            StartTime = DateTime.UtcNow
        };

        return _operations.TryAdd(operationKey, state);
    }

    /// <summary>
    /// 结束一个操作
    /// </summary>
    /// <param name="operationKey">操作唯一键</param>
    public void EndOperation(string operationKey)
    {
        _operations.TryRemove(operationKey, out _);
    }

    /// <summary>
    /// 检查操作是否正在进行中
    /// </summary>
    /// <param name="operationKey">操作唯一键</param>
    /// <returns>如果操作正在进行中返回true</returns>
    public bool IsOperationInProgress(string operationKey)
    {
        return _operations.ContainsKey(operationKey);
    }

    /// <summary>
    /// 获取操作状态
    /// </summary>
    /// <param name="operationKey">操作唯一键</param>
    /// <returns>操作状态，如果不存在返回null</returns>
    public OperationState? GetOperationState(string operationKey)
    {
        _operations.TryGetValue(operationKey, out var state);
        return state;
    }

    /// <summary>
    /// 操作状态
    /// </summary>
    public class OperationState
    {
        /// <summary>
        /// 操作名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 运行时长
        /// </summary>
        public TimeSpan Duration => DateTime.UtcNow - StartTime;
    }
}

/// <summary>
/// 操作键常量
/// </summary>
public static class OperationKeys
{
    /// <summary>
    /// 控制器初始化操作
    /// </summary>
    public const string ControllerInitialization = "controller:initialize";

    /// <summary>
    /// 控制器硬复位操作
    /// </summary>
    public const string ControllerHardReset = "controller:hard-reset";

    /// <summary>
    /// 控制器软复位操作
    /// </summary>
    public const string ControllerSoftReset = "controller:soft-reset";

    /// <summary>
    /// 总线复位操作
    /// </summary>
    public const string BusReset = "bus:reset";
}
