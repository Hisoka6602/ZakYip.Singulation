using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Host.Runtime;

namespace ZakYip.Singulation.Host.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public sealed class AdminController : ControllerBase {
        private readonly IApplicationRestarter _restarter;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IApplicationRestarter restarter, ILogger<AdminController> logger) {
            _restarter = restarter;
            _logger = logger;
        }

        /// <summary>
        /// 整个进程重启（依赖守护进程自动拉起）。
        /// </summary>
        [HttpPost("app/restart")]
        public async Task<ActionResult<object>> RestartProcess(CancellationToken ct) {
            _logger.LogInformation("收到 API 重启指令，正在调度。");
            await _restarter.RestartAsync("管理接口请求", ct).ConfigureAwait(false);
            return Ok(ApiResponse<object>.Success(new { Accepted = true }, "系统将自动重启"));
        }
    }
}
