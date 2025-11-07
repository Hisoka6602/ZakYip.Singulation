using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Utils;

namespace ZakYip.Singulation.Infrastructure.Services {
    /// <summary>
    /// 配置导入导出服务，支持配置的 JSON 文件导入导出、模板功能和配置迁移。
    /// </summary>
    public sealed class ConfigurationImportExportService {
        private readonly ILogger<ConfigurationImportExportService> _logger;
        private readonly IControllerOptionsStore _controllerStore;
        private readonly ISpeedLinkageOptionsStore _speedLinkageStore;
        private readonly IIoLinkageOptionsStore _ioLinkageStore;

        private static readonly JsonSerializerOptions JsonOptions = new() {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        public ConfigurationImportExportService(
            ILogger<ConfigurationImportExportService> logger,
            IControllerOptionsStore controllerStore,
            ISpeedLinkageOptionsStore speedLinkageStore,
            IIoLinkageOptionsStore ioLinkageStore) {
            _logger = logger;
            _controllerStore = controllerStore;
            _speedLinkageStore = speedLinkageStore;
            _ioLinkageStore = ioLinkageStore;
        }

        /// <summary>
        /// 完整配置包，用于导入导出所有配置。
        /// </summary>
        public sealed record ConfigurationPackage {
            /// <summary>配置包版本</summary>
            public string Version { get; init; } = "1.0.0";
            
            /// <summary>导出时间戳</summary>
            public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
            
            /// <summary>配置包描述</summary>
            public string? Description { get; init; }
            
            /// <summary>控制器配置</summary>
            public ControllerOptions? ControllerOptions { get; init; }
            
            /// <summary>速度联动配置</summary>
            public SpeedLinkageOptions? SpeedLinkageOptions { get; init; }
            
            /// <summary>IO 联动配置</summary>
            public IoLinkageOptions? IoLinkageOptions { get; init; }
        }

        /// <summary>
        /// 导出所有配置到 JSON 字符串。
        /// </summary>
        public async Task<string> ExportAllConfigurationsAsync(string? description = null, CancellationToken ct = default) {
            _logger.LogInformation("开始导出所有配置");

            try {
                var package = new ConfigurationPackage {
                    Description = description ?? "配置导出",
                    ControllerOptions = await _controllerStore.GetAsync(ct),
                    SpeedLinkageOptions = await _speedLinkageStore.GetAsync(ct),
                    IoLinkageOptions = await _ioLinkageStore.GetAsync(ct)
                };

                var json = JsonSerializer.Serialize(package, JsonOptions);
                
                _logger.LogInformation("配置导出成功，大小: {Size} 字节", json.Length);
                return json;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "配置导出失败");
                throw new InvalidOperationException("配置导出失败", ex);
            }
        }

        /// <summary>
        /// 导出所有配置到文件。
        /// </summary>
        public async Task ExportAllConfigurationsToFileAsync(string filePath, string? description = null, CancellationToken ct = default) {
            var json = await ExportAllConfigurationsAsync(description, ct);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, ct);
            _logger.LogInformation("配置已导出到文件: {FilePath}", filePath);
        }

        /// <summary>
        /// 导出单个配置类型到 JSON 字符串。
        /// </summary>
        public async Task<string> ExportConfigurationAsync<T>(Func<CancellationToken, Task<T>> getter, CancellationToken ct = default) where T : class {
            try {
                var config = await getter(ct);
                var json = JsonSerializer.Serialize(config, JsonOptions);
                _logger.LogInformation("配置 {Type} 导出成功", typeof(T).Name);
                return json;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "配置 {Type} 导出失败", typeof(T).Name);
                throw new InvalidOperationException($"配置 {typeof(T).Name} 导出失败", ex);
            }
        }

        /// <summary>
        /// 从 JSON 字符串导入所有配置。
        /// </summary>
        public async Task<ImportResult> ImportAllConfigurationsAsync(string json, bool validateOnly = false, CancellationToken ct = default) {
            _logger.LogInformation("开始导入配置，验证模式: {ValidateOnly}", validateOnly);

            var result = new ImportResult();

            try {
                // 反序列化配置包
                var package = JsonSerializer.Deserialize<ConfigurationPackage>(json, JsonOptions);
                if (package == null) {
                    result.AddError("无法解析配置包 JSON");
                    return result;
                }

                _logger.LogInformation("配置包版本: {Version}, 导出时间: {ExportedAt}", package.Version, package.ExportedAt);

                // 验证控制器配置
                if (package.ControllerOptions != null) {
                    var validation = ConfigurationValidator.ValidateControllerOptions(package.ControllerOptions);
                    if (!validation.IsValid) {
                        result.AddError("控制器配置", validation.Errors);
                    } else {
                        result.AddWarning("控制器配置", validation.Warnings);
                        if (!validateOnly) {
                            await _controllerStore.UpsertAsync(package.ControllerOptions, ct);
                            result.ImportedConfigurations.Add("控制器配置");
                        }
                    }
                }

                // 验证速度联动配置
                if (package.SpeedLinkageOptions != null) {
                    var validation = ConfigurationValidator.ValidateSpeedLinkageOptions(package.SpeedLinkageOptions);
                    if (!validation.IsValid) {
                        result.AddError("速度联动配置", validation.Errors);
                    } else {
                        result.AddWarning("速度联动配置", validation.Warnings);
                        if (!validateOnly) {
                            await _speedLinkageStore.SaveAsync(package.SpeedLinkageOptions, ct);
                            result.ImportedConfigurations.Add("速度联动配置");
                        }
                    }
                }

                // 验证 IO 联动配置
                if (package.IoLinkageOptions != null) {
                    var validation = ConfigurationValidator.ValidateIoLinkageOptions(package.IoLinkageOptions);
                    if (!validation.IsValid) {
                        result.AddError("IO 联动配置", validation.Errors);
                    } else {
                        result.AddWarning("IO 联动配置", validation.Warnings);
                        if (!validateOnly) {
                            await _ioLinkageStore.SaveAsync(package.IoLinkageOptions, ct);
                            result.ImportedConfigurations.Add("IO 联动配置");
                        }
                    }
                }

                if (result.IsSuccess) {
                    if (validateOnly) {
                        _logger.LogInformation("配置验证通过");
                    } else {
                        _logger.LogInformation("配置导入成功，已导入 {Count} 个配置项", result.ImportedConfigurations.Count);
                    }
                } else {
                    _logger.LogWarning("配置导入失败，存在 {ErrorCount} 个错误", result.Errors.Count);
                }

                return result;
            }
            catch (JsonException ex) {
                _logger.LogError(ex, "JSON 解析失败");
                result.AddError("JSON 格式错误: " + ex.Message);
                return result;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "配置导入失败");
                result.AddError("配置导入失败: " + ex.Message);
                return result;
            }
        }

        /// <summary>
        /// 从文件导入所有配置。
        /// </summary>
        public async Task<ImportResult> ImportAllConfigurationsFromFileAsync(string filePath, bool validateOnly = false, CancellationToken ct = default) {
            if (!File.Exists(filePath)) {
                var result = new ImportResult();
                result.AddError($"文件不存在: {filePath}");
                return result;
            }

            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct);
            _logger.LogInformation("从文件导入配置: {FilePath}, 大小: {Size} 字节", filePath, json.Length);
            
            return await ImportAllConfigurationsAsync(json, validateOnly, ct);
        }

        /// <summary>
        /// 创建配置模板。
        /// </summary>
        public string CreateConfigurationTemplate(ConfigurationType type) {
            object template = type switch {
                ConfigurationType.Controller => CreateControllerTemplate(),
                ConfigurationType.SpeedLinkage => CreateSpeedLinkageTemplate(),
                ConfigurationType.IoLinkage => CreateIoLinkageTemplate(),
                ConfigurationType.All => CreateFullTemplate(),
                _ => throw new ArgumentException($"不支持的配置类型: {type}")
            };

            return JsonSerializer.Serialize(template, JsonOptions);
        }

        private static ControllerOptions CreateControllerTemplate() {
            return new ControllerOptions {
                Vendor = "leadshine",
                ControllerIp = "192.168.5.11",
                OverrideAxisCount = null,
                LocalFixedSpeedMmps = 100.0m,
                Template = new DriverOptionsTemplateOptions {
                    Card = 8,
                    Port = 2,
                    GearRatio = 1m,
                    ScrewPitchMm = 5m,
                    MaxRpm = 1813m,
                    MaxAccelRpmPerSec = 1511m,
                    MaxDecelRpmPerSec = 1511m,
                    MinWriteInterval = 5,
                    ConsecutiveFailThreshold = 5,
                    EnableHealthMonitor = true,
                    HealthPingInterval = 500
                }
            };
        }

        private static SpeedLinkageOptions CreateSpeedLinkageTemplate() {
            return new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001, 1002 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint {
                                BitNumber = 3,
                                LevelWhenStopped = Core.Enums.TriggerLevel.ActiveHigh
                            }
                        }
                    }
                }
            };
        }

        private static IoLinkageOptions CreateIoLinkageTemplate() {
            return new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint {
                        BitNumber = 3,
                        Level = Core.Enums.TriggerLevel.ActiveLow
                    }
                },
                StoppedStateIos = new() {
                    new IoLinkagePoint {
                        BitNumber = 3,
                        Level = Core.Enums.TriggerLevel.ActiveHigh
                    }
                }
            };
        }

        private static ConfigurationPackage CreateFullTemplate() {
            return new ConfigurationPackage {
                Version = "1.0.0",
                Description = "配置模板",
                ExportedAt = DateTime.UtcNow,
                ControllerOptions = CreateControllerTemplate(),
                SpeedLinkageOptions = CreateSpeedLinkageTemplate(),
                IoLinkageOptions = CreateIoLinkageTemplate()
            };
        }

        /// <summary>
        /// 配置类型枚举。
        /// </summary>
        public enum ConfigurationType {
            /// <summary>控制器配置</summary>
            Controller,
            /// <summary>速度联动配置</summary>
            SpeedLinkage,
            /// <summary>IO 联动配置</summary>
            IoLinkage,
            /// <summary>所有配置</summary>
            All
        }

        /// <summary>
        /// 导入结果类。
        /// </summary>
        public sealed class ImportResult {
            /// <summary>是否成功</summary>
            public bool IsSuccess => Errors.Count == 0;
            
            /// <summary>错误列表</summary>
            public List<string> Errors { get; } = new();
            
            /// <summary>警告列表</summary>
            public List<string> Warnings { get; } = new();
            
            /// <summary>已导入的配置项列表</summary>
            public List<string> ImportedConfigurations { get; } = new();

            public void AddError(string error) => Errors.Add(error);
            
            public void AddError(string category, System.Collections.Generic.IEnumerable<string> errors) {
                foreach (var error in errors) {
                    Errors.Add($"{category}: {error}");
                }
            }
            
            public void AddWarning(string warning) => Warnings.Add(warning);
            
            public void AddWarning(string category, System.Collections.Generic.IEnumerable<string> warnings) {
                foreach (var warning in warnings) {
                    Warnings.Add($"{category}: {warning}");
                }
            }

            /// <summary>
            /// 获取格式化的结果报告。
            /// </summary>
            public string GetFormattedReport() {
                var sb = new StringBuilder();
                
                if (IsSuccess) {
                    sb.AppendLine("✓ 配置导入成功");
                    if (ImportedConfigurations.Count > 0) {
                        sb.AppendLine($"已导入 {ImportedConfigurations.Count} 个配置项：");
                        foreach (var config in ImportedConfigurations) {
                            sb.AppendLine($"  • {config}");
                        }
                    }
                } else {
                    sb.AppendLine("✗ 配置导入失败");
                    sb.AppendLine($"发现 {Errors.Count} 个错误：");
                    for (int i = 0; i < Errors.Count; i++) {
                        sb.AppendLine($"  {i + 1}. {Errors[i]}");
                    }
                }

                if (Warnings.Count > 0) {
                    sb.AppendLine();
                    sb.AppendLine($"发现 {Warnings.Count} 个警告：");
                    for (int i = 0; i < Warnings.Count; i++) {
                        sb.AppendLine($"  {i + 1}. {Warnings[i]}");
                    }
                }

                return sb.ToString();
            }
        }
    }
}
