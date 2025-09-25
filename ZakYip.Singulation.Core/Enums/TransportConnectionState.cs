using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 传输连接状态机（长连接生命周期）。
    /// </summary>
    public enum TransportConnectionState {

        /// <summary>正在发起连接（包含首次连接与重连的拨号阶段）。</summary>
        [Description("正在建立连接")]
        Connecting,

        /// <summary>连接已建立，收发通畅。</summary>
        [Description("已连接")]
        Connected,

        /// <summary>断线后进入重试窗口（带退避与抖动）。</summary>
        [Description("准备重连")]
        Retrying,

        /// <summary>连接已断开（可能是被动断开或应用触发的 Stop 过程中的过渡态）。</summary>
        [Description("已断开")]
        Disconnected,

        /// <summary>传输组件已停止（不会再发起重连）。</summary>
        [Description("已停止")]
        Stopped
    }
}