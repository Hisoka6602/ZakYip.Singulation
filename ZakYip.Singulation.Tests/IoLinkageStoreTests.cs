using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 IoLinkage 配置持久化存储功能。
    /// </summary>
    internal sealed class IoLinkageStoreTests {

        [MiniFact]
        public async Task CanSaveAndRetrieveConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoLinkageOptionsStore(
                db,
                NullLogger<LiteDbIoLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator());

            // 创建测试配置
            var options = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveHigh },
                    new IoLinkagePoint { BitNumber = 5, Level = TriggerLevel.ActiveHigh },
                    new IoLinkagePoint { BitNumber = 6, Level = TriggerLevel.ActiveHigh }
                },
                StoppedStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveLow },
                    new IoLinkagePoint { BitNumber = 5, Level = TriggerLevel.ActiveLow },
                    new IoLinkagePoint { BitNumber = 6, Level = TriggerLevel.ActiveLow }
                }
            };

            // 保存配置
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(true, retrieved.Enabled, "Enabled 应该为 true");
            MiniAssert.Equal(3, retrieved.RunningStateIos.Count, "RunningStateIos 应该有 3 个元素");
            MiniAssert.Equal(3, retrieved.StoppedStateIos.Count, "StoppedStateIos 应该有 3 个元素");
            
            // 验证运行中状态的 IO
            MiniAssert.Equal(3, retrieved.RunningStateIos[0].BitNumber, "第一个运行状态 IO 位号应该为 3");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, retrieved.RunningStateIos[0].Level, "第一个运行状态 IO 应该为高电平");
            MiniAssert.Equal(5, retrieved.RunningStateIos[1].BitNumber, "第二个运行状态 IO 位号应该为 5");
            MiniAssert.Equal(6, retrieved.RunningStateIos[2].BitNumber, "第三个运行状态 IO 位号应该为 6");
            
            // 验证停止状态的 IO
            MiniAssert.Equal(3, retrieved.StoppedStateIos[0].BitNumber, "第一个停止状态 IO 位号应该为 3");
            MiniAssert.Equal(TriggerLevel.ActiveLow, retrieved.StoppedStateIos[0].Level, "第一个停止状态 IO 应该为低电平");
        }

        [MiniFact]
        public async Task ReturnsDefaultWhenNotFound() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoLinkageOptionsStore(
                db,
                NullLogger<LiteDbIoLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator());

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证默认值
            MiniAssert.Equal(true, retrieved.Enabled, "默认 Enabled 应该为 true");
            MiniAssert.Equal(0, retrieved.RunningStateIos.Count, "默认 RunningStateIos 应该为空");
            MiniAssert.Equal(0, retrieved.StoppedStateIos.Count, "默认 StoppedStateIos 应该为空");
        }

        [MiniFact]
        public async Task CanUpdateExistingConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoLinkageOptionsStore(
                db,
                NullLogger<LiteDbIoLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator());

            // 创建初始配置
            var options1 = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveHigh }
                }
            };
            await store.SaveAsync(options1);

            // 更新配置
            var options2 = new IoLinkageOptions {
                Enabled = false,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 7, Level = TriggerLevel.ActiveLow },
                    new IoLinkagePoint { BitNumber = 8, Level = TriggerLevel.ActiveHigh }
                },
                StoppedStateIos = new() {
                    new IoLinkagePoint { BitNumber = 9, Level = TriggerLevel.ActiveLow }
                }
            };
            await store.SaveAsync(options2);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证更新后的值
            MiniAssert.Equal(false, retrieved.Enabled, "更新后 Enabled 应该为 false");
            MiniAssert.Equal(2, retrieved.RunningStateIos.Count, "更新后 RunningStateIos 应该有 2 个元素");
            MiniAssert.Equal(1, retrieved.StoppedStateIos.Count, "更新后 StoppedStateIos 应该有 1 个元素");
            MiniAssert.Equal(7, retrieved.RunningStateIos[0].BitNumber, "第一个运行状态 IO 位号应该为 7");
            MiniAssert.Equal(9, retrieved.StoppedStateIos[0].BitNumber, "停止状态 IO 位号应该为 9");
        }

        [MiniFact]
        public async Task CanDeleteConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoLinkageOptionsStore(
                db,
                NullLogger<LiteDbIoLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator());

            // 创建配置
            var options = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveHigh }
                }
            };
            await store.SaveAsync(options);

            // 删除配置
            await store.DeleteAsync();

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证恢复到默认值
            MiniAssert.Equal(true, retrieved.Enabled, "删除后 Enabled 应该为 true（默认值）");
            MiniAssert.Equal(0, retrieved.RunningStateIos.Count, "删除后 RunningStateIos 应该为空（默认值）");
            MiniAssert.Equal(0, retrieved.StoppedStateIos.Count, "删除后 StoppedStateIos 应该为空（默认值）");
        }

        [MiniFact]
        public async Task CanHandleEmptyLists() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoLinkageOptionsStore(
                db,
                NullLogger<LiteDbIoLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator());

            // 创建空列表配置
            var options = new IoLinkageOptions {
                Enabled = false,
                RunningStateIos = new(),
                StoppedStateIos = new()
            };
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(false, retrieved.Enabled, "Enabled 应该为 false");
            MiniAssert.Equal(0, retrieved.RunningStateIos.Count, "RunningStateIos 应该为空");
            MiniAssert.Equal(0, retrieved.StoppedStateIos.Count, "StoppedStateIos 应该为空");
        }

        [MiniFact]
        public async Task CanHandleManyIoPoints() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var store = new LiteDbIoLinkageOptionsStore(
                db,
                NullLogger<LiteDbIoLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator());

            // 创建多 IO 点配置
            var options = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = Enumerable.Range(0, 20)
                    .Select(i => new IoLinkagePoint { 
                        BitNumber = i, 
                        Level = i % 2 == 0 ? TriggerLevel.ActiveHigh : TriggerLevel.ActiveLow 
                    })
                    .ToList(),
                StoppedStateIos = Enumerable.Range(20, 15)
                    .Select(i => new IoLinkagePoint { 
                        BitNumber = i, 
                        Level = TriggerLevel.ActiveLow 
                    })
                    .ToList()
            };
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(20, retrieved.RunningStateIos.Count, "RunningStateIos 应该有 20 个元素");
            MiniAssert.Equal(15, retrieved.StoppedStateIos.Count, "StoppedStateIos 应该有 15 个元素");
            MiniAssert.Equal(0, retrieved.RunningStateIos[0].BitNumber, "第一个 IO 位号应该为 0");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, retrieved.RunningStateIos[0].Level, "第一个 IO 应该为高电平");
            MiniAssert.Equal(20, retrieved.StoppedStateIos[0].BitNumber, "停止状态第一个 IO 位号应该为 20");
        }
    }
}
