using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Health {

    public sealed class AxisHealthMonitor {
        private readonly Func<CancellationToken, Task<bool>> _ping;
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cts;

        public event Action? Recovered;

        public AxisHealthMonitor(Func<CancellationToken, Task<bool>> ping, TimeSpan interval) {
            _ping = ping; _interval = interval;
        }

        public void Start() {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            _ = Task.Run(async () => {
                while (!ct.IsCancellationRequested) {
                    try {
                        if (await _ping(ct).ConfigureAwait(false)) {
                            Stop();
                            Recovered?.Invoke(); // ★ 新增：恢复事件
                            break;
                        }
                    }
                    catch { /* ignore */ }
                    await Task.Delay(_interval, ct).ConfigureAwait(false);
                }
            }, ct);
        }

        public void Stop() {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}