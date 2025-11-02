using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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
        private readonly List<decimal?> _lastSpeeds = new();

        public event EventHandler<string>? ControllerFaulted;

        public AxisController(IBusAdapter bus, IDriveRegistry registry, IAxisEventAggregator aggregator) {
            Bus = bus;
            _registry = registry;
            _aggregator = aggregator;
        }

        public IBusAdapter Bus { get; }
        public IReadOnlyList<IAxisDrive> Drives => _drives;
        public IReadOnlyList<decimal?> TargetSpeedsMmps => _drives.Select(d => d.LastTargetMmps).ToArray();
        public IReadOnlyList<decimal?> RealtimeSpeedsMmps => _drives.Select(d => d.LastFeedbackMmps).ToArray();

        public async Task<KeyValuePair<bool, string>> InitializeAsync(string vendor,
            DriverOptions template,
            int? overrideAxisCount = null, CancellationToken ct = default) {
            try {
                // 幂等：已初始化直接返回成功说明
                if (_drives.Count > 0)
                    return new KeyValuePair<bool, string>(true, $"Already initialized with {_drives.Count} axes.");

                // 1) 初始化总线（带原因文本）
                var busInit = await Bus.InitializeAsync(ct);
                if (!busInit.Key) {
                    var msg = $"Bus initialization failed: {busInit.Value}";
                    OnControllerFaulted(msg);
                    return new(false, msg);
                }

                // 2) 轴数判定：优先使用 override，其次总线探测
                ct.ThrowIfCancellationRequested();
                var count = (overrideAxisCount is > 0)
                    ? overrideAxisCount.Value
                    : await Bus.GetAxisCountAsync(ct);

                if (count <= 0) {
                    const string msg = "Axis count must be > 0.";
                    OnControllerFaulted(msg);
                    return new(false, msg);
                }

                // 3) 创建并注册驱动
                _drives.Clear();
                for (ushort i = 1; i <= count; i++) {
                    ct.ThrowIfCancellationRequested();

                    var axisId = new AxisId(Bus.TranslateNodeId(i));
                    var opts = template with {
                        NodeId = (ushort)axisId.Value,
                        IsReverse = Bus.ShouldReverse((ushort)axisId.Value)
                    };

                    try {
                        var drive = _registry.Create(vendor, axisId, port: null!, opts);
                        _drives.Add(drive);
                        _lastSpeeds.Add(null); // Initialize last speed as null for new axis
                        _aggregator.Attach(drive);
                    }
                    catch (Exception ex) {
                        var msg = $"Create axis {i} (node {axisId.Value}) failed: {ex.Message}";
                        OnControllerFaulted(msg);
                        _drives.Clear(); // 若需要可在此处补充逐个 Detach
                        _lastSpeeds.Clear();
                        return new(false, msg);
                    }
                }

                // 成功：返回说明文本，不触发 Faulted 事件
                return new(true, $"Initialized {_drives.Count} axes successfully.");
            }
            catch (OperationCanceledException) {
                const string msg = "Initialization canceled.";
                OnControllerFaulted(msg);
                return new(false, msg);
            }
            catch (Exception ex) {
                var msg = $"Unexpected error: {ex.Message}";
                OnControllerFaulted(msg);
                _drives.Clear();
                _lastSpeeds.Clear();
                return new(false, msg);
            }
        }

        private async Task ForEachDriveAsync(Func<IAxisDrive, Task> action, CancellationToken ct) {
            // 在执行轴操作前，检查总线是否已初始化
            if (!Bus.IsInitialized) {
                var msg = "总线未初始化或正在复位中，禁止轴操作";
                OnControllerFaulted(msg);
                throw new InvalidOperationException(msg);
            }

            // 并行执行所有轴的操作，提升性能
            var tasks = _drives.Select(async d => {
                ct.ThrowIfCancellationRequested();
                try {
                    await action(d);
                }
                catch (Exception ex) {
                    OnControllerFaulted($"Drive {d.Axis}: {ex.Message}");
                }
            });
            
            await Task.WhenAll(tasks);
        }

        public Task EnableAllAsync(CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.EnableAsync(ct), ct);

        public Task DisableAllAsync(CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.DisableAsync(ct).AsTask(), ct);

        public Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.SetAccelDecelByLinearAsync(accelMmPerSec2, decelMmPerSec2, ct), ct);

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
                _lastSpeeds.Clear();
                await Bus.CloseAsync(ct);
            }
        }

        public async Task ApplySpeedSetAsync(SpeedSet set, CancellationToken ct = default) {
            var main = set.MainMmps ?? [];
            var eject = set.EjectMmps ?? [];
            var totalAx = _drives.Count;

            if (totalAx == 0) {
                OnControllerFaulted($" _drives.Count={_drives.Count},无法赋值速度");
                return;
            }

            if (main.Count == 0 && eject.Count == 0) {
                OnControllerFaulted($"{JsonConvert.SerializeObject(set)}");
                OnControllerFaulted($"main.Count={main.Count}&&eject.Count={eject.Count},无法赋值速度");
                return;
            }

            // Distribute speeds based on AxisType
            var mainIndex = 0;
            var ejectIndex = 0;

            // Only write speed if it has changed from the last known value
            for (var i = 0; i < totalAx; i++) {
                if (ct.IsCancellationRequested) return;
                
                decimal newSpeed = 0m;
                var drive = _drives[i];
                
                // Assign speed based on axis type
                if (drive.AxisType == Core.Enums.AxisType.Main && mainIndex < main.Count) {
                    newSpeed = (decimal)main[mainIndex];
                    mainIndex++;
                } else if (drive.AxisType == Core.Enums.AxisType.Eject && ejectIndex < eject.Count) {
                    newSpeed = (decimal)eject[ejectIndex];
                    ejectIndex++;
                }
                
                var lastSpeed = _lastSpeeds[i];
                
                // Write speed only if it's different from the last written speed
                if (!lastSpeed.HasValue || lastSpeed.Value != newSpeed) {
                    try {
                        await drive.WriteSpeedAsync(newSpeed, ct);
                    } catch (Exception ex) {
                        OnControllerFaulted($"Failed to write speed for axis {i}: {ex.Message}");
                    }
                    _lastSpeeds[i] = newSpeed;
                }
            }
        }

        public void ResetLastSpeeds() {
            for (var i = 0; i < _lastSpeeds.Count; i++) {
                _lastSpeeds[i] = null;
            }
        }

        private void OnControllerFaulted(string msg) {
            ControllerFaulted?.Invoke(this, msg);
        }
    }
}