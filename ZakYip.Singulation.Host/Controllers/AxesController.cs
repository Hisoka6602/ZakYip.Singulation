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
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Host.Controllers {

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

        /// <summary>读取控制器模板（vendor + driver options）。</summary>
        [HttpGet("controller/options")]
        public async Task<ActionResult<ControllerOptions>> GetControllerOptions(CancellationToken ct) {
            var dto = await _ctrlOptsStore.GetAsync(ct);
            return dto is null
                ? NotFound(ApiResponse<ControllerOptions>.NotFound("未找到控制器配置"))
                : Ok(ApiResponse<ControllerOptions>.Success(dto, "获取成功"));
        }

        /// <summary>写入/更新控制器模板。</summary>
        [HttpPut("controller/options")]
        public async Task<IActionResult> PutControllerOptions([FromBody] ControllerOptions dto, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(dto.Vendor))
                return BadRequest(ApiResponse<object>.Invalid("Vendor 为必填项"));

            await _ctrlOptsStore.UpsertAsync(dto, ct);
            return Ok(ApiResponse<object>.Success(data: new { }, msg: "控制器模板已更新"));
        }

        // ================= 网格布局单例资源 =================

        /// <summary>
        /// 获取当前的轴网格布局（单例资源）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpGet("topology")]
        public async Task<ActionResult<AxisGridLayoutOptions>> GetTopology(CancellationToken ct) {
            var dto = await _layoutStore.GetAsync(ct);
            return dto is null
                ? NotFound(ApiResponse<AxisGridLayoutOptions>.NotFound("未找到轴布局"))
                : Ok(ApiResponse<AxisGridLayoutOptions>.Success(dto, "获取布局成功"));
        }

        /// <summary>
        /// 全量覆盖轴网格布局（单例资源）。
        /// </summary>
        /// <param name="req">新的布局定义（Rows/Cols/Placements）。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPut("topology")]
        public async Task<IActionResult> PutTopology([FromBody] AxisGridLayoutOptions req, CancellationToken ct) {
            if (req.Rows < 1 || req.Cols < 1)
                return BadRequest(ApiResponse<object>.Invalid("行列数必须大于等于1"));

            await _layoutStore.UpsertAsync(req, ct);
            return Ok(ApiResponse<object>.Success(new { }, "布局更新成功"));
        }

        /// <summary>
        /// 删除当前的轴网格布局（恢复为空）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpDelete("topology")]
        public async Task<IActionResult> DeleteTopology(CancellationToken ct) {
            await _layoutStore.DeleteAsync(ct);
            return Ok(ApiResponse<object>.Success(new { }, "布局已删除"));
        }

        // ================= 控制器（总线）单例资源 =================

        /// <summary>
        /// 查询控制器（总线）状态与轴数量。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpGet("controller")]
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
        /// 控制器复位（冷/热）。
        /// 冷复位：调用 <see cref="_bus.ResetAsync"/>；
        /// 热复位：默认 Close → Initialize（如有专用软复位 API 可替换）。
        /// </summary>
        /// <param name="req">复位类型（硬/软）。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPost("controller/reset")]
        public async Task<ActionResult<object>> ResetController([FromBody] ControllerResetRequestDto req, CancellationToken ct) {
            // 1) 取模板
            var opt = await _ctrlOptsStore.GetAsync(ct);
            if (opt is null)
                return BadRequest(ApiResponse<object>.Invalid("请先设置控制器模板", new { Hint = "PUT /api/axes/controller/options" }));

            var vendor = opt.Vendor;
            var tpl = MapToDriverOptions(opt.Template);
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
        /// 获取控制器当前错误码（0 表示无错）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpGet("controller/errors")]
        public async Task<object> GetControllerErrors(CancellationToken ct) {
            var err = await _axisController.Bus.GetErrorCodeAsync(ct);
            return Ok(ApiResponse<object>.Success(new { ErrorCode = err }, "获取错误码成功"));
        }

        /// <summary>
        /// 清空控制器错误（通常通过复位实现）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpDelete("controller/errors")]
        public async Task<object> ClearControllerErrors(CancellationToken ct) {
            var ok = await Safe(() => _axisController.Bus.ResetAsync(ct));
            if (!ok)
                return StatusCode(500, ApiResponse<object>.Fail("清除错误失败"));

            return Ok(ApiResponse<object>.Success(new { Accepted = true }, "错误已清除"));
        }

        // ================= 轴集合资源：/axes =================

        /// <summary>
        /// 列举当前注册的所有轴资源快照。
        /// </summary>
        [HttpGet("axes")]
        public ActionResult<IEnumerable<AxisResponseDto>> ListAxes() {
            var drives = _axisController.Drives;
            var data = drives.Select(ToAxisResource).ToList();
            return Ok(ApiResponse<IEnumerable<AxisResponseDto>>.Success(data, "获取轴列表成功"));
        }

        /// <summary>
        /// 获取指定轴的资源快照。
        /// </summary>
        /// <param name="axisId">轴的整数 ID（与 AxisId.Value 对齐）。</param>
        [HttpGet("axes/{axisId}")]
        public ActionResult<AxisResponseDto> GetAxis(int axisId) {
            var axisDrive = _axisController.Drives.FirstOrDefault(f => f.Axis.Value.Equals(axisId));
            if (axisDrive is null)
                return NotFound(ApiResponse<AxisResponseDto>.NotFound("轴未找到"));

            return Ok(ApiResponse<AxisResponseDto>.Success(ToAxisResource(axisDrive), "获取轴成功"));
        }

        /// <summary>
        /// 批量部分更新（启停、目标线速度、加减速、限幅、机械参数等）。
        /// 目标轴通过查询参数 axisIds 传入，例如：PATCH /api/axes?axisIds=1&axisIds=2。
        /// 若未传 axisIds（或为空），则对“全部轴”生效。
        /// </summary>
        /// <param name="axisIds">要更新的轴 ID 列表；为空/缺省表示全部轴。</param>
        /// <param name="req">批量更新请求体（不包含轴 ID）。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPatch("axes")]
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
        /// 单轴部分更新（启停、目标线速度、加减速、限幅、机械参数等）。
        /// </summary>
        /// <param name="axisId">轴标识（字符串形式，与注册表 key 一致）。</param>
        /// <param name="req">部分更新请求。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPatch("axes/{axisId}")]
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
        /// 批量使能轴（Enable）。
        /// 未传 axisIds 或为空时，对全部轴生效。
        /// </summary>
        /// <param name="axisIds">要操作的轴 ID；缺省/空 = 全部轴。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPost("axes/enable")]
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
        /// 批量释放/禁用轴（Disable）。
        /// 未传 axisIds 或为空时，对全部轴生效。
        /// </summary>
        /// <param name="axisIds">要操作的轴 ID；缺省/空 = 全部轴。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPost("axes/disable")]
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
        ///— 批量设置轴速度（线速度，单位 mm/s）。
        /// 未传 axisIds 或为空时，对全部轴生效。
        /// </summary>
        /// <param name="axisIds">要操作的轴 ID；缺省/空 = 全部轴。</param>
        /// <param name="req">目标线速度请求体（mm/s）。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPost("axes/speed")]
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
                LastErrorMessage = d?.LastErrorMessage
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
        /// 将“部分更新请求”映射为具体驱动调用；只更新请求中显式出现的字段。
        /// </summary>
        private async Task<bool> ApplyAxisPatch(IAxisDrive d, AxisPatchRequestDto req, CancellationToken ct) {
            var ok = true;

            if (req is { AccelMmps2: not null, DecelMmps2: not null }) {
                ok &= await Safe(() => d.SetAccelDecelAsync(req.AccelMmps2.Value, req.DecelMmps2.Value, ct));
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

        private static DriverOptions MapToDriverOptions(DriverOptionsTemplateOptions t) => new() {
            // 注意：Card/Port/NodeId/IsReverse 在 InitializeAsync 内按轴设置，这里不填
            GearRatio = t.GearRatio,
            ScrewPitchMm = t.ScrewPitchMm,
            PulleyDiameterMm = t.PulleyDiameterMm,
            PulleyPitchDiameterMm = t.PulleyPitchDiameterMm,
            MaxRpm = t.MaxRpm,
            MaxAccelRpmPerSec = t.MaxAccelRpmPerSec,
            MaxDecelRpmPerSec = t.MaxDecelRpmPerSec,
            MinWriteInterval = TimeSpan.FromMilliseconds(t.MinWriteInterval),
            ConsecutiveFailThreshold = t.ConsecutiveFailThreshold,
            EnableHealthMonitor = t.EnableHealthMonitor,
            HealthPingInterval = TimeSpan.FromMilliseconds(t.HealthPingInterval),
            Card = t.Card,
            Port = t.Port,
            NodeId = 0
        };

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