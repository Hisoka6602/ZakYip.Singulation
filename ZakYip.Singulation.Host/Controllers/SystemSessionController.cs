using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZakYip.Singulation.Host.Dto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Enums;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Singulation.Core.Abstractions.Cabinet;

namespace ZakYip.Singulation.Host.Controllers
{

    /// <summary>
    /// 提供对宿主运行会话的 RESTful 管理接口。
    /// 通过删除当前会话资源来触发宿主优雅退出，交由外部进程负责重启。
    /// </summary>
    [ApiController]
    [Route("api/system/session")]
    public sealed class SystemSessionController : ControllerBase
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<SystemSessionController> _logger;
        private readonly ICabinetPipeline _safetyPipeline;

        /// <summary>
        /// 停止操作等待时间（毫秒）。
        /// </summary>
        private const int StopOperationDelayMs = 2000;

        public SystemSessionController(IHostApplicationLifetime lifetime, ILogger<SystemSessionController> logger, ICabinetPipeline safetyPipeline)
        {
            _lifetime = lifetime;
            _logger = logger;
            _safetyPipeline = safetyPipeline;
        }

        /// <summary>
        /// 删除当前运行会话
        /// </summary>
        /// <remarks>
        /// 删除当前运行会话资源，触发宿主应用优雅退出。
        /// 此操作会先停止所有轴（失能）并将运行状态设置为停止，然后退出进程。
        /// 外部部署工具（如 Windows 服务管理器或 systemd）应配置为自动重启服务。
        ///
        /// 注意：此操作是异步执行的，API 会立即返回 202 状态码，实际退出会在后台进行。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作受理结果</returns>
        /// <response code="202">关闭请求已受理，服务正在准备退出</response>
        /// <response code="400">请求已取消</response>
        [HttpDelete]
        [SwaggerOperation(
            Summary = "删除当前运行会话",
            Description = "删除当前运行会话资源，触发宿主应用优雅退出。此操作会先停止所有轴（失能）并将运行状态设置为停止，然后退出进程。注意：此操作是异步执行的，API 会立即返回 202 状态码，实际退出会在后台进行。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 202)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Produces("application/json")]
        public ActionResult<ApiResponse<object>> DeleteCurrentSession(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                // 本地化提示
                return BadRequest(ApiResponse<object>.Invalid("请求已取消"));
            }

            // 中文日志：收到关闭请求
            _logger.LogInformation("收到关闭请求，将在后台停止所有轴并退出应用。");

            // 后台异步执行，彻底与请求线程解耦，防止异常影响调用方
            _ = Task.Run(async () =>
            {
                try
                {
                    // 在退出前确保所有轴失能，运行状态变成停止（等同于调用IO按钮停止的触发）
                    _logger.LogInformation("【退出流程】步骤1：调用安全管线停止操作，禁用所有轴并更新运行状态");
                    _safetyPipeline.RequestStop(CabinetTriggerKind.RemoteStopCommand, "系统会话删除", triggeredByIo: false);

                    await Task.Delay(TimeSpan.FromSeconds(2));
                    // 直接使用 Environment.Exit(1) 退出进程，以便外部服务管理器重启
                    _logger.LogInformation("【退出流程】步骤3：执行 Environment.Exit(1) 退出进程");
                    Environment.Exit(1);
                }
                catch (Exception ex)
                {
                    // 中文日志：异常兜底，保证非零退出
                    _logger.LogError(ex, "停止宿主时发生异常，将强制退出（退出码=1）。");
                    Environment.Exit(1);
                }
            }, CancellationToken.None);

            // 立即返回 202，提示后台正在处理
            return Accepted(ApiResponse<object>.Success(new { Accepted = true }, "服务正在准备退出"));
        }
    }
}