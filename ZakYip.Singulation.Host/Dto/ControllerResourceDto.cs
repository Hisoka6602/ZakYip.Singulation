using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {
    public record class ControllerResourceDto {
        /// <summary>
        /// 控制器发现到的轴数量。
        /// </summary>
        public int AxisCount { get; init; }

        /// <summary>
        /// 当前错误码；0 表示正常，非 0 表示控制器/总线故障。
        /// </summary>
        public int ErrorCode { get; init; }

        /// <summary>
        /// 最后一次初始化是否成功。
        /// </summary>
        public bool Initialized { get; init; }
    }
}