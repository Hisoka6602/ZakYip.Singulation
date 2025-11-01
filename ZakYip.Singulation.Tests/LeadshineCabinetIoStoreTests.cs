using System;
using System.IO;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 LeadshineCabinetIoOptions 的 LiteDB 持久化存储。
    /// </summary>
    internal sealed class LeadshineCabinetIoStoreTests {

        [MiniFact]
        public async Task SaveAndGetAsync_ReturnsCorrectOptions() {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_cabinet_{Guid.NewGuid()}.db");
            using var db = new LiteDatabase($"Filename={dbPath};Mode=Shared");
            var safetyIsolator = new FakeSafetyIsolator();
            var store = new LiteDbLeadshineCabinetIoOptionsStore(db, NullLogger<LiteDbLeadshineCabinetIoOptionsStore>.Instance, safetyIsolator);

            var options = new LeadshineCabinetIoOptions {
                Enabled = true,
                PollingIntervalMs = 100,
                CabinetInputPoint = new CabinetInputPoint {
                    EmergencyStop = 4,
                    Stop = 2,
                    Start = 1,
                    Reset = 3,
                    RemoteLocalMode = 5,
                    InvertLogic = true,
                    InvertEmergencyStopLogic = false,
                    RemoteLocalActiveHigh = false
                },
                CabinetIndicatorPoint = new CabinetIndicatorPoint {
                    RedLight = 10,
                    YellowLight = 11,
                    GreenLight = 12,
                    StartButtonLight = 13,
                    StopButtonLight = 14,
                    RemoteConnectionLight = 15,
                    InvertLightLogic = false,
                    InvertRedLightLogic = true
                }
            };

            // Act
            await store.SaveAsync(options);
            var retrieved = await store.GetAsync();

            // Assert
            MiniAssert.Equal(true, retrieved.Enabled, "Enabled 应为 true");
            MiniAssert.Equal(100, retrieved.PollingIntervalMs, "PollingIntervalMs 应为 100");
            
            // 验证输入点位
            MiniAssert.Equal(4, retrieved.CabinetInputPoint.EmergencyStop, "EmergencyStop 应为 4");
            MiniAssert.Equal(2, retrieved.CabinetInputPoint.Stop, "Stop 应为 2");
            MiniAssert.Equal(1, retrieved.CabinetInputPoint.Start, "Start 应为 1");
            MiniAssert.Equal(3, retrieved.CabinetInputPoint.Reset, "Reset 应为 3");
            MiniAssert.Equal(5, retrieved.CabinetInputPoint.RemoteLocalMode, "RemoteLocalMode 应为 5");
            MiniAssert.Equal(true, retrieved.CabinetInputPoint.InvertLogic, "InvertLogic 应为 true");
            MiniAssert.Equal(false, retrieved.CabinetInputPoint.InvertEmergencyStopLogic, "InvertEmergencyStopLogic 应为 false");
            MiniAssert.Equal(false, retrieved.CabinetInputPoint.RemoteLocalActiveHigh, "RemoteLocalActiveHigh 应为 false");
            
            // 验证指示灯点位
            MiniAssert.Equal(10, retrieved.CabinetIndicatorPoint.RedLight, "RedLight 应为 10");
            MiniAssert.Equal(11, retrieved.CabinetIndicatorPoint.YellowLight, "YellowLight 应为 11");
            MiniAssert.Equal(12, retrieved.CabinetIndicatorPoint.GreenLight, "GreenLight 应为 12");
            MiniAssert.Equal(13, retrieved.CabinetIndicatorPoint.StartButtonLight, "StartButtonLight 应为 13");
            MiniAssert.Equal(14, retrieved.CabinetIndicatorPoint.StopButtonLight, "StopButtonLight 应为 14");
            MiniAssert.Equal(15, retrieved.CabinetIndicatorPoint.RemoteConnectionLight, "RemoteConnectionLight 应为 15");
            MiniAssert.Equal(false, retrieved.CabinetIndicatorPoint.InvertLightLogic, "InvertLightLogic 应为 false");
            MiniAssert.Equal(true, retrieved.CabinetIndicatorPoint.InvertRedLightLogic, "InvertRedLightLogic 应为 true");

            // Cleanup
            db.Dispose();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [MiniFact]
        public async Task GetAsync_WithNoData_ReturnsDefaultOptions() {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_cabinet_{Guid.NewGuid()}.db");
            using var db = new LiteDatabase($"Filename={dbPath};Mode=Shared");
            var safetyIsolator = new FakeSafetyIsolator();
            var store = new LiteDbLeadshineCabinetIoOptionsStore(db, NullLogger<LiteDbLeadshineCabinetIoOptionsStore>.Instance, safetyIsolator);

            // Act
            var retrieved = await store.GetAsync();

            // Assert
            MiniAssert.Equal(false, retrieved.Enabled, "默认 Enabled 应为 false");
            MiniAssert.Equal(50, retrieved.PollingIntervalMs, "默认轮询间隔应为 50ms");
            MiniAssert.NotEqual(null, retrieved.CabinetInputPoint, "CabinetInputPoint 不应为 null");
            MiniAssert.NotEqual(null, retrieved.CabinetIndicatorPoint, "CabinetIndicatorPoint 不应为 null");

            // Cleanup
            db.Dispose();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [MiniFact]
        public async Task DeleteAsync_RemovesOptions() {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_cabinet_{Guid.NewGuid()}.db");
            using var db = new LiteDatabase($"Filename={dbPath};Mode=Shared");
            var safetyIsolator = new FakeSafetyIsolator();
            var store = new LiteDbLeadshineCabinetIoOptionsStore(db, NullLogger<LiteDbLeadshineCabinetIoOptionsStore>.Instance, safetyIsolator);

            var options = new LeadshineCabinetIoOptions {
                Enabled = true,
                PollingIntervalMs = 100
            };

            // Act
            await store.SaveAsync(options);
            var beforeDelete = await store.GetAsync();
            await store.DeleteAsync();
            var afterDelete = await store.GetAsync();

            // Assert
            MiniAssert.Equal(true, beforeDelete.Enabled, "删除前应为 true");
            MiniAssert.Equal(false, afterDelete.Enabled, "删除后应恢复为默认值 false");

            // Cleanup
            db.Dispose();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
