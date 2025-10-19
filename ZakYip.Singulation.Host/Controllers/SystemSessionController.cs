using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Host.Dto;

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
        /// 删除当前运行会话资源，通知宿主退出进程。
        /// 外部部署工具（例如 install.bat）将检测到退出并完成重启。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpDelete]
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
