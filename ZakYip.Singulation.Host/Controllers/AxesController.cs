using System;
using System.Linq;
using System.Text;
using Polly.Caching;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Core.Enums;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Common;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 轴控制器，提供对多轴系统的配置、状态查询和运动控制功能。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AxesController : ControllerBase {
        private readonly IDriveRegistry _registry;
        private readonly IAxisLayoutStore _layoutStore;
        private readonly IAxisController _axisController;
        private readonly IControllerOptionsStore _ctrlOptsStore;
        private readonly ILogger<AxesController> _logger;

        /// <summary>
        /// 通过依赖注入构造控制器。
        /// </summary>
        public AxesController(
            IDriveRegistry registry,
            IAxisLayoutStore layoutStore,
            IAxisController axisController,
            IControllerOptionsStore ctrlOptsStore,
            ILogger<AxesController> logger) {
            _registry = registry;

            _layoutStore = layoutStore;
            _axisController = axisController;
            _ctrlOptsStore = ctrlOptsStore;
            _logger = logger;

            _axisController.ControllerFaulted += (sender, s) => {
                _logger.LogError(s);
            };
        }

        /// <summary>
        /// 读取控制器模板配置
        /// </summary>
        /// <remarks>
        /// 获取当前控制器的厂商类型和驱动选项配置。
        /// 返回包含 Vendor（厂商名称）和 Template（驱动参数模板）的配置对象。
        /// </remarks>
        /// <param name="ct">取消令牌，用于请求取消</param>
        /// <returns>控制器配置对象，包含厂商和驱动选项</returns>
        /// <response code="200">获取成功</response>
        [HttpGet("controller/options")]
        [SwaggerOperation(
            Summary = "读取控制器模板配置",
            Description = "获取当前控制器的厂商类型和驱动选项配置。返回包含 Vendor（厂商名称）和 Template（驱动参数模板）的配置对象。")]
        [ProducesResponseType(typeof(ApiResponse<ControllerOptions>), 200)]
        [Produces("application/json")]
        public async Task<ActionResult<ControllerOptions>> GetControllerOptions(CancellationToken ct) {
            var dto = await _ctrlOptsStore.GetAsync(ct);
            return Ok(ApiResponse<ControllerOptions>.Success(dto, "获取成功"));
        }

        /// <summary>
        /// 更新控制器模板配置
        /// </summary>
        /// <remarks>
        /// 写入或更新控制器的厂商类型和驱动选项。
        /// Vendor 字段为必填项，必须指定有效的厂商名称。
        /// </remarks>
        /// <param name="dto">控制器配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">更新成功</response>
        /// <response code="400">参数验证失败</response>
        [HttpPut("controller/options")]
        [SwaggerOperation(
            Summary = "更新控制器模板配置",
            Description = "写入或更新控制器的厂商类型和驱动选项。Vendor 字段为必填项，必须指定有效的厂商名称。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> PutControllerOptions([FromBody] ControllerOptions dto, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(dto.Vendor))
                return BadRequest(ApiResponse<object>.Invalid("Vendor 为必填项"));

            await _ctrlOptsStore.UpsertAsync(dto, ct);
            return Ok(ApiResponse<object>.Success(data: new { }, msg: "控制器模板已更新"));
        }

        // ================= 网格布局单例资源 =================

        /// <summary>
        /// 获取轴网格布局配置
        /// </summary>
        /// <remarks>
        /// 获取当前的轴网格布局（单例资源），包含行数、列数和轴位置信息。
        /// 用于定义多轴系统的物理布局排列方式。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>轴网格布局配置对象</returns>
        /// <response code="200">获取布局成功</response>
        [HttpGet("topology")]
        [SwaggerOperation(
            Summary = "获取轴网格布局配置",
            Description = "获取当前的轴网格布局（单例资源），包含行数、列数和轴位置信息。用于定义多轴系统的物理布局排列方式。")]
        [ProducesResponseType(typeof(ApiResponse<AxisGridLayoutOptions>), 200)]
        [Produces("application/json")]
        public async Task<ActionResult<AxisGridLayoutOptions>> GetTopology(CancellationToken ct) {
            var dto = await _layoutStore.GetAsync(ct);
            return Ok(ApiResponse<AxisGridLayoutOptions>.Success(dto, "获取布局成功"));
        }

        /// <summary>
        /// 更新轴网格布局配置
        /// </summary>
        /// <remarks>
        /// 全量覆盖当前的轴网格布局配置。
        /// 需要提供完整的布局定义，包括行数（Rows）、列数（Cols）和轴位置映射（Placements）。
        /// 行列数必须大于等于 1。
        /// </remarks>
        /// <param name="req">新的布局定义</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">布局更新成功</response>
        /// <response code="400">参数验证失败</response>
        [HttpPut("topology")]
        [SwaggerOperation(
            Summary = "更新轴网格布局配置",
            Description = "全量覆盖当前的轴网格布局配置。需要提供完整的布局定义，包括行数（Rows）、列数（Cols）。行列数必须大于等于 1。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> PutTopology([FromBody] AxisGridLayoutOptions req, CancellationToken ct) {
            if (req.Rows < 1 || req.Cols < 1)
                return BadRequest(ApiResponse<object>.Invalid("行列数必须大于等于1"));

            await _layoutStore.UpsertAsync(req, ct);
            return Ok(ApiResponse<object>.Success(new { }, "布局更新成功"));
        }

        /// <summary>
        /// 删除轴网格布局配置
        /// </summary>
        /// <remarks>
        /// 删除当前的轴网格布局，恢复为空配置。
        /// 此操作会清除所有布局信息。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">布局已删除</response>
        [HttpDelete("topology")]
        [SwaggerOperation(
            Summary = "删除轴网格布局配置",
            Description = "删除当前的轴网格布局，恢复为空配置。此操作会清除所有布局信息。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [Produces("application/json")]
        public async Task<IActionResult> DeleteTopology(CancellationToken ct) {
            await _layoutStore.DeleteAsync(ct);
            return Ok(ApiResponse<object>.Success(new { }, "布局已删除"));
        }

        // ================= 控制器（总线）单例资源 =================

        /// <summary>
        /// 获取控制器状态信息
        /// </summary>
        /// <remarks>
        /// 查询控制器（总线）的当前状态，包括轴数量、错误码和初始化状态。
        /// Initialized 字段为 true 表示控制器已正常初始化且无错误。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>控制器状态对象</returns>
        /// <response code="200">获取控制器状态成功</response>
        [HttpGet("controller")]
        [SwaggerOperation(
            Summary = "获取控制器状态信息",
            Description = "查询控制器（总线）的当前状态，包括轴数量、错误码和初始化状态。Initialized 字段为 true 表示控制器已正常初始化且无错误。")]
        [ProducesResponseType(typeof(ApiResponse<ControllerResponseDto>), 200)]
        [Produces("application/json")]
        public async Task<ActionResult<ControllerResponseDto>> GetController(CancellationToken ct) {
            var count = await _axisController.Bus.GetAxisCountAsync(ct);
            var err = await _axisController.Bus.GetErrorCodeAsync(ct);
            var data = new ControllerResponseDto {
                AxisCount = count,
                ErrorCode = err,
                Initialized = count > 0 && err == 0
            };
            return Ok(ApiResponse<ControllerResponseDto>.Success(data, "获取控制器状态成功"));
        }

        /// <summary>
        /// 控制器复位操作
        /// </summary>
        /// <remarks>
        /// 执行控制器复位操作，支持硬复位和软复位两种模式：
        /// - Hard（硬复位）：调用底层硬件复位接口，完全重置控制器
        /// - Soft（软复位）：先关闭连接，然后重新初始化控制器
        /// 复位后会验证控制器是否成功初始化。
        /// </remarks>
        /// <param name="req">复位请求，指定复位类型（Hard 或 Soft）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">控制器复位成功</response>
        /// <response code="500">控制器复位失败</response>
        [HttpPost("controller/reset")]
        [SwaggerOperation(
            Summary = "控制器复位操作",
            Description = "执行控制器复位操作，支持硬复位（Hard）和软复位（Soft）两种模式。硬复位调用底层硬件复位接口，完全重置控制器；软复位先关闭连接，然后重新初始化控制器。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<object>> ResetController([FromBody] ControllerResetRequestDto req, CancellationToken ct) {
            // 1) 取模板
            var opt = await _ctrlOptsStore.GetAsync(ct);
            var vendor = opt.Vendor;
            var tpl = opt.Template.ToDriverOptionsTemplate();
            var ok = await Safe(async () => {
                switch (req.Type) {
                    case ControllerResetType.Hard:
                        await _axisController.Bus.ResetAsync(ct);
                        break;

                    case ControllerResetType.Soft:
                        await _axisController.Bus.CloseAsync(ct);
                        await _axisController.InitializeAsync(vendor, tpl, opt.OverrideAxisCount, ct);
                        break;

                    default:
                        throw new ArgumentException("type must be 'hard' or 'soft'");
                }

                if (!_axisController.Bus.IsInitialized) {
                    throw new Exception("控制器复位失败");
                }
            });

            if (!ok)
                return StatusCode(500, ApiResponse<object>.Fail("控制器复位失败", new { ResetType = req.Type }));

            return Ok(ApiResponse<object>.Success(new { Accepted = true }, "控制器复位成功"));
        }

        /// <summary>
        /// 获取控制器错误码
        /// </summary>
        /// <remarks>
        /// 获取控制器当前的错误码。
        /// ErrorCode 为 0 表示无错误，非 0 值表示存在错误。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>包含错误码的对象</returns>
        /// <response code="200">获取错误码成功</response>
        [HttpGet("controller/errors")]
        [SwaggerOperation(
            Summary = "获取控制器错误码",
            Description = "获取控制器当前的错误码。ErrorCode 为 0 表示无错误，非 0 值表示存在错误。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [Produces("application/json")]
        public async Task<object> GetControllerErrors(CancellationToken ct) {
            var err = await _axisController.Bus.GetErrorCodeAsync(ct);
            return Ok(ApiResponse<object>.Success(new { ErrorCode = err }, "获取错误码成功"));
        }

        /// <summary>
        /// 清除控制器错误
        /// </summary>
        /// <remarks>
        /// 清空控制器的错误状态。
        /// 此操作通常通过复位控制器来实现。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">错误已清除</response>
        /// <response code="500">清除错误失败</response>
        [HttpDelete("controller/errors")]
        [SwaggerOperation(
            Summary = "清除控制器错误",
            Description = "清空控制器的错误状态。此操作通常通过复位控制器来实现。")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        [Produces("application/json")]
        public async Task<object> ClearControllerErrors(CancellationToken ct) {
            var ok = await Safe(() => _axisController.Bus.ResetAsync(ct));
            if (!ok)
                return StatusCode(500, ApiResponse<object>.Fail("清除错误失败"));

            return Ok(ApiResponse<object>.Success(new { Accepted = true }, "错误已清除"));
        }

        // ================= 轴集合资源：/axes =================

        /// <summary>
        /// 获取所有轴的状态列表
        /// </summary>
        /// <remarks>
        /// 列举当前系统中注册的所有轴的资源快照。
        /// 返回每个轴的 ID、状态、使能状态、目标速度、反馈速度和错误信息等。
        /// </remarks>
        /// <returns>轴状态列表</returns>
        /// <response code="200">获取轴列表成功</response>
        [HttpGet("axes")]
        [SwaggerOperation(
            Summary = "获取所有轴的状态列表",
            Description = "列举当前系统中注册的所有轴的资源快照。返回每个轴的 ID、状态、使能状态、目标速度、反馈速度和错误信息等。")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<AxisResponseDto>>), 200)]
        [Produces("application/json")]
        public ActionResult<IEnumerable<AxisResponseDto>> ListAxes() {
            var drives = _axisController.Drives;
            var data = drives.Select(ToAxisResource).ToList();
            return Ok(ApiResponse<IEnumerable<AxisResponseDto>>.Success(data, "获取轴列表成功"));
        }

        /// <summary>
        /// 获取指定轴的状态信息
        /// </summary>
        /// <remarks>
        /// 根据轴 ID 获取单个轴的详细状态信息。
        /// 包括轴的状态、使能状态、速度信息和错误信息等。
        /// </remarks>
        /// <param name="axisId">轴的整数 ID</param>
        /// <returns>轴状态对象</returns>
        /// <response code="200">获取轴成功</response>
        /// <response code="404">轴未找到</response>
        [HttpGet("axes/{axisId}")]
        [SwaggerOperation(
            Summary = "获取指定轴的状态信息",
            Description = "根据轴 ID 获取单个轴的详细状态信息。包括轴的状态、使能状态、速度信息和错误信息等。")]
        [ProducesResponseType(typeof(ApiResponse<AxisResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AxisResponseDto>), 404)]
        [Produces("application/json")]
        public ActionResult<AxisResponseDto> GetAxis(int axisId) {
            var axisDrive = _axisController.Drives.FirstOrDefault(f => f.Axis.Value.Equals(axisId));
            if (axisDrive is null)
                return NotFound(ApiResponse<AxisResponseDto>.NotFound("轴未找到"));

            return Ok(ApiResponse<AxisResponseDto>.Success(ToAxisResource(axisDrive), "获取轴成功"));
        }

        /// <summary>
        /// 批量部分更新轴参数
        /// </summary>
        /// <remarks>
        /// 批量部分更新（启停、目标线速度、加减速、限幅、机械参数等）。
        /// 目标轴通过查询参数 axisIds 传入，例如：PATCH /api/axes?axisIds=1&amp;axisIds=2。
        /// 若未传 axisIds（或为空），则对全部轴生效。
        /// </remarks>
        /// <param name="axisIds">要更新的轴 ID 列表；为空或缺省表示全部轴</param>
        /// <param name="req">批量更新请求体（不包含轴 ID）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量操作结果</returns>
        /// <response code="200">批量更新完成</response>
        /// <response code="404">未找到匹配的轴</response>
        [HttpPatch("axes")]
        [SwaggerOperation(
            Summary = "批量部分更新轴参数",
            Description = "批量部分更新轴的加减速、限幅、机械参数等。目标轴通过查询参数 axisIds 传入。若未传 axisIds（或为空），则对全部轴生效。")]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 404)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<BatchCommandResponseDto>> PatchAxes(
            [FromQuery] int[]? axisIds,
            [FromBody] AxisPatchRequestDto req,
            CancellationToken ct) {
            var targets = ResolveTargets(axisIds);
            if (targets.Count == 0)
                return NotFound(ApiResponse<BatchCommandResponseDto>.NotFound("未找到匹配的轴"));

            var results = await ForEachAxis(targets, async d => await ApplyAxisPatch(d, req, ct));
            var response = new BatchCommandResponseDto { Results = results };

            return Ok(ApiResponse<BatchCommandResponseDto>.Success(response, "批量更新完成"));
        }

        /// <summary>
        /// 更新单个轴的参数
        /// </summary>
        /// <remarks>
        /// 部分更新指定轴的参数，包括加减速、速度限制、机械参数等。
        /// 只更新请求体中明确提供的字段，未提供的字段保持不变。
        /// </remarks>
        /// <param name="axisId">轴 ID</param>
        /// <param name="req">部分更新请求体</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">轴参数更新成功</response>
        /// <response code="400">轴参数更新失败</response>
        /// <response code="404">轴未找到</response>
        [HttpPatch("axes/{axisId}")]
        [SwaggerOperation(
            Summary = "更新单个轴的参数",
            Description = "部分更新指定轴的参数，包括加减速、速度限制、机械参数等。只更新请求体中明确提供的字段，未提供的字段保持不变。")]
        [ProducesResponseType(typeof(ApiResponse<AxisCommandResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AxisCommandResultDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<AxisCommandResultDto>), 404)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<AxisCommandResultDto>> PatchAxis(int axisId, [FromBody] AxisPatchRequestDto req, CancellationToken ct) {
            var axisDrive = _axisController.Drives.FirstOrDefault(f => f.Axis.Value.Equals(axisId));
            if (axisDrive is null)
                return NotFound(ApiResponse<AxisCommandResultDto>.NotFound("轴未找到"));

            var accepted = await ApplyAxisPatch(axisDrive, req, ct);
            var result = new AxisCommandResultDto {
                AxisId = axisDrive.Axis.ToString(),
                Accepted = accepted,
                LastError = axisDrive.LastErrorMessage
            };

            return accepted
                ? Ok(ApiResponse<AxisCommandResultDto>.Success(result, "轴参数更新成功"))
                : BadRequest(ApiResponse<AxisCommandResultDto>.Fail("轴参数更新失败", result));
        }

        /// <summary>
        /// 批量使能轴
        /// </summary>
        /// <remarks>
        /// 批量使能（启用）指定的轴或全部轴。
        /// 使能后轴可以接受运动命令并执行。
        /// 如果不指定 axisIds 或传入空数组，则对所有轴生效。
        /// </remarks>
        /// <param name="axisIds">要使能的轴 ID 列表，为空表示全部轴</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量操作结果</returns>
        /// <response code="200">批量使能完成</response>
        /// <response code="404">未找到匹配的轴</response>
        [HttpPost("axes/enable")]
        [SwaggerOperation(
            Summary = "批量使能轴",
            Description = "批量使能（启用）指定的轴或全部轴。使能后轴可以接受运动命令并执行。如果不指定 axisIds 或传入空数组，则对所有轴生效。")]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 404)]
        [Produces("application/json")]
        public async Task<ActionResult<BatchCommandResponseDto>> EnableAxes(
            [FromQuery] int[]? axisIds,
            CancellationToken ct) {
            var targets = ResolveTargets(axisIds);
            if (targets.Count == 0)
                return NotFound(ApiResponse<BatchCommandResponseDto>.NotFound("未找到匹配的轴"));

            var results = await ForEachAxis(targets, d => Safe(() => d.EnableAsync(ct)));
            var response = new BatchCommandResponseDto { Results = results };

            return Ok(ApiResponse<BatchCommandResponseDto>.Success(response, "批量使能完成"));
        }

        /// <summary>
        /// 批量禁用轴
        /// </summary>
        /// <remarks>
        /// 批量禁用（释放）指定的轴或全部轴。
        /// 禁用后轴不再响应运动命令。
        /// 如果不指定 axisIds 或传入空数组，则对所有轴生效。
        /// </remarks>
        /// <param name="axisIds">要禁用的轴 ID 列表，为空表示全部轴</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量操作结果</returns>
        /// <response code="200">批量禁用完成</response>
        /// <response code="404">未找到匹配的轴</response>
        [HttpPost("axes/disable")]
        [SwaggerOperation(
            Summary = "批量禁用轴",
            Description = "批量禁用（释放）指定的轴或全部轴。禁用后轴不再响应运动命令。如果不指定 axisIds 或传入空数组，则对所有轴生效。")]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 404)]
        [Produces("application/json")]
        public async Task<ActionResult<BatchCommandResponseDto>> DisableAxes(
            [FromQuery] int[]? axisIds,
            CancellationToken ct) {
            var targets = ResolveTargets(axisIds);
            if (targets.Count == 0)
                return NotFound(ApiResponse<BatchCommandResponseDto>.NotFound("未找到匹配的轴"));

            // 要求 IAxisDrive 提供 DisableAsync；若旧驱动暂未实现，可在实现里降级为 StopAsync。
            var results = await ForEachAxis(targets, d =>
                Safe(async () => {
                    await d.DisposeAsync().AsTask();
                }));

            var response = new BatchCommandResponseDto { Results = results };

            return Ok(ApiResponse<BatchCommandResponseDto>.Success(response, "批量禁用完成"));
        }

        /// <summary>
        /// 批量设置轴速度
        /// </summary>
        /// <remarks>
        /// 批量设置指定轴或全部轴的目标线速度。
        /// 速度单位为 mm/s（毫米/秒）。
        /// 如果不指定 axisIds 或传入空数组，则对所有轴生效。
        /// </remarks>
        /// <param name="axisIds">要设置速度的轴 ID 列表，为空表示全部轴</param>
        /// <param name="req">目标线速度请求体，包含速度值（mm/s）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量操作结果</returns>
        /// <response code="200">设置速度成功</response>
        /// <response code="404">未找到匹配的轴</response>
        [HttpPost("axes/speed")]
        [SwaggerOperation(
            Summary = "批量设置轴速度",
            Description = "批量设置指定轴或全部轴的目标线速度。速度单位为 mm/s（毫米/秒）。如果不指定 axisIds 或传入空数组，则对所有轴生效。")]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<BatchCommandResponseDto>), 404)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<BatchCommandResponseDto>> SetAxesSpeed(
            [FromQuery] int[]? axisIds,
            [FromBody] SetSpeedRequestDto req,
            CancellationToken ct) {
            var targets = ResolveTargets(axisIds);
            if (targets.Count == 0)
                return NotFound(ApiResponse<BatchCommandResponseDto>.NotFound("未找到匹配的轴"));

            // 要求 IAxisDrive 提供 DisableAsync；若旧驱动暂未实现，可在实现里降级为 StopAsync。
            var results = await ForEachAxis(targets, d =>
                Safe(async () => {
                    await d.WriteSpeedAsync((decimal)req.LinearMmps, ct);
                }));

            var response = new BatchCommandResponseDto { Results = results };

            return Ok(ApiResponse<BatchCommandResponseDto>.Success(response, "设置速度成功"));
        }

        // ================= 内部实现 =================

        /// <summary>
        /// 将驱动实例映射为对外资源 DTO（尽量走驱动的快照字段；保持前向兼容的可空设计）。
        /// </summary>
        private static AxisResponseDto ToAxisResource(IAxisDrive d) {
            return new AxisResponseDto {
                AxisId = d.Axis.ToString(),
                Status = d.Status,

                // 快照存在则映射；否则置为 null，保持 API 的前向兼容
                Enabled = d?.IsEnabled,
                TargetLinearMmps = d?.LastTargetMmps.HasValue == true ? (double?)d.LastTargetMmps.Value : null,
                FeedbackLinearMmps = d?.LastFeedbackMmps.HasValue == true ? (double?)d.LastFeedbackMmps.Value : null,
                LastErrorCode = d?.LastErrorCode,
                LastErrorMessage = d?.LastErrorMessage,
                MaxLinearMmps = d?.MaxLinearMmps.HasValue == true ? (double?)d.MaxLinearMmps.Value : null,
                MaxAccelMmps2 = d?.MaxAccelMmps2.HasValue == true ? (double?)d.MaxAccelMmps2.Value : null,
                MaxDecelMmps2 = d?.MaxDecelMmps2.HasValue == true ? (double?)d.MaxDecelMmps2.Value : null
            };
        }

        /// <summary>
        /// 根据一组轴 ID 解析为驱动集合；
        /// - 传入 null 或空数组 ⇒ 返回全部轴；
        /// - 仅返回存在的轴实例（忽略无效 ID）。</summary>
        private List<IAxisDrive> ResolveTargets(int[]? axisIds) {
            if (axisIds is null || axisIds.Length == 0)
                return _axisController.Drives.ToList();

            var set = new HashSet<int>(axisIds);
            return _axisController.Drives.Where(d => set.Contains(d.Axis.Value)).ToList();
        }

        /// <summary>
        /// 遍历执行驱动命令并收集结果（不抛异常，异常通过 Safe → false）。
        /// </summary>
        private async Task<List<AxisCommandResultDto>> ForEachAxis(IEnumerable<IAxisDrive> drives, Func<IAxisDrive, Task<bool>> act) {
            var list = new List<AxisCommandResultDto>();
            foreach (var d in drives) {
                var ok = await act(d);
                list.Add(new AxisCommandResultDto { AxisId = d.Axis.ToString(), Accepted = ok, LastError = d.LastErrorMessage });
            }
            return list;
        }

        /// <summary>
        /// 将"部分更新请求"映射为具体驱动调用；只更新请求中显式出现的字段。
        /// </summary>
        private async Task<bool> ApplyAxisPatch(IAxisDrive d, AxisPatchRequestDto req, CancellationToken ct) {
            var ok = true;

            if (req is { AccelMmps2: not null, DecelMmps2: not null }) {
                ok &= await Safe(() => d.SetAccelDecelByLinearAsync(req.AccelMmps2.Value, req.DecelMmps2.Value, ct));
            }
            if (req.Limits is { MaxLinearMmps: not null, MaxAccelMmps2: not null, MaxDecelMmps2: not null }) {
                ok &= await Safe(() => d.UpdateLinearLimitsAsync(req.Limits.MaxLinearMmps.Value,
                    req.Limits.MaxAccelMmps2.Value, req.Limits.MaxDecelMmps2.Value, ct));
            }
            if (req.Mechanics is { RollerDiameterMm: not null, GearRatio: not null, Ppr: not null }) {
                ok &= await Safe(() => d.UpdateMechanicsAsync(req.Mechanics.RollerDiameterMm.Value,
                    req.Mechanics.GearRatio.Value, req.Mechanics.Ppr.Value, ct));
            }

            return ok;
        }

        /// <summary>
        /// 安全执行某个异步动作：吞掉异常并返回 false；具体异常信息由驱动层事件和 LastErrorMessage 负责记录。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<bool> Safe(Func<Task> act) {
            try {
                await act();
                return true;
            }
            catch (Exception exception) {
                _logger.LogError(exception, "SafeException");
                return false;
            }
        }
    }
}