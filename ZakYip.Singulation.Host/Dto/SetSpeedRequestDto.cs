using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 批量设置速度请求数据传输对象，用于设置轴的线速度。
    /// </summary>
    public sealed class SetSpeedRequestDto {

        /// <summary>
        /// 目标线速度，单位为毫米每秒（mm/s）。
        /// 可为正值或负值，用于方向控制；实际下发前由驱动层按限幅进行钳制。
        /// </summary>
        [Range(-100000, 100000, ErrorMessage = "线速度必须在 -100000 到 100000 之间")]
        public double LinearMmps { get; set; }
    }
}
