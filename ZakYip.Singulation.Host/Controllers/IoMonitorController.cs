using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Core.Contracts.Dto;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// IO 状态监控配置控制器
    /// </summary>
    /// <remarks>
    /// 提供 IO 状态监控配置的查询和更新接口。
    /// 配置包括启用状态、IO 范围、轮询间隔和 SignalR 广播频道等。
    /// 配置更新后会持久化保存到数据库，并在下次轮询时自动应用（热更新）。
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class IoMonitorController : ControllerBase {
        private readonly ILogger<IoMonitorController> _logger;
        private readonly IIoStatusMonitorOptionsStore _store;

        public IoMonitorController(
            ILogger<IoMonitorController> logger,
            IIoStatusMonitorOptionsStore store) {
            _logger = logger;
            _store = store;
        }

        /// <summary>
        /// 获取 IO 状态监控配置
        /// </summary>
        /// <remarks>
        /// 获取当前的 IO 状态监控配置信息。
        /// 包含启用状态、输入/输出 IO 范围、轮询间隔和 SignalR 广播频道等设置。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>IO 状态监控配置对象</returns>
        /// <response code="200">获取配置成功</response>
        [HttpGet("configs")]
        [SwaggerOperation(
            Summary = "获取 IO 状态监控配置",
            Description = "获取当前的 IO 状态监控配置信息，包含启用状态、输入/输出 IO 范围、轮询间隔和 SignalR 广播频道等设置。")]
        [ProducesResponseType(typeof(ApiResponse<IoStatusMonitorOptions>), 200)]
        [Produces("application/json")]
        public async Task<ApiResponse<IoStatusMonitorOptions>> GetConfigAsync(CancellationToken ct) {
            var options = await _store.GetAsync(ct);
            return ApiResponse<IoStatusMonitorOptions>.Success(options);
        }

        /// <summary>
        /// 更新 IO 状态监控配置
        /// </summary>
        /// <remarks>
        /// 保存或更新 IO 状态监控配置信息。
        /// 配置更新后会持久化保存到数据库，并在下次轮询时自动应用（热更新）。
        /// 注意：如果需要禁用监控，请设置 Enabled 为 false。
        /// </remarks>
        /// <param name="options">IO 状态监控配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已保存</response>
        /// <response code="400">配置验证失败</response>
        [HttpPut("configs")]
        [SwaggerOperation(
            Summary = "更新 IO 状态监控配置",
            Description = "保存或更新 IO 状态监控配置信息。配置更新后会持久化保存到数据库，并在下次轮询时自动应用（热更新）。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateConfigAsync(
            [FromBody] IoStatusMonitorOptions options,
            CancellationToken ct) {
            
            // 验证配置
            if (!ModelState.IsValid) {
                _logger.LogWarning("IO 状态监控配置验证失败");
                return BadRequest(ApiResponse<string>.Invalid("配置验证失败"));
            }
            
            // 保存到数据库
            await _store.SaveAsync(options, ct);
            
            _logger.LogInformation(
                "IO 状态监控配置已更新：{@options}",
                options);
            
            return ApiResponse<string>.Success("配置已保存并将在下次轮询时生效（热更新）");
        }

        /// <summary>
        /// 删除 IO 状态监控配置
        /// </summary>
        /// <remarks>
        /// 删除当前的 IO 状态监控配置，恢复到默认配置状态。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已删除</response>
        [HttpDelete("configs")]
        [SwaggerOperation(
            Summary = "删除 IO 状态监控配置",
            Description = "删除当前的 IO 状态监控配置，恢复到默认配置状态。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [Produces("application/json")]
        public async Task<ApiResponse<string>> DeleteConfigAsync(CancellationToken ct) {
            await _store.DeleteAsync(ct);
            _logger.LogInformation("IO 状态监控配置已删除，将使用默认配置");
            return ApiResponse<string>.Success("配置已删除，将使用默认配置");
        }
    }
}
