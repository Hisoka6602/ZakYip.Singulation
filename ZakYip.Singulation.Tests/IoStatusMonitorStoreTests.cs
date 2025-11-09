using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 IoStatusMonitor 配置持久化存储功能。
    /// </summary>
    internal sealed class IoStatusMonitorStoreTests {

        [MiniFact]
        public async Task CanSaveAndRetrieveConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建测试配置
            var options = new IoStatusMonitorOptions {
                Enabled = false,
                InputStart = 10,
                InputCount = 16,
                OutputStart = 20,
                OutputCount = 8,
                PollingIntervalMs = 1000,
                SignalRChannel = "/custom/io"
            };

            // 保存配置
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(false, retrieved.Enabled, "Enabled 应该为 false");
            MiniAssert.Equal(10, retrieved.InputStart, "InputStart 应该为 10");
            MiniAssert.Equal(16, retrieved.InputCount, "InputCount 应该为 16");
            MiniAssert.Equal(20, retrieved.OutputStart, "OutputStart 应该为 20");
            MiniAssert.Equal(8, retrieved.OutputCount, "OutputCount 应该为 8");
            MiniAssert.Equal(1000, retrieved.PollingIntervalMs, "PollingIntervalMs 应该为 1000");
            MiniAssert.Equal("/custom/io", retrieved.SignalRChannel, "SignalRChannel 应该为 /custom/io");
        }

        [MiniFact]
        public async Task ReturnsDefaultWhenNotFound() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证默认值
            MiniAssert.Equal(true, retrieved.Enabled, "默认 Enabled 应该为 true");
            MiniAssert.Equal(0, retrieved.InputStart, "默认 InputStart 应该为 0");
            MiniAssert.Equal(32, retrieved.InputCount, "默认 InputCount 应该为 32");
            MiniAssert.Equal(0, retrieved.OutputStart, "默认 OutputStart 应该为 0");
            MiniAssert.Equal(32, retrieved.OutputCount, "默认 OutputCount 应该为 32");
            MiniAssert.Equal(500, retrieved.PollingIntervalMs, "默认 PollingIntervalMs 应该为 500");
            MiniAssert.Equal("/io/status", retrieved.SignalRChannel, "默认 SignalRChannel 应该为 /io/status");
        }

        [MiniFact]
        public async Task CanUpdateExistingConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建初始配置
            var options1 = new IoStatusMonitorOptions {
                Enabled = true,
                InputStart = 0,
                InputCount = 32
            };
            await store.SaveAsync(options1);

            // 更新配置
            var options2 = new IoStatusMonitorOptions {
                Enabled = false,
                InputStart = 10,
                InputCount = 64,
                PollingIntervalMs = 2000
            };
            await store.SaveAsync(options2);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证更新后的值
            MiniAssert.Equal(false, retrieved.Enabled, "更新后 Enabled 应该为 false");
            MiniAssert.Equal(10, retrieved.InputStart, "更新后 InputStart 应该为 10");
            MiniAssert.Equal(64, retrieved.InputCount, "更新后 InputCount 应该为 64");
            MiniAssert.Equal(2000, retrieved.PollingIntervalMs, "更新后 PollingIntervalMs 应该为 2000");
        }

        [MiniFact]
        public async Task CanDeleteConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建配置
            var options = new IoStatusMonitorOptions {
                Enabled = false,
                InputStart = 10,
                InputCount = 16
            };
            await store.SaveAsync(options);

            // 删除配置
            await store.DeleteAsync();

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证恢复到默认值
            MiniAssert.Equal(true, retrieved.Enabled, "删除后 Enabled 应该为 true（默认值）");
            MiniAssert.Equal(0, retrieved.InputStart, "删除后 InputStart 应该为 0（默认值）");
            MiniAssert.Equal(32, retrieved.InputCount, "删除后 InputCount 应该为 32（默认值）");
        }

        [MiniFact]
        public async Task HighVolumeConfigurationPersists() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建高容量配置
            var options = new IoStatusMonitorOptions {
                Enabled = true,
                InputStart = 0,
                InputCount = 512,
                OutputStart = 0,
                OutputCount = 512,
                PollingIntervalMs = 1000,
                SignalRChannel = "/io/high-volume"
            };
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(512, retrieved.InputCount, "高容量 InputCount 应该正确保存");
            MiniAssert.Equal(512, retrieved.OutputCount, "高容量 OutputCount 应该正确保存");
            MiniAssert.Equal("/io/high-volume", retrieved.SignalRChannel, "自定义频道应该正确保存");
        }
    }
}
