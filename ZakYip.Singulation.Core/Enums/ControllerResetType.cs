using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 控制器复位类型枚举。
    /// </summary>
    public enum ControllerResetType {

        /// <summary>
        /// 硬复位，调用底层硬件复位接口，完全重置控制器。
        /// </summary>
        [Description("硬复位")]
        Hard,

        /// <summary>
        /// 软复位，先关闭连接，然后重新初始化控制器。
        /// </summary>
        [Description("软复位")]
        Soft
    }
}