using System;
using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 线程执行状态标志（Windows API）。
    /// </summary>
    [Flags]
    public enum EXECUTION_STATE : uint {
        /// <summary>持续保持状态，直到下次调用。</summary>
        [Description("持续保持")]
        ES_CONTINUOUS = 0x80000000,

        /// <summary>通知系统正在使用，防止进入睡眠模式。</summary>
        [Description("系统必需")]
        ES_SYSTEM_REQUIRED = 0x00000001,

        /// <summary>通知系统显示器正在使用，防止显示器关闭。</summary>
        [Description("显示器必需")]
        ES_DISPLAY_REQUIRED = 0x00000002
        // 还有 ES_AWAYMODE_REQUIRED 等可选
    }
}
