using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 速度联动组配置。
    /// 定义一组轴和要联动的 IO 端口。
    /// </summary>
    public sealed record class SpeedLinkageGroup {
        /// <summary>
        /// 组内的轴 ID 列表。
        /// 当所有轴速度都从非0降到0时，触发 IO 联动。
        /// </summary>
        [Required(ErrorMessage = "轴 ID 列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要一个轴 ID")]
        public List<int> AxisIds { get; init; } = new();

        /// <summary>
        /// 联动的 IO 点列表。
        /// 当所有轴停止时，设置为指定电平；当轴运动时，设置为相反电平。
        /// </summary>
        [Required(ErrorMessage = "IO 点列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要一个 IO 点")]
        public List<SpeedLinkageIoPoint> IoPoints { get; init; } = new();
    }
}
