using System;
using ZakYip.Singulation.Host.Safety;

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
    }
}
