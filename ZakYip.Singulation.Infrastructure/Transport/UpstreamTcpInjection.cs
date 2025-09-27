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
        /// 并仅在端口 > 0 时注册对应的三路 IByteTransport（speed / position / heartbeat）。
        /// 仅负责注册，不启动。
        /// </summary>
        public static IServiceCollection AddUpstreamTcpFromLiteDb(this IServiceCollection services) {
            // 注意：确保你在此之前已经调用了 AddUpstreamFromLiteDb(...) 注册 IUpstreamOptionsStore
            using var temp = services.BuildServiceProvider();
            var store = temp.GetRequiredService<IUpstreamOptionsStore>();
            var dto = store.GetAsync().GetAwaiter().GetResult() ?? new UpstreamOptions();

            // ---- speed ----
            if (dto.SpeedPort > 0) {
                services.AddKeyedSingleton<IByteTransport>("speed", (IServiceProvider sp, object key) => {
                    var cur = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult() ?? dto;
                    return cur.Role == TransportRole.Server
                        ? new TouchServerByteTransport(new TcpServerOptions {
                            Address = IPAddress.Any,
                            Port = cur.SpeedPort,
                        })
                        : new TouchClientByteTransport(new TcpClientOptions {
                            Host = cur.Host,
                            Port = cur.SpeedPort
                        });
                });
            }

            // ---- position ----
            if (dto.PositionPort > 0) {
                services.AddKeyedSingleton<IByteTransport>("position", (IServiceProvider sp, object key) => {
                    var cur = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult() ?? dto;
                    return cur.Role == TransportRole.Server
                        ? new TouchServerByteTransport(new TcpServerOptions {
                            Address = IPAddress.Any,
                            Port = cur.PositionPort,
                        })
                        : new TouchClientByteTransport(new TcpClientOptions {
                            Host = cur.Host,
                            Port = cur.PositionPort
                        });
                });
            }

            // ---- heartbeat ----
            if (dto.HeartbeatPort > 0) {
                services.AddKeyedSingleton<IByteTransport>("heartbeat", (IServiceProvider sp, object key) => {
                    var cur = sp.GetRequiredService<IUpstreamOptionsStore>().GetAsync().GetAwaiter().GetResult() ?? dto;
                    return cur.Role == TransportRole.Server
                        ? new TouchServerByteTransport(new TcpServerOptions {
                            Address = IPAddress.Any,
                            Port = cur.HeartbeatPort,
                        })
                        : new TouchClientByteTransport(new TcpClientOptions {
                            Host = cur.Host,
                            Port = cur.HeartbeatPort
                        });
                });
            }

            return services;
        }
    }
}