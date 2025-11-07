using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Utils;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Infrastructure.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 速度联动配置控制器
    /// </summary>
    /// <remarks>
    /// 提供速度联动配置的查询和更新接口。
    /// 配置包括多个联动组，每组定义一组轴和要联动的 IO 端口。
    /// 当组内所有轴速度从非0降到0时，设置指定IO为指定电平；
    /// 当所有轴速度从0提升到非0时，设置相反电平。
    /// </remarks>
    [ApiController]
    [Route("api/io-linkage/speed")]
    public sealed class SpeedLinkageController : ControllerBase {
        private readonly ILogger<SpeedLinkageController> _logger;
        private readonly ISpeedLinkageOptionsStore _store;
        private readonly ISpeedLinkageService _speedLinkageService;

        public SpeedLinkageController(
            ILogger<SpeedLinkageController> logger,
            ISpeedLinkageOptionsStore store,
            ISpeedLinkageService speedLinkageService) {
            _logger = logger;
            _store = store;
            _speedLinkageService = speedLinkageService;
        }

        /// <summary>
        /// 获取速度联动配置
        /// </summary>
        /// <remarks>
        /// 获取当前的速度联动配置信息。
        /// 包含启用状态和联动组列表。
        /// 
        /// **使用示例**：
        /// 
        /// ```
        /// GET /api/io-linkage/speed/configs
        /// ```
        /// 
        /// **返回示例**：
        /// ```json
        /// {
        ///   "enabled": true,
        ///   "linkageGroups": [
        ///     {
        ///       "axisIds": [1001, 1002],
        ///       "ioPoints": [
        ///         { "bitNumber": 3, "levelWhenStopped": 0 },
        ///         { "bitNumber": 4, "levelWhenStopped": 0 }
        ///       ]
        ///     },
        ///     {
        ///       "axisIds": [1003, 1004],
        ///       "ioPoints": [
        ///         { "bitNumber": 5, "levelWhenStopped": 0 },
        ///         { "bitNumber": 6, "levelWhenStopped": 0 }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// **注意**：
        /// - levelWhenStopped: 0 = ActiveHigh (高电平), 1 = ActiveLow (低电平)
        /// - 当所有轴停止时设置为 levelWhenStopped，运动时设置为相反电平
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>速度联动配置对象</returns>
        /// <response code="200">获取配置成功</response>
        [HttpGet("configs")]
        [SwaggerOperation(
            Summary = "获取速度联动配置",
            Description = "获取当前的速度联动配置信息，包含启用状态和联动组列表。")]
        [ProducesResponseType(typeof(ApiResponse<SpeedLinkageOptions>), 200)]
        [Produces("application/json")]
        public async Task<ApiResponse<SpeedLinkageOptions>> GetConfigAsync(CancellationToken ct) {
            var options = await _store.GetAsync(ct);
            return ApiResponse<SpeedLinkageOptions>.Success(options);
        }

        /// <summary>
        /// 更新速度联动配置
        /// </summary>
        /// <remarks>
        /// 保存或更新速度联动配置信息。
        /// 配置更新后会持久化保存到数据库，并立即生效。
        /// 
        /// **使用示例**：
        /// 
        /// 配置两个联动组，当轴1001和1002都停止时，将IO 3和4设为高电平；
        /// 当轴1003和1004都停止时，将IO 5和6设为高电平：
        /// ```json
        /// PUT /api/io-linkage/speed/configs
        /// {
        ///   "enabled": true,
        ///   "linkageGroups": [
        ///     {
        ///       "axisIds": [1001, 1002],
        ///       "ioPoints": [
        ///         { "bitNumber": 3, "levelWhenStopped": 0 },
        ///         { "bitNumber": 4, "levelWhenStopped": 0 }
        ///       ]
        ///     },
        ///     {
        ///       "axisIds": [1003, 1004],
        ///       "ioPoints": [
        ///         { "bitNumber": 5, "levelWhenStopped": 0 },
        ///         { "bitNumber": 6, "levelWhenStopped": 0 }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// **注意事项**：
        /// - 轴ID必须与实际控制器中的轴ID对应
        /// - IO 端口编号从 0 开始，范围为 0-1023
        /// - levelWhenStopped 值：0 表示 ActiveHigh（高电平），1 表示 ActiveLow（低电平）
        /// - 如果需要禁用速度联动，请设置 enabled 为 false
        /// - 配置会立即生效，无需重启服务
        /// </remarks>
        /// <param name="options">速度联动配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已保存</response>
        /// <response code="400">配置验证失败</response>
        [HttpPut("configs")]
        [SwaggerOperation(
            Summary = "更新速度联动配置",
            Description = "保存或更新速度联动配置信息。配置更新后会持久化保存到数据库，并立即生效。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateConfigAsync(
            [FromBody] SpeedLinkageOptions options,
            CancellationToken ct) {
            
            // 使用增强的配置验证
            var validationResult = ConfigurationValidator.ValidateSpeedLinkageOptions(options);
            if (!validationResult.IsValid) {
                _logger.LogWarning("速度联动配置验证失败: {Errors}", 
                    string.Join("; ", validationResult.Errors));
                return BadRequest(ApiResponse<string>.Invalid(validationResult.GetFormattedErrors()));
            }
            
            // 记录警告信息
            if (validationResult.Warnings.Count > 0) {
                _logger.LogWarning("速度联动配置存在警告: {Warnings}", 
                    string.Join("; ", validationResult.Warnings));
            }
            
            // 保存到数据库
            await _store.SaveAsync(options, ct);
            
            _logger.LogInformation(
                "速度联动配置已更新：启用={Enabled}, 联动组数={GroupCount}",
                options.Enabled,
                options.LinkageGroups.Count);
            
            var response = validationResult.Warnings.Count > 0
                ? $"配置已保存并立即生效。\n警告：\n{string.Join("\n", validationResult.Warnings)}"
                : "配置已保存并立即生效";
            
            return ApiResponse<string>.Success(response);
        }

        /// <summary>
        /// 删除速度联动配置
        /// </summary>
        /// <remarks>
        /// 删除当前的速度联动配置，恢复到默认配置状态（空联动组列表）。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已删除</response>
        [HttpDelete("configs")]
        [SwaggerOperation(
            Summary = "删除速度联动配置",
            Description = "删除当前的速度联动配置，恢复到默认配置状态（空联动组列表）。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [Produces("application/json")]
        public async Task<ApiResponse<string>> DeleteConfigAsync(CancellationToken ct) {
            await _store.DeleteAsync(ct);
            _logger.LogInformation("速度联动配置已删除，将使用默认配置");
            return ApiResponse<string>.Success("配置已删除，将使用默认配置");
        }

        /// <summary>
        /// 获取速度联动服务健康状态
        /// </summary>
        /// <remarks>
        /// 监控速度联动服务的运行状态，提供健康度评分和详细的性能指标。
        /// 
        /// **使用示例**：
        /// 
        /// ```
        /// GET /api/io-linkage/speed/health
        /// ```
        /// 
        /// **返回示例**：
        /// ```json
        /// {
        ///   "success": true,
        ///   "data": {
        ///     "totalChecks": 12345,
        ///     "totalStateChanges": 56,
        ///     "totalIoWrites": 112,
        ///     "failedIoWrites": 2,
        ///     "totalErrors": 0,
        ///     "lastCheckTime": "2025-11-07T07:27:43.349Z",
        ///     "lastErrorTime": "0001-01-01T00:00:00",
        ///     "lastError": null,
        ///     "isRunning": true,
        ///     "activeGroupsCount": 2,
        ///     "healthScore": 98.5
        ///   },
        ///   "message": "速度联动服务运行正常"
        /// }
        /// ```
        /// 
        /// **健康度评分说明**：
        /// - 100-90分：服务运行优秀
        /// - 89-70分：服务运行良好，存在轻微问题
        /// - 69-50分：服务运行但存在明显问题
        /// - 50分以下：服务存在严重问题
        /// 
        /// **指标说明**：
        /// - totalChecks: 总检查次数（服务启动后累计）
        /// - totalStateChanges: 状态变化次数（轴组启停）
        /// - totalIoWrites: IO写入总次数
        /// - failedIoWrites: IO写入失败次数
        /// - totalErrors: 总错误次数
        /// - lastCheckTime: 最后检查时间
        /// - lastErrorTime: 最后错误时间
        /// - activeGroupsCount: 活跃的联动组数量
        /// - healthScore: 健康度评分（0-100）
        /// </remarks>
        /// <returns>服务健康状态信息</returns>
        /// <response code="200">获取健康状态成功</response>
        [HttpGet("health")]
        [SwaggerOperation(
            Summary = "获取速度联动服务健康状态",
            Description = "监控速度联动服务的运行状态，提供健康度评分和详细的性能指标。")]
        [ProducesResponseType(typeof(ApiResponse<SpeedLinkageStatistics>), 200)]
        [Produces("application/json")]
        public ApiResponse<SpeedLinkageStatistics> GetHealthAsync() {
            var stats = _speedLinkageService.GetStatistics();
            return ApiResponse<SpeedLinkageStatistics>.Success(stats, "速度联动服务运行正常");
        }
    }
}
