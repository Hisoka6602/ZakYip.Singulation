using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Host.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// IO 状态控制器，提供查询所有 IO 端口当前状态的功能。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class IoController : ControllerBase {
        private readonly IoStatusService _ioStatusService;
        private readonly ILogger<IoController> _logger;

        /// <summary>
        /// 通过依赖注入构造控制器。
        /// </summary>
        public IoController(
            IoStatusService ioStatusService,
            ILogger<IoController> logger) {
            _ioStatusService = ioStatusService;
            _logger = logger;
        }

        /// <summary>
        /// 查询所有 IO 的当前状态
        /// </summary>
        /// <remarks>
        /// 查询雷赛控制器的所有输入和输出 IO 端口的当前状态。
        /// 
        /// **功能说明**：
        /// - 查询指定范围的输入 IO 状态（默认 0-31，共 32 个端口）
        /// - 查询指定范围的输出 IO 状态（默认 0-31，共 32 个端口）
        /// - 返回每个 IO 的位号、类型（输入/输出）、状态（高/低）、有效性
        /// - 如果读取失败，返回错误信息
        /// 
        /// **返回数据**：
        /// - `inputIos`：输入 IO 状态列表
        /// - `outputIos`：输出 IO 状态列表
        /// - `totalCount`：总 IO 数量
        /// - `validCount`：成功读取的 IO 数量
        /// - `errorCount`：读取失败的 IO 数量
        /// 
        /// **使用示例**：
        /// 
        /// 默认查询（输入 IO 0-31，输出 IO 0-31）：
        /// ```
        /// GET /api/io/status
        /// ```
        /// 
        /// 自定义查询范围：
        /// ```
        /// GET /api/io/status?inputStart=0&amp;inputCount=16&amp;outputStart=0&amp;outputCount=16
        /// ```
        /// 
        /// **注意事项**：
        /// - IO 端口编号从 0 开始
        /// - 如果硬件不支持某些端口，对应的 IO 状态会标记为无效（isValid=false）
        /// - 建议根据实际硬件配置调整查询范围，避免查询不存在的端口
        /// </remarks>
        /// <param name="inputStart">输入 IO 起始位号，默认 0</param>
        /// <param name="inputCount">输入 IO 数量，默认 32</param>
        /// <param name="outputStart">输出 IO 起始位号，默认 0</param>
        /// <param name="outputCount">输出 IO 数量，默认 32</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>所有 IO 的当前状态</returns>
        /// <response code="200">查询成功</response>
        /// <response code="400">参数验证失败</response>
        /// <response code="500">服务器内部错误</response>
        [HttpGet("status")]
        [SwaggerOperation(
            Summary = "查询所有 IO 的当前状态",
            Description = "查询雷赛控制器的所有输入和输出 IO 端口的当前状态，包括位号、类型、状态（高/低）等信息。支持自定义查询范围。")]
        [ProducesResponseType(typeof(ApiResponse<IoStatusResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<IoStatusResponseDto>>> GetIoStatus(
            [FromQuery] int inputStart = 0,
            [FromQuery] int inputCount = 32,
            [FromQuery] int outputStart = 0,
            [FromQuery] int outputCount = 32,
            CancellationToken ct = default) {

            try {
                // 参数验证
                if (inputStart < 0 || inputCount <= 0 || inputCount > 1024) {
                    return BadRequest(ApiResponse<object>.Invalid("输入 IO 参数无效：起始位号必须 >= 0，数量必须在 1-1024 之间"));
                }

                if (outputStart < 0 || outputCount <= 0 || outputCount > 1024) {
                    return BadRequest(ApiResponse<object>.Invalid("输出 IO 参数无效：起始位号必须 >= 0，数量必须在 1-1024 之间"));
                }

                _logger.LogInformation(
                    "开始查询 IO 状态：输入 IO [{InputStart}-{InputEnd}]，输出 IO [{OutputStart}-{OutputEnd}]",
                    inputStart, inputStart + inputCount - 1,
                    outputStart, outputStart + outputCount - 1);

                var result = await _ioStatusService.GetAllIoStatusAsync(
                    inputStart, inputCount,
                    outputStart, outputCount,
                    ct);

                return Ok(ApiResponse<IoStatusResponseDto>.Success(result, "查询 IO 状态成功"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "查询 IO 状态时发生异常");
                return StatusCode(500, ApiResponse<object>.Fail("查询 IO 状态失败：" + ex.Message));
            }
        }
    }
}
