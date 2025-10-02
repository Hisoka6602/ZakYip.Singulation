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
            if (_drives.Count > 0) return; // 幂等保护

            var busInitialized = await Bus.InitializeAsync(ct);
            if (!busInitialized) {
                OnControllerFaulted("Bus initialization failed.");
                return;
            }

            var count = overrideAxisCount is > 0 ? overrideAxisCount : await Bus.GetAxisCountAsync(ct);
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
            OnControllerFaulted("完成");
        }

        private async Task ForEachDriveAsync(Func<IAxisDrive, Task> action, CancellationToken ct) {
            foreach (var d in _drives) {
                ct.ThrowIfCancellationRequested();
                try {
                    await action(d);
                }
                catch (Exception ex) {
                    OnControllerFaulted($"Drive {d.Axis}: {ex.Message}");
                }

                // 间隔至少 2ms，避免指令过于密集
                await Task.Delay(2, ct);
            }
        }

        public Task EnableAllAsync(CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.EnableAsync(ct), ct);

        public Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.SetAccelDecelByLinearAsync(accelMmPerSec2, decelMmPerSec2, ct).AsTask(), ct);

        public Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.WriteSpeedAsync(mmPerSec, ct), ct);

        public Task StopAllAsync(CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.StopAsync(ct).AsTask(), ct);

        public async Task DisposeAllAsync(CancellationToken ct = default) {
            try {
                await ForEachDriveAsync(async d => {
                    _aggregator.Detach(d);
                    await d.DisposeAsync();
                }, ct);
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

            var speeds = new List<decimal>(totalAx);
            speeds.AddRange(main.Select(x => (decimal)x));
            speeds.AddRange(eject.Select(x => (decimal)x));
            while (speeds.Count < totalAx) speeds.Add(0m);

            for (int i = 0; i < totalAx; i++) {
                if (ct.IsCancellationRequested) return;
                await _drives[i].WriteSpeedAsync(speeds[i], ct);
            }
        }

        private void OnControllerFaulted(string msg) {
            ControllerFaulted?.Invoke(this, msg);
        }
    }
}