using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ZakYip.Singulation.Core.Enums {
    /// <summary>
    /// 轴驱动的运行状态（互斥态）。用于对外可观测的“生命体征”。
    /// </summary>

    public enum DriverStatus {

        /// <summary>离线：未建立通信或已掉线。</summary>
        [Description("离线")]
        Disconnected = 0,

        /// <summary>初始化中：连接、上电、清错、自检等准备阶段。</summary>
        [Description("初始化中")]
        Initializing = 1,

        /// <summary>在线：通信正常、可收发指令。</summary>
        [Description("在线")]
        Connected = 2,

        /// <summary>降级：功能可用但存在异常（丢包/重试/轻微故障）。</summary>
        [Description("降级")]
        Degraded = 3,

        /// <summary>恢复中：自动重连/重试进行中。</summary>
        [Description("恢复中")]
        Recovering = 4,

        /// <summary>已禁用：因配置或人为操作关闭，非故障。</summary>
        [Description("已禁用")]
        Disabled = 5,

        /// <summary>故障：需人工干预，自动恢复失败。</summary>
        [Description("故障")]
        Faulted = 6,

        /// <summary>已释放：实例已Dispose，不再可用。</summary>
        [Description("已释放")]
        Disposed = 255,
    }
}