using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Exceptions;
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

        /// <summary>
        /// 初始化轴控制器，包括总线初始化、轴计数探测和驱动实例创建。
        /// </summary>
        /// <param name="vendor">硬件供应商标识符。</param>
        /// <param name="template">驱动选项模板，用于配置每个轴。</param>
        /// <param name="overrideAxisCount">可选的轴数量覆盖值，如果不提供则从总线自动探测。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>包含成功状态和描述信息的键值对。</returns>
        /// <remarks>
        /// 此方法是幂等的，如果已经初始化则直接返回成功。
        /// 初始化流程：
        /// 1. 初始化总线适配器
        /// 2. 确定轴数量（优先使用覆盖值，否则从总线探测）
        /// 3. 为每个轴创建驱动实例并注册到事件聚合器
        /// 如果初始化失败，会触发 ControllerFaulted 事件并返回错误信息。
        /// </remarks>
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
                    catch (AxisControlException) {
                        throw; // Re-throw domain exceptions
                    }
                    catch (InvalidOperationException ex) {
                        var msg = $"Create axis {i} (node {axisId.Value}) failed: {ex.Message}";
                        OnControllerFaulted(msg);
                        _drives.Clear(); // 若需要可在此处补充逐个 Detach
                        _lastSpeeds.Clear();
                        throw new AxisControlException(msg, axisId.Value, ex);
                    }
                    catch (ArgumentException ex) {
                        var msg = $"Create axis {i} (node {axisId.Value}) failed: {ex.Message}";
                        OnControllerFaulted(msg);
                        _drives.Clear();
                        _lastSpeeds.Clear();
                        throw new AxisControlException(msg, axisId.Value, ex);
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
            catch (AxisControlException ex) {
                var msg = ex.Message;
                OnControllerFaulted(msg);
                _drives.Clear();
                _lastSpeeds.Clear();
                return new(false, msg);
            }
            catch (HardwareCommunicationException ex) {
                var msg = $"Hardware communication error: {ex.Message}";
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

            // 使用 Parallel.ForEachAsync 并行执行所有轴的操作，每个操作启动前间隔1ms
            // 这样可以避免并发调用过快导致部分轴执行失败
            var index = 0;
            await Parallel.ForEachAsync(_drives, ct, async (d, token) => {
                // 为每个轴添加1ms的启动间隔，避免并发调用过快
                var currentIndex = Interlocked.Increment(ref index) - 1;
                if (currentIndex > 0) {
                    await Task.Delay(1, token);
                }
                
                token.ThrowIfCancellationRequested();
                try {
                    // 记录操作开始和轴状态
                    OnControllerFaulted($"[轴操作开始] 轴={d.Axis}, 当前状态={d.Status}, 使能状态={d.IsEnabled}");
                    await action(d);
                    OnControllerFaulted($"[轴操作完成] 轴={d.Axis}, 当前状态={d.Status}, 使能状态={d.IsEnabled}");
                }
                catch (OperationCanceledException) {
                    throw; // Let cancellation propagate
                }
                catch (AxisOperationException ex) {
                    OnControllerFaulted($"Drive {d.Axis}: {ex.Message}");
                }
                catch (HardwareCommunicationException ex) {
                    OnControllerFaulted($"Drive {d.Axis}: Hardware communication error - {ex.Message}");
                }
                catch (InvalidOperationException ex) {
                    OnControllerFaulted($"Drive {d.Axis}: Invalid operation - {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 使能所有轴，允许轴接收运动命令。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 此方法并行使能所有轴，每个轴之间有 1ms 的启动间隔。
        /// 如果单个轴使能失败，会记录错误但不会中断其他轴的操作。
        /// 使能后，如果轴没有设置默认速度或默认速度为0，将自动设置为1000mm/s。
        /// </remarks>
        public async Task EnableAllAsync(CancellationToken ct = default) {
            OnControllerFaulted($"[EnableAllAsync] 开始使能所有轴，轴数={_drives.Count}");
            foreach (var drive in _drives) {
                OnControllerFaulted($"[EnableAllAsync] 轴={drive.Axis}, 当前状态={drive.Status}, 使能状态={drive.IsEnabled}");
            }
            
            // 使能所有轴
            await ForEachDriveAsync(d => d.EnableAsync(ct), ct);
            
            // 检查并设置默认速度：如果轴没有默认速度或默认速度等于0，则设置为1000mm/s
            for (int i = 0; i < _drives.Count; i++) {
                var drive = _drives[i];
                var currentSpeed = drive.LastTargetMmps;
                
                if (!currentSpeed.HasValue || currentSpeed.Value == 0) {
                    const decimal defaultSpeed = 1000m; // 1000 mm/s
                    OnControllerFaulted($"[EnableAllAsync] 轴={drive.Axis} 未设置速度或速度为0，设置默认速度={defaultSpeed} mm/s");
                    
                    try {
                        await drive.WriteSpeedAsync(defaultSpeed, ct);
                    }
                    catch (Exception ex) {
                        OnControllerFaulted($"[EnableAllAsync] 轴={drive.Axis} 设置默认速度失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 禁用所有轴，阻止轴接收运动命令。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 此方法并行禁用所有轴，每个轴之间有 1ms 的启动间隔。
        /// 如果单个轴禁用失败，会记录错误但不会中断其他轴的操作。
        /// </remarks>
        public Task DisableAllAsync(CancellationToken ct = default) {
            OnControllerFaulted($"[DisableAllAsync] 开始禁用所有轴，轴数={_drives.Count}");
            foreach (var drive in _drives) {
                OnControllerFaulted($"[DisableAllAsync] 轴={drive.Axis}, 当前状态={drive.Status}, 使能状态={drive.IsEnabled}");
            }
            return ForEachDriveAsync(d => d.DisableAsync(ct).AsTask(), ct);
        }

        /// <summary>
        /// 为所有轴设置加速度和减速度参数。
        /// </summary>
        /// <param name="accelMmPerSec2">加速度，单位：毫米/秒²。</param>
        /// <param name="decelMmPerSec2">减速度，单位：毫米/秒²。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        public Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default) =>
            ForEachDriveAsync(d => d.SetAccelDecelByLinearAsync(accelMmPerSec2, decelMmPerSec2, ct), ct);

        /// <summary>
        /// 设置所有轴的目标速度。
        /// </summary>
        /// <param name="mmPerSec">目标速度，单位：毫米/秒。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 此方法并行设置所有轴的速度，每个轴之间有 1ms 的启动间隔。
        /// </remarks>
        public Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default) {
            OnControllerFaulted($"[WriteSpeedAllAsync] 开始设置所有轴速度={mmPerSec} mm/s，轴数={_drives.Count}");
            foreach (var drive in _drives) {
                OnControllerFaulted($"[WriteSpeedAllAsync] 轴={drive.Axis}, 当前状态={drive.Status}, 使能状态={drive.IsEnabled}");
            }
            return ForEachDriveAsync(d => d.WriteSpeedAsync(mmPerSec, ct), ct);
        }

        /// <summary>
        /// 停止所有轴的运动。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 此方法并行停止所有轴，每个轴之间有 1ms 的启动间隔。
        /// </remarks>
        public Task StopAllAsync(CancellationToken ct = default) {
            OnControllerFaulted($"[StopAllAsync] 开始停止所有轴，轴数={_drives.Count}");
            foreach (var drive in _drives) {
                OnControllerFaulted($"[StopAllAsync] 轴={drive.Axis}, 当前状态={drive.Status}, 使能状态={drive.IsEnabled}");
            }
            return ForEachDriveAsync(d => d.StopAsync(ct).AsTask(), ct);
        }

        /// <summary>
        /// 释放所有轴资源并关闭总线连接。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 此方法会：
        /// 1. 从事件聚合器中分离所有驱动
        /// 2. 释放每个驱动的资源
        /// 3. 清空驱动和速度缓存列表
        /// 4. 关闭总线适配器连接
        /// </remarks>
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

        /// <summary>
        /// 根据速度集配置分别为主轴和出料轴设置速度。
        /// </summary>
        /// <param name="set">包含主轴和出料轴速度配置的速度集对象。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 此方法执行以下操作：
        /// 1. 根据轴的类型（主轴或出料轴）分配相应的速度值
        /// 2. 仅当速度与上次记录的速度不同时才写入，以减少不必要的硬件通信
        /// 3. 遇到错误时记录但不中断其他轴的速度设置
        /// </remarks>
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

            // 根据轴类型分配速度
            var mainIndex = 0;
            var ejectIndex = 0;

            // 仅当速度与上次已知值不同时才写入
            for (var i = 0; i < totalAx; i++) {
                if (ct.IsCancellationRequested) return;
                
                decimal newSpeed = 0m;
                var drive = _drives[i];
                
                // 根据轴类型分配速度
                if (drive.AxisType == Core.Enums.AxisType.Main && mainIndex < main.Count) {
                    newSpeed = (decimal)main[mainIndex];
                    mainIndex++;
                } else if (drive.AxisType == Core.Enums.AxisType.Eject && ejectIndex < eject.Count) {
                    newSpeed = (decimal)eject[ejectIndex];
                    ejectIndex++;
                }
                
                var lastSpeed = _lastSpeeds[i];
                
                // 仅当速度与上次写入的速度不同时才写入
                if (!lastSpeed.HasValue || lastSpeed.Value != newSpeed) {
                    try {
                        await drive.WriteSpeedAsync(newSpeed, ct);
                    }
                    catch (OperationCanceledException) {
                        throw; // Let cancellation propagate
                    }
                    catch (AxisOperationException ex) {
                        OnControllerFaulted($"Failed to write speed for axis {i}: {ex.Message}");
                    }
                    catch (HardwareCommunicationException ex) {
                        OnControllerFaulted($"Failed to write speed for axis {i}: Hardware error - {ex.Message}");
                    }
                    catch (InvalidOperationException ex) {
                        OnControllerFaulted($"Failed to write speed for axis {i}: {ex.Message}");
                    }
                    _lastSpeeds[i] = newSpeed;
                }
            }
        }

        /// <summary>
        /// 重置所有轴的上次记录速度为空值。
        /// </summary>
        /// <remarks>
        /// 调用此方法后，下一次 <see cref="ApplySpeedSetAsync"/> 调用将强制写入所有轴的速度，
        /// 无论速度值是否与之前相同。这在系统重置或重新同步时很有用。
        /// </remarks>
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