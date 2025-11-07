using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 SpeedLinkage 配置持久化存储功能。
    /// </summary>
    internal sealed class SpeedLinkageStoreTests {

        [MiniFact]
        public async Task CanSaveAndRetrieveConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbSpeedLinkageOptionsStore(
                db,
                NullLogger<LiteDbSpeedLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建测试配置
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001, 1002 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh },
                            new SpeedLinkageIoPoint { BitNumber = 4, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    },
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1003, 1004 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 5, LevelWhenStopped = TriggerLevel.ActiveHigh },
                            new SpeedLinkageIoPoint { BitNumber = 6, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };

            // 保存配置
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(true, retrieved.Enabled, "Enabled 应该为 true");
            MiniAssert.Equal(2, retrieved.LinkageGroups.Count, "LinkageGroups 应该有 2 个元素");
            
            // 验证第一个组
            MiniAssert.Equal(2, retrieved.LinkageGroups[0].AxisIds.Count, "第一个组应该有 2 个轴ID");
            MiniAssert.Equal(1001, retrieved.LinkageGroups[0].AxisIds[0], "第一个组第一个轴ID应该为 1001");
            MiniAssert.Equal(1002, retrieved.LinkageGroups[0].AxisIds[1], "第一个组第二个轴ID应该为 1002");
            MiniAssert.Equal(2, retrieved.LinkageGroups[0].IoPoints.Count, "第一个组应该有 2 个IO点");
            MiniAssert.Equal(3, retrieved.LinkageGroups[0].IoPoints[0].BitNumber, "第一个组第一个IO位号应该为 3");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, retrieved.LinkageGroups[0].IoPoints[0].LevelWhenStopped, "第一个组第一个IO应该为高电平");
            
            // 验证第二个组
            MiniAssert.Equal(2, retrieved.LinkageGroups[1].AxisIds.Count, "第二个组应该有 2 个轴ID");
            MiniAssert.Equal(1003, retrieved.LinkageGroups[1].AxisIds[0], "第二个组第一个轴ID应该为 1003");
            MiniAssert.Equal(5, retrieved.LinkageGroups[1].IoPoints[0].BitNumber, "第二个组第一个IO位号应该为 5");
        }

        [MiniFact]
        public async Task ReturnsDefaultWhenNotFound() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbSpeedLinkageOptionsStore(
                db,
                NullLogger<LiteDbSpeedLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证默认值
            MiniAssert.Equal(true, retrieved.Enabled, "默认 Enabled 应该为 true");
            MiniAssert.Equal(0, retrieved.LinkageGroups.Count, "默认 LinkageGroups 应该为空");
        }

        [MiniFact]
        public async Task CanUpdateExistingConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbSpeedLinkageOptionsStore(
                db,
                NullLogger<LiteDbSpeedLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建初始配置
            var options1 = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };
            await store.SaveAsync(options1);

            // 更新配置
            var options2 = new SpeedLinkageOptions {
                Enabled = false,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 2001, 2002, 2003 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 7, LevelWhenStopped = TriggerLevel.ActiveLow },
                            new SpeedLinkageIoPoint { BitNumber = 8, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };
            await store.SaveAsync(options2);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证更新后的值
            MiniAssert.Equal(false, retrieved.Enabled, "更新后 Enabled 应该为 false");
            MiniAssert.Equal(1, retrieved.LinkageGroups.Count, "更新后 LinkageGroups 应该有 1 个元素");
            MiniAssert.Equal(3, retrieved.LinkageGroups[0].AxisIds.Count, "更新后第一个组应该有 3 个轴ID");
            MiniAssert.Equal(2001, retrieved.LinkageGroups[0].AxisIds[0], "第一个轴ID应该为 2001");
            MiniAssert.Equal(2, retrieved.LinkageGroups[0].IoPoints.Count, "更新后第一个组应该有 2 个IO点");
            MiniAssert.Equal(7, retrieved.LinkageGroups[0].IoPoints[0].BitNumber, "第一个IO位号应该为 7");
        }

        [MiniFact]
        public async Task CanDeleteConfig() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbSpeedLinkageOptionsStore(
                db,
                NullLogger<LiteDbSpeedLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建配置
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };
            await store.SaveAsync(options);

            // 删除配置
            await store.DeleteAsync();

            // 读取配置（应返回默认值）
            var retrieved = await store.GetAsync();

            // 验证恢复到默认值
            MiniAssert.Equal(true, retrieved.Enabled, "删除后 Enabled 应该为 true（默认值）");
            MiniAssert.Equal(0, retrieved.LinkageGroups.Count, "删除后 LinkageGroups 应该为空（默认值）");
        }

        [MiniFact]
        public async Task CanHandleEmptyGroups() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbSpeedLinkageOptionsStore(
                db,
                NullLogger<LiteDbSpeedLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建空组配置
            var options = new SpeedLinkageOptions {
                Enabled = false,
                LinkageGroups = new()
            };
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(false, retrieved.Enabled, "Enabled 应该为 false");
            MiniAssert.Equal(0, retrieved.LinkageGroups.Count, "LinkageGroups 应该为空");
        }

        [MiniFact]
        public async Task CanHandleManyGroups() {
            // 使用内存数据库
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbSpeedLinkageOptionsStore(
                db,
                NullLogger<LiteDbSpeedLinkageOptionsStore>.Instance,
                new FakeCabinetIsolator(),
                cache);

            // 创建多组配置
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = Enumerable.Range(0, 10)
                    .Select(i => new SpeedLinkageGroup {
                        AxisIds = new() { 1000 + i, 2000 + i },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { 
                                BitNumber = i, 
                                LevelWhenStopped = i % 2 == 0 ? TriggerLevel.ActiveHigh : TriggerLevel.ActiveLow 
                            }
                        }
                    })
                    .ToList()
            };
            await store.SaveAsync(options);

            // 读取配置
            var retrieved = await store.GetAsync();

            // 验证
            MiniAssert.Equal(10, retrieved.LinkageGroups.Count, "LinkageGroups 应该有 10 个元素");
            MiniAssert.Equal(2, retrieved.LinkageGroups[0].AxisIds.Count, "第一个组应该有 2 个轴ID");
            MiniAssert.Equal(1000, retrieved.LinkageGroups[0].AxisIds[0], "第一个组第一个轴ID应该为 1000");
            MiniAssert.Equal(0, retrieved.LinkageGroups[0].IoPoints[0].BitNumber, "第一个组第一个IO位号应该为 0");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, retrieved.LinkageGroups[0].IoPoints[0].LevelWhenStopped, "第一个组第一个IO应该为高电平");
            MiniAssert.Equal(TriggerLevel.ActiveLow, retrieved.LinkageGroups[1].IoPoints[0].LevelWhenStopped, "第二个组第一个IO应该为低电平");
        }
    }
}
