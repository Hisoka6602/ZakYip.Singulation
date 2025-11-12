using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// 雷赛（Leadshine）通用辅助工具类。
    /// <para>
    /// 提供跨多个 Leadshine 类使用的通用工具方法，避免代码重复。
    /// </para>
    /// </summary>
    public static class LeadshineHelpers
    {
        /// <summary>
        /// 非阻塞事件广播：逐订阅者独立执行，订阅方异常被隔离。
        /// </summary>
        /// <typeparam name="T">事件参数类型</typeparam>
        /// <param name="multicast">多播委托</param>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FireEachNonBlocking<T>(EventHandler<T>? multicast, object sender, T args)
        {
            if (multicast is null)
                return;
            foreach (var d in multicast.GetInvocationList())
            {
                var h = (EventHandler<T>)d;
                var state = new EvState<T>(sender, h, args);
                ThreadPool.UnsafeQueueUserWorkItem(static s =>
                {
                    var st = (EvState<T>)s!;
                    try
                    {
                        st.Handler(st.Sender, st.Args);
                    }
                    catch
                    {
                        // 订阅方异常被隔离，不反噬主流程
                    }
                }, state, preferLocal: true);
            }
        }

        /// <summary>
        /// 将 TimeSpan 转换为 Stopwatch ticks。
        /// </summary>
        /// <param name="timeSpan">时间跨度</param>
        /// <returns>Stopwatch ticks</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToStopwatchTicks(TimeSpan timeSpan)
            => (long)Math.Round(timeSpan.TotalSeconds * Stopwatch.Frequency);

        /// <summary>
        /// 事件状态封装结构体（用于非阻塞事件广播）。
        /// </summary>
        private readonly record struct EvState<T>(object Sender, EventHandler<T> Handler, T Args);
    }
}
