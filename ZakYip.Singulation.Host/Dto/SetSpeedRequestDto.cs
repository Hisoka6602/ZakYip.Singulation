using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 批量设速请求（线速度，单位 mm/s）。
    /// </summary>
    public sealed class SetSpeedRequestDto {

        /// <summary>目标线速度（mm/s）。可为正/负，用于方向控制；实际下发前由驱动层按限幅钳制。</summary>
        public double LinearMmps { get; set; }
    }
}