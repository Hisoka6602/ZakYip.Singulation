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
        /// 仅当端口 > 0 时才创建传输实例。
        /// </summary>
        public static IServiceCollection AddUpstreamTcpFromLiteDb(this IServiceCollection services) {
            // 注意：确保在此之前已经调用了 AddUpstreamFromLiteDb(...) 注册 IUpstreamOptionsStore

            // 注册传输管理器（单例）
            services.AddSingleton<UpstreamTransportManager>();

            // 注册一个包装器，提供所有传输作为 IEnumerable
            services.AddSingleton<IEnumerable<IByteTransport>>(sp => {
                var manager = sp.GetRequiredService<UpstreamTransportManager>();
                return manager.GetAllTransports().ToList();
            });

            return services;
        }
    }
}