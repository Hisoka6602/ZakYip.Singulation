using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace ZakYip.Singulation.Drivers.Health {

    public sealed class AxisHealthMonitor : BackgroundService {
        private readonly Func<CancellationToken, Task<bool>> _ping;
        private readonly TimeSpan _interval;
        private volatile bool _enabled;

        public event Action? Recovered;

        public AxisHealthMonitor(Func<CancellationToken, Task<bool>> ping, TimeSpan interval) {
            _ping = ping; _interval = interval;
        }

        public void Start() => _enabled = true;

        public void Stop() => _enabled = false;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                if (!_enabled) continue;
                bool ok = false;
                try { ok = await _ping(stoppingToken); } catch { }
                if (ok) Recovered?.Invoke();
            }
        }
    }
}