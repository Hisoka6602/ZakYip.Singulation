using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Host.Dto;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// IO 联动配置控制器
    /// </summary>
    /// <remarks>
    /// 提供 IO 联动配置的查询和更新接口。
    /// 配置包括在不同系统状态（运行中、停止/复位）下要联动的 IO 端口及其电平状态。
    /// 配置更新后会持久化保存到数据库，并在下次状态切换时自动应用。
    /// </remarks>
    [ApiController]
    [Route("api/io-linkage")]
    public sealed class IoLinkageController : ControllerBase {
        private readonly ILogger<IoLinkageController> _logger;
        private readonly IIoLinkageOptionsStore _store;

        public IoLinkageController(
            ILogger<IoLinkageController> logger,
            IIoLinkageOptionsStore store) {
            _logger = logger;
            _store = store;
        }

        /// <summary>
        /// 获取 IO 联动配置
        /// </summary>
        /// <remarks>
        /// 获取当前的 IO 联动配置信息。
        /// 包含启用状态、运行中状态时的 IO 联动点列表、停止/复位状态时的 IO 联动点列表。
        /// 
        /// **使用示例**：
        /// 
        /// ```
        /// GET /api/io-linkage/configs
        /// ```
        /// 
        /// **返回示例**：
        /// ```json
        /// {
        ///   "enabled": true,
        ///   "runningStateIos": [
        ///     { "bitNumber": 3, "level": 0 },
        ///     { "bitNumber": 5, "level": 0 },
        ///     { "bitNumber": 6, "level": 0 }
        ///   ],
        ///   "stoppedStateIos": [
        ///     { "bitNumber": 3, "level": 1 },
        ///     { "bitNumber": 5, "level": 1 },
        ///     { "bitNumber": 6, "level": 1 }
        ///   ]
        /// }
        /// ```
        /// 
        /// **注意**：
        /// - level: 0 = ActiveHigh (高电平), 1 = ActiveLow (低电平)
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>IO 联动配置对象</returns>
        /// <response code="200">获取配置成功</response>
        [HttpGet("configs")]
        [SwaggerOperation(
            Summary = "获取 IO 联动配置",
            Description = "获取当前的 IO 联动配置信息，包含启用状态、运行中状态时的 IO 联动点列表、停止/复位状态时的 IO 联动点列表。")]
        [ProducesResponseType(typeof(ApiResponse<IoLinkageOptions>), 200)]
        [Produces("application/json")]
        public async Task<ApiResponse<IoLinkageOptions>> GetConfigAsync(CancellationToken ct) {
            var options = await _store.GetAsync(ct);
            return ApiResponse<IoLinkageOptions>.Success(options);
        }

        /// <summary>
        /// 更新 IO 联动配置
        /// </summary>
        /// <remarks>
        /// 保存或更新 IO 联动配置信息。
        /// 配置更新后会持久化保存到数据库，并在下次状态切换时自动应用。
        /// 
        /// **使用示例**：
        /// 
        /// 配置运行中时将 IO 3、5、6 设置为高电平，停止/复位时将 IO 3、5、6 设置为低电平：
        /// ```json
        /// PUT /api/io-linkage/configs
        /// {
        ///   "enabled": true,
        ///   "runningStateIos": [
        ///     { "bitNumber": 3, "level": 0 },
        ///     { "bitNumber": 5, "level": 0 },
        ///     { "bitNumber": 6, "level": 0 }
        ///   ],
        ///   "stoppedStateIos": [
        ///     { "bitNumber": 3, "level": 1 },
        ///     { "bitNumber": 5, "level": 1 },
        ///     { "bitNumber": 6, "level": 1 }
        ///   ]
        /// }
        /// ```
        /// 
        /// **注意事项**：
        /// - IO 端口编号从 0 开始，范围为 0-1023
        /// - level 值：0 表示 ActiveHigh（高电平），1 表示 ActiveLow（低电平）
        /// - 如果需要禁用 IO 联动，请设置 enabled 为 false
        /// - 配置会在下次状态切换时生效
        /// </remarks>
        /// <param name="options">IO 联动配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已保存</response>
        /// <response code="400">配置验证失败</response>
        [HttpPut("configs")]
        [SwaggerOperation(
            Summary = "更新 IO 联动配置",
            Description = "保存或更新 IO 联动配置信息。配置更新后会持久化保存到数据库，并在下次状态切换时自动应用。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateConfigAsync(
            [FromBody] IoLinkageOptions options,
            CancellationToken ct) {
            
            // 验证配置
            if (!ModelState.IsValid) {
                _logger.LogWarning("IO 联动配置验证失败");
                return BadRequest(ApiResponse<string>.Invalid("配置验证失败"));
            }
            
            // 保存到数据库
            await _store.SaveAsync(options, ct);
            
            _logger.LogInformation(
                "IO 联动配置已更新：启用={Enabled}, 运行中状态IO数={RunningCount}, 停止状态IO数={StoppedCount}",
                options.Enabled,
                options.RunningStateIos.Count,
                options.StoppedStateIos.Count);
            
            return ApiResponse<string>.Success("配置已保存并将在下次状态切换时生效");
        }

        /// <summary>
        /// 删除 IO 联动配置
        /// </summary>
        /// <remarks>
        /// 删除当前的 IO 联动配置，恢复到默认配置状态（空联动列表）。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已删除</response>
        [HttpDelete("configs")]
        [SwaggerOperation(
            Summary = "删除 IO 联动配置",
            Description = "删除当前的 IO 联动配置，恢复到默认配置状态（空联动列表）。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [Produces("application/json")]
        public async Task<ApiResponse<string>> DeleteConfigAsync(CancellationToken ct) {
            await _store.DeleteAsync(ct);
            _logger.LogInformation("IO 联动配置已删除，将使用默认配置");
            return ApiResponse<string>.Success("配置已删除，将使用默认配置");
        }
    }
}
