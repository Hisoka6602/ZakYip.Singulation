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

    /// <summary>
    /// SignalR 配置扩展方法，用于配置实时通信功能。
    /// </summary>
    /// <remarks>
    /// 此类提供了配置 SignalR 服务和端点映射的扩展方法，
    /// 包括超时设置、消息大小限制、通道配置和健康检查等。
    /// </remarks>
    public static class SignalRSetup {

        /// <summary>
        /// 添加分料系统的 SignalR 服务及相关依赖。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns>更新后的服务集合，支持链式调用。</returns>
        /// <remarks>
        /// 此方法配置：
        /// 1. SignalR 服务选项（超时、消息大小、并发等）
        /// 2. 有界通道用于消息队列，容量 50,000，采用丢弃最旧消息策略
        /// 3. 实时通知器和调度服务
        /// 4. SignalR 和速度联动的健康检查
        /// </remarks>
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

        /// <summary>
        /// 映射分料系统的 SignalR Hub 端点。
        /// </summary>
        /// <param name="app">应用程序构建器。</param>
        /// <returns>更新后的应用程序构建器，支持链式调用。</returns>
        /// <remarks>
        /// 此方法将 EventsHub 映射到 "/hubs/events" 端点，
        /// 客户端可以通过此端点连接到 SignalR 服务以接收实时事件通知。
        /// </remarks>
        public static IApplicationBuilder MapSingulationHubs(this IApplicationBuilder app)
            => app.UseEndpoints(e => e.MapHub<EventsHub>("/hubs/events"));
    }
}