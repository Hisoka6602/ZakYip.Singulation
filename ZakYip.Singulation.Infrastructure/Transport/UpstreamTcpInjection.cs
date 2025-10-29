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

            // ---- speed ----
            services.AddSingleton<IByteTransport>(sp => {
                var store = sp.GetRequiredService<IUpstreamOptionsStore>();
                var options = store.GetAsync().GetAwaiter().GetResult();
                
                if (options.SpeedPort <= 0) {
                    return null!;
                }
                
                return options.Role == TransportRole.Server
                    ? new TouchServerByteTransport(new TcpServerOptions {
                        Address = IPAddress.Any,
                        Port = options.SpeedPort,
                    })
                    : new TouchClientByteTransport(new TcpClientOptions {
                        Host = options.Host,
                        Port = options.SpeedPort
                    });
            });

            // ---- position ----
            services.AddSingleton<IByteTransport>(sp => {
                var store = sp.GetRequiredService<IUpstreamOptionsStore>();
                var options = store.GetAsync().GetAwaiter().GetResult();
                
                if (options.PositionPort <= 0) {
                    return null!;
                }
                
                return options.Role == TransportRole.Server
                    ? new TouchServerByteTransport(new TcpServerOptions {
                        Address = IPAddress.Any,
                        Port = options.PositionPort,
                    })
                    : new TouchClientByteTransport(new TcpClientOptions {
                        Host = options.Host,
                        Port = options.PositionPort
                    });
            });

            // ---- heartbeat ----
            services.AddSingleton<IByteTransport>(sp => {
                var store = sp.GetRequiredService<IUpstreamOptionsStore>();
                var options = store.GetAsync().GetAwaiter().GetResult();
                
                if (options.HeartbeatPort <= 0) {
                    return null!;
                }
                
                return options.Role == TransportRole.Server
                    ? new TouchServerByteTransport(new TcpServerOptions {
                        Address = IPAddress.Any,
                        Port = options.HeartbeatPort,
                    })
                    : new TouchClientByteTransport(new TcpClientOptions {
                        Host = options.Host,
                        Port = options.HeartbeatPort
                    });
            });

            return services;
        }
    }
}