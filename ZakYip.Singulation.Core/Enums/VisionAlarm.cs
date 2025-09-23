using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 视觉报警标志。不同厂商的具体枚举值在协议层翻译后汇总为此处语义。
    /// </summary>
    [Flags]
    public enum VisionAlarm {

        /// <summary>无异常。</summary>
        None = 0

        // 后续可扩展：如 CameraTriggerFault=1<<0, DongleFault=1<<1 等
    }
}