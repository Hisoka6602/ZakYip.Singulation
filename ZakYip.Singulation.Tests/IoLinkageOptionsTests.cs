using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 IoLinkageOptions 和 IoLinkagePoint 类的正确性。
    /// </summary>
    internal sealed class IoLinkageOptionsTests {

        [MiniFact]
        public void DefaultValuesAreCorrect() {
            var options = new IoLinkageOptions();

            MiniAssert.Equal(true, options.Enabled, "默认 Enabled 应该为 true");
            MiniAssert.NotNull(options.RunningStateIos, "RunningStateIos 不应为 null");
            MiniAssert.NotNull(options.StoppedStateIos, "StoppedStateIos 不应为 null");
            MiniAssert.Equal(0, options.RunningStateIos.Count, "默认 RunningStateIos 应该为空");
            MiniAssert.Equal(0, options.StoppedStateIos.Count, "默认 StoppedStateIos 应该为空");
        }

        [MiniFact]
        public void CanCreateWithCustomValues() {
            var options = new IoLinkageOptions {
                Enabled = false,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 1, Level = TriggerLevel.ActiveLow }
                },
                StoppedStateIos = new() {
                    new IoLinkagePoint { BitNumber = 2, Level = TriggerLevel.ActiveHigh }
                }
            };

            MiniAssert.Equal(false, options.Enabled, "Enabled 应该为 false");
            MiniAssert.Equal(1, options.RunningStateIos.Count, "RunningStateIos 应该有 1 个元素");
            MiniAssert.Equal(1, options.StoppedStateIos.Count, "StoppedStateIos 应该有 1 个元素");
            MiniAssert.Equal(1, options.RunningStateIos[0].BitNumber, "第一个运行状态 IO 位号应该为 1");
            MiniAssert.Equal(TriggerLevel.ActiveLow, options.RunningStateIos[0].Level, "第一个运行状态 IO 应该为低电平");
        }

        [MiniFact]
        public void IoLinkagePointDefaultsAreZero() {
            var point = new IoLinkagePoint();

            MiniAssert.Equal(0, point.BitNumber, "默认 BitNumber 应该为 0");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, point.Level, "默认 Level 应该为 ActiveHigh (0)");
        }

        [MiniFact]
        public void IoLinkagePointCanStoreAllLevels() {
            var highPoint = new IoLinkagePoint { BitNumber = 5, Level = TriggerLevel.ActiveHigh };
            var lowPoint = new IoLinkagePoint { BitNumber = 10, Level = TriggerLevel.ActiveLow };

            MiniAssert.Equal(5, highPoint.BitNumber, "高电平点位号应该为 5");
            MiniAssert.Equal(TriggerLevel.ActiveHigh, highPoint.Level, "高电平点状态应该为 ActiveHigh");
            MiniAssert.Equal(10, lowPoint.BitNumber, "低电平点位号应该为 10");
            MiniAssert.Equal(TriggerLevel.ActiveLow, lowPoint.Level, "低电平点状态应该为 ActiveLow");
        }

        [MiniFact]
        public void RecordSemanticsCopyCorrectly() {
            var original = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, Level = TriggerLevel.ActiveHigh }
                }
            };

            // 使用 with 创建副本
            var copy = original with { Enabled = false };

            MiniAssert.Equal(true, original.Enabled, "原始对象 Enabled 应该为 true");
            MiniAssert.Equal(false, copy.Enabled, "副本 Enabled 应该为 false");
            MiniAssert.Equal(1, copy.RunningStateIos.Count, "副本应该保留 RunningStateIos");
        }
    }
}
