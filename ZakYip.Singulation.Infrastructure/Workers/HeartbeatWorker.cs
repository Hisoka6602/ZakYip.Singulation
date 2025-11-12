using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Infrastructure.Runtime;
using ZakYip.Singulation.Infrastructure.Logging;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Infrastructure.Workers {

    /// <summary>
    /// 演示多订阅：心跳帧的独立消费者。
    /// </summary>
    public sealed class HeartbeatWorker : BackgroundService {
        private readonly ILogger<HeartbeatWorker> _log;
        private readonly IUpstreamFrameHub _hub;
        private readonly LogSampler _logSampler = new();
        private long _heartbeatSequence = 0;

        public HeartbeatWorker(ILogger<HeartbeatWorker> log, IUpstreamFrameHub hub) {
            _log = log;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var (reader, unsub) = _hub.SubscribeHeartbeat(capacity: 128);
            using (unsub) {
                try {
                    await foreach (var mem in reader.ReadAllAsync(stoppingToken)) {
                        try {
                            // 在此解析/统计心跳；保持轻量
                            _heartbeatSequence++;
                            // 使用采样记录心跳（每100次记录一次，避免日志泛滥）
                            if (_logSampler.ShouldLog("Heartbeat", 100)) {
                                _log.HeartbeatReceived(mem.Length, _heartbeatSequence);
                            }
                        }
                        catch (Exception ex) {
                            // 异常隔离：单个心跳包处理失败不影响其他心跳包
                            _log.HeartbeatProcessingFailed(ex, mem.Length);
                        }
                    }
                }
                catch (OperationCanceledException) {
                    // 正常停止，不记录错误
                }
                catch (Exception ex) {
                    _log.HeartbeatListenerException(ex);
                }
            }
        }
    }
}