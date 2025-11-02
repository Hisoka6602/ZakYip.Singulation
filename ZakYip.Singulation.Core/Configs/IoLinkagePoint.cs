using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 单个 IO 联动点配置。
    /// 表示一个 IO 端口号和应设置的电平状态。
    /// </summary>
    public sealed record class IoLinkagePoint {
        /// <summary>
        /// IO 端口编号（0-1023）。
        /// </summary>
        [Required(ErrorMessage = "IO 端口编号不能为空")]
        [Range(0, 1023, ErrorMessage = "IO 端口编号必须在 0-1023 之间")]
        public int BitNumber { get; init; }

        /// <summary>
        /// 目标电平状态（High=1 或 Low=0）。
        /// </summary>
        [Required(ErrorMessage = "IO 状态不能为空")]
        public IoState State { get; init; }
    }
}
