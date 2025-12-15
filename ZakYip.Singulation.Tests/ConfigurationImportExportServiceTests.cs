using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Services;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 ConfigurationImportExportService 的正确性。
    /// </summary>
    internal sealed class ConfigurationImportExportServiceTests {

        [MiniFact]
        public async Task ExportAllConfigurations_ShouldReturnValidJson() {
            // Arrange
            var controllerStore = new FakeControllerStore();
            var speedLinkageStore = new FakeSpeedLinkageStore();
            var ioLinkageStore = new FakeIoLinkageStore();
            
            var service = new ConfigurationImportExportService(
                NullLogger<ConfigurationImportExportService>.Instance,
                controllerStore,
                speedLinkageStore,
                ioLinkageStore,
                FakeSystemClock.CreateDefault());

            // Act
            var json = await service.ExportAllConfigurationsAsync("测试导出");

            // Assert
            MiniAssert.True(!string.IsNullOrWhiteSpace(json), "导出的JSON不应为空");
            MiniAssert.True(json.Contains("version"), "JSON应包含version字段");
            MiniAssert.True(json.Contains("exportedAt"), "JSON应包含exportedAt字段");
            
            // 验证可以反序列化
            var package = JsonSerializer.Deserialize<ConfigurationImportExportService.ConfigurationPackage>(
                json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            
            MiniAssert.NotNull(package, "应该能够反序列化配置包");
            MiniAssert.Equal("1.0.0", package!.Version, "版本应该为1.0.0");
            MiniAssert.NotNull(package.ControllerOptions, "应该包含控制器配置");
        }

        [MiniFact]
        public async Task ExportAndImport_ShouldRoundTrip() {
            // Arrange
            var controllerStore = new FakeControllerStore();
            var speedLinkageStore = new FakeSpeedLinkageStore();
            var ioLinkageStore = new FakeIoLinkageStore();
            
            var service = new ConfigurationImportExportService(
                NullLogger<ConfigurationImportExportService>.Instance,
                controllerStore,
                speedLinkageStore,
                ioLinkageStore,
                FakeSystemClock.CreateDefault());

            // 设置初始配置
            var originalSpeedLinkage = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001, 1002 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 5, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };
            await speedLinkageStore.SaveAsync(originalSpeedLinkage);

            // Act - 导出
            var json = await service.ExportAllConfigurationsAsync();
            
            // 清空存储
            await speedLinkageStore.DeleteAsync();
            
            // Act - 导入
            var result = await service.ImportAllConfigurationsAsync(json, validateOnly: false);

            // Assert
            MiniAssert.True(result.IsSuccess, $"导入应该成功: {result.GetFormattedReport()}");
            MiniAssert.True(result.ImportedConfigurations.Count > 0, "应该导入了配置");
            
            // 验证导入的配置与原始配置一致
            var importedSpeedLinkage = await speedLinkageStore.GetAsync();
            MiniAssert.Equal(originalSpeedLinkage.Enabled, importedSpeedLinkage.Enabled, "Enabled应该一致");
            MiniAssert.Equal(
                originalSpeedLinkage.LinkageGroups.Count, 
                importedSpeedLinkage.LinkageGroups.Count, 
                "联动组数量应该一致"
            );
        }

        [MiniFact]
        public async Task ImportWithValidateOnly_ShouldNotSaveToStore() {
            // Arrange
            var controllerStore = new FakeControllerStore();
            var speedLinkageStore = new FakeSpeedLinkageStore();
            var ioLinkageStore = new FakeIoLinkageStore();
            
            var service = new ConfigurationImportExportService(
                NullLogger<ConfigurationImportExportService>.Instance,
                controllerStore,
                speedLinkageStore,
                ioLinkageStore,
                FakeSystemClock.CreateDefault());

            var json = await service.ExportAllConfigurationsAsync();
            
            // 清空存储以确保初始状态
            await speedLinkageStore.DeleteAsync();

            // Act - 仅验证模式导入
            var result = await service.ImportAllConfigurationsAsync(json, validateOnly: true);

            // Assert
            MiniAssert.True(result.IsSuccess, "验证应该成功");
            MiniAssert.Equal(0, result.ImportedConfigurations.Count, "验证模式不应导入配置");
            
            // 验证存储未被修改
            var speedLinkage = await speedLinkageStore.GetAsync();
            MiniAssert.Equal(0, speedLinkage.LinkageGroups.Count, "存储应该保持为空");
        }

        [MiniFact]
        public async Task ImportInvalidConfig_ShouldReturnErrors() {
            // Arrange
            var controllerStore = new FakeControllerStore();
            var speedLinkageStore = new FakeSpeedLinkageStore();
            var ioLinkageStore = new FakeIoLinkageStore();
            
            var service = new ConfigurationImportExportService(
                NullLogger<ConfigurationImportExportService>.Instance,
                controllerStore,
                speedLinkageStore,
                ioLinkageStore,
                FakeSystemClock.CreateDefault());

            // 创建无效的配置（轴ID为0）
            var invalidConfig = new ConfigurationImportExportService.ConfigurationPackage {
                Version = "1.0.0",
                SpeedLinkageOptions = new SpeedLinkageOptions {
                    Enabled = true,
                    LinkageGroups = new() {
                        new SpeedLinkageGroup {
                            AxisIds = new() { 0 },  // 无效：轴ID必须大于0
                            IoPoints = new() {
                                new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(invalidConfig, new JsonSerializerOptions { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            // Act
            var result = await service.ImportAllConfigurationsAsync(json, validateOnly: false);

            // Assert
            MiniAssert.False(result.IsSuccess, "导入无效配置应该失败");
            MiniAssert.True(result.Errors.Count > 0, "应该有错误信息");
            MiniAssert.True(
                result.Errors.Any(e => e.Contains("轴 ID")), 
                "错误信息应该提及轴ID问题"
            );
        }

        [MiniFact]
        public void CreateTemplate_ShouldReturnValidJson() {
            // Arrange
            var controllerStore = new FakeControllerStore();
            var speedLinkageStore = new FakeSpeedLinkageStore();
            var ioLinkageStore = new FakeIoLinkageStore();
            
            var service = new ConfigurationImportExportService(
                NullLogger<ConfigurationImportExportService>.Instance,
                controllerStore,
                speedLinkageStore,
                ioLinkageStore,
                FakeSystemClock.CreateDefault());

            // Act
            var controllerTemplate = service.CreateConfigurationTemplate(
                ConfigurationImportExportService.ConfigurationType.Controller
            );
            var speedLinkageTemplate = service.CreateConfigurationTemplate(
                ConfigurationImportExportService.ConfigurationType.SpeedLinkage
            );
            var allTemplate = service.CreateConfigurationTemplate(
                ConfigurationImportExportService.ConfigurationType.All
            );

            // Assert
            MiniAssert.True(!string.IsNullOrWhiteSpace(controllerTemplate), "控制器模板不应为空");
            MiniAssert.True(!string.IsNullOrWhiteSpace(speedLinkageTemplate), "速度联动模板不应为空");
            MiniAssert.True(!string.IsNullOrWhiteSpace(allTemplate), "完整模板不应为空");
            
            MiniAssert.True(controllerTemplate.Contains("vendor"), "控制器模板应包含vendor");
            MiniAssert.True(speedLinkageTemplate.Contains("linkageGroups"), "速度联动模板应包含linkageGroups");
            MiniAssert.True(allTemplate.Contains("version"), "完整模板应包含version");
        }

        [MiniFact]
        public async Task ImportTemplate_ShouldSucceed() {
            // Arrange
            var controllerStore = new FakeControllerStore();
            var speedLinkageStore = new FakeSpeedLinkageStore();
            var ioLinkageStore = new FakeIoLinkageStore();
            
            var service = new ConfigurationImportExportService(
                NullLogger<ConfigurationImportExportService>.Instance,
                controllerStore,
                speedLinkageStore,
                ioLinkageStore,
                FakeSystemClock.CreateDefault());

            // Act - 获取模板并导入
            var template = service.CreateConfigurationTemplate(
                ConfigurationImportExportService.ConfigurationType.All
            );
            var result = await service.ImportAllConfigurationsAsync(template, validateOnly: false);

            // Assert
            MiniAssert.True(result.IsSuccess, $"导入模板应该成功: {result.GetFormattedReport()}");
            MiniAssert.True(result.ImportedConfigurations.Count > 0, "应该导入了配置");
        }

        [MiniFact]
        public async Task ImportMalformedJson_ShouldReturnError() {
            // Arrange
            var controllerStore = new FakeControllerStore();
            var speedLinkageStore = new FakeSpeedLinkageStore();
            var ioLinkageStore = new FakeIoLinkageStore();
            
            var service = new ConfigurationImportExportService(
                NullLogger<ConfigurationImportExportService>.Instance,
                controllerStore,
                speedLinkageStore,
                ioLinkageStore,
                FakeSystemClock.CreateDefault());

            var malformedJson = "{ this is not valid json }";

            // Act
            var result = await service.ImportAllConfigurationsAsync(malformedJson, validateOnly: false);

            // Assert
            MiniAssert.False(result.IsSuccess, "导入格式错误的JSON应该失败");
            MiniAssert.True(result.Errors.Count > 0, "应该有错误信息");
            MiniAssert.True(
                result.Errors.Any(e => e.Contains("JSON")), 
                "错误信息应该提及JSON格式问题"
            );
        }
    }

    #region Test Helpers

    /// <summary>
    /// 用于测试的虚拟控制器配置存储。
    /// </summary>
    internal sealed class FakeControllerStore : ZakYip.Singulation.Core.Contracts.IControllerOptionsStore {
        private ControllerOptions _options = new ControllerOptions {
            Vendor = "leadshine",
            ControllerIp = "192.168.5.11",
            LocalFixedSpeedMmps = 100.0m,
            Template = new DriverOptionsTemplateOptions {
                Card = 8,
                Port = 2,
                GearRatio = 1m,
                ScrewPitchMm = 5m,
                MaxRpm = 1813m,
                MaxAccelRpmPerSec = 1511m,
                MaxDecelRpmPerSec = 1511m
            }
        };

        public Task<ControllerOptions> GetAsync(System.Threading.CancellationToken ct = default) {
            return Task.FromResult(_options);
        }

        public Task UpsertAsync(ControllerOptions dto, System.Threading.CancellationToken ct = default) {
            _options = dto;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 用于测试的虚拟速度联动配置存储。
    /// </summary>
    internal sealed class FakeSpeedLinkageStore : ZakYip.Singulation.Core.Contracts.ISpeedLinkageOptionsStore {
        private SpeedLinkageOptions _options = new SpeedLinkageOptions();

        public Task<SpeedLinkageOptions> GetAsync(System.Threading.CancellationToken ct = default) {
            return Task.FromResult(_options);
        }

        public Task SaveAsync(SpeedLinkageOptions options, System.Threading.CancellationToken ct = default) {
            _options = options;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(System.Threading.CancellationToken ct = default) {
            _options = new SpeedLinkageOptions();
            return Task.CompletedTask;
        }
    }

    #endregion
}
