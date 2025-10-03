using NLog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Dto;

namespace ZakYip.Singulation.Host.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public sealed class AdminController : ControllerBase {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 整个进程重启（依赖 Windows Service 或 systemd 的自动恢复）。
        /// </summary>
        [HttpPost("app/restart")]
        public ActionResult<object> RestartProcess() {
            _log.Info("App restart requested via API.");

            // 延迟退出：给 HTTP 响应/日志留出时间
            _ = Task.Run(async () => {
                try {
                    await Task.Delay(500);
                    LogManager.Flush(TimeSpan.FromMilliseconds(200)); // NLog 刷盘
                }
                catch { /* 忽略 */ }
                // 非零退出码：Windows 服务视为异常退出，触发“恢复”；systemd 配置 Restart=always 也会重启
                Environment.Exit(3);
            });

            return new {
                ok = true,
                message = "正在重启进程（由服务管理器自动拉起）。"
            };
        }
    }
}