using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Infrastructure.Telemetry;

namespace ZakYip.Singulation.Host.Workers {

    /// <summary>
    /// 调试联机流程后台服务：监听安全事件并驱动联机序列。
    /// </summary>
    public sealed class CommissioningWorker : BackgroundService {
        /// <summary>日志记录器。</summary>
        private readonly ILogger<CommissioningWorker> _log;

        /// <summary>安全管线，用于监听 Start/Stop/Reset 请求。</summary>
        private readonly ISafetyPipeline _safety;

        /// <summary>联机动作序列执行器。</summary>
        private readonly ICommissioningSequence _sequence;

        /// <summary>内部命令队列。</summary>
        private readonly Channel<CommissioningCommand> _queue;

        /// <summary>当前联机状态。</summary>
        private CommissioningState _state = CommissioningState.Idle;

        /// <summary>当前运行序列的取消源。</summary>
        private CancellationTokenSource? _runCts;

        /// <summary>
        /// 初始化联机后台服务并订阅安全事件。
        /// </summary>
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

        /// <inheritdoc />
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

        /// <summary>
        /// 将内部命令投递到处理队列。
        /// </summary>
        private void Enqueue(CommissioningCommand command) {
            var ok = _queue.Writer.TryWrite(command);
            if (!ok) _log.LogWarning("Commissioning queue full, dropping command {Kind}", command.Kind);
        }

        /// <summary>
        /// 处理单条联机命令。
        /// </summary>
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

        /// <summary>
        /// 执行上电、回零、对位等联机动作。
        /// </summary>
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

        /// <summary>
        /// 停止联机流程并执行故障安全。
        /// </summary>
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

        /// <summary>
        /// 响应重置命令，允许从故障态恢复为待命态。
        /// </summary>
        private void HandleReset(string? reason) {
            if (_state == CommissioningState.Faulted) {
                _state = CommissioningState.Idle;
                _log.LogInformation("Commissioning reset: {Reason}", reason);
            }
        }
    }
}
