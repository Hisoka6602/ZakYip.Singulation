using System;
using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 编解码标志位（位域标记）。
    /// </summary>
    [Flags]
    public enum CodecFlags {
        /// <summary>无标志。</summary>
        [Description("无标志")]
        None = 0,

        /// <summary>校验和验证通过。</summary>
        [Description("校验和正确")]
        ChecksumOk = 1 << 0,

        /// <summary>接收数据包乱序。</summary>
        [Description("数据包乱序")]
        OutOfOrder = 1 << 1,

        /// <summary>数据包重复。</summary>
        [Description("数据包重复")]
        Duplicated = 1 << 2,

        /// <summary>数据包不完整。</summary>
        [Description("数据包不完整")]
        Partial = 1 << 3
    }
}