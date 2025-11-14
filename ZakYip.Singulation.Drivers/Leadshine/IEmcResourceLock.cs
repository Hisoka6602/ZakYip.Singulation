using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// 定义 EMC 硬件资源分布式锁的接口。
    /// <para>
    /// 用于协调多个进程实例对共享 EMC 硬件资源的访问，特别是在执行冷/热复位操作时。
    /// </para>
    /// </summary>
    public interface IEmcResourceLock : IDisposable
    {
        /// <summary>
        /// 尝试获取分布式锁。
        /// </summary>
        /// <param name="timeout">等待超时时间。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>如果成功获取锁返回 true；否则返回 false。</returns>
        Task<bool> TryAcquireAsync(TimeSpan timeout, CancellationToken ct = default);

        /// <summary>
        /// 释放分布式锁。
        /// </summary>
        void Release();

        /// <summary>
        /// 获取锁的唯一标识符（如资源名称）。
        /// </summary>
        string LockIdentifier { get; }

        /// <summary>
        /// 指示当前实例是否持有锁。
        /// </summary>
        bool IsLockHeld { get; }
    }
}
