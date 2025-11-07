using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Host.Dto;

namespace ZakYip.Singulation.Host.Controllers {
    /// <summary>
    /// PPR 监控控制器，提供 PPR 变化历史查询接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class PprMonitoringController : ControllerBase {
        private readonly ILogger<PprMonitoringController> _logger;
        private readonly IPprChangeRecordStore _recordStore;

        public PprMonitoringController(
            ILogger<PprMonitoringController> logger,
            IPprChangeRecordStore recordStore) {
            _logger = logger;
            _recordStore = recordStore;
        }

        /// <summary>
        /// 获取指定轴的 PPR 变化历史
        /// </summary>
        /// <remarks>
        /// 查询指定轴的所有 PPR 值变化记录，按时间倒序排列。
        /// </remarks>
        /// <param name="axisId">轴标识符</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>PPR 变化记录列表</returns>
        /// <response code="200">查询成功</response>
        [HttpGet("history/{axisId}")]
        [SwaggerOperation(
            Summary = "获取指定轴的 PPR 变化历史",
            Description = "查询指定轴的所有 PPR 值变化记录，按时间倒序排列")]
        [ProducesResponseType(typeof(ApiResponse<List<PprChangeRecordDto>>), 200)]
        [Produces("application/json")]
        public async Task<IActionResult> GetAxisPprHistory(string axisId, CancellationToken ct) {
            try {
                var records = await _recordStore.GetByAxisIdAsync(axisId, ct);
                
                var dtos = records.Select(r => new PprChangeRecordDto {
                    Id = r.Id,
                    AxisId = r.AxisId,
                    OldPpr = r.OldPpr,
                    NewPpr = r.NewPpr,
                    Reason = r.Reason,
                    ChangedAt = r.ChangedAt,
                    IsAnomalous = r.IsAnomalous,
                    Notes = r.Notes
                }).ToList();

                return Ok(ApiResponse<List<PprChangeRecordDto>>.Success(dtos, 
                    $"查询成功，共 {dtos.Count} 条记录"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "查询轴 {AxisId} 的 PPR 历史失败", axisId);
                return StatusCode(500, ApiResponse<object>.Fail("查询 PPR 历史失败"));
            }
        }

        /// <summary>
        /// 获取所有 PPR 变化记录
        /// </summary>
        /// <remarks>
        /// 分页查询所有轴的 PPR 变化记录，按时间倒序排列。
        /// </remarks>
        /// <param name="skip">跳过的记录数（默认 0）</param>
        /// <param name="take">获取的记录数（默认 100，最大 100）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>PPR 变化记录列表</returns>
        /// <response code="200">查询成功</response>
        [HttpGet("history")]
        [SwaggerOperation(
            Summary = "获取所有 PPR 变化记录",
            Description = "分页查询所有轴的 PPR 变化记录，按时间倒序排列")]
        [ProducesResponseType(typeof(ApiResponse<List<PprChangeRecordDto>>), 200)]
        [Produces("application/json")]
        public async Task<IActionResult> GetAllPprHistory(
            [FromQuery] int skip = 0, 
            [FromQuery] int take = 100,
            CancellationToken ct = default) {
            try {
                // 限制 take 的最大值
                take = Math.Min(take, 100);
                
                var records = await _recordStore.GetAllAsync(skip, take, ct);
                
                var dtos = records.Select(r => new PprChangeRecordDto {
                    Id = r.Id,
                    AxisId = r.AxisId,
                    OldPpr = r.OldPpr,
                    NewPpr = r.NewPpr,
                    Reason = r.Reason,
                    ChangedAt = r.ChangedAt,
                    IsAnomalous = r.IsAnomalous,
                    Notes = r.Notes
                }).ToList();

                return Ok(ApiResponse<List<PprChangeRecordDto>>.Success(dtos, 
                    $"查询成功，共 {dtos.Count} 条记录"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "查询所有 PPR 历史失败");
                return StatusCode(500, ApiResponse<object>.Fail("查询 PPR 历史失败"));
            }
        }

        /// <summary>
        /// 获取异常 PPR 变化记录
        /// </summary>
        /// <remarks>
        /// 查询所有标记为异常的 PPR 变化记录。
        /// 异常变化通常指变化幅度过大或非预期的 PPR 值变化。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>异常 PPR 变化记录列表</returns>
        /// <response code="200">查询成功</response>
        [HttpGet("anomalies")]
        [SwaggerOperation(
            Summary = "获取异常 PPR 变化记录",
            Description = "查询所有标记为异常的 PPR 变化记录")]
        [ProducesResponseType(typeof(ApiResponse<List<PprChangeRecordDto>>), 200)]
        [Produces("application/json")]
        public async Task<IActionResult> GetAnomalousPprChanges(CancellationToken ct) {
            try {
                var records = await _recordStore.GetAnomalousAsync(ct);
                
                var dtos = records.Select(r => new PprChangeRecordDto {
                    Id = r.Id,
                    AxisId = r.AxisId,
                    OldPpr = r.OldPpr,
                    NewPpr = r.NewPpr,
                    Reason = r.Reason,
                    ChangedAt = r.ChangedAt,
                    IsAnomalous = r.IsAnomalous,
                    Notes = r.Notes
                }).ToList();

                return Ok(ApiResponse<List<PprChangeRecordDto>>.Success(dtos, 
                    $"查询成功，共发现 {dtos.Count} 条异常记录"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "查询异常 PPR 记录失败");
                return StatusCode(500, ApiResponse<object>.Fail("查询异常 PPR 记录失败"));
            }
        }

        /// <summary>
        /// 清理旧的 PPR 变化记录
        /// </summary>
        /// <remarks>
        /// 删除指定日期之前的 PPR 变化记录，用于定期清理历史数据。
        /// 建议保留至少 30 天的数据用于故障追溯。
        /// </remarks>
        /// <param name="beforeDate">删除此日期之前的记录（ISO 8601 格式）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">删除成功</response>
        /// <response code="400">参数验证失败</response>
        [HttpDelete("cleanup")]
        [SwaggerOperation(
            Summary = "清理旧的 PPR 变化记录",
            Description = "删除指定日期之前的 PPR 变化记录")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Produces("application/json")]
        public async Task<IActionResult> CleanupOldRecords(
            [FromQuery] DateTime beforeDate,
            CancellationToken ct) {
            try {
                // 验证日期不能是未来日期
                if (beforeDate > DateTime.Now) {
                    return BadRequest(ApiResponse<object>.Invalid("删除日期不能是未来日期"));
                }

                // 至少保留 7 天的数据
                var minRetentionDate = DateTime.Now.AddDays(-7);
                if (beforeDate > minRetentionDate) {
                    return BadRequest(ApiResponse<object>.Invalid("至少需要保留最近 7 天的数据"));
                }

                await _recordStore.DeleteOlderThanAsync(beforeDate, ct);
                
                return Ok(ApiResponse<object>.Success(null, 
                    $"已删除 {beforeDate:yyyy-MM-dd} 之前的 PPR 变化记录"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "清理 PPR 记录失败");
                return StatusCode(500, ApiResponse<object>.Fail("清理 PPR 记录失败"));
            }
        }
    }
}
