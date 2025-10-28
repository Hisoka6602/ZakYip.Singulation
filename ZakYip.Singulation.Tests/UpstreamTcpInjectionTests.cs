using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Infrastructure.Transport;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Safety;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试上游 TCP 传输注入是否正确注册所有三个传输通道。
    /// </summary>
    internal static class UpstreamTcpInjectionTests {

        [MiniFact]
        public static async Task AllThreeTransportsAreRegistered() {
            // Arrange: 构建包含所有必要依赖的服务容器
            var services = new ServiceCollection();
            
            // 注册 LiteDB 和持久化存储
            services.AddLiteDbAxisSettings("test_upstream.db");
            services.AddUpstreamFromLiteDb("test_upstream.db");
            
            // 注册安全隔离器（LiteDbUpstreamOptionsStore 的依赖）
            services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
            
            // 调用被测试的方法
            services.AddUpstreamTcpFromLiteDb();

            // 构建服务提供者
            var sp = services.BuildServiceProvider();

            // Act & Assert: 验证三个传输通道都能成功解析
            var speedTransport = sp.GetKeyedService<IByteTransport>("speed");
            MiniAssert.True(speedTransport is not null, "speed transport should be registered");

            var positionTransport = sp.GetKeyedService<IByteTransport>("position");
            MiniAssert.True(positionTransport is not null, "position transport should be registered");

            var heartbeatTransport = sp.GetKeyedService<IByteTransport>("heartbeat");
            MiniAssert.True(heartbeatTransport is not null, "heartbeat transport should be registered");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task TransportUsesCorrectConfiguration() {
            // Arrange: 构建服务容器并设置自定义配置
            var services = new ServiceCollection();
            services.AddLiteDbAxisSettings("test_upstream_config.db");
            services.AddUpstreamFromLiteDb("test_upstream_config.db");
            services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
            services.AddUpstreamTcpFromLiteDb();

            var sp = services.BuildServiceProvider();

            // 设置自定义端口配置
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var customConfig = new UpstreamOptions {
                Host = "192.168.1.100",
                SpeedPort = 6001,
                PositionPort = 6002,
                HeartbeatPort = 6003,
                Role = TransportRole.Client
            };
            await store.SaveAsync(customConfig);

            // Act: 解析传输（这将使用刚保存的配置）
            var speedTransport = sp.GetKeyedService<IByteTransport>("speed");
            
            // Assert: 验证传输确实被创建
            MiniAssert.True(speedTransport is not null, "speed transport should be created with custom config");
            MiniAssert.Equal(6001, speedTransport.RemotePort, "speed transport should use configured port");
            MiniAssert.Equal(false, speedTransport.IsServer, "speed transport should be client mode");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task ServerModeTransportsAreCreated() {
            // Arrange: 测试服务器模式
            var services = new ServiceCollection();
            services.AddLiteDbAxisSettings("test_upstream_server.db");
            services.AddUpstreamFromLiteDb("test_upstream_server.db");
            services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
            services.AddUpstreamTcpFromLiteDb();

            var sp = services.BuildServiceProvider();

            // 设置为服务器模式
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var serverConfig = new UpstreamOptions {
                Role = TransportRole.Server,
                SpeedPort = 7001,
                PositionPort = 7002,
                HeartbeatPort = 7003
            };
            await store.SaveAsync(serverConfig);

            // Act: 解析传输
            var speedTransport = sp.GetKeyedService<IByteTransport>("speed");
            
            // Assert: 验证传输是服务器模式
            MiniAssert.True(speedTransport is not null, "speed transport should be created in server mode");
            MiniAssert.Equal(true, speedTransport.IsServer, "speed transport should be server mode");
            MiniAssert.Equal(7001, speedTransport.RemotePort, "speed transport should use configured port");

            // 清理
            await sp.DisposeAsync();
        }
    }
}
