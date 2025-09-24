using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {
    public record ControllerResetRequestDto {
        [Required(ErrorMessage = "类型不能为空")]
        public required ControllerResetType Type { get; init; }
    }
}