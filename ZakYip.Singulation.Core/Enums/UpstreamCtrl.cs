using System;
using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 上游协议控制命令字。
    /// </summary>
    public enum UpstreamCtrl : byte {
        /// <summary>速度端报文。</summary>
        [Description("速度端")]
        Speed = 0x81,

        /// <summary>位置端报文。</summary>
        [Description("位置端")]
        Pos = 0x82,

        /// <summary>启动停止命令。</summary>
        [Description("启动停止")]
        StartStop = 0x89,

        /// <summary>设置速度模式。</summary>
        [Description("设置速度模式")]
        SetModeSpeed = 0x84,

        /// <summary>设置间距。</summary>
        [Description("设置间距")]
        SetSpacing = 0x86,

        /// <summary>暂停恢复命令。</summary>
        [Description("暂停恢复")]
        PauseResume = 0x85,

        /// <summary>设置参数。</summary>
        [Description("设置参数")]
        SetParams = 0x83,

        /// <summary>查询状态（返回 0x5B）。</summary>
        [Description("查询状态")]
        QueryStatus = 0x87,

        /// <summary>获取参数（返回 0x5C）。</summary>
        [Description("获取参数")]
        GetParams = 0x88,

        /// <summary>状态响应报文。</summary>
        [Description("状态响应")]
        StatusResp = 0x5B,

        /// <summary>参数响应报文。</summary>
        [Description("参数响应")]
        ParamsResp = 0x5C,
    }
}