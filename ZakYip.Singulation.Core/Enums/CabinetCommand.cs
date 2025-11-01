using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 来自控制面板 IO 的高层控制命令。
    /// </summary>
    public enum CabinetCommand {
        /// <summary>无命令。</summary>
        [Description("无命令")]
        None = 0,

        /// <summary>启动命令。</summary>
        [Description("启动")]
        Start = 1,

        /// <summary>停止命令。</summary>
        [Description("停止")]
        Stop = 2,

        /// <summary>复位命令。</summary>
        [Description("复位")]
        Reset = 3,

        /// <summary>急停命令。</summary>
        [Description("急停")]
        EmergencyStop = 4
    }
}
