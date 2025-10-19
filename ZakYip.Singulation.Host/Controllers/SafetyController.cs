using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Host.Dto;

namespace ZakYip.Singulation.Host.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public sealed class SafetyController : ControllerBase {
        private readonly ISafetyPipeline _safety;
        private readonly ILogger<SafetyController> _logger;

        public SafetyController(ISafetyPipeline safety, ILogger<SafetyController> logger) {
            _safety = safety;
            _logger = logger;
        }

        [HttpPost("commands")]
        public ActionResult<ApiResponse<object>> ExecuteCommand([FromBody] SafetyCommandRequestDto? request) {
            if (request is null) {
                return BadRequest(ApiResponse<object>.Invalid("请求体不能为空"));
            }

            var reason = request.Reason;
            switch (request.Command) {
                case SafetyCommand.Start:
                    _logger.LogInformation("收到远程启动指令：{Reason}", reason);
                    _safety.RequestStart(SafetyTriggerKind.RemoteStartCommand, reason);
                    break;

                case SafetyCommand.Stop:
                    _logger.LogInformation("收到远程停止指令：{Reason}", reason);
                    _safety.RequestStop(SafetyTriggerKind.RemoteStopCommand, reason);
                    break;

                case SafetyCommand.Reset:
                    _logger.LogInformation("收到远程复位指令：{Reason}", reason);
                    _safety.RequestReset(SafetyTriggerKind.RemoteResetCommand, reason);
                    break;

                default:
                    _logger.LogWarning("收到未知安全命令：{Command}", request.Command);
                    return BadRequest(ApiResponse<object>.Invalid("不支持的安全命令"));
            }

            return Accepted(ApiResponse<object>.Success(new { Accepted = true }, "安全命令已受理"));
        }
    }
}
