using System;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine;
using ZakYip.Singulation.Infrastructure.Transport;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试数据库存储层的缓存行为。
    /// Tests the caching behavior of database storage layer.
    /// </summary>
    internal sealed class CacheBehaviorTests {

        [MiniFact]
        public async Task ControllerOptionsStore_UsesCacheOnSecondRead() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbControllerOptionsStore(db, NullLogger<LiteDbControllerOptionsStore>.Instance, isolator, cache);

            // Act - First read (should hit database)
            var result1 = await store.GetAsync();
            
            // Modify database directly to prove cache is being used
            var collection = db.GetCollection<Infrastructure.Configs.Entities.ControllerOptionsDoc>("controller_options");
            var doc = collection.FindById("default");
            if (doc != null) {
                doc.LocalFixedSpeedMmps = 999.99m;
                collection.Update(doc);
            }

            // Second read (should hit cache, not see the 999.99 value)
            var result2 = await store.GetAsync();

            // Assert
            MiniAssert.NotEqual(999.99m, result2.LocalFixedSpeedMmps, "Second read should return cached value, not database value");
            MiniAssert.Equal(result1.LocalFixedSpeedMmps, result2.LocalFixedSpeedMmps, "Both reads should return the same cached value");
        }

        [MiniFact]
        public async Task ControllerOptionsStore_InvalidatesCacheOnUpdate() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbControllerOptionsStore(db, NullLogger<LiteDbControllerOptionsStore>.Instance, isolator, cache);

            // Act - Initial read to populate cache
            var result1 = await store.GetAsync();

            // Update through store (should invalidate cache)
            var updatedOptions = result1 with { LocalFixedSpeedMmps = 500.0m };
            await store.UpsertAsync(updatedOptions);

            // Read again (should hit database with new value)
            var result2 = await store.GetAsync();

            // Assert
            MiniAssert.Equal(500.0m, result2.LocalFixedSpeedMmps, "After update, should read new value from database");
        }

        [MiniFact]
        public async Task AxisLayoutStore_UsesCacheOnSecondRead() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbAxisLayoutStore(db, NullLogger<LiteDbAxisLayoutStore>.Instance, isolator, cache);

            // Act - First read (should hit database)
            var result1 = await store.GetAsync();
            
            // Modify database directly
            var collection = db.GetCollection<Infrastructure.Configs.Entities.AxisGridLayoutDoc>("axis_layout");
            var doc = collection.FindById("singleton");
            if (doc != null) {
                doc.Rows = 99;
                doc.Cols = 99;
                collection.Update(doc);
            }

            // Second read (should hit cache)
            var result2 = await store.GetAsync();

            // Assert
            MiniAssert.NotEqual(99, result2.Rows, "Second read should return cached value");
            MiniAssert.Equal(result1.Rows, result2.Rows, "Both reads should return the same cached value");
        }

        [MiniFact]
        public async Task IoStatusMonitorStore_InvalidatesCacheOnDelete() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbIoStatusMonitorOptionsStore(db, NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance, isolator, cache);

            // Act - Save a custom configuration
            var options = new IoStatusMonitorOptions {
                Enabled = false,
                InputStart = 10,
                InputCount = 16
            };
            await store.SaveAsync(options);

            // Read to populate cache
            var result1 = await store.GetAsync();
            MiniAssert.Equal(false, result1.Enabled, "Should read saved value");

            // Delete configuration
            await store.DeleteAsync();

            // Read again (should return default after cache invalidation)
            var result2 = await store.GetAsync();

            // Assert
            MiniAssert.Equal(true, result2.Enabled, "After delete, should return default value (true)");
            MiniAssert.NotEqual(result1.Enabled, result2.Enabled, "Values should differ after delete");
        }

        [MiniFact]
        public async Task LeadshineCabinetIoStore_UsesCacheCorrectly() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbLeadshineCabinetIoOptionsStore(db, NullLogger<LiteDbLeadshineCabinetIoOptionsStore>.Instance, isolator, cache);

            // Act - Save configuration
            var options = new LeadshineCabinetIoOptions {
                Enabled = true,
                PollingIntervalMs = 200
            };
            await store.SaveAsync(options);

            // First read (populates cache)
            var result1 = await store.GetAsync();
            MiniAssert.Equal(200, result1.PollingIntervalMs, "Should read saved value");

            // Modify database directly
            var collection = db.GetCollection<Infrastructure.Configs.Vendors.Leadshine.Entities.LeadshineCabinetIoOptionsDoc>("leadshine_cabinet_io_options");
            var doc = collection.FindById("default");
            if (doc != null) {
                doc.PollingIntervalMs = 999;
                collection.Update(doc);
            }

            // Second read (should hit cache)
            var result2 = await store.GetAsync();

            // Assert
            MiniAssert.Equal(200, result2.PollingIntervalMs, "Second read should return cached value");
        }

        [MiniFact]
        public async Task UpstreamOptionsStore_CacheInvalidatesOnUpdate() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbUpstreamOptionsStore(db, NullLogger<LiteDbUpstreamOptionsStore>.Instance, isolator, cache);

            // Act - Initial read
            var result1 = await store.GetAsync();

            // Update with new values
            var updatedOptions = result1 with { SpeedPort = 9999 };
            await store.SaveAsync(updatedOptions);

            // Read again
            var result2 = await store.GetAsync();

            // Assert
            MiniAssert.Equal(9999, result2.SpeedPort, "Should read updated value after cache invalidation");
        }

        [MiniFact]
        public async Task SpeedLinkageStore_CacheWorksWithComplexObjects() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbSpeedLinkageOptionsStore(db, NullLogger<LiteDbSpeedLinkageOptionsStore>.Instance, isolator, cache);

            // Act - Save configuration with complex structure
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new System.Collections.Generic.List<SpeedLinkageGroup> {
                    new SpeedLinkageGroup {
                        AxisIds = new System.Collections.Generic.List<int> { 1, 2, 3 }
                    }
                }
            };
            await store.SaveAsync(options);

            // First read (populates cache)
            var result1 = await store.GetAsync();

            // Second read (from cache)
            var result2 = await store.GetAsync();

            // Assert - Both reads should return equivalent data
            MiniAssert.Equal(true, result1.Enabled, "First read should have Enabled=true");
            MiniAssert.Equal(true, result2.Enabled, "Second read should have Enabled=true");
            MiniAssert.Equal(1, result1.LinkageGroups.Count, "First read should have 1 group");
            MiniAssert.Equal(1, result2.LinkageGroups.Count, "Second read should have 1 group");
        }

        [MiniFact]
        public async Task IoLinkageStore_ConcurrentAccessDoesNotCorruptCache() {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new FakeCabinetIsolator();
            var store = new LiteDbIoLinkageOptionsStore(db, NullLogger<LiteDbIoLinkageOptionsStore>.Instance, isolator, cache);

            // Act - Save initial configuration
            var options = new IoLinkageOptions {
                Enabled = true
            };
            await store.SaveAsync(options);

            // Simulate concurrent reads
            var task1 = store.GetAsync();
            var task2 = store.GetAsync();
            var task3 = store.GetAsync();

            await Task.WhenAll(task1, task2, task3);

            var result1 = await task1;
            var result2 = await task2;
            var result3 = await task3;

            // Assert - All reads should return consistent data
            MiniAssert.Equal(true, result1.Enabled, "Read 1 should be consistent");
            MiniAssert.Equal(true, result2.Enabled, "Read 2 should be consistent");
            MiniAssert.Equal(true, result3.Enabled, "Read 3 should be consistent");
        }
    }
}
