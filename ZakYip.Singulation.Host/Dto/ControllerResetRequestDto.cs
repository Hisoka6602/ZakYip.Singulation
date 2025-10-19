using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>
    /// 控制器复位请求模型。
    /// </summary>
    public sealed record class ControllerResetRequestDto {
        /// <summary>复位类型。</summary>
        [Required(ErrorMessage = "类型不能为空")]
        public required ControllerResetType Type { get; init; }
    }
}
