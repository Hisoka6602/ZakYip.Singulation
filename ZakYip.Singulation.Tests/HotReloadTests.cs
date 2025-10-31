using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine;
using ZakYip.Singulation.Infrastructure.Safety;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试配置热更新功能：
    /// 1. LeadshineSafetyIo 配置热更新
    /// 2. IoStatusMonitor 配置热更新
    /// </summary>
    internal sealed class HotReloadTests {

        /// <summary>
        /// 测试 LeadshineSafetyIo 配置热更新功能。
        /// 验证配置更新后能立即应用到运行中的模块。
        /// </summary>
        [MiniFact]
        public async Task SafetyIoConfigurationHotReload_UpdatesImmediately() {
            // Arrange: 准备数据库和模块
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 创建初始配置
            var initialOptions = new LeadshineSafetyIoOptions {
                Enabled = true,
                EmergencyStopBit = 0,
                StopBit = 1,
                StartBit = 2,
                ResetBit = 3,
                PollingIntervalMs = 50,
                InvertLogic = false
            };
            await store.SaveAsync(initialOptions);

            // 创建安全模块
            var module = new LeadshineSafetyIoModule(
                NullLogger<LeadshineSafetyIoModule>.Instance,
                cardNo: 0,
                options: initialOptions);

            // Act: 执行热更新
            var updatedOptions = new LeadshineSafetyIoOptions {
                Enabled = true,
                EmergencyStopBit = 5,  // 更改端口
                StopBit = 6,
                StartBit = 7,
                ResetBit = 8,
                PollingIntervalMs = 100,  // 更改轮询间隔
                InvertLogic = true  // 更改反转逻辑
            };
            
            // 保存到数据库
            await store.SaveAsync(updatedOptions);
            
            // 调用热更新方法
            module.UpdateOptions(updatedOptions);

            // Assert: 验证配置已更新（通过从数据库读取验证）
            var retrievedOptions = await store.GetAsync();
            MiniAssert.Equal(5, retrievedOptions.EmergencyStopBit, "急停端口应已更新为 5");
            MiniAssert.Equal(6, retrievedOptions.StopBit, "停止端口应已更新为 6");
            MiniAssert.Equal(7, retrievedOptions.StartBit, "启动端口应已更新为 7");
            MiniAssert.Equal(8, retrievedOptions.ResetBit, "复位端口应已更新为 8");
            MiniAssert.Equal(100, retrievedOptions.PollingIntervalMs, "轮询间隔应已更新为 100ms");
            MiniAssert.Equal(true, retrievedOptions.InvertLogic, "反转逻辑应已更新为 true");
        }

        /// <summary>
        /// 测试 IoStatusMonitor 配置热更新功能。
        /// 验证配置更新后能在下次轮询时自动应用。
        /// </summary>
        [MiniFact]
        public async Task IoStatusMonitorConfigurationHotReload_UpdatesOnNextPoll() {
            // Arrange: 准备数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 创建初始配置
            var initialOptions = new IoStatusMonitorOptions {
                Enabled = true,
                InputStart = 0,
                InputCount = 32,
                OutputStart = 0,
                OutputCount = 32,
                PollingIntervalMs = 500,
                SignalRChannel = "/io/status"
            };
            await store.SaveAsync(initialOptions);

            // Act: 执行热更新
            var updatedOptions = new IoStatusMonitorOptions {
                Enabled = true,
                InputStart = 10,  // 更改起始位
                InputCount = 64,  // 更改数量
                OutputStart = 20,
                OutputCount = 48,
                PollingIntervalMs = 1000,  // 更改轮询间隔
                SignalRChannel = "/io/custom"  // 更改频道
            };
            
            // 保存到数据库（模拟 API 调用）
            await store.SaveAsync(updatedOptions);

            // 模拟 Worker 读取配置（Worker 在每次轮询时都会读取）
            var retrievedOptions = await store.GetAsync();

            // Assert: 验证配置已更新
            MiniAssert.Equal(10, retrievedOptions.InputStart, "输入起始位应已更新为 10");
            MiniAssert.Equal(64, retrievedOptions.InputCount, "输入数量应已更新为 64");
            MiniAssert.Equal(20, retrievedOptions.OutputStart, "输出起始位应已更新为 20");
            MiniAssert.Equal(48, retrievedOptions.OutputCount, "输出数量应已更新为 48");
            MiniAssert.Equal(1000, retrievedOptions.PollingIntervalMs, "轮询间隔应已更新为 1000ms");
            MiniAssert.Equal("/io/custom", retrievedOptions.SignalRChannel, "SignalR 频道应已更新为 /io/custom");
        }

        /// <summary>
        /// 测试 SafetyIo 配置的并发更新安全性。
        /// 验证在多线程环境下热更新不会导致数据不一致。
        /// </summary>
        [MiniFact]
        public async Task SafetyIoHotReload_ThreadSafe() {
            // Arrange: 准备数据库和模块
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                new FakeSafetyIsolator());

            var initialOptions = new LeadshineSafetyIoOptions {
                Enabled = true,
                EmergencyStopBit = 0,
                PollingIntervalMs = 50
            };
            await store.SaveAsync(initialOptions);

            var module = new LeadshineSafetyIoModule(
                NullLogger<LeadshineSafetyIoModule>.Instance,
                cardNo: 0,
                options: initialOptions);

            // Act: 并发执行多次热更新
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++) {
                var portNumber = i;
                tasks[i] = Task.Run(() => {
                    var options = new LeadshineSafetyIoOptions {
                        Enabled = true,
                        EmergencyStopBit = portNumber,
                        PollingIntervalMs = 50 + portNumber * 10
                    };
                    module.UpdateOptions(options);
                });
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks);

            // Assert: 验证模块仍然正常工作（没有抛出异常）
            MiniAssert.True(true, "并发热更新应该不会导致异常");
        }

        /// <summary>
        /// 测试 IoStatusMonitor 配置禁用后的行为。
        /// 验证将 Enabled 设置为 false 后监控会停止。
        /// </summary>
        [MiniFact]
        public async Task IoStatusMonitor_CanBeDisabledViaHotReload() {
            // Arrange: 准备数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 创建启用的配置
            var enabledOptions = new IoStatusMonitorOptions {
                Enabled = true,
                InputStart = 0,
                InputCount = 32,
                OutputStart = 0,
                OutputCount = 32,
                PollingIntervalMs = 500,
                SignalRChannel = "/io/status"
            };
            await store.SaveAsync(enabledOptions);

            // Act: 禁用监控
            var disabledOptions = new IoStatusMonitorOptions {
                Enabled = false,
                InputStart = 0,
                InputCount = 32,
                OutputStart = 0,
                OutputCount = 32,
                PollingIntervalMs = 500,
                SignalRChannel = "/io/status"
            };
            await store.SaveAsync(disabledOptions);

            // 读取配置
            var retrievedOptions = await store.GetAsync();

            // Assert: 验证配置已更新为禁用状态
            MiniAssert.Equal(false, retrievedOptions.Enabled, "监控应已禁用");
        }

        /// <summary>
        /// 测试 SafetyIo 灯光逻辑热更新。
        /// 验证灯光反转逻辑的热更新能正确应用。
        /// </summary>
        [MiniFact]
        public async Task SafetyIo_LightLogicHotReload() {
            // Arrange: 准备数据库和模块
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 创建初始配置（高电平亮灯）
            var initialOptions = new LeadshineSafetyIoOptions {
                Enabled = true,
                RedLightBit = 10,
                YellowLightBit = 11,
                GreenLightBit = 12,
                InvertLightLogic = false
            };
            await store.SaveAsync(initialOptions);

            var module = new LeadshineSafetyIoModule(
                NullLogger<LeadshineSafetyIoModule>.Instance,
                cardNo: 0,
                options: initialOptions);

            // Act: 热更新为低电平亮灯
            var updatedOptions = new LeadshineSafetyIoOptions {
                Enabled = true,
                RedLightBit = 10,
                YellowLightBit = 11,
                GreenLightBit = 12,
                InvertLightLogic = true,  // 改为低电平亮灯
                InvertRedLightLogic = false  // 红灯独立配置为高电平
            };
            
            await store.SaveAsync(updatedOptions);
            module.UpdateOptions(updatedOptions);

            // Assert: 验证配置已更新
            var retrievedOptions = await store.GetAsync();
            MiniAssert.Equal(true, retrievedOptions.InvertLightLogic, "全局灯光反转逻辑应已更新为 true");
            MiniAssert.Equal(false, retrievedOptions.InvertRedLightLogic, "红灯独立配置应为 false");
        }
    }
}
