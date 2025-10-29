using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Infrastructure.Transport;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Safety;
using ZakYip.Singulation.Host.Controllers;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 UpstreamController 是否能正确获取传输实例。
    /// </summary>
    internal static class UpstreamControllerTests {

        [MiniFact]
        public static async Task GetConnectionsAsync_ReturnsTransports_AfterInitialization() {
            // Arrange: 构建包含所有必要依赖的服务容器
            var services = new ServiceCollection();
            
            // 注册日志
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            // 注册 LiteDB 和持久化存储
            services.AddLiteDbAxisSettings("test_upstream_controller.db");
            services.AddUpstreamFromLiteDb("test_upstream_controller.db");
            
            // 注册安全隔离器
            services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
            
            // 注册上游传输
            services.AddUpstreamTcpFromLiteDb();

            // 构建服务提供者
            var sp = services.BuildServiceProvider();

            // 设置配置
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var config = new UpstreamOptions {
                Host = "127.0.0.1",
                SpeedPort = 5001,
                PositionPort = 5002,
                HeartbeatPort = 5003,
                Role = TransportRole.Client
            };
            await store.SaveAsync(config);

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            // 获取传输实例
            var transports = sp.GetRequiredService<IEnumerable<IByteTransport>>();

            // 创建 UpstreamController
            var logger = sp.GetRequiredService<ILogger<UpstreamController>>();
            var controller = new UpstreamController(logger, store, sp, transports, manager);

            // Act: 获取连接状态
            var response = await controller.GetConnectionsAsync(CancellationToken.None);

            // Assert: 验证响应包含传输信息
            MiniAssert.True(response.Result, "GetConnectionsAsync should succeed");
            MiniAssert.True(response.Data is not null, "Data should not be null");
            MiniAssert.True(response.Data.Items.Count == 3, $"Should have 3 transports, but got {response.Data.Items.Count}");
            MiniAssert.True(response.Data.Enabled, "Enabled should be true");

            // 验证每个传输的基本信息
            MiniAssert.True(response.Data.Items.Any(t => t.Port == 5001), "Should have speed transport on port 5001");
            MiniAssert.True(response.Data.Items.Any(t => t.Port == 5002), "Should have position transport on port 5002");
            MiniAssert.True(response.Data.Items.Any(t => t.Port == 5003), "Should have heartbeat transport on port 5003");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task Reconnect_WorksWithValidIndex() {
            // Arrange: 构建服务容器
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddLiteDbAxisSettings("test_upstream_controller_reconnect.db");
            services.AddUpstreamFromLiteDb("test_upstream_controller_reconnect.db");
            services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
            services.AddUpstreamTcpFromLiteDb();

            var sp = services.BuildServiceProvider();

            // 设置配置
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var config = new UpstreamOptions {
                Host = "127.0.0.1",
                SpeedPort = 5001,
                PositionPort = 5002,
                HeartbeatPort = 5003,
                Role = TransportRole.Client
            };
            await store.SaveAsync(config);

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            // 获取传输实例
            var transports = sp.GetRequiredService<IEnumerable<IByteTransport>>();

            // 创建 UpstreamController
            var logger = sp.GetRequiredService<ILogger<UpstreamController>>();
            var controller = new UpstreamController(logger, store, sp, transports, manager);

            // Act: 重连第一个传输（索引 0）
            var response = await controller.Reconnect(0, CancellationToken.None);

            // Assert: 验证重连请求成功
            MiniAssert.True(response.Result, "Reconnect should succeed");
            MiniAssert.Equal("reconnect", response.Data, "Response data should be 'reconnect'");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task Reconnect_ReturnsNotFound_WithInvalidIndex() {
            // Arrange: 构建服务容器
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddLiteDbAxisSettings("test_upstream_controller_notfound.db");
            services.AddUpstreamFromLiteDb("test_upstream_controller_notfound.db");
            services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
            services.AddUpstreamTcpFromLiteDb();

            var sp = services.BuildServiceProvider();

            // 设置配置
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var config = new UpstreamOptions {
                Host = "127.0.0.1",
                SpeedPort = 5001,
                PositionPort = 5002,
                HeartbeatPort = 5003,
                Role = TransportRole.Client
            };
            await store.SaveAsync(config);

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            // 获取传输实例
            var transports = sp.GetRequiredService<IEnumerable<IByteTransport>>();

            // 创建 UpstreamController
            var logger = sp.GetRequiredService<ILogger<UpstreamController>>();
            var controller = new UpstreamController(logger, store, sp, transports, manager);

            // Act: 尝试重连无效的索引
            var response = await controller.Reconnect(999, CancellationToken.None);

            // Assert: 验证返回 NotFound
            MiniAssert.False(response.Result, "Reconnect with invalid index should fail");

            // 清理
            await sp.DisposeAsync();
        }

        [MiniFact]
        public static async Task GetConnectionsAsync_SkipsInvalidPorts() {
            // Arrange: 构建服务容器
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            services.AddLiteDbAxisSettings("test_upstream_controller_invalid_ports.db");
            services.AddUpstreamFromLiteDb("test_upstream_controller_invalid_ports.db");
            services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
            services.AddUpstreamTcpFromLiteDb();

            var sp = services.BuildServiceProvider();

            // 设置配置：只有 SpeedPort 有效，其他端口 <= 0
            var store = sp.GetRequiredService<IUpstreamOptionsStore>();
            var config = new UpstreamOptions {
                Host = "127.0.0.1",
                SpeedPort = 5001,
                PositionPort = 0,        // 无效端口
                HeartbeatPort = -1,      // 无效端口
                Role = TransportRole.Client
            };
            await store.SaveAsync(config);

            // 初始化传输管理器
            var manager = sp.GetRequiredService<UpstreamTransportManager>();
            await manager.InitializeAsync();

            // 获取传输实例
            var transports = sp.GetRequiredService<IEnumerable<IByteTransport>>();

            // 创建 UpstreamController
            var logger = sp.GetRequiredService<ILogger<UpstreamController>>();
            var controller = new UpstreamController(logger, store, sp, transports, manager);

            // Act: 获取连接状态
            var response = await controller.GetConnectionsAsync(CancellationToken.None);

            // Assert: 验证只有一个传输（speed）
            MiniAssert.True(response.Result, "GetConnectionsAsync should succeed");
            MiniAssert.True(response.Data is not null, "Data should not be null");
            MiniAssert.True(response.Data.Items.Count == 1, $"Should have only 1 transport (speed), but got {response.Data.Items.Count}");
            MiniAssert.True(response.Data.Items.Any(t => t.Port == 5001), "Should have speed transport on port 5001");
            MiniAssert.False(response.Data.Items.Any(t => t.Port == 0), "Should not have transport on port 0");
            MiniAssert.False(response.Data.Items.Any(t => t.Port == -1), "Should not have transport on port -1");

            // 清理
            await sp.DisposeAsync();
        }
    }
}
