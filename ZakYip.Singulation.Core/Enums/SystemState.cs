using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 系统全局状态：用于控制三色灯和按钮灯的状态。
    /// </summary>
    public enum SystemState {
        /// <summary>已停止：系统已停止运行。</summary>
        [Description("已停止")]
        Stopped = 0,

        /// <summary>准备中：系统已复位，准备启动。</summary>
        [Description("准备中")]
        Ready = 1,

        /// <summary>运行中：系统正在运行。</summary>
        [Description("运行中")]
        Running = 2,

        /// <summary>报警：系统处于报警状态（急停）。</summary>
        [Description("报警")]
        Alarm = 3
    }
}
