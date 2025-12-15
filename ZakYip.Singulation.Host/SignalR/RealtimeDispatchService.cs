using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
﻿using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Host.SignalR;
using ZakYip.Singulation.Host.SignalR.Hubs;

namespace ZakYip.Singulation.Host.SignalR {

    /// <summary>
    /// 实时事件分发服务，负责通过 SignalR 向客户端推送事件。
    /// </summary>
    public sealed class RealtimeDispatchService : BackgroundService {
        private readonly Channel<SignalRQueueItem> _chan;
        private readonly IHubContext<EventsHub> _hub;
        private readonly ILogger<RealtimeDispatchService> _logger;
        private readonly ConcurrentDictionary<string, long> _seq = new();
        private readonly ObjectPool<MessageEnvelope> _envelopePool;
        private readonly ISystemClock _clock;

        // 监控指标
        private long _messagesProcessed = 0;
        private long _messagesDropped = 0;
        private long _messagesFailed = 0;

        // 断路器状态
        private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();

        /// <summary>
        /// 初始化 <see cref="RealtimeDispatchService"/> 类的新实例。
        /// </summary>
        /// <param name="chan">事件队列通道。</param>
        /// <param name="hub">SignalR 事件中心上下文。</param>
        /// <param name="logger">日志记录器。</param>
        public RealtimeDispatchService(
            Channel<SignalRQueueItem> chan,
            IHubContext<EventsHub> hub,
            ILogger<RealtimeDispatchService> logger,
            ISystemClock clock) {
            _chan = chan;
            _hub = hub;
            _logger = logger;
            _clock = clock;
            _envelopePool = new DefaultObjectPool<MessageEnvelope>(new MessageEnvelopePoolPolicy());
        }

        /// <summary>
        /// 获取消息处理统计信息。
        /// </summary>
        public (long Processed, long Dropped, long Failed) GetStatistics()
            => (_messagesProcessed, _messagesDropped, _messagesFailed);

        /// <summary>
        /// 执行后台服务，持续从队列中读取事件并分发到相应频道。
        /// </summary>
        /// <param name="stoppingToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // 启动队列深度监控任务
            _ = Task.Run(() => MonitorQueueDepthAsync(stoppingToken), stoppingToken);

            await foreach (var item in _chan.Reader.ReadAllAsync(stoppingToken)) {
                try {
                    // 检查断路器状态
                    var breaker = _circuitBreakers.GetOrAdd(item.Channel, _ => new CircuitBreakerState(_clock));
                    if (breaker.IsOpen) {
                        if (breaker.ShouldAttemptReset()) {
                            _logger.LogInformation("Circuit breaker half-open for channel {Channel}, attempting reset", item.Channel);
                        } else {
                            _messagesDropped++;
                            continue; // 跳过此消息
                        }
                    }

                    // 为每个频道维护序列号
                    var seq = _seq.AddOrUpdate(item.Channel, 1, (_, old) => old + 1);

                    // 从对象池获取信封
                    var envelope = _envelopePool.Get();
                    try {
                        envelope.Version = 1;
                        envelope.Type = item.Payload.GetType().Name;
                        envelope.Timestamp = new DateTimeOffset(_clock.Now);
                        envelope.Channel = item.Channel;
                        envelope.Data = item.Payload;
                        envelope.TraceId = Activity.Current?.Id;
                        envelope.Sequence = seq;

                        await _hub.Clients.Group(item.Channel).SendAsync("event", envelope, stoppingToken);

                        _messagesProcessed++;
                        breaker.RecordSuccess();
                    }
                    finally {
                        // 归还到对象池
                        _envelopePool.Return(envelope);
                    }
                }
                catch (HubException hex) {
                    // Hub 级别错误
                    _logger.LogError(hex, "Hub error broadcasting to {Channel}", item.Channel);
                    _messagesFailed++;
                    RecordFailure(item.Channel);
                }
                catch (JsonException jex) {
                    // 序列化错误
                    _logger.LogError(jex, "Serialization error for {Channel}, Type: {Type}",
                        item.Channel, item.Payload.GetType().Name);
                    _messagesFailed++;
                    RecordFailure(item.Channel);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    // 正常关闭
                    break;
                }
                catch (Exception ex) {
                    // 其他未知错误
                    _logger.LogWarning(ex, "Unexpected error broadcasting to {Channel}", item.Channel);
                    _messagesFailed++;
                    RecordFailure(item.Channel);
                }
            }

            _logger.LogInformation(
                "RealtimeDispatchService stopped. Processed: {Processed}, Failed: {Failed}, Dropped: {Dropped}",
                _messagesProcessed, _messagesFailed, _messagesDropped);
        }

        /// <summary>
        /// 监控队列深度。
        /// </summary>
        private async Task MonitorQueueDepthAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                try {
                    var count = _chan.Reader.Count;
                    if (count > 40000) {
                        _logger.LogWarning(
                            "SignalR queue depth high: {Count}/50000, Processed: {Processed}, Failed: {Failed}, Dropped: {Dropped}",
                            count, _messagesProcessed, _messagesFailed, _messagesDropped);
                    }
                    await Task.Delay(10000, ct); // 每 10 秒检查一次
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error in queue depth monitoring");
                }
            }
        }

        /// <summary>
        /// 记录失败并更新断路器状态。
        /// </summary>
        private void RecordFailure(string channel) {
            if (_circuitBreakers.TryGetValue(channel, out var breaker)) {
                breaker.RecordFailure();
                if (breaker.IsOpen) {
                    _logger.LogWarning("Circuit breaker opened for channel {Channel} due to repeated failures", channel);
                }
            }
        }

        /// <summary>
        /// 断路器状态。
        /// </summary>
        private sealed class CircuitBreakerState {
            private const int FailureThreshold = 5;
            private const int TimeoutSeconds = 30;
            private int _failureCount = 0;
            private readonly ISystemClock _clock;
            private DateTime _lastFailureTime = DateTime.MinValue;
            private DateTime _openedTime = DateTime.MinValue;

            public CircuitBreakerState(ISystemClock clock) {
                _clock = clock;
            }

            public bool IsOpen => _failureCount >= FailureThreshold &&
                                  (_clock.UtcNow - _openedTime).TotalSeconds < TimeoutSeconds;

            public void RecordSuccess() {
                _failureCount = 0;
                _lastFailureTime = DateTime.MinValue;
                _openedTime = DateTime.MinValue;
            }

            public void RecordFailure() {
                _failureCount++;
                _lastFailureTime = _clock.UtcNow;
                if (_failureCount >= FailureThreshold) {
                    _openedTime = _clock.UtcNow;
                }
            }

            public bool ShouldAttemptReset() {
                return IsOpen && (_clock.UtcNow - _openedTime).TotalSeconds >= TimeoutSeconds;
            }
        }
    }
}