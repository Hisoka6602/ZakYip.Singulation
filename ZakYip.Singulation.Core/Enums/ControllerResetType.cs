using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    public enum ControllerResetType {

        /// <summary>
        /// 冷复位
        /// </summary>
        [Description("冷复位")]
        Hard,

        /// <summary>
        /// 软复位
        /// </summary>
        [Description("软复位")]
        Soft
    }
}