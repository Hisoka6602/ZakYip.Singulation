using System;
using System.Collections.Generic;
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
using ZakYip.Singulation.Host.Runtime;

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
        private readonly IApplicationRestarter _restarter;
        private readonly Channel<SafetyOperation> _operations;

        public SafetyPipeline(
            ILogger<SafetyPipeline> log,
            ISafetyIsolator isolator,
            IEnumerable<ISafetyIoModule> ioModules,
            IAxisController axisController,
            IAxisEventAggregator axisEvents,
            IRealtimeNotifier realtime,
            IApplicationRestarter restarter) {
            _log = log;
            _isolator = isolator;
            _ioModules = ioModules.ToArray();
            _axisController = axisController;
            _axisEvents = axisEvents;
            _realtime = realtime;
            _restarter = restarter;
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
            switch (operation.Command) {
                case SafetyCommand.Start:
                    if (_isolator.IsIsolated) {
                        _log.LogWarning("忽略启动请求：系统处于隔离状态");
                        return;
                    }
                    StartRequested?.Invoke(this, args);
                    await _realtime.PublishDeviceAsync(new {
                        kind = "safety.start", reason = operation.CommandReason
                    }, ct).ConfigureAwait(false);
                    if (operation.TriggeredByIo) {
                        await _restarter.RestartAsync($"IO 启动信号({operation.CommandKind})", ct).ConfigureAwait(false);
                    }
                    break;
                case SafetyCommand.Stop:
                    StopRequested?.Invoke(this, args);
                    if (operation.CommandKind == SafetyTriggerKind.EmergencyStop) {
                        await StopAllAsync("emergency-stop", operation.CommandReason, ct).ConfigureAwait(false);
                    }
                    _ = _isolator.TryEnterDegraded(operation.CommandKind, operation.CommandReason ?? "stop");
                    break;
                case SafetyCommand.Reset:
                    ResetRequested?.Invoke(this, args);
                    if (_isolator.IsIsolated) {
                        var reset = _isolator.TryResetIsolation(operation.CommandReason ?? "reset", ct);
                        _log.LogInformation("隔离复位结果={Result}", reset);
                    }
                    else if (_isolator.IsDegraded) {
                        var ok = _isolator.TryRecoverFromDegraded(operation.CommandReason ?? "reset");
                        _log.LogInformation("降级恢复结果={Result}", ok);
                    }
                    if (operation.TriggeredByIo) {
                        await _restarter.RestartAsync($"IO 复位信号({operation.CommandKind})", ct).ConfigureAwait(false);
                    }
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
