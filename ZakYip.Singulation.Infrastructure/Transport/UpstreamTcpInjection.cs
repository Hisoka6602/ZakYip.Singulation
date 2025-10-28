using System.Net;
using Microsoft.Extensions.Hosting;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Transport.Tcp;
using ZakYip.Singulation.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Transport.Abstractions;
using ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport;
using ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport;

namespace ZakYip.Singulation.Infrastructure.Transport {

    public static class UpstreamTcpInjection {

        /// <summary>
        /// 从 LiteDB 读取单文档配置，按 Role 选择 Client/Server，
        /// 并注册对应的三路 IByteTransport（speed / position / heartbeat）。
        /// 支持配置热更新：当配置变更时，自动重新创建连接。
        /// </summary>
        public static IServiceCollection AddUpstreamTcpFromLiteDb(this IServiceCollection services) {
            // 注意：确保在此之前已经调用了 AddUpstreamFromLiteDb(...) 注册 IUpstreamOptionsStore

            // 注册传输管理器（单例）
            services.AddSingleton<UpstreamTransportManager>();

            // ---- speed ----
            services.AddKeyedSingleton<IByteTransport>("speed", (IServiceProvider sp, object? key) => {
                var manager = sp.GetRequiredService<UpstreamTransportManager>();
                return manager.SpeedTransport 
                    ?? throw new InvalidOperationException("Speed transport not initialized. Call InitializeAsync on UpstreamTransportManager first.");
            });

            // ---- position ----
            services.AddKeyedSingleton<IByteTransport>("position", (IServiceProvider sp, object? key) => {
                var manager = sp.GetRequiredService<UpstreamTransportManager>();
                return manager.PositionTransport 
                    ?? throw new InvalidOperationException("Position transport not initialized. Call InitializeAsync on UpstreamTransportManager first.");
            });

            // ---- heartbeat ----
            services.AddKeyedSingleton<IByteTransport>("heartbeat", (IServiceProvider sp, object? key) => {
                var manager = sp.GetRequiredService<UpstreamTransportManager>();
                return manager.HeartbeatTransport 
                    ?? throw new InvalidOperationException("Heartbeat transport not initialized. Call InitializeAsync on UpstreamTransportManager first.");
            });

            return services;
        }
    }
}