using System;
using System.ComponentModel;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试 SystemState 枚举的正确性。
    /// </summary>
    internal sealed class SystemStateTests {

        [MiniFact]
        public void EnumValuesAreCorrect() {
            MiniAssert.Equal(0, (int)SystemState.Stopped, "已停止状态值应为 0");
            MiniAssert.Equal(1, (int)SystemState.Ready, "准备中状态值应为 1");
            MiniAssert.Equal(2, (int)SystemState.Running, "运行中状态值应为 2");
            MiniAssert.Equal(3, (int)SystemState.Alarm, "报警状态值应为 3");
        }

        [MiniFact]
        public void DescriptionAttributesExist() {
            // 验证每个枚举值都有 Description 特性
            var stoppedDesc = GetDescription(SystemState.Stopped);
            var readyDesc = GetDescription(SystemState.Ready);
            var runningDesc = GetDescription(SystemState.Running);
            var alarmDesc = GetDescription(SystemState.Alarm);

            MiniAssert.Equal("已停止", stoppedDesc, "已停止状态应有正确的 Description");
            MiniAssert.Equal("准备中", readyDesc, "准备中状态应有正确的 Description");
            MiniAssert.Equal("运行中", runningDesc, "运行中状态应有正确的 Description");
            MiniAssert.Equal("报警", alarmDesc, "报警状态应有正确的 Description");
        }

        private static string GetDescription(SystemState state) {
            var fieldInfo = typeof(SystemState).GetField(state.ToString());
            if (fieldInfo == null) return string.Empty;

            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }
}
