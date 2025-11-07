using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using ZakYip.Singulation.Infrastructure.Runtime;
using ZakYip.Singulation.Host.SignalR;
using ZakYip.Singulation.Host.SignalR.Hubs;
using ZakYip.Singulation.Core.Abstractions.Realtime;

namespace ZakYip.Singulation.Host.Extensions {

    public static class SignalRSetup {

        public static IServiceCollection AddSingulationSignalR(this IServiceCollection services) {
            services.AddSignalR(opt => {
                opt.HandshakeTimeout = TimeSpan.FromMinutes(1);
                opt.EnableDetailedErrors = true;
                opt.MaximumReceiveMessageSize = null;
                opt.KeepAliveInterval = TimeSpan.FromMinutes(1);
                opt.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
                opt.MaximumParallelInvocationsPerClient = 10;
                opt.StreamBufferCapacity = int.MaxValue;
            })
                .AddNewtonsoftJsonProtocol(); // 与全局 JSON 一致

            // 有界通道，防止堆积；策略：丢旧保新
            // 增加容量到 50,000 以支持高并发场景
            var chan = Channel.CreateBounded<SignalRQueueItem>(new BoundedChannelOptions(50_000) {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            services.AddSingleton(chan);

            // 抽象 → 实现
            services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
            // 后台出队广播
            services.AddSingleton<RealtimeDispatchService>();
            services.AddHostedService(sp => sp.GetRequiredService<RealtimeDispatchService>());

            // 健康检查
            services.AddHealthChecks()
                .AddCheck<SignalRHealthCheck>("signalr", tags: new[] { "realtime", "signalr" })
                .AddCheck<SpeedLinkageHealthCheck>("speed-linkage", tags: new[] { "background", "speed-linkage" });

            return services;
        }

        public static IApplicationBuilder MapSingulationHubs(this IApplicationBuilder app)
            => app.UseEndpoints(e => e.MapHub<EventsHub>("/hubs/events"));
    }
}