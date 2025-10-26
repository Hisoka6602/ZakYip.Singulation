using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Host.Dto;
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
                return BadRequest(ApiResponse<object>.Invalid("请求已取消"));
            }

            _logger.LogInformation("收到关闭请求，将在后台停止宿主应用。");

            _ = Task.Run(() => {
                try {
                    _logger.LogInformation("触发宿主停止。");
                    _lifetime.StopApplication();
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "停止宿主时发生异常。");
                }
            }, CancellationToken.None);

            return Accepted(ApiResponse<object>.Success(new { Accepted = true }, "服务正在准备退出"));
        }
    }
}
