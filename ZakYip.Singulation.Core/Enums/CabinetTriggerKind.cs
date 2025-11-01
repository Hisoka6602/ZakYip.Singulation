using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 控制面板触发来源分类。
    /// </summary>
    public enum CabinetTriggerKind {
        /// <summary>未知触发源。</summary>
        [Description("未知")]
        Unknown = 0,

        /// <summary>急停按钮触发。</summary>
        [Description("急停按钮")]
        EmergencyStop = 1,

        /// <summary>停止按钮触发。</summary>
        [Description("停止按钮")]
        StopButton = 2,

        /// <summary>启动按钮触发。</summary>
        [Description("启动按钮")]
        StartButton = 3,

        /// <summary>复位按钮触发。</summary>
        [Description("复位按钮")]
        ResetButton = 4,

        /// <summary>轴故障触发。</summary>
        [Description("轴故障")]
        AxisFault = 5,

        /// <summary>轴断开连接触发。</summary>
        [Description("轴断开连接")]
        AxisDisconnected = 6,

        /// <summary>心跳超时触发。</summary>
        [Description("心跳超时")]
        HeartbeatTimeout = 7,

        /// <summary>健康状态恢复。</summary>
        [Description("健康恢复")]
        HealthRecovered = 8,

        /// <summary>调试失败触发。</summary>
        [Description("调试失败")]
        CommissioningFailure = 9,

        /// <summary>远程启动命令。</summary>
        [Description("远程启动")]
        RemoteStartCommand = 10,

        /// <summary>远程停止命令。</summary>
        [Description("远程停止")]
        RemoteStopCommand = 11,

        /// <summary>远程复位命令。</summary>
        [Description("远程复位")]
        RemoteResetCommand = 12
    }
}
