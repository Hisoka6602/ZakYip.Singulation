using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Resilience {

    /// <summary>
    /// 连续失败计数器，用于跟踪连续失败次数并在达到阈值时触发回调。
    /// </summary>
    public sealed class ConsecutiveFailCounter {
        private int _count, _fired;
        private readonly int _threshold;
        private Action? _onReached;

        /// <summary>
        /// 初始化 <see cref="ConsecutiveFailCounter"/> 类的新实例。
        /// </summary>
        /// <param name="threshold">触发回调的失败次数阈值（最小为1）。</param>
        public ConsecutiveFailCounter(int threshold) => _threshold = Math.Max(1, threshold);

        /// <summary>
        /// 设置达到阈值时的回调函数。
        /// </summary>
        /// <param name="cb">回调函数。</param>
        public void OnReached(Action cb) => _onReached = cb;

        /// <summary>
        /// 增加连续失败计数。达到阈值时触发回调（仅触发一次）。
        /// </summary>
        /// <returns>当前失败计数。</returns>
        public int Increment() {
            var v = Interlocked.Increment(ref _count);
            if (v >= _threshold && Interlocked.Exchange(ref _fired, 1) == 0)
                _onReached?.Invoke();
            return v;
        }

        /// <summary>
        /// 重置失败计数和触发状态。
        /// </summary>
        public void Reset() {
            Interlocked.Exchange(ref _count, 0);
            Interlocked.Exchange(ref _fired, 0);
        }
    }
}