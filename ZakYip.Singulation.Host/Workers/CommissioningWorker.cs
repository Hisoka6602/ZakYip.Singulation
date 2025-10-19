using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Infrastructure.Telemetry;

namespace ZakYip.Singulation.Host.Workers {

    public sealed class CommissioningWorker : BackgroundService {
        private readonly ILogger<CommissioningWorker> _log;
        private readonly ISafetyPipeline _safety;
        private readonly ICommissioningSequence _sequence;
        private readonly Channel<CommissioningCommand> _queue;

        private CommissioningState _state = CommissioningState.Idle;
        private CancellationTokenSource? _runCts;

        public CommissioningWorker(
            ILogger<CommissioningWorker> log,
            ISafetyPipeline safety,
            ICommissioningSequence sequence) {
            _log = log;
            _safety = safety;
            _sequence = sequence;
            _queue = Channel.CreateUnbounded<CommissioningCommand>(new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = false
            });

            _safety.StartRequested += (_, e) => Enqueue(new CommissioningCommand(CommissioningCommandKind.Start, e.Description));
            _safety.StopRequested += (_, e) => Enqueue(new CommissioningCommand(CommissioningCommandKind.Stop, e.Description));
            _safety.ResetRequested += (_, e) => Enqueue(new CommissioningCommand(CommissioningCommandKind.Reset, e.Description));
            _safety.StateChanged += (_, e) => Enqueue(new CommissioningCommand(CommissioningCommandKind.SafetyStateChanged, e));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var reader = _queue.Reader;
            while (!stoppingToken.IsCancellationRequested) {
                CommissioningCommand command;
                try {
                    command = await reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    break;
                }
                catch (ChannelClosedException) {
                    break;
                }

                try {
                    await HandleCommandAsync(command, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _log.LogError(ex, "Commissioning command failed: {Command}", command.Kind);
                }
            }
        }

        private void Enqueue(CommissioningCommand command) {
            var ok = _queue.Writer.TryWrite(command);
            if (!ok) _log.LogWarning("Commissioning queue full, dropping command {Kind}", command.Kind);
        }

        private async Task HandleCommandAsync(CommissioningCommand command, CancellationToken outerCt) {
            switch (command.Kind) {
                case CommissioningCommandKind.Start:
                    await RunStartSequenceAsync(command.Reason, outerCt).ConfigureAwait(false);
                    break;
                case CommissioningCommandKind.Stop:
                    await HandleStopAsync(command.Reason, outerCt).ConfigureAwait(false);
                    break;
                case CommissioningCommandKind.Reset:
                    HandleReset(command.Reason);
                    break;
                case CommissioningCommandKind.SafetyStateChanged:
                    if (command.StateArgs!.Current == SafetyIsolationState.Isolated)
                        _state = CommissioningState.Faulted;
                    break;
            }
        }

        private async Task RunStartSequenceAsync(string? reason, CancellationToken outerCt) {
            if (_safety.State == SafetyIsolationState.Isolated) {
                _log.LogWarning("Commissioning start ignored: safety isolated.");
                return;
            }

            if (_state is CommissioningState.PowerOn or CommissioningState.Homing or CommissioningState.Aligning) {
                _log.LogWarning("Commissioning already running.");
                return;
            }

            _runCts?.Cancel();
            _runCts?.Dispose();
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
            var ct = _runCts.Token;

            var sw = Stopwatch.StartNew();
            try {
                _state = CommissioningState.PowerOn;
                await _sequence.PowerOnAsync(ct).ConfigureAwait(false);

                _state = CommissioningState.Homing;
                await _sequence.HomeAsync(ct).ConfigureAwait(false);

                _state = CommissioningState.Aligning;
                await _sequence.AlignAsync(ct).ConfigureAwait(false);

                _state = CommissioningState.Ready;
                sw.Stop();
                SingulationMetrics.Instance.CommissioningCycle.Record(sw.Elapsed.TotalMilliseconds);
                _log.LogInformation("Commissioning sequence completed in {Elapsed} ms.", sw.Elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) {
                _log.LogWarning("Commissioning sequence canceled: {Reason}", reason);
                _state = CommissioningState.Idle;
            }
            catch (Exception ex) {
                sw.Stop();
                _state = CommissioningState.Faulted;
                await _sequence.FailToSafeAsync(ex.Message, CancellationToken.None).ConfigureAwait(false);
                _safety.TryTrip(SafetyTriggerKind.CommissioningFailure, ex.Message ?? "commissioning error");
                _log.LogError(ex, "Commissioning sequence failed.");
            }
        }

        private async Task HandleStopAsync(string? reason, CancellationToken outerCt) {
            _runCts?.Cancel();
            if (_state == CommissioningState.Idle) {
                _log.LogInformation("Commissioning stop ignored: already idle.");
                return;
            }
            try {
                await _sequence.FailToSafeAsync(reason ?? "stop", outerCt).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _log.LogError(ex, "Commissioning stop sequence failed.");
            }
            finally {
                _state = CommissioningState.Idle;
            }
        }

        private void HandleReset(string? reason) {
            if (_state == CommissioningState.Faulted) {
                _state = CommissioningState.Idle;
                _log.LogInformation("Commissioning reset: {Reason}", reason);
            }
        }

        private enum CommissioningState {
            Idle,
            PowerOn,
            Homing,
            Aligning,
            Ready,
            Faulted
        }

        private readonly record struct CommissioningCommand(CommissioningCommandKind Kind, string? Reason) {
            public CommissioningCommand(CommissioningCommandKind kind, SafetyStateChangedEventArgs args) : this(kind, args.ReasonText) {
                StateArgs = args;
            }

            public SafetyStateChangedEventArgs? StateArgs { get; init; }
        }

        private enum CommissioningCommandKind {
            Start,
            Stop,
            Reset,
            SafetyStateChanged
        }
    }
}
