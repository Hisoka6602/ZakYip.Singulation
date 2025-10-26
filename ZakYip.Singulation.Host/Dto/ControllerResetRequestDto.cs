using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>
    /// 控制器复位请求数据传输对象。
    /// </summary>
    public sealed record class ControllerResetRequestDto {
        /// <summary>
        /// 复位类型，指定执行硬复位还是软复位。
        /// </summary>
        [Required(ErrorMessage = "复位类型不能为空")]
        public required ControllerResetType Type { get; init; }
    }
}
