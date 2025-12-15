using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Infrastructure.Services;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 配置管理控制器
    /// </summary>
    /// <remarks>
    /// 提供配置的导入、导出、验证和模板功能。
    /// 支持配置的 JSON 文件导入导出，配置模板生成，以及配置迁移工具。
    /// </remarks>
    [ApiController]
    [Route("api/configurations")]
    public sealed class ConfigurationController : ControllerBase {
        private readonly ILogger<ConfigurationController> _logger;
        private readonly ConfigurationImportExportService _configService;
        private readonly ISystemClock _clock;

        public ConfigurationController(
            ILogger<ConfigurationController> logger,
            ConfigurationImportExportService configService,
            ISystemClock clock) {
            _logger = logger;
            _configService = configService;
            _clock = clock;
        }

        /// <summary>
        /// 导出所有配置
        /// </summary>
        /// <remarks>
        /// 导出系统的所有配置到 JSON 格式。
        /// 包括控制器配置、速度联动配置和 IO 联动配置。
        /// 
        /// **使用示例**：
        /// 
        /// ```
        /// GET /api/configurations/export
        /// ```
        /// 
        /// **返回示例**：
        /// ```json
        /// {
        ///   "version": "1.0.0",
        ///   "exportedAt": "2025-11-07T06:00:00Z",
        ///   "description": "配置导出",
        ///   "controllerOptions": { ... },
        ///   "speedLinkageOptions": { ... },
        ///   "ioLinkageOptions": { ... }
        /// }
        /// ```
        /// </remarks>
        /// <param name="description">可选的导出描述</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>包含所有配置的 JSON 对象</returns>
        /// <response code="200">导出成功</response>
        /// <response code="500">导出失败</response>
        [HttpGet("export")]
        [SwaggerOperation(
            Summary = "导出所有配置",
            Description = "导出系统的所有配置到 JSON 格式，包括控制器、速度联动和 IO 联动配置。")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 500)]
        [Produces("application/json")]
        public async Task<IActionResult> ExportAllConfigurationsAsync(
            [FromQuery] string? description = null,
            CancellationToken ct = default) {
            
            try {
                var json = await _configService.ExportAllConfigurationsAsync(description, ct);
                return Content(json, "application/json", Encoding.UTF8);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "导出配置失败");
                return StatusCode(500, ApiResponse<string>.Fail("配置导出失败: " + ex.Message));
            }
        }

        /// <summary>
        /// 下载配置文件
        /// </summary>
        /// <remarks>
        /// 下载系统的所有配置为 JSON 文件。
        /// 文件名格式：config-export-{timestamp}.json
        /// </remarks>
        /// <param name="description">可选的导出描述</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>配置文件下载</returns>
        /// <response code="200">下载成功</response>
        /// <response code="500">下载失败</response>
        [HttpGet("export/download")]
        [SwaggerOperation(
            Summary = "下载配置文件",
            Description = "下载系统的所有配置为 JSON 文件。")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 500)]
        public async Task<IActionResult> DownloadConfigurationAsync(
            [FromQuery] string? description = null,
            CancellationToken ct = default) {
            
            try {
                var json = await _configService.ExportAllConfigurationsAsync(description, ct);
                var bytes = Encoding.UTF8.GetBytes(json);
                var fileName = $"config-export-{_clock.Now:yyyyMMdd-HHmmss}.json";
                
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "下载配置文件失败");
                return StatusCode(500, ApiResponse<string>.Fail("配置下载失败: " + ex.Message));
            }
        }

        /// <summary>
        /// 导入配置
        /// </summary>
        /// <remarks>
        /// 从 JSON 导入配置到系统。
        /// 支持导入控制器配置、速度联动配置和 IO 联动配置。
        /// 
        /// **使用示例**：
        /// 
        /// ```
        /// POST /api/configurations/import
        /// Content-Type: application/json
        /// 
        /// {
        ///   "version": "1.0.0",
        ///   "controllerOptions": { ... },
        ///   "speedLinkageOptions": { ... },
        ///   "ioLinkageOptions": { ... }
        /// }
        /// ```
        /// 
        /// **注意事项**：
        /// - 导入前会进行完整的配置验证
        /// - 验证失败时不会保存任何配置
        /// - 建议先使用 validateOnly=true 进行预检查
        /// - 导入成功后配置会立即生效
        /// </remarks>
        /// <param name="configJson">配置 JSON 字符串</param>
        /// <param name="validateOnly">仅验证不导入</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>导入结果</returns>
        /// <response code="200">导入成功或验证通过</response>
        /// <response code="400">配置验证失败</response>
        [HttpPost("import")]
        [SwaggerOperation(
            Summary = "导入配置",
            Description = "从 JSON 导入配置到系统，支持验证模式和完整导入模式。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<string>>> ImportConfigurationsAsync(
            [FromBody] string configJson,
            [FromQuery] bool validateOnly = false,
            CancellationToken ct = default) {
            
            try {
                var result = await _configService.ImportAllConfigurationsAsync(configJson, validateOnly, ct);
                
                if (result.IsSuccess) {
                    var message = validateOnly 
                        ? "配置验证通过" 
                        : $"配置导入成功，已导入 {result.ImportedConfigurations.Count} 个配置项";
                    
                    _logger.LogInformation(message);
                    return ApiResponse<string>.Success(result.GetFormattedReport());
                }
                
                _logger.LogWarning("配置导入失败，错误数: {ErrorCount}", result.Errors.Count);
                return BadRequest(ApiResponse<string>.Invalid(result.GetFormattedReport()));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "导入配置失败");
                return BadRequest(ApiResponse<string>.Invalid("配置导入失败: " + ex.Message));
            }
        }

        /// <summary>
        /// 上传并导入配置文件
        /// </summary>
        /// <remarks>
        /// 上传 JSON 配置文件并导入到系统。
        /// </remarks>
        /// <param name="file">配置文件（JSON 格式）</param>
        /// <param name="validateOnly">仅验证不导入</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>导入结果</returns>
        /// <response code="200">导入成功或验证通过</response>
        /// <response code="400">配置验证失败或文件格式错误</response>
        [HttpPost("import/upload")]
        [SwaggerOperation(
            Summary = "上传并导入配置文件",
            Description = "上传 JSON 配置文件并导入到系统。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<string>>> UploadAndImportConfigurationAsync(
            IFormFile file,
            [FromQuery] bool validateOnly = false,
            CancellationToken ct = default) {
            
            if (file == null || file.Length == 0) {
                return BadRequest(ApiResponse<string>.Invalid("请上传有效的配置文件"));
            }

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
                return BadRequest(ApiResponse<string>.Invalid("仅支持 JSON 格式的配置文件"));
            }

            try {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var json = await reader.ReadToEndAsync(ct);

                var result = await _configService.ImportAllConfigurationsAsync(json, validateOnly, ct);
                
                if (result.IsSuccess) {
                    var message = validateOnly 
                        ? "配置验证通过" 
                        : $"配置导入成功，已导入 {result.ImportedConfigurations.Count} 个配置项";
                    
                    _logger.LogInformation("{Message}，文件: {FileName}", message, file.FileName);
                    return ApiResponse<string>.Success(result.GetFormattedReport());
                }
                
                _logger.LogWarning("配置文件导入失败，文件: {FileName}, 错误数: {ErrorCount}", 
                    file.FileName, result.Errors.Count);
                return BadRequest(ApiResponse<string>.Invalid(result.GetFormattedReport()));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "上传配置文件失败，文件: {FileName}", file.FileName);
                return BadRequest(ApiResponse<string>.Invalid("配置文件导入失败: " + ex.Message));
            }
        }

        /// <summary>
        /// 获取配置模板
        /// </summary>
        /// <remarks>
        /// 获取指定类型的配置模板 JSON。
        /// 可用于快速创建新配置或了解配置结构。
        /// 
        /// **配置类型**：
        /// - Controller: 控制器配置模板
        /// - SpeedLinkage: 速度联动配置模板
        /// - IoLinkage: IO 联动配置模板
        /// - All: 完整配置包模板（包含所有配置）
        /// 
        /// **使用示例**：
        /// 
        /// ```
        /// GET /api/configurations/template?type=All
        /// ```
        /// </remarks>
        /// <param name="type">配置类型</param>
        /// <returns>配置模板 JSON</returns>
        /// <response code="200">获取成功</response>
        /// <response code="400">无效的配置类型</response>
        [HttpGet("template")]
        [SwaggerOperation(
            Summary = "获取配置模板",
            Description = "获取指定类型的配置模板 JSON，可用于快速创建新配置或了解配置结构。")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [Produces("application/json")]
        public IActionResult GetConfigurationTemplate(
            [FromQuery] ConfigurationImportExportService.ConfigurationType type = 
                ConfigurationImportExportService.ConfigurationType.All) {
            
            try {
                var template = _configService.CreateConfigurationTemplate(type);
                return Content(template, "application/json", Encoding.UTF8);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "获取配置模板失败，类型: {Type}", type);
                return BadRequest(ApiResponse<string>.Invalid("获取配置模板失败: " + ex.Message));
            }
        }

        /// <summary>
        /// 下载配置模板文件
        /// </summary>
        /// <remarks>
        /// 下载指定类型的配置模板为 JSON 文件。
        /// </remarks>
        /// <param name="type">配置类型</param>
        /// <returns>模板文件下载</returns>
        /// <response code="200">下载成功</response>
        /// <response code="400">无效的配置类型</response>
        [HttpGet("template/download")]
        [SwaggerOperation(
            Summary = "下载配置模板文件",
            Description = "下载指定类型的配置模板为 JSON 文件。")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public IActionResult DownloadConfigurationTemplate(
            [FromQuery] ConfigurationImportExportService.ConfigurationType type = 
                ConfigurationImportExportService.ConfigurationType.All) {
            
            try {
                var template = _configService.CreateConfigurationTemplate(type);
                var bytes = Encoding.UTF8.GetBytes(template);
                var fileName = $"config-template-{type.ToString().ToLower()}.json";
                
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "下载配置模板失败，类型: {Type}", type);
                return BadRequest(ApiResponse<string>.Invalid("下载配置模板失败: " + ex.Message));
            }
        }
    }
}
