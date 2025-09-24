using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
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

        public AxisController(IBusAdapter bus, IDriveRegistry registry, IAxisEventAggregator aggregator) {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        }

        public IBusAdapter Bus { get; }
        public IReadOnlyList<IAxisDrive> Drives => _drives;

        public async Task InitializeAsync(string vendor, DriverOptions template, int? overrideAxisCount = null, CancellationToken ct = default) {
            await Bus.InitializeAsync(ct);

            // 轴数：优先外部覆盖，其次总线查询，再次模板 NodeId 范围
            var count = overrideAxisCount ?? await Bus.GetAxisCountAsync(ct);
            if (count <= 0) throw new InvalidOperationException("Axis count must be > 0.");

            _drives.Clear();
            for (ushort i = 1; i <= count; i++) {
                var axisId = new AxisId(Bus.TranslateNodeId(i));
                var opts = template with { NodeId = (ushort)axisId.Value };
                var drive = _registry.Create(vendor, axisId, port: null!, opts); // 若有 IAxisPort 决定是否传入
                _drives.Add(drive);
                _aggregator.Attach(drive);
            }
        }

        public async Task EnableAllAsync(CancellationToken ct = default) {
            await Task.WhenAll(_drives.Select(d => d.EnableAsync(ct).AsTask()));
        }

        public async Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default) {
            await Task.WhenAll(_drives.Select(d => d.SetAccelDecelByLinearAsync(accelMmPerSec2, decelMmPerSec2, ct).AsTask()));
        }

        public async Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default) {
            await Task.WhenAll(_drives.Select(d => d.WriteSpeedAsync(mmPerSec, ct).AsTask()));
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
    }
}