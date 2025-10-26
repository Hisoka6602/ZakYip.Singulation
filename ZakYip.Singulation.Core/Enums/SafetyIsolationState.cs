using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 安全隔离状态：用于描述当前运行是否处于降级或隔离。
    /// </summary>
    public enum SafetyIsolationState {
        /// <summary>正常运行，允许全速执行。</summary>
        [Description("正常运行")]
        Normal = 0,

        /// <summary>降级运行：仍允许运行，但需要联动降速。</summary>
        [Description("降级运行")]
        Degraded = 1,

        /// <summary>已隔离：必须停止所有运动，等待人工复位。</summary>
        [Description("已隔离")]
        Isolated = 2
    }
}
