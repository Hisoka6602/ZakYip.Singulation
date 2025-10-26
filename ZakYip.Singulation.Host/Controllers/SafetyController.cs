using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Host.Dto;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 安全命令控制器
    /// </summary>
    /// <remarks>
    /// 提供安全相关的命令接口，包括启动、停止、复位和急停操作。
    /// 所有安全命令都会被记录并通过安全管线处理。
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class SafetyController : ControllerBase {
        private readonly ISafetyPipeline _safety;
        private readonly ILogger<SafetyController> _logger;

        public SafetyController(ISafetyPipeline safety, ILogger<SafetyController> logger) {
            _safety = safety;
            _logger = logger;
        }

        /// <summary>
        /// 执行安全命令
        /// </summary>
        /// <remarks>
        /// 接收并执行安全相关命令。支持以下命令类型：
        /// - Start (1): 启动系统运行
        /// - Stop (2): 停止系统运行
        /// - Reset (3): 复位系统状态
        /// - EmergencyStop (4): 紧急停止（急停）
        /// 
        /// 所有命令都会被记录到日志中，并通过安全管线处理。
        /// </remarks>
        /// <param name="request">安全命令请求对象，包含命令类型和原因说明</param>
        /// <returns>命令受理结果</returns>
        /// <response code="202">安全命令已受理</response>
        /// <response code="400">请求参数无效</response>
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

                case SafetyCommand.EmergencyStop:
                    _logger.LogWarning("收到远程急停指令：{Reason}", reason);
                    _safety.RequestStop(SafetyTriggerKind.EmergencyStop, reason);
                    break;

                default:
                    _logger.LogWarning("收到未知安全命令：{Command}", request.Command);
                    return BadRequest(ApiResponse<object>.Invalid("不支持的安全命令"));
            }

            return Accepted(ApiResponse<object>.Success(new { Accepted = true }, "安全命令已受理"));
        }
    }
}
