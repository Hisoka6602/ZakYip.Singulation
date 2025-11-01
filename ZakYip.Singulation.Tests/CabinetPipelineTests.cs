using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Infrastructure.Cabinet;
using ZakYip.Singulation.Tests.TestHelpers;

namespace ZakYip.Singulation.Tests {

    internal sealed class SafetyPipelineTests {

        [MiniFact]
        public async Task EmergencyStopFromIoZeroesSpeedAsync() {
            var axis = new RecordingAxisController();
            var isolator = new FakeCabinetIsolator();
            var notifier = new FakeRealtimeNotifier();
            var pipeline = new CabinetPipeline(
                NullLogger<CabinetPipeline>.Instance,
                isolator,
                Array.Empty<ICabinetIoModule>(),
                axis,
                new FakeAxisEventAggregator(),
                notifier,
                new FakeControllerOptionsStore());

            await pipeline.StartAsync(CancellationToken.None).ConfigureAwait(false);
            pipeline.RequestStop(CabinetTriggerKind.EmergencyStop, "测试急停", true);
            await axis.WaitForEmergencyAsync().ConfigureAwait(false);
            MiniAssert.SequenceEqual(new[] { "write:0", "stop" }, axis.Calls, "急停应先写零速再停机");
            await pipeline.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [MiniFact]
        public async Task IoResetClearsIsolationAsync() {
            var axis = new RecordingAxisController();
            var isolator = new FakeCabinetIsolator();
            isolator.SetState(CabinetIsolationState.Isolated);
            var notifier = new FakeRealtimeNotifier();
            var pipeline = new CabinetPipeline(
                NullLogger<CabinetPipeline>.Instance,
                isolator,
                Array.Empty<ICabinetIoModule>(),
                axis,
                new FakeAxisEventAggregator(),
                notifier,
                new FakeControllerOptionsStore());

            await pipeline.StartAsync(CancellationToken.None).ConfigureAwait(false);
            pipeline.RequestReset(CabinetTriggerKind.ResetButton, "IO复位", true);
            await isolator.WaitForStateAsync(CabinetIsolationState.Normal).ConfigureAwait(false);
            await pipeline.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [MiniFact]
        public async Task IoStartPublishesRealtimeNotificationAsync() {
            var axis = new RecordingAxisController();
            var isolator = new FakeCabinetIsolator();
            var notifier = new FakeRealtimeNotifier();
            var pipeline = new CabinetPipeline(
                NullLogger<CabinetPipeline>.Instance,
                isolator,
                Array.Empty<ICabinetIoModule>(),
                axis,
                new FakeAxisEventAggregator(),
                notifier,
                new FakeControllerOptionsStore());

            await pipeline.StartAsync(CancellationToken.None).ConfigureAwait(false);
            pipeline.RequestStart(CabinetTriggerKind.StartButton, "IO启动", true);
            await notifier.WaitForPublishAsync().ConfigureAwait(false);
            MiniAssert.True(notifier.Payloads.Count == 1, "应发布一次实时通知");
            await pipeline.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private sealed class RecordingAxisController : IAxisController {
            private readonly TaskCompletionSource<bool> _writeZeroTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _stopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly IBusAdapter _bus = new DummyBusAdapter();

            public List<string> Calls { get; } = new();

            public event EventHandler<string>? ControllerFaulted;

            public IBusAdapter Bus => _bus;

            public IReadOnlyList<IAxisDrive> Drives => Array.Empty<IAxisDrive>();

            public IReadOnlyList<decimal?> RealtimeSpeedsMmps => Array.Empty<decimal?>();

            public Task<KeyValuePair<bool, string>> InitializeAsync(string vendor, ZakYip.Singulation.Drivers.Common.DriverOptions template, int? overrideAxisCount = null, CancellationToken ct = default)
                => Task.FromResult(new KeyValuePair<bool, string>(true, "stub"));

            public Task EnableAllAsync(CancellationToken ct = default) => Task.CompletedTask;

            public Task DisableAllAsync(CancellationToken ct = default) => Task.CompletedTask;

            public Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default) => Task.CompletedTask;

            public Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default) {
                Calls.Add($"write:{mmPerSec}");
                if (mmPerSec == 0m) _writeZeroTcs.TrySetResult(true);
                return Task.CompletedTask;
            }

            public Task StopAllAsync(CancellationToken ct = default) {
                Calls.Add("stop");
                _stopTcs.TrySetResult(true);
                return Task.CompletedTask;
            }

            public Task DisposeAllAsync(CancellationToken ct = default) => Task.CompletedTask;

            public Task ApplySpeedSetAsync(ZakYip.Singulation.Core.Contracts.Dto.SpeedSet set, CancellationToken ct = default) => Task.CompletedTask;

            public async Task WaitForEmergencyAsync() {
                await Task.WhenAll(_writeZeroTcs.Task, _stopTcs.Task).ConfigureAwait(false);
            }
        }

        private sealed class FakeCabinetIsolator : ICabinetIsolator {
            private CabinetIsolationState _state = CabinetIsolationState.Normal;
            private readonly TaskCompletionSource<CabinetIsolationState> _stateTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

            public CabinetIsolationState State => _state;

            public bool IsDegraded => _state == CabinetIsolationState.Degraded;

            public bool IsIsolated => _state == CabinetIsolationState.Isolated;

            public CabinetTriggerKind LastTriggerKind { get; private set; }

            public string? LastTriggerReason { get; private set; }

            public bool TryTrip(CabinetTriggerKind kind, string reason) {
                LastTriggerKind = kind;
                LastTriggerReason = reason;
                if (_state == CabinetIsolationState.Isolated) return false;
                var previous = _state;
                _state = CabinetIsolationState.Isolated;
                StateChanged?.Invoke(this, new CabinetStateChangedEventArgs { Previous = previous, Current = _state, ReasonKind = kind, ReasonText = reason });
                _stateTcs.TrySetResult(_state);
                return true;
            }

            public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) {
                LastTriggerKind = kind;
                LastTriggerReason = reason;
                if (_state == CabinetIsolationState.Isolated) return false;
                var previous = _state;
                _state = CabinetIsolationState.Degraded;
                StateChanged?.Invoke(this, new CabinetStateChangedEventArgs { Previous = previous, Current = _state, ReasonKind = kind, ReasonText = reason });
                _stateTcs.TrySetResult(_state);
                return true;
            }

            public bool TryRecoverFromDegraded(string reason) {
                if (_state != CabinetIsolationState.Degraded) return false;
                var previous = _state;
                _state = CabinetIsolationState.Normal;
                StateChanged?.Invoke(this, new CabinetStateChangedEventArgs { Previous = previous, Current = _state, ReasonKind = CabinetTriggerKind.HealthRecovered, ReasonText = reason });
                _stateTcs.TrySetResult(_state);
                return true;
            }

            public bool TryResetIsolation(string reason, CancellationToken ct = default) {
                if (_state != CabinetIsolationState.Isolated) return false;
                var previous = _state;
                _state = CabinetIsolationState.Normal;
                StateChanged?.Invoke(this, new CabinetStateChangedEventArgs { Previous = previous, Current = _state, ReasonKind = CabinetTriggerKind.ResetButton, ReasonText = reason });
                _stateTcs.TrySetResult(_state);
                return true;
            }

            public void SetState(CabinetIsolationState state) => _state = state;

            public async Task WaitForStateAsync(CabinetIsolationState expected) {
                if (_state == expected) return;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _stateTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
                if (_state != expected) {
                    throw new InvalidOperationException($"期待状态 {expected}，实际 {_state}");
                }
            }
        }

        private sealed class FakeRealtimeNotifier : IRealtimeNotifier {
            public List<object> Payloads { get; } = new();
            private readonly TaskCompletionSource<bool> _publishTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default) {
                Payloads.Add(payload);
                _publishTcs.TrySetResult(true);
                return ValueTask.CompletedTask;
            }

            public Task WaitForPublishAsync() => _publishTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }

        private sealed class FakeAxisEventAggregator : IAxisEventAggregator {
            public event EventHandler<ZakYip.Singulation.Core.Contracts.Events.AxisSpeedFeedbackEventArgs>? SpeedFeedback;
            public event EventHandler<ZakYip.Singulation.Core.Contracts.Events.AxisCommandIssuedEventArgs>? CommandIssued;
            public event EventHandler<ZakYip.Singulation.Core.Contracts.Events.AxisErrorEventArgs>? AxisFaulted;
            public event EventHandler<ZakYip.Singulation.Core.Contracts.Events.AxisDisconnectedEventArgs>? AxisDisconnected;
            public event EventHandler<ZakYip.Singulation.Core.Contracts.Events.DriverNotLoadedEventArgs>? DriverNotLoaded;
            public void Attach(IAxisDrive drive) { }
            public void Detach(IAxisDrive drive) { }
        }

        private sealed class DummyBusAdapter : IBusAdapter {
            public bool IsInitialized => true;
            public Task<KeyValuePair<bool, string>> InitializeAsync(CancellationToken ct = default) => Task.FromResult(new KeyValuePair<bool, string>(true, "ok"));
            public Task CloseAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task<int> GetAxisCountAsync(CancellationToken ct = default) => Task.FromResult(0);
            public Task<int> GetErrorCodeAsync(CancellationToken ct = default) => Task.FromResult(0);
            public Task ResetAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task WarmResetAsync(CancellationToken ct = default) => Task.CompletedTask;
            public ushort TranslateNodeId(ushort logicalNodeId) => logicalNodeId;
            public bool ShouldReverse(ushort logicalNodeId) => false;
        }
    }
}
