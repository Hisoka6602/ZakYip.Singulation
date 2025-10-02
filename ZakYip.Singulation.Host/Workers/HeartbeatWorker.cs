using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Runtime;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Host.Workers {

    /// <summary>
    /// 演示多订阅：心跳帧的独立消费者。
    /// </summary>
    public sealed class HeartbeatWorker : BackgroundService {
        private readonly ILogger<HeartbeatWorker> _log;
        private readonly IUpstreamFrameHub _hub;

        public HeartbeatWorker(ILogger<HeartbeatWorker> log, IUpstreamFrameHub hub) {
            _log = log;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var (reader, unsub) = _hub.SubscribeHeartbeat(capacity: 128);
            using (unsub) {
                await foreach (var mem in reader.ReadAllAsync(stoppingToken)) {
                    // 在此解析/统计心跳；保持轻量
                    _log.LogDebug("Heartbeat {len}B", mem.Length);
                }
            }
        }
    }
}