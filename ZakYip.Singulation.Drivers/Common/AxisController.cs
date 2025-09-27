using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Common {

    /// <summary>
    /// IAxisController 默认实现：基于 IBusAdapter + IDriveRegistry + IAxisEventAggregator。
    /// </summary>
    public sealed class AxisController : IAxisController {
        private readonly IDriveRegistry _registry;
        private readonly IAxisEventAggregator _aggregator;
        private readonly List<IAxisDrive> _drives = new();

        public event EventHandler<string>? ControllerFaulted;

        public AxisController(IBusAdapter bus, IDriveRegistry registry, IAxisEventAggregator aggregator) {
            Bus = bus;
            _registry = registry;
            _aggregator = aggregator;
        }

        public IBusAdapter Bus { get; }
        public IReadOnlyList<IAxisDrive> Drives => _drives;

        public async Task InitializeAsync(string vendor, DriverOptions template, int? overrideAxisCount = null, CancellationToken ct = default) {
            await Bus.InitializeAsync(ct);

            var count = overrideAxisCount ?? await Bus.GetAxisCountAsync(ct);
            if (count <= 0) {
                OnControllerFaulted("Axis count must be > 0.");
                return;
            }

            _drives.Clear();
            for (ushort i = 1; i <= count; i++) {
                var axisId = new AxisId(Bus.TranslateNodeId(i));
                var opts = template with {
                    NodeId = (ushort)axisId.Value,
                    IsReverse = Bus.ShouldReverse((ushort)axisId.Value)
                };
                var drive = _registry.Create(vendor, axisId, port: null!, opts);
                _drives.Add(drive);
                _aggregator.Attach(drive);
            }
        }

        public async Task EnableAllAsync(CancellationToken ct = default) {
            await Task.WhenAll(_drives.Select(d => d.EnableAsync(ct)));
        }

        public async Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default) {
            await Task.WhenAll(_drives.Select(d => d.SetAccelDecelByLinearAsync(accelMmPerSec2, decelMmPerSec2, ct).AsTask()));
        }

        public async Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default) {
            await Task.WhenAll(_drives.Select(d => d.WriteSpeedAsync(mmPerSec, ct)));
        }

        public async Task StopAllAsync(CancellationToken ct = default) {
            await Task.WhenAll(_drives.Select(d => d.StopAsync(ct).AsTask()));
        }

        public async Task DisposeAllAsync(CancellationToken ct = default) {
            try {
                await Task.WhenAll(_drives.Select(async d => {
                    _aggregator.Detach(d);
                    await d.DisposeAsync();
                }));
            }
            finally {
                _drives.Clear();
                await Bus.CloseAsync(ct);
            }
        }

        public async Task ApplySpeedSetAsync(SpeedSet set, CancellationToken ct = default) {
            var main = set.MainMmps ?? [];
            var eject = set.EjectMmps ?? [];
            var totalAx = _drives.Count;

            if (totalAx == 0) return;
            if (main.Count == 0 && eject.Count == 0) return;

            var axis = 0;

            // 1) 先写 Main；若超过轴数自动截断
            for (var i = 0; axis < totalAx && i < main.Count; i++, axis++) {
                if (ct.IsCancellationRequested) return;
                await _drives[axis].WriteSpeedAsync((decimal)main[i], ct);
            }

            // 2) 再用 Eject 补齐；同样不越界
            for (var i = 0; axis < totalAx && i < eject.Count; i++, axis++) {
                if (ct.IsCancellationRequested) return;
                await _drives[axis].WriteSpeedAsync((decimal)eject[i], ct);
            }

            // 3) 还不够就补零到 totalAx
            for (; axis < totalAx; axis++) {
                if (ct.IsCancellationRequested) return;
                await _drives[axis].WriteSpeedAsync(0m, ct);
            }
        }

        private void OnControllerFaulted(string msg) {
            // 不抛异常，直接触发事件
            ControllerFaulted?.Invoke(this, msg);
        }
    }
}