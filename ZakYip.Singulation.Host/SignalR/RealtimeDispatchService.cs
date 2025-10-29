using Microsoft.Extensions.Hosting;
﻿using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
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

        /// <summary>
        /// 初始化 <see cref="RealtimeDispatchService"/> 类的新实例。
        /// </summary>
        /// <param name="chan">事件队列通道。</param>
        /// <param name="hub">SignalR 事件中心上下文。</param>
        /// <param name="logger">日志记录器。</param>
        public RealtimeDispatchService(
            Channel<SignalRQueueItem> chan,
            IHubContext<EventsHub> hub,
            ILogger<RealtimeDispatchService> logger) { _chan = chan; _hub = hub; _logger = logger; }

        /// <summary>
        /// 执行后台服务，持续从队列中读取事件并分发到相应频道。
        /// </summary>
        /// <param name="stoppingToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await foreach (var item in _chan.Reader.ReadAllAsync(stoppingToken)) {
                try {
                    // 为每个频道维护序列号
                    var seq = _seq.AddOrUpdate(item.Channel, 1, (_, old) => old + 1);
                    var envelope = new {
                        v = 1,
                        type = item.Payload.GetType().Name,
                        ts = DateTimeOffset.Now,
                        channel = item.Channel,
                        data = item.Payload,
                        traceId = Activity.Current?.Id,
                        seq
                    };
                    await _hub.Clients.Group(item.Channel).SendAsync("event", envelope, stoppingToken);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "SignalR broadcast failed for {Channel}", item.Channel);
                }
            }
        }
    }
}