using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Host.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public sealed class AxesController : ControllerBase {
        private readonly IDriveRegistry _registry;
        private readonly IBusAdapter _bus;
        private readonly AxisUnitConverter _conv = new();

        public AxesController(IDriveRegistry registry, IBusAdapter bus) {
            _registry = registry;
            _bus = bus;
        }

        // ================= 控制器（总线）单例资源 =================

        // GET /api/v1/controller
        [HttpGet("controller")]
        public async Task<ActionResult<ControllerResource>> GetController(CancellationToken ct) {
            var count = await _bus.GetAxisCountAsync(ct);
            var err = await _bus.GetErrorCodeAsync(ct);
            return Ok(new ControllerResource { AxisCount = count, ErrorCode = err });
        }

        // POST /api/v1/controller/reset   { "type": "hard" | "soft" }
        public sealed class ControllerResetRequest { public string Type { get; init; } }

        [HttpPost("controller/reset")]
        public async Task<ActionResult<object>> ResetController([FromBody] ControllerResetRequest req, CancellationToken ct) {
            var ok = await Safe(async () => {
                var type = req.Type?.Trim().ToLowerInvariant();
                if (type == "hard") {
                    await _bus.ResetAsync(ct); // 冷复位
                }
                else if (type == "soft") {
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
        public ActionResult<IEnumerable<AxisResource>> ListAxes() {
            var drives = _registry.GetAll();
            return Ok(drives.Select(ToAxisResource));
        }

        // GET /api/v1/axes/{id}
        [HttpGet("axes/{axisId}")]
        public ActionResult<AxisResource> GetAxis(string axisId) {
            var d = _registry.Get(axisId);
            if (d is null) return NotFound();
            return Ok(ToAxisResource(d));
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

        // GET /api/v1/axes/{id}/stream  （只读子资源：事件流）
        [HttpGet("axes/{axisId}/stream")]
        public async Task Stream(string axisId, CancellationToken ct) {
            var d = _registry.Get(axisId);
            if (d is null) { Response.StatusCode = 404; return; }

            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("X-Accel-Buffering", "no");

            await WriteSse($"event: hello\ndata: {axisId}\n\n", ct);

            void OnFeedback(object? s, Drivers.Abstractions.Events.AxisSpeedFeedbackEventArgs e) {
                var mmps = _conv.RpmToLinearMmps(e.FeedbackRpm, d.Mechanics);
                _ = WriteSse($"event: feedback\ndata: {mmps:F3}\n\n", ct);
            }
            void OnError(object? s, Drivers.Abstractions.Events.AxisErrorEventArgs e) {
                _ = WriteSse($"event: error\ndata: {e.ErrorCode}:{e.Message}\n\n", ct);
            }

            d.AxisSpeedFeedback += OnFeedback;
            d.AxisFaulted += OnError;
            try { while (!ct.IsCancellationRequested) await Task.Delay(1000, ct); }
            catch (TaskCanceledException) { }
            finally { d.AxisSpeedFeedback -= OnFeedback; d.AxisFaulted -= OnError; }

            async Task WriteSse(string payload, CancellationToken token) {
                var bytes = Encoding.UTF8.GetBytes(payload);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length, token);
                await Response.Body.FlushAsync(token);
            }
        }

        // ================= 内部实现 =================

        private AxisResource ToAxisResource(IAxisDrive d) {
            var limits = new LimitsDto {
                MaxLinearMmps = _conv.RpmToLinearMmps((double)d.Options.MaxRpm, d.Mechanics),
                MaxAccelMmps2 = d.Options.MaxAccelLinearMmps2,
                MaxDecelMmps2 = d.Options.MaxDecelLinearMmps2
            };
            var mech = new MechanicsDto {
                RollerDiameterMm = d.Mechanics.RollerDiameterMm,
                GearRatio = d.Mechanics.GearRatio,
                Ppr = d.Mechanics.Ppr
            };
            return new AxisResource {
                AxisId = d.Axis.ToString(),
                Enabled = d.IsEnabled,
                TargetLinearMmps = _conv.RpmToLinearMmps(d.TargetRpm, d.Mechanics),
                FeedbackLinearMmps = _conv.RpmToLinearMmps(d.FeedbackRpm, d.Mechanics),
                DriverStatus = d.Status.ToString(),
                LastErrorCode = d.LastErrorCode,
                Limits = limits,
                Mechanics = mech
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