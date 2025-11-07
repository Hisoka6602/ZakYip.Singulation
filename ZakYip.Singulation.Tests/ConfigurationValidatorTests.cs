using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Utils;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 ConfigurationValidator 的正确性。
    /// </summary>
    internal sealed class ConfigurationValidatorTests {

        #region ControllerOptions Validation Tests

        [MiniFact]
        public void ValidControllerOptions_ShouldPass() {
            var options = new ControllerOptions {
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

            var result = ConfigurationValidator.ValidateControllerOptions(options);

            MiniAssert.Equal(true, result.IsValid, "有效的控制器配置应该通过验证");
            MiniAssert.Equal(0, result.Errors.Count, "不应有错误");
        }

        [MiniFact]
        public void ControllerOptions_WithNullVendor_ShouldFail() {
            var options = new ControllerOptions {
                Vendor = "",
                ControllerIp = "192.168.5.11"
            };

            var result = ConfigurationValidator.ValidateControllerOptions(options);

            MiniAssert.Equal(false, result.IsValid, "空的厂商标识应该验证失败");
            MiniAssert.True(result.Errors.Count > 0, "应该有错误信息");
        }

        [MiniFact]
        public void ControllerOptions_WithInvalidIp_ShouldFail() {
            var options = new ControllerOptions {
                Vendor = "leadshine",
                ControllerIp = "999.999.999.999"
            };

            var result = ConfigurationValidator.ValidateControllerOptions(options);

            MiniAssert.Equal(false, result.IsValid, "无效的IP地址应该验证失败");
            MiniAssert.True(result.Errors.Any(e => e.Contains("IP")), "错误信息应该包含 IP");
        }

        [MiniFact]
        public void ControllerOptions_WithZeroSpeed_ShouldWarn() {
            var options = new ControllerOptions {
                Vendor = "leadshine",
                ControllerIp = "192.168.5.11",
                LocalFixedSpeedMmps = 0m,
                Template = new DriverOptionsTemplateOptions {
                    ScrewPitchMm = 5m,
                    MaxRpm = 1813m,
                    MaxAccelRpmPerSec = 1511m,
                    MaxDecelRpmPerSec = 1511m
                }
            };

            var result = ConfigurationValidator.ValidateControllerOptions(options);

            MiniAssert.Equal(true, result.IsValid, "配置应该有效");
            MiniAssert.True(result.Warnings.Count > 0, "应该有警告信息");
            MiniAssert.True(result.Warnings.Any(w => w.Contains("0")), "警告信息应该提及速度为0");
        }

        [MiniFact]
        public void DriverOptionsTemplate_WithoutMechanism_ShouldFail() {
            var template = new DriverOptionsTemplateOptions {
                Card = 8,
                Port = 2,
                GearRatio = 1m,
                ScrewPitchMm = 0m,
                PulleyDiameterMm = 0m,
                PulleyPitchDiameterMm = 0m,
                MaxRpm = 1813m,
                MaxAccelRpmPerSec = 1511m,
                MaxDecelRpmPerSec = 1511m
            };

            var result = ConfigurationValidator.ValidateDriverOptionsTemplate(template);

            MiniAssert.Equal(false, result.IsValid, "缺少传动机构应该验证失败");
            MiniAssert.True(result.Errors.Any(e => e.Contains("传动机构")), "错误信息应该提及传动机构");
        }

        [MiniFact]
        public void DriverOptionsTemplate_WithMultipleMechanisms_ShouldWarn() {
            var template = new DriverOptionsTemplateOptions {
                Card = 8,
                Port = 2,
                GearRatio = 1m,
                ScrewPitchMm = 5m,
                PulleyDiameterMm = 50m,
                MaxRpm = 1813m,
                MaxAccelRpmPerSec = 1511m,
                MaxDecelRpmPerSec = 1511m
            };

            var result = ConfigurationValidator.ValidateDriverOptionsTemplate(template);

            MiniAssert.Equal(true, result.IsValid, "配置应该有效");
            MiniAssert.True(result.Warnings.Count > 0, "应该有警告信息");
            MiniAssert.True(result.Warnings.Any(w => w.Contains("多种传动机构")), "警告信息应该提及多种传动机构");
        }

        #endregion

        #region SpeedLinkageOptions Validation Tests

        [MiniFact]
        public void ValidSpeedLinkageOptions_ShouldPass() {
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001, 1002 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };

            var result = ConfigurationValidator.ValidateSpeedLinkageOptions(options);

            MiniAssert.Equal(true, result.IsValid, "有效的速度联动配置应该通过验证");
            MiniAssert.Equal(0, result.Errors.Count, "不应有错误");
        }

        [MiniFact]
        public void SpeedLinkageOptions_WithEnabledButNoGroups_ShouldWarn() {
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new()
            };

            var result = ConfigurationValidator.ValidateSpeedLinkageOptions(options);

            MiniAssert.Equal(true, result.IsValid, "配置应该有效");
            MiniAssert.True(result.Warnings.Count > 0, "应该有警告信息");
        }

        [MiniFact]
        public void SpeedLinkageOptions_WithDuplicateIos_ShouldWarn() {
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    },
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1002 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveLow }
                        }
                    }
                }
            };

            var result = ConfigurationValidator.ValidateSpeedLinkageOptions(options);

            MiniAssert.Equal(true, result.IsValid, "配置应该有效");
            MiniAssert.True(result.Warnings.Count > 0, "应该有重复IO的警告");
            MiniAssert.True(result.Warnings.Any(w => w.Contains("重复")), "警告信息应该提及重复");
        }

        [MiniFact]
        public void SpeedLinkageGroup_WithDuplicateAxisIds_ShouldFail() {
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001, 1001 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };

            var result = ConfigurationValidator.ValidateSpeedLinkageOptions(options);

            MiniAssert.Equal(false, result.IsValid, "重复的轴ID应该验证失败");
            MiniAssert.True(result.Errors.Any(e => e.Contains("重复")), "错误信息应该提及重复");
        }

        [MiniFact]
        public void SpeedLinkageGroup_WithInvalidAxisId_ShouldFail() {
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 0 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };

            var result = ConfigurationValidator.ValidateSpeedLinkageOptions(options);

            MiniAssert.Equal(false, result.IsValid, "轴ID为0应该验证失败");
            MiniAssert.True(result.Errors.Any(e => e.Contains("轴 ID 必须大于 0")), "错误信息应该提及轴ID必须大于0");
        }

        #endregion

        #region IoLinkageOptions Validation Tests

        [MiniFact]
        public void ValidIoLinkageOptions_ShouldPass() {
            var options = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveLow }
                },
                StoppedStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveHigh }
                }
            };

            var result = ConfigurationValidator.ValidateIoLinkageOptions(options);

            MiniAssert.Equal(true, result.IsValid, "有效的IO联动配置应该通过验证");
            MiniAssert.Equal(0, result.Errors.Count, "不应有错误");
        }

        [MiniFact]
        public void IoLinkageOptions_WithEnabledButNoIos_ShouldWarn() {
            var options = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new(),
                StoppedStateIos = new()
            };

            var result = ConfigurationValidator.ValidateIoLinkageOptions(options);

            MiniAssert.Equal(true, result.IsValid, "配置应该有效");
            MiniAssert.True(result.Warnings.Count > 0, "应该有警告信息");
        }

        [MiniFact]
        public void IoLinkageOptions_WithOverlappingIos_ShouldWarn() {
            var options = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveLow }
                },
                StoppedStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveHigh }
                }
            };

            var result = ConfigurationValidator.ValidateIoLinkageOptions(options);

            MiniAssert.Equal(true, result.IsValid, "配置应该有效");
            MiniAssert.True(result.Warnings.Count > 0, "应该有重叠IO的警告");
        }

        [MiniFact]
        public void IoLinkageOptions_WithDuplicateInSameState_ShouldFail() {
            var options = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveLow },
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveLow }
                }
            };

            var result = ConfigurationValidator.ValidateIoLinkageOptions(options);

            MiniAssert.Equal(false, result.IsValid, "同一状态中的重复IO应该验证失败");
            MiniAssert.True(result.Errors.Any(e => e.Contains("重复")), "错误信息应该提及重复");
        }

        #endregion

        #region PreCheck Tests

        [MiniFact]
        public void PreCheck_WithValidConfig_ShouldReturnSuccessMessage() {
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

            var report = ConfigurationValidator.PreCheckConfiguration(
                options, 
                ConfigurationValidator.ValidateSpeedLinkageOptions
            );

            MiniAssert.True(report.Contains("✓"), "报告应该包含成功标记");
            MiniAssert.True(report.Contains("通过"), "报告应该提及验证通过");
        }

        [MiniFact]
        public void PreCheck_WithInvalidConfig_ShouldReturnFormattedErrors() {
            var options = new SpeedLinkageOptions {
                Enabled = true,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 0 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };

            var report = ConfigurationValidator.PreCheckConfiguration(
                options, 
                ConfigurationValidator.ValidateSpeedLinkageOptions
            );

            MiniAssert.True(report.Contains("失败"), "报告应该提及验证失败");
            MiniAssert.True(report.Contains("轴 ID"), "报告应该包含错误详情");
        }

        #endregion
    }
}
