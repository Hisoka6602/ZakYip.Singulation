using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    public enum AxisEventType {

        /// <summary>
        /// 轴/驱动抛错
        /// </summary>
        [Description("轴/驱动抛错")]
        Faulted,

        /// <summary>
        /// 轴连接断开
        /// </summary>
        [Description("轴连接断开")]
        Disconnected,

        /// <summary>
        /// 驱动库未加载
        /// </summary>
        [Description("驱动库未加载")]
        DriverNotLoaded,

        /// <summary>
        /// 控制器层面报错
        /// </summary>
        [Description("控制器层面报错")]
        ControllerFaulted
    }
}