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
        private readonly Channel<SafetyOperation> _operations;

        public SafetyPipeline(
            ILogger<SafetyPipeline> log,
            ISafetyIsolator isolator,
            IEnumerable<ISafetyIoModule> ioModules,
            IAxisController axisController,
            IAxisEventAggregator axisEvents,
            IRealtimeNotifier realtime) {
            _log = log;
            _isolator = isolator;
            _ioModules = ioModules.ToArray();
            _axisController = axisController;
            _axisEvents = axisEvents;
            _realtime = realtime;
            _operations = Channel.CreateUnbounded<SafetyOperation>(new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

            _isolator.StateChanged += (_, e) => Enqueue(SafetyOperation.StateChanged(e));

            foreach (var module in _ioModules) {
                module.EmergencyStop += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Stop, SafetyTriggerKind.EmergencyStop, e.Description));
                module.StopRequested += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Stop, SafetyTriggerKind.StopButton, e.Description));
                module.StartRequested += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Start, SafetyTriggerKind.StartButton, e.Description));
                module.ResetRequested += (_, e) => Enqueue(SafetyOperation.Trigger(SafetyCommand.Reset, SafetyTriggerKind.ResetButton, e.Description));
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
                    _log.LogError(ex, "Safety pipeline error when processing {Operation}", currentOp);
                }
            }
        }

        private void Enqueue(SafetyOperation op) {
            var ok = _operations.Writer.TryWrite(op);
            if (!ok) _log.LogWarning("Safety pipeline busy, dropping operation {Operation}", op.Kind);
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
                    _log.LogWarning("Unhandled safety operation {Kind}", op.Kind);
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
                    _log.LogInformation("Safety pipeline recovered: {Reason}", ev.ReasonText);
                    break;
            }
        }

        private async Task HandleCommandAsync(SafetyOperation operation, CancellationToken ct) {
            var args = new SafetyTriggerEventArgs(operation.CommandKind, operation.CommandReason);
            switch (operation.Command) {
                case SafetyCommand.Start:
                    if (_isolator.IsIsolated) {
                        _log.LogWarning("Start ignored: system isolated");
                        return;
                    }
                    StartRequested?.Invoke(this, args);
                    await _realtime.PublishDeviceAsync(new {
                        kind = "safety.start", reason = operation.CommandReason
                    }, ct).ConfigureAwait(false);
                    break;
                case SafetyCommand.Stop:
                    StopRequested?.Invoke(this, args);
                    _ = _isolator.TryEnterDegraded(operation.CommandKind, operation.CommandReason ?? "stop");
                    break;
                case SafetyCommand.Reset:
                    ResetRequested?.Invoke(this, args);
                    if (_isolator.IsIsolated) {
                        var reset = _isolator.TryResetIsolation(operation.CommandReason ?? "reset", ct);
                        _log.LogInformation("Safety reset result={Result}", reset);
                    }
                    else if (_isolator.IsDegraded) {
                        var ok = _isolator.TryRecoverFromDegraded(operation.CommandReason ?? "reset");
                        _log.LogInformation("Safety degrade recovery result={Result}", ok);
                    }
                    break;
            }
        }

        private void HandleAxisHealth(SafetyTriggerKind kind, string? axisName, string? reason) {
            var text = string.IsNullOrWhiteSpace(reason) ? "axis health" : reason;
            switch (kind) {
                case SafetyTriggerKind.AxisFault:
                    if (_isolator.TryEnterDegraded(kind, text)) {
                        _log.LogWarning("Axis fault detected ({Axis}): {Reason}", axisName, text);
                        SingulationMetrics.Instance.AxisFaultCounter.Add(1);
                    }
                    break;
                case SafetyTriggerKind.AxisDisconnected:
                    if (_isolator.TryTrip(kind, text)) {
                        _log.LogError("Axis disconnected ({Axis}): {Reason}", axisName, text);
                    }
                    break;
                default:
                    _log.LogWarning("Unhandled axis safety kind {Kind}", kind);
                    break;
            }
        }

        private async Task StopAllAsync(string source, string? reason, CancellationToken ct) {
            try {
                _log.LogWarning("[{Source}] StopAll due to {Reason}", source, reason);
                await _axisController.StopAllAsync(ct).ConfigureAwait(false);
                await _realtime.PublishDeviceAsync(new {
                    kind = "safety.stopall", source, reason
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _log.LogError(ex, "Safety StopAll failed");
            }
        }

    }
}
