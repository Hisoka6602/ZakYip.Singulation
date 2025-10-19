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

        [HttpPost("start")]
        public ActionResult<object> Start([FromBody] SafetyCommandRequestDto? request) {
            var reason = request?.Reason;
            _logger.LogInformation("收到远程启动指令：{Reason}", reason);
            _safety.RequestStart(SafetyTriggerKind.RemoteStartCommand, reason);
            return Ok(ApiResponse<object>.Success(new { Accepted = true }, "启动指令已受理"));
        }

        [HttpPost("stop")]
        public ActionResult<object> Stop([FromBody] SafetyCommandRequestDto? request) {
            var reason = request?.Reason;
            _logger.LogInformation("收到远程停止指令：{Reason}", reason);
            _safety.RequestStop(SafetyTriggerKind.RemoteStopCommand, reason);
            return Ok(ApiResponse<object>.Success(new { Accepted = true }, "停止指令已受理"));
        }

        [HttpPost("reset")]
        public ActionResult<object> Reset([FromBody] SafetyCommandRequestDto? request) {
            var reason = request?.Reason;
            _logger.LogInformation("收到远程复位指令：{Reason}", reason);
            _safety.RequestReset(SafetyTriggerKind.RemoteResetCommand, reason);
            return Ok(ApiResponse<object>.Success(new { Accepted = true }, "复位指令已受理"));
        }
    }
}
