using System;
using System.Linq;
using System.Text;
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
        private readonly IBusAdapter _bus;
        private readonly IAxisLayoutStore _layoutStore;
        private readonly IAxisController _axisController;
        private readonly AxisUnitConverter _conv = new();

        public AxesController(IDriveRegistry registry, IBusAdapter bus,
            IAxisLayoutStore layoutStore, IAxisController axisController) {
            _registry = registry;
            _bus = bus;
            _layoutStore = layoutStore;
            _axisController = axisController;
        }

        // ================= 网格布局单例资源 =================
        /// <summary>
        /// 获取当前的轴网格布局（单例资源）。
        /// </summary>
        [HttpGet("topology")]
        public async Task<ActionResult<AxisGridLayoutDto>> GetTopology(CancellationToken ct) {
            var dto = await _layoutStore.GetAsync(ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// 全量更新/覆盖轴网格布局（单例资源）。
        /// 注意：该操作会替换 Rows/Cols 与 Placements。
        /// </summary>
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
        [HttpDelete("topology")]
        public async Task<IActionResult> DeleteTopology(CancellationToken ct) {
            await _layoutStore.DeleteAsync(ct);
            return NoContent();
        }

        // ================= 控制器（总线）单例资源 =================

        // GET /api/v1/controller
        [HttpGet("controller")]
        public async Task<ActionResult<ControllerResourceDto>> GetController(CancellationToken ct) {
            var count = await _bus.GetAxisCountAsync(ct);
            var err = await _bus.GetErrorCodeAsync(ct);
            return Ok(new ControllerResourceDto { AxisCount = count, ErrorCode = err, Initialized = count > 0 && err == 0 });
        }

        [HttpPost("controller/reset")]
        public async Task<ActionResult<object>> ResetController([FromBody] ControllerResetRequestDto req, CancellationToken ct) {
            var ok = await Safe(async () => {
                if (req.Type == ControllerResetType.Hard) {
                    await _bus.ResetAsync(ct); // 冷复位
                }
                else if (req.Type == ControllerResetType.Soft) {
                    // 若有专门软复位 API，请替换为 _bus.SoftResetAsync(ct)
                    await _bus.CloseAsync(ct);
                    await _bus.InitializeAsync(ct);
                }
                else {
                    throw new ArgumentException("type must be 'hard' or 'soft'");
                }
            });
            return Ok(new { Accepted = ok });
        }

        // GET /api/v1/controller/errors
        [HttpGet("controller/errors")]
        public async Task<object> GetControllerErrors(CancellationToken ct) {
            var err = await _bus.GetErrorCodeAsync(ct);
            return new { ErrorCode = err };
        }

        // DELETE /api/v1/controller/errors
        [HttpDelete("controller/errors")]
        public async Task<object> ClearControllerErrors(CancellationToken ct) {
            var ok = await Safe(() => _bus.ResetAsync(ct)); // 常见：复位清错
            return new { Accepted = ok };
        }

        // ================= 轴集合资源：/axes =================

        // GET /api/v1/axes
        [HttpGet("axes")]
        public ActionResult<IEnumerable<AxisResourceDto>> ListAxes() {
            var drives = _axisController.Drives;
            return Ok(drives.Select(ToAxisResource));
        }

        // GET /api/v1/axes/{id}
        [HttpGet("axes/{axisId}")]
        public ActionResult<AxisResourceDto> GetAxis(int axisId) {
            var axisDrive = _axisController.Drives.FirstOrDefault(f => f.Axis.Value.Equals(axisId));
            if (axisDrive is null) return NotFound();
            return Ok(ToAxisResource(axisDrive));
        }

        // PATCH /api/v1/axes          （批量部分更新，主用）
        [HttpPatch("axes")]
        public async Task<ActionResult<BatchCommandResponse>> PatchAxes([FromBody] AxesPatchRequest req, CancellationToken ct) {
            var targets = ResolveTargets(req.Targets);
            var results = await ForEachAxis(targets, async d => await ApplyAxisPatch(d, req, ct));
            return Ok(new BatchCommandResponse { Results = results });
        }

        // PATCH /api/v1/axes/{id}     （单轴部分更新）
        [HttpPatch("axes/{axisId}")]
        public async Task<ActionResult<AxisCommandResult>> PatchAxis(string axisId, [FromBody] AxisPatchRequest req, CancellationToken ct) {
            var d = _registry.Get(axisId);
            if (d is null) return NotFound();
            var accepted = await ApplyAxisPatch(d, req, ct);
            return Ok(new AxisCommandResult { AxisId = d.Axis.ToString(), Accepted = accepted, LastError = d.LastErrorMessage });
        }

        // ================= 内部实现 =================

        private static AxisResourceDto ToAxisResource(IAxisDrive d) {
            // 尽可能从快照接口读取（最近一次目标/反馈、使能、错误等）

            return new AxisResourceDto {
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

        private IEnumerable<IAxisDrive> ResolveTargets(List<string> targets) {
            if (targets.Count == 1 && targets[0].Equals("all", StringComparison.OrdinalIgnoreCase))
                return _registry.GetAll();
            return targets.Select(id => _registry.Get(id)).Where(d => d is not null)!;
        }

        private async Task<List<AxisCommandResult>> ForEachAxis(IEnumerable<IAxisDrive> drives, Func<IAxisDrive, Task<bool>> act) {
            var list = new List<AxisCommandResult>();
            foreach (var d in drives) {
                var ok = await act(d);
                list.Add(new AxisCommandResult { AxisId = d.Axis.ToString(), Accepted = ok, LastError = d.LastErrorMessage });
            }
            return list;
        }

        // 统一将“更新请求”映射为驱动层调用；只更新请求里出现的字段
        private async Task<bool> ApplyAxisPatch(IAxisDrive d, AxisPatchRequest req, CancellationToken ct) {
            var ok = true;

            if (req.Enabled.HasValue) {
                ok &= await Safe(() => req.Enabled.Value ? d.EnableAsync(ct) : d.DisableAsync(ct));
            }
            if (req.TargetLinearMmps.HasValue) {
                ok &= await Safe(() => d.WriteSpeedLinearAsync(req.TargetLinearMmps.Value, ct));
            }
            if (req.AccelMmps2.HasValue || req.DecelMmps2.HasValue) {
                var a = req.AccelMmps2 ?? d.Options.MaxAccelLinearMmps2;
                var b = req.DecelMmps2 ?? d.Options.MaxDecelLinearMmps2;
                ok &= await Safe(() => d.SetAccelDecelAsync(a, b, ct));
            }
            if (req.Limits is not null &&
                (req.Limits.MaxLinearMmps.HasValue || req.Limits.MaxAccelMmps2.HasValue || req.Limits.MaxDecelMmps2.HasValue)) {
                var vmax = req.Limits.MaxLinearMmps ?? _conv.RpmToLinearMmps((double)d.Options.MaxRpm, d.Mechanics);
                var amax = req.Limits.MaxAccelMmps2 ?? d.Options.MaxAccelLinearMmps2;
                var dmax = req.Limits.MaxDecelMmps2 ?? d.Options.MaxDecelLinearMmps2;
                ok &= await Safe(() => d.UpdateLinearLimitsAsync(vmax, amax, dmax, ct));
            }
            if (req.Mechanics is not null &&
                (req.Mechanics.RollerDiameterMm.HasValue || req.Mechanics.GearRatio.HasValue || req.Mechanics.Ppr.HasValue)) {
                var dia = req.Mechanics.RollerDiameterMm ?? d.Mechanics.RollerDiameterMm;
                var gr = req.Mechanics.GearRatio ?? d.Mechanics.GearRatio;
                var ppr = req.Mechanics.Ppr ?? d.Mechanics.Ppr;
                ok &= await Safe(() => d.UpdateMechanicsAsync(dia, gr, ppr, ct));
            }

            return ok;
        }

        private Task<bool> ApplyAxisPatch(IAxisDrive d, AxesPatchRequest req, CancellationToken ct)
            => ApplyAxisPatch(d, new AxisPatchRequest {
                Enabled = req.Enabled,
                TargetLinearMmps = req.TargetLinearMmps,
                AccelMmps2 = req.AccelMmps2,
                DecelMmps2 = req.DecelMmps2,
                Limits = req.Limits is null ? null : new AxisPatchRequest.LimitsPatch {
                    MaxLinearMmps = req.Limits.MaxLinearMmps,
                    MaxAccelMmps2 = req.Limits.MaxAccelMmps2,
                    MaxDecelMmps2 = req.Limits.MaxDecelMmps2
                },
                Mechanics = req.Mechanics is null ? null : new AxisPatchRequest.MechanicsPatch {
                    RollerDiameterMm = req.Mechanics.RollerDiameterMm,
                    GearRatio = req.Mechanics.GearRatio,
                    Ppr = req.Mechanics.Ppr
                }
            }, ct);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<bool> Safe(Func<Task> act) {
            try { await act(); return true; }
            catch { return false; } // 具体错误已在驱动层事件与 LastErrorMessage 中记录
        }
    }
}