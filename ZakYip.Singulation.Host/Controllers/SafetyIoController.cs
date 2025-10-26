using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Host.Safety;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 安全 IO 配置控制器
    /// </summary>
    /// <remarks>
    /// 提供雷赛安全 IO 配置的查询和更新接口。
    /// 支持配置的持久化存储和热更新。
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class SafetyIoController : ControllerBase {
        private readonly ILogger<SafetyIoController> _logger;
        private readonly ILeadshineSafetyIoOptionsStore _store;
        private readonly LeadshineSafetyIoModule? _safetyModule;

        public SafetyIoController(
            ILogger<SafetyIoController> logger,
            ILeadshineSafetyIoOptionsStore store,
            IServiceProvider serviceProvider) {
            _logger = logger;
            _store = store;
            
            // 尝试获取安全模块（可能不存在，如果使用的是 LoopbackSafetyIoModule）
            if (serviceProvider.GetService(typeof(LeadshineSafetyIoModule)) is LeadshineSafetyIoModule module) {
                _safetyModule = module;
            }
        }

        /// <summary>
        /// 获取安全 IO 配置
        /// </summary>
        /// <remarks>
        /// 获取当前的雷赛安全 IO 配置信息。
        /// 包含所有按键的端口配置、轮询间隔、逻辑反转等设置。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>安全 IO 配置对象</returns>
        /// <response code="200">获取配置成功</response>
        [HttpGet("configs")]
        public async Task<ApiResponse<LeadshineSafetyIoOptions>> GetConfigAsync(CancellationToken ct) {
            var options = await _store.GetAsync(ct);
            return ApiResponse<LeadshineSafetyIoOptions>.Success(options);
        }

        /// <summary>
        /// 更新安全 IO 配置
        /// </summary>
        /// <remarks>
        /// 保存或更新雷赛安全 IO 配置信息。
        /// 配置更新后会持久化保存，并立即应用到运行中的安全模块（热更新）。
        /// </remarks>
        /// <param name="options">安全 IO 配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已保存并应用</response>
        [HttpPut("configs")]
        public async Task<ApiResponse<string>> UpdateConfigAsync(
            [FromBody] LeadshineSafetyIoOptions options,
            CancellationToken ct) {
            
            // 保存到数据库
            await _store.SaveAsync(options, ct);
            
            // 热更新到运行中的模块
            if (_safetyModule is not null) {
                _safetyModule.UpdateOptions(options);
                _logger.LogInformation("安全 IO 配置已更新并应用到运行中的模块");
                return ApiResponse<string>.Success("配置已保存并应用（热更新成功）");
            } else {
                _logger.LogInformation("安全 IO 配置已保存（当前未使用硬件安全模块）");
                return ApiResponse<string>.Success("配置已保存（重启后生效）");
            }
        }
    }
}
