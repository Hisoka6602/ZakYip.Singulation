using Microsoft.AspNetCore.Mvc;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Protocol.Abstractions;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 版本信息控制器，提供系统版本和厂商信息查询功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class VersionController : ControllerBase {
        private readonly IControllerOptionsStore _controllerStore;
        private readonly IUpstreamCodec _codec;
        private readonly ILogger<VersionController> _logger;

        /// <summary>
        /// 通过依赖注入构造版本控制器
        /// </summary>
        public VersionController(
            IControllerOptionsStore controllerStore,
            IUpstreamCodec codec,
            ILogger<VersionController> logger) {
            _controllerStore = controllerStore;
            _codec = codec;
            _logger = logger;
        }

        /// <summary>
        /// 获取系统版本信息
        /// </summary>
        /// <remarks>
        /// 返回当前系统的版本号、轴驱动厂商名称和上游数据厂商名称。
        /// 
        /// 响应示例：
        /// ```json
        /// {
        ///   "result": true,
        ///   "msg": "获取成功",
        ///   "data": {
        ///     "version": "1.0.0",
        ///     "axisVendor": "leadshine",
        ///     "upstreamVendor": "huarary"
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <param name="ct">取消令牌，用于请求取消</param>
        /// <returns>版本信息对象</returns>
        /// <response code="200">获取成功</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "获取系统版本信息",
            Description = "返回当前系统的版本号、轴驱动厂商名称和上游数据厂商名称")]
        [ProducesResponseType(typeof(ApiResponse<VersionResponseDto>), 200)]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<VersionResponseDto>>> GetVersion(CancellationToken ct) {
            try {
                // 获取当前程序集版本
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

                // 获取轴驱动厂商名称
                var controllerOptions = await _controllerStore.GetAsync(ct);
                var axisVendor = controllerOptions.Vendor?.Trim().ToLowerInvariant() ?? "unknown";

                // 获取上游数据厂商名称（从编解码器类型名称推导）
                var codecType = _codec.GetType().Name;
                var upstreamVendor = codecType.Replace("Codec", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

                var versionInfo = new VersionResponseDto {
                    Version = version,
                    AxisVendor = axisVendor,
                    UpstreamVendor = upstreamVendor
                };

                return Ok(ApiResponse<VersionResponseDto>.Success(versionInfo, "获取成功"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "获取版本信息失败");
                return StatusCode(500, ApiResponse<VersionResponseDto>.Fail("获取版本信息失败"));
            }
        }
    }
}
