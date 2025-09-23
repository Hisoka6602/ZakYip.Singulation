using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Resilience {

    public sealed class ConsecutiveFailCounter {
        private int _count, _fired;
        private readonly int _threshold;
        private Action? _onReached;

        public ConsecutiveFailCounter(int threshold) => _threshold = Math.Max(1, threshold);

        public void OnReached(Action cb) => _onReached = cb;

        public int Increment() {
            var v = Interlocked.Increment(ref _count);
            if (v >= _threshold && Interlocked.Exchange(ref _fired, 1) == 0)
                _onReached?.Invoke();
            return v;
        }

        public void Reset() {
            Interlocked.Exchange(ref _count, 0);
            Interlocked.Exchange(ref _fired, 0);
        }
    }
}