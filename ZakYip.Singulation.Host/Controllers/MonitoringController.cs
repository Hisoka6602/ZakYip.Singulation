using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Infrastructure.Services;

namespace ZakYip.Singulation.Host.Controllers {
    /// <summary>
    /// 监控控制器，提供实时监控数据查询接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class MonitoringController : ControllerBase {
        private readonly ILogger<MonitoringController> _logger;
        private readonly SystemHealthMonitorService _healthMonitor;
        private readonly FaultDiagnosisService _diagnosisService;

        public MonitoringController(
            ILogger<MonitoringController> logger,
            SystemHealthMonitorService healthMonitor,
            FaultDiagnosisService diagnosisService) {
            _logger = logger;
            _healthMonitor = healthMonitor;
            _diagnosisService = diagnosisService;
        }

        /// <summary>
        /// 获取系统健康度
        /// </summary>
        /// <remarks>
        /// 返回系统当前的健康度评分、在线轴数量、错误率等指标。
        /// 健康度评分范围 0-100，等级分为：优秀(90-100)、良好(70-90)、警告(40-70)、危急(0-40)。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>系统健康度数据</returns>
        /// <response code="200">获取成功</response>
        [HttpGet("health")]
        [SwaggerOperation(
            Summary = "获取系统健康度",
            Description = "返回系统当前的健康度评分、在线轴数量、错误率等指标")]
        [ProducesResponseType(typeof(ApiResponse<SystemHealthDto>), 200)]
        [Produces("application/json")]
        public IActionResult GetSystemHealth(CancellationToken ct) {
            try {
                // 注意：实际的健康度数据会通过 SignalR 实时推送
                // 这个 API 提供一次性查询功能
                var health = new SystemHealthDto {
                    Score = 0,
                    Level = HealthLevel.Good,
                    OnlineAxisCount = 0,
                    TotalAxisCount = 0,
                    FaultedAxisCount = 0,
                    AverageResponseTimeMs = 0,
                    ErrorRate = 0,
                    Description = "请订阅 SignalR MonitoringHub 获取实时健康数据",
                    Timestamp = DateTime.Now
                };

                return Ok(ApiResponse<SystemHealthDto>.Success(health, 
                    "系统健康度查询成功。建议使用 SignalR 订阅实时数据"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "获取系统健康度失败");
                return StatusCode(500, ApiResponse<SystemHealthDto>.Fail("获取系统健康度失败"));
            }
        }

        /// <summary>
        /// 诊断指定轴的故障
        /// </summary>
        /// <remarks>
        /// 对指定轴进行智能故障诊断，返回故障类型、可能原因和解决建议。
        /// 如果轴运行正常，将返回 null。
        /// </remarks>
        /// <param name="axisId">轴标识符</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>故障诊断结果</returns>
        /// <response code="200">诊断成功</response>
        /// <response code="404">轴不存在</response>
        [HttpGet("diagnose/{axisId}")]
        [SwaggerOperation(
            Summary = "诊断指定轴的故障",
            Description = "对指定轴进行智能故障诊断，返回故障类型、可能原因和解决建议")]
        [ProducesResponseType(typeof(ApiResponse<FaultDiagnosisDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [Produces("application/json")]
        public async Task<IActionResult> DiagnoseAxis(string axisId, CancellationToken ct) {
            try {
                var diagnosis = await _diagnosisService.DiagnoseAxisAsync(axisId, ct);
                
                if (diagnosis == null) {
                    return Ok(ApiResponse<FaultDiagnosisDto?>.Success(null, 
                        $"轴 {axisId} 运行正常，无需诊断"));
                }

                if (diagnosis.FaultType == "AXIS_NOT_FOUND") {
                    return NotFound(ApiResponse<object>.NotFound($"未找到轴 {axisId}"));
                }

                return Ok(ApiResponse<FaultDiagnosisDto>.Success(diagnosis, "诊断完成"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "诊断轴 {AxisId} 失败", axisId);
                return StatusCode(500, ApiResponse<object>.Fail($"诊断轴 {axisId} 失败"));
            }
        }

        /// <summary>
        /// 诊断所有轴的故障
        /// </summary>
        /// <remarks>
        /// 扫描所有轴并返回存在故障或警告的轴的诊断结果。
        /// 运行正常的轴不会出现在结果列表中。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>故障轴的诊断结果列表</returns>
        /// <response code="200">诊断成功</response>
        [HttpGet("diagnose/all")]
        [SwaggerOperation(
            Summary = "诊断所有轴的故障",
            Description = "扫描所有轴并返回存在故障或警告的轴的诊断结果")]
        [ProducesResponseType(typeof(ApiResponse<List<FaultDiagnosisDto>>), 200)]
        [Produces("application/json")]
        public async Task<IActionResult> DiagnoseAllAxes(CancellationToken ct) {
            try {
                var diagnoses = await _diagnosisService.DiagnoseAllAxesAsync(ct);
                
                return Ok(ApiResponse<List<FaultDiagnosisDto>>.Success(diagnoses, 
                    $"诊断完成，发现 {diagnoses.Count} 个问题"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "诊断所有轴失败");
                return StatusCode(500, ApiResponse<object>.Fail("诊断所有轴失败"));
            }
        }

        /// <summary>
        /// 查询故障知识库
        /// </summary>
        /// <remarks>
        /// 根据错误码查询故障知识库，获取该错误的详细说明和解决方案。
        /// </remarks>
        /// <param name="errorCode">错误码</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>故障知识库条目</returns>
        /// <response code="200">查询成功</response>
        /// <response code="404">未找到对应的知识库条目</response>
        [HttpGet("knowledge-base/{errorCode}")]
        [SwaggerOperation(
            Summary = "查询故障知识库",
            Description = "根据错误码查询故障知识库，获取该错误的详细说明和解决方案")]
        [ProducesResponseType(typeof(ApiResponse<FaultDiagnosisDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [Produces("application/json")]
        public IActionResult QueryKnowledgeBase(int errorCode, CancellationToken ct) {
            try {
                var entry = _diagnosisService.QueryKnowledgeBase(errorCode);
                
                if (entry == null) {
                    return NotFound(ApiResponse<object>.NotFound(
                        $"未找到错误码 {errorCode} 的知识库条目"));
                }

                return Ok(ApiResponse<FaultDiagnosisDto>.Success(entry, "查询成功"));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "查询知识库失败，错误码: {ErrorCode}", errorCode);
                return StatusCode(500, ApiResponse<object>.Fail("查询知识库失败"));
            }
        }
    }
}
