using System;
using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 LeadshineSafetyIoOptions 配置的正确性，特别是各按键独立的反转逻辑配置。
    /// </summary>
    internal sealed class LeadshineSafetyIoOptionsTests {

        [MiniFact]
        public void DefaultValuesAreCorrect() {
            var options = new LeadshineSafetyIoOptions();
            
            MiniAssert.Equal(-1, options.EmergencyStopBit, "急停按键默认应禁用");
            MiniAssert.Equal(-1, options.StopBit, "停止按键默认应禁用");
            MiniAssert.Equal(-1, options.StartBit, "启动按键默认应禁用");
            MiniAssert.Equal(-1, options.ResetBit, "复位按键默认应禁用");
            MiniAssert.Equal(50, options.PollingIntervalMs, "轮询间隔默认应为 50ms");
            MiniAssert.Equal(false, options.InvertLogic, "全局反转逻辑默认应为 false");
            MiniAssert.Equal(null, options.InvertEmergencyStopLogic, "急停反转逻辑默认应为 null");
            MiniAssert.Equal(null, options.InvertStopLogic, "停止反转逻辑默认应为 null");
            MiniAssert.Equal(null, options.InvertStartLogic, "启动反转逻辑默认应为 null");
            MiniAssert.Equal(null, options.InvertResetLogic, "复位反转逻辑默认应为 null");
            
            // 新增三色灯和按钮灯默认值测试
            MiniAssert.Equal(-1, options.RedLightBit, "红灯默认应禁用");
            MiniAssert.Equal(-1, options.YellowLightBit, "黄灯默认应禁用");
            MiniAssert.Equal(-1, options.GreenLightBit, "绿灯默认应禁用");
            MiniAssert.Equal(-1, options.StartButtonLightBit, "启动按钮灯默认应禁用");
            MiniAssert.Equal(-1, options.StopButtonLightBit, "停止按钮灯默认应禁用");
            
            // 新增灯光反转逻辑默认值测试
            MiniAssert.Equal(true, options.InvertLightLogic, "全局灯光反转逻辑默认应为 true（低电平亮灯）");
            MiniAssert.Equal(null, options.InvertRedLightLogic, "红灯反转逻辑默认应为 null");
            MiniAssert.Equal(null, options.InvertYellowLightLogic, "黄灯反转逻辑默认应为 null");
            MiniAssert.Equal(null, options.InvertGreenLightLogic, "绿灯反转逻辑默认应为 null");
            MiniAssert.Equal(null, options.InvertStartButtonLightLogic, "启动按钮灯反转逻辑默认应为 null");
            MiniAssert.Equal(null, options.InvertStopButtonLightLogic, "停止按钮灯反转逻辑默认应为 null");
            
            // 新增远程连接指示灯默认值测试
            MiniAssert.Equal(-1, options.RemoteConnectionLightBit, "远程连接指示灯默认应禁用");
            MiniAssert.Equal(null, options.InvertRemoteConnectionLightLogic, "远程连接指示灯反转逻辑默认应为 null");
        }

        [MiniFact]
        public void IndividualInvertLogicOverridesGlobal() {
            var options = new LeadshineSafetyIoOptions {
                InvertLogic = false,
                InvertEmergencyStopLogic = true,
                InvertStopLogic = null,
                InvertStartLogic = false,
                InvertResetLogic = true
            };
            
            // 测试 null 合并运算符的行为
            var emergencyStopInvert = options.InvertEmergencyStopLogic ?? options.InvertLogic;
            var stopInvert = options.InvertStopLogic ?? options.InvertLogic;
            var startInvert = options.InvertStartLogic ?? options.InvertLogic;
            var resetInvert = options.InvertResetLogic ?? options.InvertLogic;
            
            MiniAssert.Equal(true, emergencyStopInvert, "急停应使用独立配置 true");
            MiniAssert.Equal(false, stopInvert, "停止应使用全局默认 false");
            MiniAssert.Equal(false, startInvert, "启动应使用独立配置 false");
            MiniAssert.Equal(true, resetInvert, "复位应使用独立配置 true");
        }

        [MiniFact]
        public void AllIndividualNullUsesGlobalTrue() {
            var options = new LeadshineSafetyIoOptions {
                InvertLogic = true,
                InvertEmergencyStopLogic = null,
                InvertStopLogic = null,
                InvertStartLogic = null,
                InvertResetLogic = null
            };
            
            var emergencyStopInvert = options.InvertEmergencyStopLogic ?? options.InvertLogic;
            var stopInvert = options.InvertStopLogic ?? options.InvertLogic;
            var startInvert = options.InvertStartLogic ?? options.InvertLogic;
            var resetInvert = options.InvertResetLogic ?? options.InvertLogic;
            
            MiniAssert.Equal(true, emergencyStopInvert, "所有按键都应使用全局默认 true");
            MiniAssert.Equal(true, stopInvert, "所有按键都应使用全局默认 true");
            MiniAssert.Equal(true, startInvert, "所有按键都应使用全局默认 true");
            MiniAssert.Equal(true, resetInvert, "所有按键都应使用全局默认 true");
        }

        [MiniFact]
        public void MixedButtonTypes() {
            // 场景：急停和启动为常闭（需要反转），停止和复位为常开（不需要反转）
            var options = new LeadshineSafetyIoOptions {
                EmergencyStopBit = 0,
                StopBit = 1,
                StartBit = 2,
                ResetBit = 3,
                InvertLogic = false,  // 全局默认常开
                InvertEmergencyStopLogic = true,  // 急停常闭
                InvertStopLogic = false,  // 停止常开（显式指定）
                InvertStartLogic = true,  // 启动常闭
                InvertResetLogic = false  // 复位常开（显式指定）
            };
            
            MiniAssert.Equal(0, options.EmergencyStopBit, "急停按键应配置到端口 0");
            MiniAssert.Equal(1, options.StopBit, "停止按键应配置到端口 1");
            MiniAssert.Equal(2, options.StartBit, "启动按键应配置到端口 2");
            MiniAssert.Equal(3, options.ResetBit, "复位按键应配置到端口 3");
            
            var emergencyStopInvert = options.InvertEmergencyStopLogic ?? options.InvertLogic;
            var stopInvert = options.InvertStopLogic ?? options.InvertLogic;
            var startInvert = options.InvertStartLogic ?? options.InvertLogic;
            var resetInvert = options.InvertResetLogic ?? options.InvertLogic;
            
            MiniAssert.Equal(true, emergencyStopInvert, "急停应反转（常闭）");
            MiniAssert.Equal(false, stopInvert, "停止不应反转（常开）");
            MiniAssert.Equal(true, startInvert, "启动应反转（常闭）");
            MiniAssert.Equal(false, resetInvert, "复位不应反转（常开）");
        }

        [MiniFact]
        public void TriColorLightConfiguration() {
            // 场景：配置三色灯和按钮灯的输出端口
            var options = new LeadshineSafetyIoOptions {
                RedLightBit = 10,
                YellowLightBit = 11,
                GreenLightBit = 12,
                StartButtonLightBit = 13,
                StopButtonLightBit = 14
            };
            
            MiniAssert.Equal(10, options.RedLightBit, "红灯应配置到端口 10");
            MiniAssert.Equal(11, options.YellowLightBit, "黄灯应配置到端口 11");
            MiniAssert.Equal(12, options.GreenLightBit, "绿灯应配置到端口 12");
            MiniAssert.Equal(13, options.StartButtonLightBit, "启动按钮灯应配置到端口 13");
            MiniAssert.Equal(14, options.StopButtonLightBit, "停止按钮灯应配置到端口 14");
        }

        [MiniFact]
        public void LightInvertLogicConfiguration() {
            // 场景：配置灯光反转逻辑
            var options = new LeadshineSafetyIoOptions {
                InvertLightLogic = false,  // 全局默认高电平亮灯
                InvertRedLightLogic = true,  // 红灯低电平亮灯
                InvertYellowLightLogic = null,  // 黄灯使用全局配置
                InvertGreenLightLogic = false,  // 绿灯高电平亮灯（显式指定）
                InvertStartButtonLightLogic = true,  // 启动按钮灯低电平亮灯
                InvertStopButtonLightLogic = null  // 停止按钮灯使用全局配置
            };
            
            var redInvert = options.InvertRedLightLogic ?? options.InvertLightLogic;
            var yellowInvert = options.InvertYellowLightLogic ?? options.InvertLightLogic;
            var greenInvert = options.InvertGreenLightLogic ?? options.InvertLightLogic;
            var startButtonInvert = options.InvertStartButtonLightLogic ?? options.InvertLightLogic;
            var stopButtonInvert = options.InvertStopButtonLightLogic ?? options.InvertLightLogic;
            
            MiniAssert.Equal(true, redInvert, "红灯应使用独立配置（低电平亮灯）");
            MiniAssert.Equal(false, yellowInvert, "黄灯应使用全局默认（高电平亮灯）");
            MiniAssert.Equal(false, greenInvert, "绿灯应使用独立配置（高电平亮灯）");
            MiniAssert.Equal(true, startButtonInvert, "启动按钮灯应使用独立配置（低电平亮灯）");
            MiniAssert.Equal(false, stopButtonInvert, "停止按钮灯应使用全局默认（高电平亮灯）");
        }

        [MiniFact]
        public void AllLightsInvertedConfiguration() {
            // 场景：所有灯都使用低电平亮灯
            var options = new LeadshineSafetyIoOptions {
                InvertLightLogic = true,  // 全局低电平亮灯
                InvertRedLightLogic = null,
                InvertYellowLightLogic = null,
                InvertGreenLightLogic = null,
                InvertStartButtonLightLogic = null,
                InvertStopButtonLightLogic = null
            };
            
            var redInvert = options.InvertRedLightLogic ?? options.InvertLightLogic;
            var yellowInvert = options.InvertYellowLightLogic ?? options.InvertLightLogic;
            var greenInvert = options.InvertGreenLightLogic ?? options.InvertLightLogic;
            var startButtonInvert = options.InvertStartButtonLightLogic ?? options.InvertLightLogic;
            var stopButtonInvert = options.InvertStopButtonLightLogic ?? options.InvertLightLogic;
            
            MiniAssert.Equal(true, redInvert, "所有灯都应使用全局配置（低电平亮灯）");
            MiniAssert.Equal(true, yellowInvert, "所有灯都应使用全局配置（低电平亮灯）");
            MiniAssert.Equal(true, greenInvert, "所有灯都应使用全局配置（低电平亮灯）");
            MiniAssert.Equal(true, startButtonInvert, "所有灯都应使用全局配置（低电平亮灯）");
            MiniAssert.Equal(true, stopButtonInvert, "所有灯都应使用全局配置（低电平亮灯）");
        }

        [MiniFact]
        public void RemoteConnectionLightConfiguration() {
            // 场景：配置远程连接指示灯
            var options = new LeadshineSafetyIoOptions {
                RemoteConnectionLightBit = 15,
                InvertLightLogic = true,  // 全局低电平亮灯
                InvertRemoteConnectionLightLogic = null  // 使用全局配置
            };
            
            MiniAssert.Equal(15, options.RemoteConnectionLightBit, "远程连接指示灯应配置到端口 15");
            var remoteConnectionInvert = options.InvertRemoteConnectionLightLogic ?? options.InvertLightLogic;
            MiniAssert.Equal(true, remoteConnectionInvert, "远程连接指示灯应使用全局配置（低电平亮灯）");
        }

        [MiniFact]
        public void RemoteConnectionLightWithCustomLogic() {
            // 场景：远程连接指示灯使用独立的反转逻辑
            var options = new LeadshineSafetyIoOptions {
                RemoteConnectionLightBit = 15,
                InvertLightLogic = false,  // 全局高电平亮灯
                InvertRemoteConnectionLightLogic = true  // 远程连接灯独立配置为低电平亮灯
            };
            
            var remoteConnectionInvert = options.InvertRemoteConnectionLightLogic ?? options.InvertLightLogic;
            MiniAssert.Equal(true, remoteConnectionInvert, "远程连接指示灯应使用独立配置（低电平亮灯）");
        }
    }
}
