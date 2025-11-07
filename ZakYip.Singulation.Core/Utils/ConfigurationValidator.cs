using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Utils {
    /// <summary>
    /// 配置验证工具类，提供全面的配置校验和友好的错误提示。
    /// </summary>
    public static class ConfigurationValidator {
        /// <summary>
        /// 验证结果类，包含验证是否通过和详细的错误信息。
        /// </summary>
        public sealed record ValidationResult {
            /// <summary>验证是否通过</summary>
            public bool IsValid { get; init; }
            
            /// <summary>错误信息列表</summary>
            public List<string> Errors { get; init; } = new();
            
            /// <summary>警告信息列表</summary>
            public List<string> Warnings { get; init; } = new();
            
            /// <summary>获取格式化的错误消息</summary>
            public string GetFormattedErrors() {
                if (IsValid) return string.Empty;
                
                var sb = new StringBuilder();
                sb.AppendLine("配置验证失败：");
                for (int i = 0; i < Errors.Count; i++) {
                    sb.AppendLine($"  {i + 1}. {Errors[i]}");
                }
                
                if (Warnings.Count > 0) {
                    sb.AppendLine("警告：");
                    for (int i = 0; i < Warnings.Count; i++) {
                        sb.AppendLine($"  {i + 1}. {Warnings[i]}");
                    }
                }
                
                return sb.ToString();
            }
            
            /// <summary>创建成功的验证结果</summary>
            public static ValidationResult Success() => new() { IsValid = true };
            
            /// <summary>创建失败的验证结果</summary>
            public static ValidationResult Failure(params string[] errors) => new() {
                IsValid = false,
                Errors = errors.ToList()
            };
        }

        /// <summary>
        /// 验证控制器配置。
        /// </summary>
        public static ValidationResult ValidateControllerOptions(ControllerOptions? options) {
            if (options == null) {
                return ValidationResult.Failure("控制器配置不能为 null");
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            // 使用 DataAnnotations 进行基本验证
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var context = new ValidationContext(options);
            if (!Validator.TryValidateObject(options, context, validationResults, validateAllProperties: true)) {
                errors.AddRange(validationResults.Select(r => r.ErrorMessage ?? "未知错误"));
            }

            // 额外的业务逻辑验证
            if (string.IsNullOrWhiteSpace(options.Vendor)) {
                errors.Add("驱动厂商标识不能为空或空白字符");
            }

            if (string.IsNullOrWhiteSpace(options.ControllerIp)) {
                errors.Add("控制器 IP 地址不能为空或空白字符");
            } else if (!IsValidIpAddress(options.ControllerIp)) {
                errors.Add($"控制器 IP 地址格式无效: {options.ControllerIp}");
            }

            if (options.OverrideAxisCount.HasValue && options.OverrideAxisCount.Value < 1) {
                errors.Add("覆盖轴数量必须大于 0");
            }

            if (options.LocalFixedSpeedMmps < 0) {
                errors.Add("本地固定速度不能为负数");
            } else if (options.LocalFixedSpeedMmps == 0) {
                warnings.Add("本地固定速度设置为 0，可能导致轴无法运动");
            }

            // 验证驱动参数模板
            if (options.Template != null) {
                var templateResult = ValidateDriverOptionsTemplate(options.Template);
                errors.AddRange(templateResult.Errors);
                warnings.AddRange(templateResult.Warnings);
            }

            return new ValidationResult {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }

        /// <summary>
        /// 验证驱动参数模板配置。
        /// </summary>
        public static ValidationResult ValidateDriverOptionsTemplate(DriverOptionsTemplateOptions? options) {
            if (options == null) {
                return ValidationResult.Failure("驱动参数模板不能为 null");
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            // 使用 DataAnnotations 进行基本验证
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var context = new ValidationContext(options);
            if (!Validator.TryValidateObject(options, context, validationResults, validateAllProperties: true)) {
                errors.AddRange(validationResults.Select(r => r.ErrorMessage ?? "未知错误"));
            }

            // 业务逻辑验证
            var hasScrewPitch = options.ScrewPitchMm > 0;
            var hasPulleyDiameter = options.PulleyDiameterMm > 0;
            var hasPitchDiameter = options.PulleyPitchDiameterMm > 0;

            var mechanismCount = (hasScrewPitch ? 1 : 0) + (hasPulleyDiameter ? 1 : 0) + (hasPitchDiameter ? 1 : 0);
            
            if (mechanismCount == 0) {
                errors.Add("必须至少配置一种传动机构：丝杠螺距、皮带轮直径或辊筒直径");
            } else if (mechanismCount > 1) {
                warnings.Add("同时配置了多种传动机构，系统将按优先级选择：丝杠螺距 > 辊筒直径 > 皮带轮直径");
            }

            if (options.MaxRpm <= 0) {
                errors.Add("最大转速必须大于 0");
            }

            if (options.MaxAccelRpmPerSec <= 0) {
                errors.Add("最大加速度必须大于 0");
            }

            if (options.MaxDecelRpmPerSec <= 0) {
                errors.Add("最大减速度必须大于 0");
            }

            if (options.MinWriteInterval < 0) {
                errors.Add("最小写入间隔不能为负数");
            }

            if (options.EnableHealthMonitor && options.HealthPingInterval < 100) {
                warnings.Add("健康监测 Ping 间隔过小（< 100ms），可能影响性能");
            }

            return new ValidationResult {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }

        /// <summary>
        /// 验证速度联动配置。
        /// </summary>
        public static ValidationResult ValidateSpeedLinkageOptions(SpeedLinkageOptions? options) {
            if (options == null) {
                return ValidationResult.Failure("速度联动配置不能为 null");
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            // 验证联动组
            if (options.Enabled && options.LinkageGroups.Count == 0) {
                warnings.Add("速度联动已启用但未配置任何联动组");
            }

            // 检测重复的 IO 端口
            var allIoBitNumbers = options.LinkageGroups
                .SelectMany(g => g.IoPoints)
                .Select(p => p.BitNumber)
                .ToList();

            var duplicateIos = allIoBitNumbers
                .GroupBy(b => b)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIos.Count > 0) {
                warnings.Add($"以下 IO 端口在多个联动组中重复使用: {string.Join(", ", duplicateIos)}");
            }

            // 验证每个联动组
            for (int i = 0; i < options.LinkageGroups.Count; i++) {
                var group = options.LinkageGroups[i];
                var groupPrefix = $"联动组 {i + 1}";

                // 使用 DataAnnotations 验证
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                var context = new ValidationContext(group);
                if (!Validator.TryValidateObject(group, context, validationResults, validateAllProperties: true)) {
                    errors.AddRange(validationResults.Select(r => $"{groupPrefix}: {r.ErrorMessage}"));
                }

                if (group.AxisIds.Count == 0) {
                    errors.Add($"{groupPrefix}: 轴 ID 列表不能为空");
                }

                if (group.AxisIds.Any(id => id <= 0)) {
                    errors.Add($"{groupPrefix}: 轴 ID 必须大于 0");
                }

                // 检查重复的轴 ID
                var duplicateAxisIds = group.AxisIds
                    .GroupBy(id => id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateAxisIds.Count > 0) {
                    errors.Add($"{groupPrefix}: 存在重复的轴 ID: {string.Join(", ", duplicateAxisIds)}");
                }

                if (group.IoPoints.Count == 0) {
                    errors.Add($"{groupPrefix}: IO 点列表不能为空");
                }

                // 验证每个 IO 点
                for (int j = 0; j < group.IoPoints.Count; j++) {
                    var ioPoint = group.IoPoints[j];
                    var ioPrefix = $"{groupPrefix} IO 点 {j + 1}";

                    var ioValidationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                    var ioContext = new ValidationContext(ioPoint);
                    if (!Validator.TryValidateObject(ioPoint, ioContext, ioValidationResults, validateAllProperties: true)) {
                        errors.AddRange(ioValidationResults.Select(r => $"{ioPrefix}: {r.ErrorMessage}"));
                    }
                }
            }

            return new ValidationResult {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }

        /// <summary>
        /// 验证 IO 联动配置。
        /// </summary>
        public static ValidationResult ValidateIoLinkageOptions(IoLinkageOptions? options) {
            if (options == null) {
                return ValidationResult.Failure("IO 联动配置不能为 null");
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            if (options.Enabled) {
                if (options.RunningStateIos.Count == 0 && options.StoppedStateIos.Count == 0) {
                    warnings.Add("IO 联动已启用但未配置任何 IO 点");
                }
            }

            // 验证运行状态 IO 点
            errors.AddRange(ValidateIoPoints(options.RunningStateIos, "运行状态"));

            // 验证停止状态 IO 点
            errors.AddRange(ValidateIoPoints(options.StoppedStateIos, "停止状态"));

            // 检查是否有 IO 点在两个状态列表中都出现
            var runningBits = options.RunningStateIos.Select(p => p.BitNumber).ToHashSet();
            var stoppedBits = options.StoppedStateIos.Select(p => p.BitNumber).ToHashSet();
            var overlappingBits = runningBits.Intersect(stoppedBits).ToList();

            if (overlappingBits.Count > 0) {
                warnings.Add($"以下 IO 端口同时在运行状态和停止状态中配置: {string.Join(", ", overlappingBits)}");
            }

            return new ValidationResult {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }

        /// <summary>
        /// 验证 IO 点列表。
        /// </summary>
        private static List<string> ValidateIoPoints(List<IoLinkagePoint> ioPoints, string category) {
            var errors = new List<string>();

            // 检查重复的 IO 端口
            var duplicates = ioPoints
                .GroupBy(p => p.BitNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0) {
                errors.Add($"{category}: 存在重复的 IO 端口: {string.Join(", ", duplicates)}");
            }

            // 验证每个 IO 点
            for (int i = 0; i < ioPoints.Count; i++) {
                var point = ioPoints[i];
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                var context = new ValidationContext(point);
                if (!Validator.TryValidateObject(point, context, validationResults, validateAllProperties: true)) {
                    errors.AddRange(validationResults.Select(r => $"{category} IO 点 {i + 1}: {r.ErrorMessage}"));
                }
            }

            return errors;
        }

        /// <summary>
        /// 验证 IP 地址格式。
        /// </summary>
        private static bool IsValidIpAddress(string ipAddress) {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;

            var parts = ipAddress.Split('.');
            if (parts.Length != 4) return false;

            return parts.All(part => {
                if (!int.TryParse(part, out int value)) return false;
                return value >= 0 && value <= 255;
            });
        }

        /// <summary>
        /// 执行配置预检查，验证配置的有效性并返回详细报告。
        /// </summary>
        public static string PreCheckConfiguration<T>(T config, Func<T, ValidationResult> validator) where T : class {
            if (config == null) {
                return "配置对象为 null";
            }

            var result = validator(config);
            
            if (result.IsValid) {
                var report = new StringBuilder();
                report.AppendLine("✓ 配置验证通过");
                
                if (result.Warnings.Count > 0) {
                    report.AppendLine();
                    report.AppendLine($"发现 {result.Warnings.Count} 个警告：");
                    foreach (var warning in result.Warnings) {
                        report.AppendLine($"  ⚠ {warning}");
                    }
                }
                
                return report.ToString();
            }
            
            return result.GetFormattedErrors();
        }
    }
}
