using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Infrastructure.Cabinet;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 面板控制器
    /// </summary>
    /// <remarks>
    /// 提供控制面板相关的命令接口和 IO 配置管理。
    /// 包括启动、停止、复位、急停操作，以及雷赛控制面板 IO 配置的查询和更新。
    /// 所有命令都会被记录并通过安全管线处理。
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CabinetController : ControllerBase {
        private readonly ICabinetPipeline _safety;
        private readonly ILogger<CabinetController> _logger;
        private readonly ILeadshineCabinetIoOptionsStore _store;
        private readonly LeadshineCabinetIoModule? _cabinetModule;

        public CabinetController(
            ICabinetPipeline safety, 
            ILogger<CabinetController> logger,
            ILeadshineCabinetIoOptionsStore store,
            LeadshineCabinetIoModule? cabinetModule = null) {
            _safety = safety;
            _logger = logger;
            _store = store;
            _cabinetModule = cabinetModule;
        }

        /// <summary>
        /// 执行控制命令
        /// </summary>
        /// <remarks>
        /// 接收并执行控制相关命令。支持以下命令类型：
        /// - Start (1): 启动系统运行
        /// - Stop (2): 停止系统运行
        /// - Reset (3): 复位系统状态
        /// - EmergencyStop (4): 紧急停止（急停）
        /// 
        /// 所有命令都会被记录到日志中，并通过安全管线处理。
        /// </remarks>
        /// <param name="request">控制命令请求对象，包含命令类型和原因说明</param>
        /// <returns>命令受理结果</returns>
        /// <response code="202">控制命令已受理</response>
        /// <response code="400">请求参数无效</response>
        [HttpPost("commands")]
        [SwaggerOperation(
            Summary = "执行控制命令",
            Description = "接收并执行控制相关命令，包括启动、停止、复位和急停操作。所有命令都会被记录并通过安全管线处理。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 202)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<ApiResponse<object>> ExecuteCommand([FromBody] CabinetCommandRequestDto? request) {
            if (request is null) {
                return BadRequest(ApiResponse<object>.Invalid("请求体不能为空"));
            }

            // 获取调用堆栈信息
            var stackTrace = new StackTrace(true);
            var callerInfo = GetCallerInfo(stackTrace);

            var reason = request.Reason;
            switch (request.Command) {
                case CabinetCommand.Start:
                    _logger.LogInformation("【API调用】收到远程启动指令 - 原因：{Reason}，调用方：{Caller}", reason, callerInfo);
                    _safety.RequestStart(CabinetTriggerKind.RemoteStartCommand, reason);
                    break;

                case CabinetCommand.Stop:
                    _logger.LogInformation("【API调用】收到远程停止指令 - 原因：{Reason}，调用方：{Caller}", reason, callerInfo);
                    _safety.RequestStop(CabinetTriggerKind.RemoteStopCommand, reason);
                    break;

                case CabinetCommand.Reset:
                    _logger.LogInformation("【API调用】收到远程复位指令 - 原因：{Reason}，调用方：{Caller}", reason, callerInfo);
                    _safety.RequestReset(CabinetTriggerKind.RemoteResetCommand, reason);
                    break;

                case CabinetCommand.EmergencyStop:
                    _logger.LogWarning("【API调用】收到远程急停指令 - 原因：{Reason}，调用方：{Caller}", reason, callerInfo);
                    _safety.RequestStop(CabinetTriggerKind.EmergencyStop, reason);
                    break;

                default:
                    _logger.LogWarning("收到未知控制命令：{Command}", request.Command);
                    return BadRequest(ApiResponse<object>.Invalid("不支持的控制命令"));
            }

            return Accepted(ApiResponse<object>.Success(new { Accepted = true }, "控制命令已受理"));
        }

        /// <summary>
        /// 获取调用方信息（用于审计日志）
        /// </summary>
        private static string GetCallerInfo(StackTrace stackTrace) {
            // 跳过当前方法和 ExecuteCommand，查找实际调用者
            for (int i = 2; i < Math.Min(stackTrace.FrameCount, 10); i++) {
                var frame = stackTrace.GetFrame(i);
                if (frame == null) continue;

                var method = frame.GetMethod();
                if (method == null) continue;

                var typeName = method.DeclaringType?.Name ?? "Unknown";
                var methodName = method.Name;
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                // 跳过框架内部方法
                if (typeName.StartsWith("Microsoft.") || typeName.StartsWith("System.")) {
                    continue;
                }

                if (!string.IsNullOrEmpty(fileName)) {
                    return $"{typeName}.{methodName} (文件: {System.IO.Path.GetFileName(fileName)}, 行: {lineNumber})";
                }
                return $"{typeName}.{methodName}";
            }
            return "API端点";
        }

        /// <summary>
        /// 获取控制面板 IO 配置
        /// </summary>
        /// <remarks>
        /// 获取当前的雷赛控制面板 IO 配置信息。
        /// 包含所有按键的端口配置、轮询间隔、逻辑反转等设置。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>控制面板 IO 配置对象</returns>
        /// <response code="200">获取配置成功</response>
        [HttpGet("io-configs")]
        [SwaggerOperation(
            Summary = "获取控制面板 IO 配置",
            Description = "获取当前的雷赛控制面板 IO 配置信息，包含所有按键的端口配置、轮询间隔、逻辑反转等设置。")]
        [ProducesResponseType(typeof(ApiResponse<LeadshineCabinetIoOptions>), 200)]
        [Produces("application/json")]
        public async Task<ApiResponse<LeadshineCabinetIoOptions>> GetIoConfigAsync(CancellationToken ct) {
            var options = await _store.GetAsync(ct);
            return ApiResponse<LeadshineCabinetIoOptions>.Success(options);
        }

        /// <summary>
        /// 更新控制面板 IO 配置
        /// </summary>
        /// <remarks>
        /// 保存或更新雷赛控制面板 IO 配置信息。
        /// 配置更新后会持久化保存，并立即应用到运行中的控制面板模块（热更新）。
        /// </remarks>
        /// <param name="options">控制面板 IO 配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已保存并应用</response>
        [HttpPut("io-configs")]
        [SwaggerOperation(
            Summary = "更新控制面板 IO 配置",
            Description = "保存或更新雷赛控制面板 IO 配置信息。配置更新后会持久化保存，并立即应用到运行中的控制面板模块（热更新）。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ApiResponse<string>> UpdateIoConfigAsync(
            [FromBody] LeadshineCabinetIoOptions options,
            CancellationToken ct) {
            
            // 保存到数据库
            await _store.SaveAsync(options, ct);
            
            // 热更新到运行中的模块
            if (_cabinetModule is not null) {
                _cabinetModule.UpdateOptions(options);
                _logger.LogInformation("控制面板 IO 配置已更新并应用到运行中的模块");
                return ApiResponse<string>.Success("配置已保存并应用（热更新成功）");
            } else {
                _logger.LogInformation("控制面板 IO 配置已保存（当前未使用硬件控制面板模块）");
                return ApiResponse<string>.Success("配置已保存（重启后生效）");
            }
        }
    }
}
