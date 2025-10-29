using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace ZakYip.Singulation.Drivers.Health {

    /// <summary>
    /// 轴健康监控服务，定期执行心跳检测以监控轴的健康状态。
    /// </summary>
    public sealed class AxisHealthMonitor : BackgroundService {
        private readonly Func<CancellationToken, Task<bool>> _ping;
        private readonly TimeSpan _interval;
        private volatile bool _enabled;

        /// <summary>
        /// 当轴从故障状态恢复时触发。
        /// </summary>
        public event Action? Recovered;

        /// <summary>
        /// 初始化 <see cref="AxisHealthMonitor"/> 类的新实例。
        /// </summary>
        /// <param name="ping">用于检测轴健康状态的心跳函数。</param>
        /// <param name="interval">心跳检测的时间间隔。</param>
        public AxisHealthMonitor(Func<CancellationToken, Task<bool>> ping, TimeSpan interval) {
            _ping = ping; _interval = interval;
        }

        /// <summary>
        /// 启动健康监控。
        /// </summary>
        public void Start() => _enabled = true;

        /// <summary>
        /// 停止健康监控。
        /// </summary>
        public void Stop() => _enabled = false;

        /// <summary>
        /// 执行后台监控任务。
        /// </summary>
        /// <param name="stoppingToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
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