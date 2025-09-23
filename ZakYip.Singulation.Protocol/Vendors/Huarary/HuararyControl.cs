using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Protocol.Vendors.Huarary {

    /// <summary>
    /// 华睿控制码定义（见协议文档）。
    /// </summary>
    public static class HuararyControl {

        /// <summary>帧起始字节。</summary>
        public const byte Start = 0x2A;

        /// <summary>帧结束字节。</summary>
        public const byte End = 0x3B;

        /// <summary>速度帧控制码（视觉→PLC/我们）。</summary>
        public const byte CtrlSpeed = 0x81;

        /// <summary>位置帧控制码（视觉→PLC/我们）。</summary>
        public const byte CtrlPos = 0x82;

        /// <summary>设置参数。</summary>
        public const byte CtrlSetParams = 0x83;

        /// <summary>设置模式与速度上下限。</summary>
        public const byte CtrlModeSpeed = 0x84;

        /// <summary>暂停控制。</summary>
        public const byte CtrlPause = 0x85;

        /// <summary>设置分离距离。</summary>
        public const byte CtrlSpacing = 0x86;

        /// <summary>查询状态。</summary>
        public const byte CtrlQueryStatus = 0x87;

        /// <summary>获取参数。</summary>
        public const byte CtrlGetParams = 0x88;

        /// <summary>启动/停止。</summary>
        public const byte CtrlStartStop = 0x89;

        /// <summary>状态响应。</summary>
        public const byte CtrlStatusResp = 0x5B;

        /// <summary>参数响应。</summary>
        public const byte CtrlParamsResp = 0x5C;
    }
}