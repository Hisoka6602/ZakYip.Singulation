using System;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 LeadshineCabinetIoOptions 配置的正确性，特别是嵌套的输入和输出点位配置。
    /// </summary>
    internal sealed class LeadshineCabinetIoOptionsTests {

        [MiniFact]
        public void DefaultValuesAreCorrect() {
            var options = new LeadshineCabinetIoOptions();
            
            MiniAssert.Equal(false, options.Enabled, "默认应禁用");
            MiniAssert.Equal(50, options.PollingIntervalMs, "轮询间隔默认应为 50ms");
            MiniAssert.NotEqual(null, options.CabinetInputPoint, "CabinetInputPoint 不应为 null");
            MiniAssert.NotEqual(null, options.CabinetIndicatorPoint, "CabinetIndicatorPoint 不应为 null");
        }

        [MiniFact]
        public void InputPointDefaultValuesAreCorrect() {
            var inputPoint = new CabinetInputPoint();
            
            MiniAssert.Equal(-1, inputPoint.EmergencyStop, "急停按键默认应禁用");
            MiniAssert.Equal(-1, inputPoint.Stop, "停止按键默认应禁用");
            MiniAssert.Equal(-1, inputPoint.Start, "启动按键默认应禁用");
            MiniAssert.Equal(-1, inputPoint.Reset, "复位按键默认应禁用");
            MiniAssert.Equal(-1, inputPoint.RemoteLocalMode, "远程/本地模式默认应禁用");
            MiniAssert.Equal(false, inputPoint.ActiveLow, "全局低电平有效默认应为 false");
            MiniAssert.Equal(null, inputPoint.EmergencyStopActiveLow, "急停低电平有效默认应为 null");
            MiniAssert.Equal(null, inputPoint.StopActiveLow, "停止低电平有效默认应为 null");
            MiniAssert.Equal(null, inputPoint.StartActiveLow, "启动低电平有效默认应为 null");
            MiniAssert.Equal(null, inputPoint.ResetActiveLow, "复位低电平有效默认应为 null");
            MiniAssert.Equal(null, inputPoint.RemoteLocalActiveLow, "远程/本地模式低电平有效默认应为 null");
            MiniAssert.Equal(true, inputPoint.RemoteLocalActiveHigh, "RemoteLocalActiveHigh 默认应为 true");
        }

        [MiniFact]
        public void IndicatorPointDefaultValuesAreCorrect() {
            var indicatorPoint = new CabinetIndicatorPoint();
            
            MiniAssert.Equal(-1, indicatorPoint.RedLight, "红灯默认应禁用");
            MiniAssert.Equal(-1, indicatorPoint.YellowLight, "黄灯默认应禁用");
            MiniAssert.Equal(-1, indicatorPoint.GreenLight, "绿灯默认应禁用");
            MiniAssert.Equal(-1, indicatorPoint.StartButtonLight, "启动按钮灯默认应禁用");
            MiniAssert.Equal(-1, indicatorPoint.StopButtonLight, "停止按钮灯默认应禁用");
            MiniAssert.Equal(-1, indicatorPoint.RemoteConnectionLight, "远程连接指示灯默认应禁用");
            
            MiniAssert.Equal(true, indicatorPoint.LightActiveLow, "全局灯光低电平有效默认应为 true（低电平亮灯）");
            MiniAssert.Equal(null, indicatorPoint.RedLightActiveLow, "红灯低电平有效默认应为 null");
            MiniAssert.Equal(null, indicatorPoint.YellowLightActiveLow, "黄灯低电平有效默认应为 null");
            MiniAssert.Equal(null, indicatorPoint.GreenLightActiveLow, "绿灯低电平有效默认应为 null");
            MiniAssert.Equal(null, indicatorPoint.StartButtonLightActiveLow, "启动按钮灯低电平有效默认应为 null");
            MiniAssert.Equal(null, indicatorPoint.StopButtonLightActiveLow, "停止按钮灯低电平有效默认应为 null");
            MiniAssert.Equal(null, indicatorPoint.RemoteConnectionLightActiveLow, "远程连接指示灯低电平有效默认应为 null");
        }

        [MiniFact]
        public void IndividualInvertLogicOverridesGlobal_InputPoint() {
            var inputPoint = new CabinetInputPoint {
                ActiveLow = false,
                EmergencyStopActiveLow = true,
                StopActiveLow = null,
                StartActiveLow = false,
                ResetActiveLow = true
            };
            
            // 验证独立配置的按键使用自己的电平配置
            MiniAssert.Equal(true, inputPoint.EmergencyStopActiveLow, "急停应使用独立配置 true");
            MiniAssert.Equal(null, inputPoint.StopActiveLow, "停止为 null 应回退到全局配置");
            MiniAssert.Equal(false, inputPoint.StartActiveLow, "启动应使用独立配置 false");
            MiniAssert.Equal(true, inputPoint.ResetActiveLow, "复位应使用独立配置 true");
        }

        [MiniFact]
        public void IndividualInvertLogicOverridesGlobal_IndicatorPoint() {
            var indicatorPoint = new CabinetIndicatorPoint {
                LightActiveLow = true,
                RedLightActiveLow = false,
                YellowLightActiveLow = null,
                GreenLightActiveLow = true,
                StartButtonLightActiveLow = false
            };
            
            // 验证独立配置的灯使用自己的电平配置
            MiniAssert.Equal(false, indicatorPoint.RedLightActiveLow, "红灯应使用独立配置 false");
            MiniAssert.Equal(null, indicatorPoint.YellowLightActiveLow, "黄灯为 null 应回退到全局配置");
            MiniAssert.Equal(true, indicatorPoint.GreenLightActiveLow, "绿灯应使用独立配置 true");
            MiniAssert.Equal(false, indicatorPoint.StartButtonLightActiveLow, "启动按钮灯应使用独立配置 false");
        }

        [MiniFact]
        public void NestedStructureIsCorrect() {
            var options = new LeadshineCabinetIoOptions {
                Enabled = true,
                PollingIntervalMs = 100,
                CabinetInputPoint = new CabinetInputPoint {
                    Start = 1,
                    Stop = 2,
                    Reset = 3,
                    EmergencyStop = 4,
                    RemoteLocalMode = 5
                },
                CabinetIndicatorPoint = new CabinetIndicatorPoint {
                    RedLight = 10,
                    YellowLight = 11,
                    GreenLight = 12,
                    StartButtonLight = 13,
                    StopButtonLight = 14,
                    RemoteConnectionLight = 15
                }
            };
            
            // 验证顶层配置
            MiniAssert.Equal(true, options.Enabled, "Enabled 应为 true");
            MiniAssert.Equal(100, options.PollingIntervalMs, "轮询间隔应为 100ms");
            
            // 验证输入点位配置
            MiniAssert.Equal(1, options.CabinetInputPoint.Start, "Start 应为 1");
            MiniAssert.Equal(2, options.CabinetInputPoint.Stop, "Stop 应为 2");
            MiniAssert.Equal(3, options.CabinetInputPoint.Reset, "Reset 应为 3");
            MiniAssert.Equal(4, options.CabinetInputPoint.EmergencyStop, "EmergencyStop 应为 4");
            MiniAssert.Equal(5, options.CabinetInputPoint.RemoteLocalMode, "RemoteLocalMode 应为 5");
            
            // 验证指示灯点位配置
            MiniAssert.Equal(10, options.CabinetIndicatorPoint.RedLight, "RedLight 应为 10");
            MiniAssert.Equal(11, options.CabinetIndicatorPoint.YellowLight, "YellowLight 应为 11");
            MiniAssert.Equal(12, options.CabinetIndicatorPoint.GreenLight, "GreenLight 应为 12");
            MiniAssert.Equal(13, options.CabinetIndicatorPoint.StartButtonLight, "StartButtonLight 应为 13");
            MiniAssert.Equal(14, options.CabinetIndicatorPoint.StopButtonLight, "StopButtonLight 应为 14");
            MiniAssert.Equal(15, options.CabinetIndicatorPoint.RemoteConnectionLight, "RemoteConnectionLight 应为 15");
        }
    }
}
