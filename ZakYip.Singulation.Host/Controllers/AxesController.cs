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
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Core.Contracts.Dto;
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

        /// <summary>
        /// 通过依赖注入构造控制器。
        /// </summary>
        public AxesController(
            IDriveRegistry registry,
            IAxisLayoutStore layoutStore,
            IAxisController axisController,
            IControllerOptionsStore ctrlOptsStore) {
            _registry = registry;

            _layoutStore = layoutStore;
            _axisController = axisController;
            _ctrlOptsStore = ctrlOptsStore;
        }

        /// <summary>读取控制器模板（vendor + driver options）。</summary>
        [HttpGet("controller/options")]
        public async Task<ActionResult<ControllerOptionsDto>> GetControllerOptions(CancellationToken ct) {
            var dto = await _ctrlOptsStore.GetAsync(ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        /// <summary>写入/更新控制器模板。</summary>
        [HttpPut("controller/options")]
        public async Task<IActionResult> PutControllerOptions([FromBody] ControllerOptionsDto dto, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(dto.Vendor)) return BadRequest("Vendor is required.");
            await _ctrlOptsStore.UpsertAsync(dto, ct);
            return NoContent();
        }

        // ================= 网格布局单例资源 =================

        /// <summary>
        /// 获取当前的轴网格布局（单例资源）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpGet("topology")]
        public async Task<ActionResult<AxisGridLayoutDto>> GetTopology(CancellationToken ct) {
            var dto = await _layoutStore.GetAsync(ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// 全量覆盖轴网格布局（单例资源）。
        /// </summary>
        /// <param name="req">新的布局定义（Rows/Cols/Placements）。</param>
        /// <param name="ct">取消令牌。</param>
        [HttpPut("topology")]
        public async Task<IActionResult> PutTopology([FromBody] AxisGridLayoutDto req, CancellationToken ct) {
            if (req.Rows < 1 || req.Cols < 1)
                return BadRequest("Rows/Cols must be >= 1.");

            await _layoutStore.UpsertAsync(req, ct);
            return NoContent();
        }

        /// <summary>
        /// 删除当前的轴网格布局（恢复为空）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpDelete("topology")]
        public async Task<IActionResult> DeleteTopology(CancellationToken ct) {
            await _layoutStore.DeleteAsync(ct);
            return NoContent();
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
            return Ok(new ControllerResponseDto {
                AxisCount = count,
                ErrorCode = err,
                Initialized = count > 0 && err == 0
            });
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
            if (opt is null) return BadRequest("Controller options not set. PUT /api/axes/controller/options first.");

            var vendor = opt.Vendor;
            var tpl = MapToDriverOptions(opt.Template);
            var ok = await Safe(async () => {
                if (req.Type == ControllerResetType.Hard) {
                    await _axisController.Bus.ResetAsync(ct); // 冷复位
                }
                else if (req.Type == ControllerResetType.Soft) {
                    // 若有专门软复位 API，请替换为 _bus.SoftResetAsync(ct)
                    await _axisController.Bus.CloseAsync(ct);
                    await _axisController.InitializeAsync(vendor, tpl, opt.OverrideAxisCount, ct);
                }
                else {
                    throw new ArgumentException("type must be 'hard' or 'soft'");
                }
            });
            return Ok(new { Accepted = ok });
        }

        /// <summary>
        /// 获取控制器当前错误码（0 表示无错）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpGet("controller/errors")]
        public async Task<object> GetControllerErrors(CancellationToken ct) {
            var err = await _axisController.Bus.GetErrorCodeAsync(ct);
            return new { ErrorCode = err };
        }

        /// <summary>
        /// 清空控制器错误（通常通过复位实现）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        [HttpDelete("controller/errors")]
        public async Task<object> ClearControllerErrors(CancellationToken ct) {
            var ok = await Safe(() => _axisController.Bus.ResetAsync(ct)); // 常见：复位清错
            return new { Accepted = ok };
        }

        // ================= 轴集合资源：/axes =================

        /// <summary>
        /// 列举当前注册的所有轴资源快照。
        /// </summary>
        [HttpGet("axes")]
        public ActionResult<IEnumerable<AxisResponseDto>> ListAxes() {
            var drives = _axisController.Drives;
            return Ok(drives.Select(ToAxisResource));
        }

        /// <summary>
        /// 获取指定轴的资源快照。
        /// </summary>
        /// <param name="axisId">轴的整数 ID（与 AxisId.Value 对齐）。</param>
        [HttpGet("axes/{axisId}")]
        public ActionResult<AxisResponseDto> GetAxis(int axisId) {
            var axisDrive = _axisController.Drives.FirstOrDefault(f => f.Axis.Value.Equals(axisId));
            if (axisDrive is null) return NotFound();
            return Ok(ToAxisResource(axisDrive));
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
            // 解析目标轴：null/空 => 全部
            var targets = ResolveTargets(axisIds);
            if (targets.Count == 0)
                return NotFound("No matching axis found for given axisIds (or no axes registered).");

            // 逐轴执行
            var results = await ForEachAxis(targets, async d => await ApplyAxisPatch(d, req, ct));
            return Ok(new BatchCommandResponseDto { Results = results });
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
            if (axisDrive is null) return NotFound();
            var accepted = await ApplyAxisPatch(axisDrive, req, ct);
            return Ok(new AxisCommandResultDto { AxisId = axisDrive.Axis.ToString(), Accepted = accepted, LastError = axisDrive.LastErrorMessage });
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
                return NotFound("No matching axis found for given axisIds (or no axes registered).");

            var results = await ForEachAxis(targets, d => Safe(() => d.EnableAsync(ct)));
            return Ok(new BatchCommandResponseDto { Results = results });
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
                return NotFound("No matching axis found for given axisIds (or no axes registered).");

            // 要求 IAxisDrive 提供 DisableAsync；若旧驱动暂未实现，可在实现里降级为 StopAsync。
            var results = await ForEachAxis(targets, d =>
                Safe(() => {
                    var task = d.DisposeAsync();
                    return task.AsTask();
                }));
            return Ok(new BatchCommandResponseDto { Results = results });
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
                return NotFound("No matching axis found for given axisIds (or no axes registered).");

            // 使用 IAxisDrive 的线速度写入重载：WriteSpeedAsync(mm/s)
            var results = await ForEachAxis(targets, d => Safe(() => d.WriteSpeedAsync((decimal)req.LinearMmps, ct)));
            return Ok(new BatchCommandResponseDto { Results = results });
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

        private static DriverOptions MapToDriverOptions(DriverOptionsTemplateDto t) => new() {
            // 注意：Card/Port/NodeId/IsReverse 在 InitializeAsync 内按轴设置，这里不填
            GearRatio = t.GearRatio,
            ScrewPitchMm = t.ScrewPitchMm,
            PulleyDiameterMm = t.PulleyDiameterMm,
            PulleyPitchDiameterMm = t.PulleyPitchDiameterMm,
            MaxRpm = t.MaxRpm,
            MaxAccelRpmPerSec = t.MaxAccelRpmPerSec,
            MaxDecelRpmPerSec = t.MaxDecelRpmPerSec,
            MinWriteInterval = t.MinWriteInterval,
            ConsecutiveFailThreshold = t.ConsecutiveFailThreshold,
            EnableHealthMonitor = t.EnableHealthMonitor,
            HealthPingInterval = t.HealthPingInterval,
            Card = t.Card,
            Port = t.Port,
            NodeId = 0
        };

        /// <summary>
        /// 安全执行某个异步动作：吞掉异常并返回 false；具体异常信息由驱动层事件和 LastErrorMessage 负责记录。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<bool> Safe(Func<Task> act) {
            try { await act(); return true; }
            catch { return false; }
        }
    }
}