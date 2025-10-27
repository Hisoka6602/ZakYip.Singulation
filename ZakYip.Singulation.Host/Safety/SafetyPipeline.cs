using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Contracts;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Services;

namespace ZakYip.Singulation.Host.Safety {

    /// <summary>
    /// 安全联动管线：把 IO、驱动健康事件汇聚到安全隔离器，并触发 StopAll。
    /// </summary>
    public sealed class SafetyPipeline : BackgroundService, ISafetyPipeline {
        private readonly ILogger<SafetyPipeline> _log;
        private readonly ISafetyIsolator _isolator;
        private readonly IReadOnlyCollection<ISafetyIoModule> _ioModules;
        private readonly IAxisController _axisController;
        private readonly IAxisEventAggregator _axisEvents;
        private readonly IRealtimeNotifier _realtime;
        private readonly IControllerOptionsStore _controllerOptionsStore;
        private readonly IndicatorLightService? _indicatorLightService;
        private readonly Channel<SafetyOperation> _operations;

        // 当前远程/本地模式状态：true=远程模式，false=本地模式
        private bool _isRemoteMode = true;
        private readonly object _modeLock = new();

        public SafetyPipeline(
            ILogger<SafetyPipeline> log,
            ISafetyIsolator isolator,
            IEnumerable<ISafetyIoModule> ioModules,
            IAxisController axisController,
            IAxisEventAggregator axisEvents,
            IRealtimeNotifier realtime,
            IControllerOptionsStore controllerOptionsStore,
            IndicatorLightService? indicatorLightService = null) {
            _log = log;
            _isolator = isolator;
            _ioModules = ioModules.ToArray();
            _axisController = axisController;
            _axisEvents = axisEvents;
            _realtime = realtime;
            _controllerOptionsStore = controllerOptionsStore;
            _indicatorLightService = indicatorLightService;
            _operations = Channel.CreateUnbounded<SafetyOperation>(new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

            _isolator.StateChanged += (_, e) => Enqueue(SafetyOperation.StateChanged(e));

            foreach (var module in _ioModules) {
                module.EmergencyStop += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Stop, SafetyTriggerKind.EmergencyStop, e.Description, true));
                module.StopRequested += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Stop, SafetyTriggerKind.StopButton, e.Description, true));
                module.StartRequested += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Start, SafetyTriggerKind.StartButton, e.Description, true));
                module.ResetRequested += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Reset, SafetyTriggerKind.ResetButton, e.Description, true));
                module.RemoteLocalModeChanged += (_, e) => {
                    lock (_modeLock) {
                        _isRemoteMode = e.IsRemoteMode;
                    }
                    _log.LogInformation("远程/本地模式已切换：{Mode}", e.IsRemoteMode ? "远程模式" : "本地模式");
                };
            }

            _axisEvents.AxisFaulted += (_, e) => Enqueue(SafetyOperation.AxisHealth(SafetyTriggerKind.AxisFault, e.Axis.ToString(), e.Exception?.Message));
            _axisEvents.AxisDisconnected += (_, e) => Enqueue(SafetyOperation.AxisHealth(SafetyTriggerKind.AxisDisconnected, e.Axis.ToString(), e.Reason));
            _axisEvents.DriverNotLoaded += (_, e) => Enqueue(SafetyOperation.AxisHealth(SafetyTriggerKind.AxisFault, e.LibraryName, e.Message));
        }

        public event EventHandler<SafetyStateChangedEventArgs>? StateChanged;
        public event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        public event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        public event EventHandler<SafetyTriggerEventArgs>? ResetRequested;

        public SafetyIsolationState State => _isolator.State;

        public bool TryTrip(SafetyTriggerKind kind, string reason) => _isolator.TryTrip(kind, reason);

        public bool TryEnterDegraded(SafetyTriggerKind kind, string reason) => _isolator.TryEnterDegraded(kind, reason);

        public bool TryRecoverFromDegraded(string reason) => _isolator.TryRecoverFromDegraded(reason);

        public bool TryResetIsolation(string reason, CancellationToken ct = default) => _isolator.TryResetIsolation(reason, ct);

        public void RequestStart(SafetyTriggerKind kind, string? reason = null, bool triggeredByIo = false)
            => Enqueue(SafetyOperation.Trigger(SafetyCommand.Start, kind, reason, triggeredByIo));

        public void RequestStop(SafetyTriggerKind kind, string? reason = null, bool triggeredByIo = false)
            => Enqueue(SafetyOperation.Trigger(SafetyCommand.Stop, kind, reason, triggeredByIo));

        public void RequestReset(SafetyTriggerKind kind, string? reason = null, bool triggeredByIo = false)
            => Enqueue(SafetyOperation.Trigger(SafetyCommand.Reset, kind, reason, triggeredByIo));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var startTasks = _ioModules
                .Select(module => module.StartAsync(stoppingToken))
                .ToArray();
            await Task.WhenAll(startTasks).ConfigureAwait(false);

            var reader = _operations.Reader;
            while (!stoppingToken.IsCancellationRequested) {
                SafetyOperation currentOp = default!;
                try {
                    currentOp = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                    await HandleOperationAsync(currentOp, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    break;
                }
                catch (ChannelClosedException) {
                    break;
                }
                catch (Exception ex) {
                    _log.LogError(ex, "处理安全操作 {Operation} 时发生异常", currentOp);
                }
            }
        }

        private void Enqueue(SafetyOperation op) {
            var ok = _operations.Writer.TryWrite(op);
            if (!ok) _log.LogWarning("安全管线繁忙，已丢弃操作 {Operation}", op.Kind);
        }

        private async Task HandleOperationAsync(SafetyOperation op, CancellationToken ct) {
            switch (op.Kind) {
                case SafetyOperationKind.StateChanged:
                    await HandleStateChangedAsync(op.StateArgs!, ct).ConfigureAwait(false);
                    break;
                case SafetyOperationKind.Command:
                    await HandleCommandAsync(op, ct).ConfigureAwait(false);
                    break;
                case SafetyOperationKind.AxisHealth:
                    HandleAxisHealth(op.AxisKind, op.AxisName, op.AxisReason);
                    break;
                default:
                    _log.LogWarning("未处理的安全操作类型 {Kind}", op.Kind);
                    break;
            }
        }

        private async Task HandleStateChangedAsync(SafetyStateChangedEventArgs ev, CancellationToken ct) {
            StateChanged?.Invoke(this, ev);
            switch (ev.Current) {
                case SafetyIsolationState.Isolated:
                    await StopAllAsync("safety-isolated", ev.ReasonText, ct).ConfigureAwait(false);
                    break;
                case SafetyIsolationState.Degraded:
                    await StopAllAsync("safety-degraded", ev.ReasonText, ct).ConfigureAwait(false);
                    break;
                case SafetyIsolationState.Normal:
                    _log.LogInformation("安全状态已恢复：{Reason}", ev.ReasonText);
                    break;
            }
        }

        private async Task HandleCommandAsync(SafetyOperation operation, CancellationToken ct) {
            var args = new SafetyTriggerEventArgs(operation.CommandKind, operation.CommandReason);
            
            // 记录安全命令调用来源
            var source = operation.TriggeredByIo ? "IO端点" : "API端点";
            _log.LogInformation("安全命令调用 - 命令：{Command}，来源：{Source}，触发类型：{Kind}，原因：{Reason}",
                operation.Command, source, operation.CommandKind, operation.CommandReason);
            
            switch (operation.Command) {
                case SafetyCommand.Start:
                    // 检测到启动IO变化时->检测当前状态是否运行中,如果是运行中或者报警则不做任何操作
                    if (_indicatorLightService != null) {
                        var currentState = _indicatorLightService.CurrentState;
                        if (currentState == SystemState.Running) {
                            _log.LogWarning("忽略启动请求：系统已处于运行中状态");
                            return;
                        }
                        if (currentState == SystemState.Alarm) {
                            _log.LogWarning("忽略启动请求：系统处于报警状态，请先复位");
                            return;
                        }
                    }
                    
                    if (_isolator.IsIsolated) {
                        _log.LogWarning("忽略启动请求：系统处于隔离状态");
                        return;
                    }
                    StartRequested?.Invoke(this, args);
                    
                    // 启动流程：1) 使能所有轴 2) 根据远程/本地模式设置速度
                    // 等同于调用 /api/Axes/axes/enable + /api/Axes/axes/speed (使用默认的localFixedSpeedMmps)
                    try {
                        _log.LogInformation("【启动流程开始】步骤1：调用 IAxisController.EnableAllAsync() 使能所有轴");
                        await _axisController.EnableAllAsync(ct).ConfigureAwait(false);
                        _log.LogInformation("【启动流程】步骤1完成：所有轴已使能");
                        
                        bool isRemote;
                        lock (_modeLock) {
                            isRemote = _isRemoteMode;
                        }
                        
                        if (isRemote) {
                            _log.LogInformation("【启动流程】步骤2：远程模式 - 等待 Upstream 推送速度参数");
                            // 远程模式：速度由 Upstream 控制，不在这里设置
                        } else {
                            _log.LogInformation("【启动流程】步骤2：本地模式 - 设置固定速度");
                            // 本地模式：从配置中读取固定速度并设置
                            var controllerOpts = await _controllerOptionsStore.GetAsync(ct).ConfigureAwait(false);
                            var fixedSpeed = controllerOpts.LocalFixedSpeedMmps;
                            _log.LogInformation("【启动流程】调用 IAxisController.WriteSpeedAllAsync(speed: {Speed} mm/s) 设置本地固定速度", fixedSpeed);
                            await _axisController.WriteSpeedAllAsync(fixedSpeed, ct).ConfigureAwait(false);
                            _log.LogInformation("【启动流程】步骤2完成：速度已设置为 {Speed} mm/s", fixedSpeed);
                        }
                        
                        // 更新系统状态为运行中
                        if (_indicatorLightService != null) {
                            _log.LogInformation("【启动流程】步骤3：调用 IndicatorLightService.UpdateStateAsync(state: Running) 更新指示灯状态");
                            await _indicatorLightService.UpdateStateAsync(SystemState.Running, ct).ConfigureAwait(false);
                            _log.LogInformation("【启动流程】步骤3完成：系统状态已更新为运行中");
                        }
                        
                        _log.LogInformation("【启动流程完成】所有步骤执行成功");
                    } catch (Exception ex) {
                        _log.LogError(ex, "【启动流程失败】执行过程中发生异常");
                    }
                    
                    await _realtime.PublishDeviceAsync(new {
                        kind = "safety.start", reason = operation.CommandReason
                    }, ct).ConfigureAwait(false);
                    break;
                case SafetyCommand.Stop:
                    // 检测到停止IO变化时->检测当前状态是否已停止/准备中,如果是则不做任何操作
                    if (_indicatorLightService != null) {
                        var currentState = _indicatorLightService.CurrentState;
                        if (currentState == SystemState.Stopped || currentState == SystemState.Ready) {
                            _log.LogInformation("忽略停止请求：系统已处于停止/准备状态");
                            return;
                        }
                    }
                    
                    StopRequested?.Invoke(this, args);
                    _log.LogInformation("【停止流程开始】触发类型：{Kind}", operation.CommandKind);
                    
                    // 等同于调用 /api/Axes/axes/speed (设置0速度) + /api/Axes/axes/disable
                    if (operation.CommandKind == SafetyTriggerKind.EmergencyStop) {
                        _log.LogInformation("【停止流程】急停模式：调用 StopAllAsync() 紧急停机");
                        await StopAllAsync("emergency-stop", operation.CommandReason, ct).ConfigureAwait(false);
                        // 更新系统状态为报警
                        if (_indicatorLightService != null) {
                            _log.LogInformation("【停止流程】调用 IndicatorLightService.UpdateStateAsync(state: Alarm) 更新指示灯为报警状态");
                            await _indicatorLightService.UpdateStateAsync(SystemState.Alarm, ct).ConfigureAwait(false);
                        }
                    } else {
                        // 正常停止流程
                        try {
                            _log.LogInformation("【停止流程】步骤1：设置所有轴速度为0");
                            await _axisController.WriteSpeedAllAsync(0m, ct).ConfigureAwait(false);
                            _log.LogInformation("【停止流程】步骤2：禁用所有轴使能");
                            await _axisController.DisableAllAsync(ct).ConfigureAwait(false);
                            _log.LogInformation("【停止流程】步骤完成：所有轴已停止并禁用");
                        } catch (Exception ex) {
                            _log.LogError(ex, "【停止流程】执行失败");
                        }
                        
                        // 更新系统状态为已停止
                        if (_indicatorLightService != null) {
                            _log.LogInformation("【停止流程】调用 IndicatorLightService.UpdateStateAsync(state: Stopped) 更新指示灯为停止状态");
                            await _indicatorLightService.UpdateStateAsync(SystemState.Stopped, ct).ConfigureAwait(false);
                        }
                    }
                    var degraded = _isolator.TryEnterDegraded(operation.CommandKind, operation.CommandReason ?? "stop");
                    _log.LogInformation("【停止流程完成】系统进入降级状态：{Result}", degraded);
                    break;
                case SafetyCommand.Reset:
                    ResetRequested?.Invoke(this, args);
                    
                    // 复位流程：等同于调用 /api/Axes/axes/speed (设置0速度) + /api/Axes/axes/disable + /api/system/session
                    try {
                        _log.LogInformation("【复位流程开始】步骤1：设置所有轴速度为0");
                        await _axisController.WriteSpeedAllAsync(0m, ct).ConfigureAwait(false);
                        
                        _log.LogInformation("【复位流程】步骤2：禁用所有轴使能");
                        await _axisController.DisableAllAsync(ct).ConfigureAwait(false);
                        
                        _log.LogInformation("【复位流程】步骤3：调用 IAxisController.Bus.ResetAsync() 清除控制器错误");
                        await _axisController.Bus.ResetAsync(ct).ConfigureAwait(false);
                        _log.LogInformation("【复位流程】步骤3完成：控制器错误已清除");
                    } catch (Exception ex) {
                        _log.LogError(ex, "【复位流程】步骤1-3失败");
                    }
                    
                    if (_isolator.IsIsolated) {
                        _log.LogInformation("【复位流程】步骤4：系统处于隔离状态，调用 ISafetyIsolator.TryResetIsolation(reason: {Reason}) 尝试恢复", operation.CommandReason ?? "reset");
                        var reset = _isolator.TryResetIsolation(operation.CommandReason ?? "reset", ct);
                        _log.LogInformation("【复位流程】隔离复位结果={Result}", reset);
                    }
                    else if (_isolator.IsDegraded) {
                        _log.LogInformation("【复位流程】步骤4：系统处于降级状态，调用 ISafetyIsolator.TryRecoverFromDegraded(reason: {Reason}) 尝试恢复", operation.CommandReason ?? "reset");
                        var ok = _isolator.TryRecoverFromDegraded(operation.CommandReason ?? "reset");
                        _log.LogInformation("【复位流程】降级恢复结果={Result}", ok);
                    }
                    
                    // 更新系统状态为准备中
                    if (_indicatorLightService != null) {
                        _log.LogInformation("【复位流程】步骤5：调用 IndicatorLightService.UpdateStateAsync(state: Ready) 更新指示灯为准备状态");
                        await _indicatorLightService.UpdateStateAsync(SystemState.Ready, ct).ConfigureAwait(false);
                        _log.LogInformation("【复位流程】步骤5完成：系统状态已更新为准备中");
                    }
                    _log.LogInformation("【复位流程完成】所有步骤执行成功");
                    break;
            }
        }

        private void HandleAxisHealth(SafetyTriggerKind kind, string? axisName, string? reason) {
            var text = string.IsNullOrWhiteSpace(reason) ? "轴状态异常" : reason;
            switch (kind) {
                case SafetyTriggerKind.AxisFault:
                    if (_isolator.TryEnterDegraded(kind, text)) {
                        _log.LogWarning("检测到轴故障（{Axis}）：{Reason}", axisName, text);
                        SingulationMetrics.Instance.AxisFaultCounter.Add(1);
                    }
                    break;
                case SafetyTriggerKind.AxisDisconnected:
                    if (_isolator.TryTrip(kind, text)) {
                        _log.LogError("检测到轴掉线（{Axis}）：{Reason}", axisName, text);
                    }
                    break;
                default:
                    _log.LogWarning("未处理的轴安全类型 {Kind}", kind);
                    break;
            }
        }

        private async Task StopAllAsync(string source, string? reason, CancellationToken ct) {
            try {
                var text = string.IsNullOrWhiteSpace(reason) ? "未知原因" : reason;
                _log.LogWarning("【{Source}】执行紧急停机，原因：{Reason}", source, text);
                await _axisController.WriteSpeedAllAsync(0m, ct).ConfigureAwait(false);
                await _axisController.StopAllAsync(ct).ConfigureAwait(false);
                await _axisController.DisableAllAsync(ct).ConfigureAwait(false);
                await _realtime.PublishDeviceAsync(new {
                    kind = "safety.stopall", source, reason = text
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _log.LogError(ex, "执行紧急停机失败");
            }
        }

    }
}
