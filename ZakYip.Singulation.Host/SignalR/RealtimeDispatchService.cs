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

    public sealed class RealtimeDispatchService : BackgroundService {
        private readonly Channel<SignalRQueueItem> _chan;
        private readonly IHubContext<EventsHub> _hub;
        private readonly ILogger<RealtimeDispatchService> _logger;
        private readonly ConcurrentDictionary<string, long> _seq = new();

        public RealtimeDispatchService(
            Channel<SignalRQueueItem> chan,
            IHubContext<EventsHub> hub,
            ILogger<RealtimeDispatchService> logger) { _chan = chan; _hub = hub; _logger = logger; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await foreach (var item in _chan.Reader.ReadAllAsync(stoppingToken)) {
                try {
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