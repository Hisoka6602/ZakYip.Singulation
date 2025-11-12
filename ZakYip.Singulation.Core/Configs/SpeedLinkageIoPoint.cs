using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 速度联动 IO 点配置。
    /// 表示一个 IO 端口号和应设置的电平状态。
    /// </summary>
    public sealed record class SpeedLinkageIoPoint {
        /// <summary>
        /// IO 端口编号（0-1023）。
        /// </summary>
        [Required(ErrorMessage = "IO 端口编号不能为空")]
        [Range(0, 1023, ErrorMessage = "IO 端口编号必须在 0-1023 之间")]
        public required int BitNumber { get; init; }

        /// <summary>
        /// 当所有轴速度为0时的目标电平状态（ActiveHigh=高电平，ActiveLow=低电平）。
        /// </summary>
        [Required(ErrorMessage = "IO 电平状态不能为空")]
        public required TriggerLevel LevelWhenStopped { get; init; }
    }
}
