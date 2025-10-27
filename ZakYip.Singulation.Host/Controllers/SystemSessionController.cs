using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZakYip.Singulation.Host.Dto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 提供对宿主运行会话的 RESTful 管理接口。
    /// 通过删除当前会话资源来触发宿主优雅退出，交由外部进程负责重启。
    /// </summary>
    [ApiController]
    [Route("api/system/session")]
    public sealed class SystemSessionController : ControllerBase {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<SystemSessionController> _logger;

        public SystemSessionController(IHostApplicationLifetime lifetime, ILogger<SystemSessionController> logger) {
            _lifetime = lifetime;
            _logger = logger;
        }

        /// <summary>
        /// 删除当前运行会话
        /// </summary>
        /// <remarks>
        /// 删除当前运行会话资源，触发宿主应用优雅退出。
        /// 此操作会通知宿主停止运行并退出进程。
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
            Description = "删除当前运行会话资源，触发宿主应用优雅退出。此操作会通知宿主停止运行并退出进程。注意：此操作是异步执行的，API 会立即返回 202 状态码，实际退出会在后台进行。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 202)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Produces("application/json")]
        public ActionResult<ApiResponse<object>> DeleteCurrentSession(CancellationToken ct) {
            if (ct.IsCancellationRequested) {
                // 本地化提示
                return BadRequest(ApiResponse<object>.Invalid("请求已取消"));
            }

            // 中文日志：收到关闭请求
            _logger.LogInformation("收到关闭请求，将在后台停止宿主应用。");

            // 后台异步执行，彻底与请求线程解耦，防止异常影响调用方
            _ = Task.Run(async () => {
                try {
                    // 中文日志：优雅停止
                    _logger.LogInformation("触发宿主优雅停止，准备退出。");
                    _lifetime.StopApplication();

                    // 等待优雅退出完成（容忍时间，可按需调参）
                    var gracefulWaitMs = 3000; // 3 秒容忍时间
                    await Task.Delay(gracefulWaitMs).ConfigureAwait(false);

                    // ——兜底：若进程仍未退出，则以非零码强制结束以触发服务恢复——
                    // 若项目已注册 ForceNonZeroExitHostedService，则可删除以下强制退出段
                    _logger.LogWarning("优雅停止未在 {WaitMs}ms 内完成，执行强制退出，退出码=1。", gracefulWaitMs);
                    Environment.Exit(1);
                }
                catch (Exception ex) {
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