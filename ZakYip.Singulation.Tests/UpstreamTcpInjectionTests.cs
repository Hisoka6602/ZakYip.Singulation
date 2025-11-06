using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Infrastructure.Transport;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Cabinet;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试上游 TCP 传输注入和热更新功能。
    /// </summary>
    internal static class UpstreamTcpInjectionTests {

        [MiniFact]
        public static async Task AllThreeTransportsAreRegistered() {
            // Arrange: 构建包含所有必要依赖的服务容器
            var services = new ServiceCollection();
            
            // 注册日志（UpstreamTransportManager 需要）
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            // 注册 LiteDB 和持久化存储
            services.AddLiteDbAxisSettings("test_upstream.db");
            services.AddUpstreamFromLiteDb("test_upstream.db");
            
            // 注册安全隔离器（LiteDbUpstreamOptionsStore 的依赖）
            services.AddSingleton<ICabinetIsolator, CabinetIsolator>();
            
            // 调用被测试的方法
            services.AddUpstreamTcpFromLiteDb();

            // 构建服务提供者
            var sp = services.BuildServiceProvider();

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

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
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddLiteDbAxisSettings("test_upstream_config.db");
            services.AddUpstreamFromLiteDb("test_upstream_config.db");
            services.AddSingleton<ICabinetIsolator, CabinetIsolator>();
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

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            // Act: 解析传输（这将使用刚保存的配置）
            var speedTransport = sp.GetKeyedService<IByteTransport>("speed");
            
            // Assert: 验证传输确实被创建
            MiniAssert.True(speedTransport is not null, "speed transport should be created with custom config");
            MiniAssert.Equal(6001, speedTransport!.RemotePort, "speed transport should use configured port");
            MiniAssert.Equal(false, speedTransport.IsServer, "speed transport should be client mode");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task ServerModeTransportsAreCreated() {
            // Arrange: 测试服务器模式
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddLiteDbAxisSettings("test_upstream_server.db");
            services.AddUpstreamFromLiteDb("test_upstream_server.db");
            services.AddSingleton<ICabinetIsolator, CabinetIsolator>();
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

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            // Act: 解析传输
            var speedTransport = sp.GetKeyedService<IByteTransport>("speed");
            
            // Assert: 验证传输是服务器模式
            MiniAssert.True(speedTransport is not null, "speed transport should be created in server mode");
            MiniAssert.Equal(true, speedTransport!.IsServer, "speed transport should be server mode");
            MiniAssert.Equal(7001, speedTransport.RemotePort, "speed transport should use configured port");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task HotUpdateChangesConfiguration() {
            // Arrange: 测试热更新功能
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddLiteDbAxisSettings("test_upstream_hotupdate.db");
            services.AddUpstreamFromLiteDb("test_upstream_hotupdate.db");
            services.AddSingleton<ICabinetIsolator, CabinetIsolator>();
            services.AddUpstreamTcpFromLiteDb();

            var sp = services.BuildServiceProvider();

            // 初始配置
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var initialConfig = new UpstreamOptions {
                Host = "127.0.0.1",
                SpeedPort = 5001,
                PositionPort = 5002,
                HeartbeatPort = 5003,
                Role = TransportRole.Client
            };
            await store.SaveAsync(initialConfig);

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            // 获取初始传输
            var initialSpeedTransport = sp.GetKeyedService<IByteTransport>("speed");
            MiniAssert.True(initialSpeedTransport is not null, "initial speed transport should exist");
            MiniAssert.Equal(5001, initialSpeedTransport!.RemotePort, "initial port should be 5001");

            // Act: 执行热更新
            var newConfig = new UpstreamOptions {
                Host = "192.168.1.200",
                SpeedPort = 8001,
                PositionPort = 8002,
                HeartbeatPort = 8003,
                Role = TransportRole.Client
            };
            await manager.ReloadTransportsAsync(newConfig, startImmediately: false);

            // Assert: 验证传输已更新
            var updatedSpeedTransport = sp.GetKeyedService<IByteTransport>("speed");
            MiniAssert.True(updatedSpeedTransport is not null, "updated speed transport should exist");
            MiniAssert.Equal(8001, updatedSpeedTransport!.RemotePort, "port should be updated to 8001");
            MiniAssert.Equal("192.168.1.200", updatedSpeedTransport.RemoteIp, "host should be updated to 192.168.1.200");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task HotUpdateSwitchesRoleFromClientToServer() {
            // Arrange: 测试从 Client 切换到 Server 模式
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddLiteDbAxisSettings("test_upstream_role_switch.db");
            services.AddUpstreamFromLiteDb("test_upstream_role_switch.db");
            services.AddSingleton<ICabinetIsolator, CabinetIsolator>();
            services.AddUpstreamTcpFromLiteDb();

            var sp = services.BuildServiceProvider();

            // 初始配置：Client 模式
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var clientConfig = new UpstreamOptions {
                Host = "127.0.0.1",
                SpeedPort = 5001,
                Role = TransportRole.Client
            };
            await store.SaveAsync(clientConfig);

            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            var initialTransport = sp.GetKeyedService<IByteTransport>("speed");
            MiniAssert.Equal(false, initialTransport?.IsServer ?? true, "should be client mode initially");

            // Act: 热更新为 Server 模式
            var serverConfig = new UpstreamOptions {
                SpeedPort = 9001,
                Role = TransportRole.Server
            };
            await manager.ReloadTransportsAsync(serverConfig, startImmediately: false);

            // Assert: 验证已切换为服务器模式
            var updatedTransport = sp.GetKeyedService<IByteTransport>("speed");
            MiniAssert.Equal(true, updatedTransport?.IsServer ?? false, "should be server mode after update");
            MiniAssert.Equal(9001, updatedTransport?.RemotePort ?? 0, "should use updated port");

            // 清理
            await sp.DisposeAsync();
        }
    }
}
