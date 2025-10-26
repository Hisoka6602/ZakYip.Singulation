using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 安全控制命令通用请求体。
    /// </summary>
    [SwaggerSchema(Description = "安全控制命令请求对象，用于执行启动、停止、复位、急停等安全相关操作")]
    public sealed class SafetyCommandRequestDto {
        /// <summary>指示要执行的命令类型。</summary>
        [SwaggerSchema(Description = "要执行的安全命令类型：None(0)=无命令, Start(1)=启动, Stop(2)=停止, Reset(3)=复位, EmergencyStop(4)=急停")]
        [Required(ErrorMessage = "命令类型不能为空")]
        public ZakYip.Singulation.Core.Enums.SafetyCommand Command { get; set; }

        /// <summary>附带原因说明。</summary>
        [SwaggerSchema(Description = "操作原因说明，用于记录日志和审计", Nullable = true)]
        public string? Reason { get; set; }
    }
}
