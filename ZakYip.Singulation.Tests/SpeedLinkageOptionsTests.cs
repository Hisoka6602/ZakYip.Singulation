using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 SpeedLinkageOptions 和相关类的正确性。
    /// </summary>
    internal sealed class SpeedLinkageOptionsTests {

        [MiniFact]
        public void DefaultValuesAreCorrect() {
            var options = new SpeedLinkageOptions();

            MiniAssert.Equal(true, options.Enabled, "默认 Enabled 应该为 true");
            MiniAssert.NotNull(options.LinkageGroups, "LinkageGroups 不应为 null");
            MiniAssert.Equal(0, options.LinkageGroups.Count, "默认 LinkageGroups 应该为空");
        }

        [MiniFact]
        public void CanCreateWithCustomValues() {
            var options = new SpeedLinkageOptions {
                Enabled = false,
                LinkageGroups = new() {
                    new SpeedLinkageGroup {
                        AxisIds = new() { 1001, 1002 },
                        IoPoints = new() {
                            new SpeedLinkageIoPoint { BitNumber = 3, LevelWhenStopped = TriggerLevel.ActiveHigh },
                            new SpeedLinkageIoPoint { BitNumber = 4, LevelWhenStopped = TriggerLevel.ActiveHigh }
                        }
                    }
                }
            };

            MiniAssert.Equal(false, options.Enabled, "Enabled 应该为 false");
            MiniAssert.Equal(1, options.LinkageGroups.Count, "LinkageGroups 应该有 1 个元素");
            MiniAssert.Equal(2, options.LinkageGroups[0].AxisIds.Count, "第一个组应该有 2 个轴ID");
            MiniAssert.Equal(1001, options.LinkageGroups[0].AxisIds[0], "第一个轴ID应该为 1001");
            MiniAssert.Equal(2, options.LinkageGroups[0].IoPoints.Count, "第一个组应该有 2 个IO点");
        }

        [MiniFact]
        public void SpeedLinkageGroupDefaultsAreEmpty() {
            var group = new SpeedLinkageGroup();

            MiniAssert.NotNull(group.AxisIds, "AxisIds 不应为 null");
            MiniAssert.NotNull(group.IoPoints, "IoPoints 不应为 null");
            MiniAssert.Equal(0, group.AxisIds.Count, "默认 AxisIds 应该为空");
            MiniAssert.Equal(0, group.IoPoints.Count, "默认 IoPoints 应该为空");
        }

        [MiniFact]
        public void SpeedLinkageIoPointDefaultsAreZero() {
            var point = new SpeedLinkageIoPoint { BitNumber = 0, LevelWhenStopped = TriggerLevel.ActiveHigh };

            MiniAssert.Equal(0, point.BitNumber, "默认 BitNumber 应该为 0");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, point.LevelWhenStopped, "默认 LevelWhenStopped 应该为 ActiveHigh (0)");
        }

        [MiniFact]
        public void SpeedLinkageIoPointCanStoreAllLevels() {
            var highPoint = new SpeedLinkageIoPoint { BitNumber = 5, LevelWhenStopped = TriggerLevel.ActiveHigh };
            var lowPoint = new SpeedLinkageIoPoint { BitNumber = 10, LevelWhenStopped = TriggerLevel.ActiveLow };

            MiniAssert.Equal(5, highPoint.BitNumber, "高电平点位号应该为 5");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, highPoint.LevelWhenStopped, "高电平点状态应该为 ActiveHigh");
            MiniAssert.Equal(10, lowPoint.BitNumber, "低电平点位号应该为 10");
            MiniAssert.Equal(TriggerLevel.ActiveLow, lowPoint.LevelWhenStopped, "低电平点状态应该为 ActiveLow");
        }

        [MiniFact]
        public void RecordSemanticsCopyCorrectly() {
            var original = new SpeedLinkageOptions {
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

            // 使用 with 创建副本
            var copy = original with { Enabled = false };

            MiniAssert.Equal(true, original.Enabled, "原始对象 Enabled 应该为 true");
            MiniAssert.Equal(false, copy.Enabled, "副本 Enabled 应该为 false");
            MiniAssert.Equal(1, copy.LinkageGroups.Count, "副本应该保留 LinkageGroups");
        }

        [MiniFact]
        public void CanCreateMultipleLinkageGroups() {
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

            MiniAssert.Equal(2, options.LinkageGroups.Count, "应该有 2 个联动组");
            MiniAssert.Equal(2, options.LinkageGroups[0].AxisIds.Count, "第一个组应该有 2 个轴ID");
            MiniAssert.Equal(2, options.LinkageGroups[1].AxisIds.Count, "第二个组应该有 2 个轴ID");
            MiniAssert.Equal(1003, options.LinkageGroups[1].AxisIds[0], "第二个组第一个轴ID应该为 1003");
        }
    }
}
