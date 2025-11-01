using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Contracts;
using System.Collections.Generic;
using ZakYip.Singulation.Infrastructure.Services;

namespace ZakYip.Singulation.Infrastructure.Cabinet {

    /// <summary>
    /// 安全联动管线：把 IO、驱动健康事件汇聚到安全隔离器，并触发 StopAll。
    /// 支持远程/本地模式切换的自动使能/禁用功能。
    /// </summary>
    public sealed class CabinetPipeline : BackgroundService, ICabinetPipeline {
        /// <summary>日志记录器。</summary>
        private readonly ILogger<CabinetPipeline> _log;
        
        /// <summary>安全隔离器。</summary>
        private readonly ICabinetIsolator _isolator;
        
        /// <summary>安全 IO 模块集合。</summary>
        private readonly IReadOnlyCollection<ICabinetIoModule> _ioModules;
        
        /// <summary>轴控制器。</summary>
        private readonly IAxisController _axisController;
        
        /// <summary>轴事件聚合器。</summary>
        private readonly IAxisEventAggregator _axisEvents;
        
        /// <summary>实时通知器。</summary>
        private readonly IRealtimeNotifier _realtime;
        
        /// <summary>控制器选项存储。</summary>
        private readonly IControllerOptionsStore _controllerOptionsStore;
        
        /// <summary>指示灯服务（可选）。</summary>
        private readonly IndicatorLightService? _indicatorLightService;
        
        /// <summary>安全操作通道。</summary>
        private readonly Channel<CabinetOperation> _operations;

        /// <summary>当前远程/本地模式状态：true=远程模式，false=本地模式。默认为本地模式。</summary>
        private bool _isRemoteMode = false;
        
        /// <summary>模式状态的线程锁。</summary>
        private readonly object _modeLock = new();

        /// <summary>
        /// 初始化 <see cref="CabinetPipeline"/> 类的新实例。
        /// </summary>
        /// <param name="log">日志记录器。</param>
        /// <param name="isolator">安全隔离器。</param>
        /// <param name="ioModules">安全 IO 模块集合。</param>
        /// <param name="axisController">轴控制器。</param>
        /// <param name="axisEvents">轴事件聚合器。</param>
        /// <param name="realtime">实时通知器。</param>
        /// <param name="controllerOptionsStore">控制器选项存储。</param>
        /// <param name="indicatorLightService">指示灯服务（可选）。</param>
        public CabinetPipeline(
            ILogger<CabinetPipeline> log,
            ICabinetIsolator isolator,
            IEnumerable<ICabinetIoModule> ioModules,
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
            _operations = Channel.CreateUnbounded<CabinetOperation>(new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

            _isolator.StateChanged += (_, e) => Enqueue(CabinetOperation.StateChanged(e));

            foreach (var module in _ioModules) {
                module.EmergencyStop += (_, e) => Enqueue(CabinetOperation.Trigger(CabinetCommand.Stop, CabinetTriggerKind.EmergencyStop, e.Description, true));
                module.StopRequested += (_, e) => Enqueue(CabinetOperation.Trigger(CabinetCommand.Stop, CabinetTriggerKind.StopButton, e.Description, true));
                module.StartRequested += (_, e) => Enqueue(CabinetOperation.Trigger(CabinetCommand.Start, CabinetTriggerKind.StartButton, e.Description, true));
                module.ResetRequested += (_, e) => Enqueue(CabinetOperation.Trigger(CabinetCommand.Reset, CabinetTriggerKind.ResetButton, e.Description, true));
                module.RemoteLocalModeChanged += async (_, e) => {
                    bool previousMode;
                    lock (_modeLock) {
                        previousMode = _isRemoteMode;
                        _isRemoteMode = e.IsRemoteMode;
                    }
                    
                    var modeText = e.IsRemoteMode ? "远程模式" : "本地模式";
                    _log.LogInformation("【远程/本地模式切换】从 {PrevMode} 切换到 {NewMode}", 
                        previousMode ? "远程模式" : "本地模式", modeText);
                    
                    // 根据切换方向执行不同操作
                    try {
                        // 切换远程/本地模式前，先将所有轴的速度设置为0
                        _log.LogInformation("【远程/本地模式切换】设置所有轴速度为0");
                        await _axisController.WriteSpeedAllAsync(0m, CancellationToken.None).ConfigureAwait(false);
                        
                        if (!previousMode && e.IsRemoteMode) {
                            // [本地] -> [远程]：自动调用使能，不需要按启动按钮
                            _log.LogInformation("【远程/本地模式切换】检测到切换为远程模式，自动调用使能");
                            await _axisController.EnableAllAsync(CancellationToken.None).ConfigureAwait(false);
                            _log.LogInformation("【远程/本地模式切换】自动使能完成，等待远程速度推送");
                        } else if (previousMode && !e.IsRemoteMode) {
                            // [远程] -> [本地]：调用禁用使能
                            _log.LogInformation("【远程/本地模式切换】检测到切换为本地模式，调用禁用使能");
                            await _axisController.DisableAllAsync(CancellationToken.None).ConfigureAwait(false);
                            _log.LogInformation("【远程/本地模式切换】禁用使能完成");
                        }
                    } catch (Exception ex) {
                        _log.LogError(ex, "【远程/本地模式切换】执行自动操作失败");
                    }
                };
            }

            _axisEvents.AxisFaulted += (_, e) => Enqueue(CabinetOperation.AxisHealth(CabinetTriggerKind.AxisFault, e.Axis.ToString(), e.Exception?.Message));
            _axisEvents.AxisDisconnected += (_, e) => Enqueue(CabinetOperation.AxisHealth(CabinetTriggerKind.AxisDisconnected, e.Axis.ToString(), e.Reason));
            _axisEvents.DriverNotLoaded += (_, e) => Enqueue(CabinetOperation.AxisHealth(CabinetTriggerKind.AxisFault, e.LibraryName, e.Message));
        }

        /// <summary>
        /// 当安全隔离状态发生变化时触发的事件。
        /// </summary>
        public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;
        
        /// <summary>
        /// 当收到启动请求时触发的事件。
        /// </summary>
        public event EventHandler<CabinetTriggerEventArgs>? StartRequested;
        
        /// <summary>
        /// 当收到停止请求时触发的事件。
        /// </summary>
        /// <summary>
        /// 当收到停止请求时触发的事件。
        /// </summary>
        public event EventHandler<CabinetTriggerEventArgs>? StopRequested;
        
        /// <summary>
        /// 当收到复位请求时触发的事件。
        /// </summary>
        public event EventHandler<CabinetTriggerEventArgs>? ResetRequested;

        /// <summary>
        /// 获取当前安全隔离状态。
        /// </summary>
        public CabinetIsolationState State => _isolator.State;

        /// <summary>
        /// 尝试进入隔离状态。
        /// </summary>
        /// <param name="kind">触发类型。</param>
        /// <param name="reason">触发原因。</param>
        /// <returns>是否成功进入隔离状态。</returns>
        public bool TryTrip(CabinetTriggerKind kind, string reason) => _isolator.TryTrip(kind, reason);

        /// <summary>
        /// 尝试进入降级状态。
        /// </summary>
        /// <param name="kind">触发类型。</param>
        /// <param name="reason">触发原因。</param>
        /// <returns>是否成功进入降级状态。</returns>
        public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) => _isolator.TryEnterDegraded(kind, reason);

        /// <summary>
        /// 尝试从降级状态恢复。
        /// </summary>
        /// <param name="reason">恢复原因。</param>
        /// <returns>是否成功恢复。</returns>
        public bool TryRecoverFromDegraded(string reason) => _isolator.TryRecoverFromDegraded(reason);

        /// <summary>
        /// 尝试重置隔离状态。
        /// </summary>
        /// <param name="reason">重置原因。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>是否成功重置。</returns>
        public bool TryResetIsolation(string reason, CancellationToken ct = default) => _isolator.TryResetIsolation(reason, ct);

        /// <summary>
        /// 请求启动操作。
        /// </summary>
        /// <param name="kind">触发类型。</param>
        /// <param name="reason">触发原因（可选）。</param>
        /// <param name="triggeredByIo">是否由 IO 触发（默认为 false）。</param>
        public void RequestStart(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false)
            => Enqueue(CabinetOperation.Trigger(CabinetCommand.Start, kind, reason, triggeredByIo));

        /// <summary>
        /// 请求停止操作。
        /// </summary>
        /// <param name="kind">触发类型。</param>
        /// <param name="reason">触发原因（可选）。</param>
        /// <param name="triggeredByIo">是否由 IO 触发（默认为 false）。</param>
        public void RequestStop(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false)
            => Enqueue(CabinetOperation.Trigger(CabinetCommand.Stop, kind, reason, triggeredByIo));

        /// <summary>
        /// 请求复位操作。
        /// </summary>
        /// <param name="kind">触发类型。</param>
        /// <param name="reason">触发原因（可选）。</param>
        /// <param name="triggeredByIo">是否由 IO 触发（默认为 false）。</param>
        public void RequestReset(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false)
            => Enqueue(CabinetOperation.Trigger(CabinetCommand.Reset, kind, reason, triggeredByIo));

        /// <summary>
        /// 执行安全管线的后台任务。
        /// </summary>
        /// <param name="stoppingToken">停止令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // 等待轴控制器完全初始化完成后再启动 IO 模块监控
            // 这样可以避免在初始化过程中物理按钮（特别是复位按钮）导致的重复复位和崩溃问题
            _log.LogInformation("【CabinetPipeline】等待轴控制器初始化完成...");
            
            // 轮询检查控制器是否初始化完成，最长等待60秒
            var maxWaitTime = TimeSpan.FromSeconds(60);
            var pollInterval = TimeSpan.FromMilliseconds(500);
            using var timeoutCts = new CancellationTokenSource(maxWaitTime);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);
            
            while (!linkedCts.Token.IsCancellationRequested) {
                if (_axisController.Bus.IsInitialized) {
                    _log.LogInformation("【CabinetPipeline】轴控制器已初始化，开始启动 IO 模块监控");
                    break;
                }
                
                try {
                    await Task.Delay(pollInterval, linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    // Expected during shutdown
                    break;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested) {
                    _log.LogWarning("【CabinetPipeline】等待轴控制器初始化超时（{Timeout}秒），继续启动 IO 模块", maxWaitTime.TotalSeconds);
                    break;
                }
            }
            
            if (stoppingToken.IsCancellationRequested) {
                _log.LogInformation("【CabinetPipeline】应用程序正在关闭，取消启动");
                return;
            }
            
            // 启动所有 IO 模块（包括物理按钮监控）
            var startTasks = _ioModules
                .Select(module => module.StartAsync(stoppingToken))
                .ToArray();
            await Task.WhenAll(startTasks).ConfigureAwait(false);
            
            _log.LogInformation("【CabinetPipeline】所有 IO 模块已启动，开始处理安全操作");

            var reader = _operations.Reader;
            while (!stoppingToken.IsCancellationRequested) {
                CabinetOperation currentOp = default!;
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

        /// <summary>
        /// 将安全操作加入队列。
        /// </summary>
        /// <param name="op">安全操作。</param>
        private void Enqueue(CabinetOperation op) {
            var ok = _operations.Writer.TryWrite(op);
            if (!ok) _log.LogWarning("安全管线繁忙，已丢弃操作 {Operation}", op.Kind);
        }

        /// <summary>
        /// 处理安全操作。
        /// </summary>
        /// <param name="op">安全操作。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        private async Task HandleOperationAsync(CabinetOperation op, CancellationToken ct) {
            switch (op.Kind) {
                case CabinetOperationKind.StateChanged:
                    await HandleStateChangedAsync(op.StateArgs!, ct).ConfigureAwait(false);
                    break;
                case CabinetOperationKind.Command:
                    await HandleCommandAsync(op, ct).ConfigureAwait(false);
                    break;
                case CabinetOperationKind.AxisHealth:
                    HandleAxisHealth(op.AxisKind, op.AxisName, op.AxisReason);
                    break;
                default:
                    _log.LogWarning("未处理的安全操作类型 {Kind}", op.Kind);
                    break;
            }
        }

        /// <summary>
        /// 处理安全状态变化事件。
        /// </summary>
        /// <param name="ev">安全状态变化事件参数。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        private async Task HandleStateChangedAsync(CabinetStateChangedEventArgs ev, CancellationToken ct) {
            StateChanged?.Invoke(this, ev);
            switch (ev.Current) {
                case CabinetIsolationState.Isolated:
                    await StopAllAsync("safety-isolated", ev.ReasonText, ct).ConfigureAwait(false);
                    break;
                case CabinetIsolationState.Degraded:
                    await StopAllAsync("safety-degraded", ev.ReasonText, ct).ConfigureAwait(false);
                    break;
                case CabinetIsolationState.Normal:
                    _log.LogInformation("安全状态已恢复：{Reason}", ev.ReasonText);
                    break;
            }
        }

        /// <summary>
        /// 处理安全命令。
        /// </summary>
        /// <param name="operation">安全操作。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        private async Task HandleCommandAsync(CabinetOperation operation, CancellationToken ct) {
            var args = new CabinetTriggerEventArgs {
                Kind = operation.CommandKind,
                Description = operation.CommandReason
            };
            
            // 记录安全命令调用来源
            var source = operation.TriggeredByIo ? "IO端点" : "API端点";
            _log.LogInformation("安全命令调用 - 命令：{Command}，来源：{Source}，触发类型：{Kind}，原因：{Reason}",
                operation.Command, source, operation.CommandKind, operation.CommandReason);
            
            switch (operation.Command) {
                case CabinetCommand.Start:
                    // 检测到启动IO变化时->检测当前状态是否运行中,如果是运行中或者报警则不做任何操作
                    if (_indicatorLightService != null) {
                        var currentState = _indicatorLightService.CurrentState;
                        if (currentState == SystemState.Running || currentState == SystemState.Alarm) {
                            _log.LogWarning(currentState == SystemState.Running
                                ? "忽略启动请求：系统已处于运行中状态"
                                : "忽略启动请求：系统处于报警状态，请先复位");
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
                            _log.LogInformation("【启动流程】步骤2：远程模式 - 设置初始速度为0，等待 Upstream 推送速度参数");
                            // 远程模式：先设置速度为0（安全起见），然后等待 Upstream 推送实际速度
                            await _axisController.WriteSpeedAllAsync(0m, ct).ConfigureAwait(false);
                            _log.LogInformation("【启动流程】步骤2完成：初始速度已设置为0，等待远程速度推送");
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
                case CabinetCommand.Stop:
                    // 检测到停止IO变化时->检测当前状态是否已停止/准备中,如果是则不做任何操作
                    if (_indicatorLightService != null) {
                        var currentState = _indicatorLightService.CurrentState;
                        bool isSystemInStoppedOrReadyState = currentState == SystemState.Stopped || currentState == SystemState.Ready;
                        if (isSystemInStoppedOrReadyState) {
                            _log.LogInformation("忽略停止请求：系统已处于停止/准备状态");
                            return;
                        }
                    }
                    
                    StopRequested?.Invoke(this, args);
                    _log.LogInformation("【停止流程开始】触发类型：{Kind}", operation.CommandKind);
                    
                    // 等同于调用 /api/Axes/axes/speed (设置0速度) + /api/Axes/axes/disable
                    if (operation.CommandKind == CabinetTriggerKind.EmergencyStop) {
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
                case CabinetCommand.Reset:
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
                        _log.LogInformation("【复位流程】步骤4：系统处于隔离状态，调用 ICabinetIsolator.TryResetIsolation(reason: {Reason}) 尝试恢复", operation.CommandReason ?? "reset");
                        var reset = _isolator.TryResetIsolation(operation.CommandReason ?? "reset", ct);
                        _log.LogInformation("【复位流程】隔离复位结果={Result}", reset);
                    }
                    else if (_isolator.IsDegraded) {
                        _log.LogInformation("【复位流程】步骤4：系统处于降级状态，调用 ICabinetIsolator.TryRecoverFromDegraded(reason: {Reason}) 尝试恢复", operation.CommandReason ?? "reset");
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

        /// <summary>
        /// 处理轴健康状态事件。
        /// </summary>
        /// <param name="kind">触发类型。</param>
        /// <param name="axisName">轴名称（可选）。</param>
        /// <param name="reason">原因（可选）。</param>
        private void HandleAxisHealth(CabinetTriggerKind kind, string? axisName, string? reason) {
            var text = string.IsNullOrWhiteSpace(reason) ? "轴状态异常" : reason;
            switch (kind) {
                case CabinetTriggerKind.AxisFault:
                    if (_isolator.TryEnterDegraded(kind, text)) {
                        _log.LogWarning("检测到轴故障（{Axis}）：{Reason}", axisName, text);
                        SingulationMetrics.Instance.AxisFaultCounter.Add(1);
                    }
                    break;
                case CabinetTriggerKind.AxisDisconnected:
                    if (_isolator.TryTrip(kind, text)) {
                        _log.LogError("检测到轴掉线（{Axis}）：{Reason}", axisName, text);
                    }
                    break;
                default:
                    _log.LogWarning("未处理的轴安全类型 {Kind}", kind);
                    break;
            }
        }

        /// <summary>
        /// 执行紧急停机操作。
        /// </summary>
        /// <param name="source">停机来源。</param>
        /// <param name="reason">停机原因（可选）。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
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
