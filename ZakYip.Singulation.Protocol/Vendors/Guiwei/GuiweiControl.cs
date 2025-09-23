using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Protocol.Vendors.Guiwei {

    /// <summary>
    /// 归位协议的常量定义。
    /// </summary>
    public static class GuiweiControl {

        /// <summary>帧起始字节。</summary>
        public const byte Start = 0x2A;

        /// <summary>帧结束字节。</summary>
        public const byte End = 0x3B;
    }
}