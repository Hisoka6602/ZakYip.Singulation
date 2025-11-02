using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts.Dto;

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
                    new IoLinkagePoint { BitNumber = 1, State = IoState.High }
                },
                StoppedStateIos = new() {
                    new IoLinkagePoint { BitNumber = 2, State = IoState.Low }
                }
            };

            MiniAssert.Equal(false, options.Enabled, "Enabled 应该为 false");
            MiniAssert.Equal(1, options.RunningStateIos.Count, "RunningStateIos 应该有 1 个元素");
            MiniAssert.Equal(1, options.StoppedStateIos.Count, "StoppedStateIos 应该有 1 个元素");
            MiniAssert.Equal(1, options.RunningStateIos[0].BitNumber, "第一个运行状态 IO 位号应该为 1");
            MiniAssert.Equal(IoState.High, options.RunningStateIos[0].State, "第一个运行状态 IO 应该为高电平");
        }

        [MiniFact]
        public void IoLinkagePointDefaultsAreZero() {
            var point = new IoLinkagePoint();

            MiniAssert.Equal(0, point.BitNumber, "默认 BitNumber 应该为 0");
            MiniAssert.Equal(IoState.Low, point.State, "默认 State 应该为 Low (0)");
        }

        [MiniFact]
        public void IoLinkagePointCanStoreAllStates() {
            var lowPoint = new IoLinkagePoint { BitNumber = 5, State = IoState.Low };
            var highPoint = new IoLinkagePoint { BitNumber = 10, State = IoState.High };

            MiniAssert.Equal(5, lowPoint.BitNumber, "低电平点位号应该为 5");
            MiniAssert.Equal(IoState.Low, lowPoint.State, "低电平点状态应该为 Low");
            MiniAssert.Equal(10, highPoint.BitNumber, "高电平点位号应该为 10");
            MiniAssert.Equal(IoState.High, highPoint.State, "高电平点状态应该为 High");
        }

        [MiniFact]
        public void RecordSemanticsCopyCorrectly() {
            var original = new IoLinkageOptions {
                Enabled = true,
                RunningStateIos = new() {
                    new IoLinkagePoint { BitNumber = 3, State = IoState.Low }
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
