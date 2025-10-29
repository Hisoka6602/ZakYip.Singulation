using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 LeadshineSafetyIo 配置持久化存储功能。
    /// </summary>
    internal sealed class LeadshineSafetyIoStoreTests {

        [MiniFact]
        public async Task CanSaveAndRetrieveConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 创建测试配置
            var options = new LeadshineSafetyIoOptions {
                Enabled = true,
                EmergencyStopBit = 0,
                StopBit = 1,
                StartBit = 2,
                ResetBit = 3,
                PollingIntervalMs = 100,
                InvertLogic = true,
                InvertEmergencyStopLogic = false,
                InvertStopLogic = null,
                InvertStartLogic = true,
                InvertResetLogic = null
            };

            // 保存配置
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(true, retrieved.Enabled, "Enabled 应该为 true");
            MiniAssert.Equal(0, retrieved.EmergencyStopBit, "EmergencyStopBit 应该为 0");
            MiniAssert.Equal(1, retrieved.StopBit, "StopBit 应该为 1");
            MiniAssert.Equal(2, retrieved.StartBit, "StartBit 应该为 2");
            MiniAssert.Equal(3, retrieved.ResetBit, "ResetBit 应该为 3");
            MiniAssert.Equal(100, retrieved.PollingIntervalMs, "PollingIntervalMs 应该为 100");
            MiniAssert.Equal(true, retrieved.InvertLogic, "InvertLogic 应该为 true");
            MiniAssert.Equal(false, retrieved.InvertEmergencyStopLogic, "InvertEmergencyStopLogic 应该为 false");
            MiniAssert.Equal(null, retrieved.InvertStopLogic, "InvertStopLogic 应该为 null");
            MiniAssert.Equal(true, retrieved.InvertStartLogic, "InvertStartLogic 应该为 true");
            MiniAssert.Equal(null, retrieved.InvertResetLogic, "InvertResetLogic 应该为 null");
        }

        [MiniFact]
        public async Task ReturnsDefaultWhenNotFound() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证默认值
            MiniAssert.Equal(false, retrieved.Enabled, "默认 Enabled 应该为 false");
            MiniAssert.Equal(-1, retrieved.EmergencyStopBit, "默认 EmergencyStopBit 应该为 -1");
            MiniAssert.Equal(-1, retrieved.StopBit, "默认 StopBit 应该为 -1");
            MiniAssert.Equal(-1, retrieved.StartBit, "默认 StartBit 应该为 -1");
            MiniAssert.Equal(-1, retrieved.ResetBit, "默认 ResetBit 应该为 -1");
            MiniAssert.Equal(50, retrieved.PollingIntervalMs, "默认 PollingIntervalMs 应该为 50");
            MiniAssert.Equal(false, retrieved.InvertLogic, "默认 InvertLogic 应该为 false");
        }

        [MiniFact]
        public async Task CanUpdateExistingConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 创建初始配置
            var options1 = new LeadshineSafetyIoOptions {
                Enabled = true,
                EmergencyStopBit = 0
            };
            await store.SaveAsync(options1);

            // 更新配置
            var options2 = new LeadshineSafetyIoOptions {
                Enabled = false,
                EmergencyStopBit = 5,
                PollingIntervalMs = 200
            };
            await store.SaveAsync(options2);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证更新后的值
            MiniAssert.Equal(false, retrieved.Enabled, "更新后 Enabled 应该为 false");
            MiniAssert.Equal(5, retrieved.EmergencyStopBit, "更新后 EmergencyStopBit 应该为 5");
            MiniAssert.Equal(200, retrieved.PollingIntervalMs, "更新后 PollingIntervalMs 应该为 200");
        }

        [MiniFact]
        public async Task CanDeleteConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                new FakeSafetyIsolator());

            // 创建配置
            var options = new LeadshineSafetyIoOptions {
                Enabled = true,
                EmergencyStopBit = 0
            };
            await store.SaveAsync(options);

            // 删除配置
            await store.DeleteAsync();

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证恢复到默认值
            MiniAssert.Equal(false, retrieved.Enabled, "删除后 Enabled 应该为 false（默认值）");
            MiniAssert.Equal(-1, retrieved.EmergencyStopBit, "删除后 EmergencyStopBit 应该为 -1（默认值）");
        }
    }
}
