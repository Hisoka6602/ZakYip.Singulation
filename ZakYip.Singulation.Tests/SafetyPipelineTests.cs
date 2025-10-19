using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Host.Runtime;
using ZakYip.Singulation.Host.Safety;

namespace ZakYip.Singulation.Tests {

    internal sealed class SafetyPipelineTests {

        [MiniFact]
        public async Task EmergencyStopFromIoZeroesSpeedAsync() {
            var axis = new RecordingAxisController();
            var isolator = new FakeSafetyIsolator();
            var notifier = new FakeRealtimeNotifier();
            var restarter = new FakeRestarter();
            var pipeline = new SafetyPipeline(
                NullLogger<SafetyPipeline>.Instance,
                isolator,
                Array.Empty<ISafetyIoModule>(),
                axis,
                new FakeAxisEventAggregator(),
                notifier,
                restarter);

            await pipeline.StartAsync(CancellationToken.None).ConfigureAwait(false);
            pipeline.RequestStop(SafetyTriggerKind.EmergencyStop, "测试急停", true);
            await axis.WaitForEmergencyAsync().ConfigureAwait(false);
            MiniAssert.SequenceEqual(new[] { "write:0", "stop" }, axis.Calls, "急停应先写零速再停机");
            MiniAssert.True(restarter.Calls.Count == 0, "急停不应触发重启");
            await pipeline.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [MiniFact]
        public async Task IoResetTriggersRestartAsync() {
            var axis = new RecordingAxisController();
            var isolator = new FakeSafetyIsolator();
            isolator.SetState(SafetyIsolationState.Isolated);
            var notifier = new FakeRealtimeNotifier();
            var restarter = new FakeRestarter();
            var pipeline = new SafetyPipeline(
                NullLogger<SafetyPipeline>.Instance,
                isolator,
                Array.Empty<ISafetyIoModule>(),
                axis,
                new FakeAxisEventAggregator(),
                notifier,
                restarter);

            await pipeline.StartAsync(CancellationToken.None).ConfigureAwait(false);
            pipeline.RequestReset(SafetyTriggerKind.ResetButton, "IO复位", true);
            await restarter.WaitForRestartAsync().ConfigureAwait(false);
            MiniAssert.True(restarter.Calls[0].Contains("IO 复位信号"), "应记录 IO 复位重启原因");
            await pipeline.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [MiniFact]
        public async Task IoStartTriggersRestartAsync() {
            var axis = new RecordingAxisController();
            var isolator = new FakeSafetyIsolator();
            var notifier = new FakeRealtimeNotifier();
            var restarter = new FakeRestarter();
            var pipeline = new SafetyPipeline(
                NullLogger<SafetyPipeline>.Instance,
                isolator,
                Array.Empty<ISafetyIoModule>(),
                axis,
                new FakeAxisEventAggregator(),
                notifier,
                restarter);

            await pipeline.StartAsync(CancellationToken.None).ConfigureAwait(false);
            pipeline.RequestStart(SafetyTriggerKind.StartButton, "IO启动", true);
            await restarter.WaitForRestartAsync().ConfigureAwait(false);
            MiniAssert.True(restarter.Calls[0].Contains("IO 启动信号"), "应记录 IO 启动重启原因");
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

            public Task<KeyValuePair<bool, string>> InitializeAsync(string vendor, ZakYip.Singulation.Drivers.Common.DriverOptions template, int? overrideAxisCount = null, CancellationToken ct = default)
                => Task.FromResult(new KeyValuePair<bool, string>(true, "stub"));

            public Task EnableAllAsync(CancellationToken ct = default) => Task.CompletedTask;

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

        private sealed class FakeSafetyIsolator : ISafetyIsolator {
            private SafetyIsolationState _state = SafetyIsolationState.Normal;

            public event EventHandler<SafetyStateChangedEventArgs>? StateChanged;

            public SafetyIsolationState State => _state;

            public bool IsDegraded => _state == SafetyIsolationState.Degraded;

            public bool IsIsolated => _state == SafetyIsolationState.Isolated;

            public SafetyTriggerKind LastTriggerKind { get; private set; }

            public string? LastTriggerReason { get; private set; }

            public bool TryTrip(SafetyTriggerKind kind, string reason) {
                LastTriggerKind = kind;
                LastTriggerReason = reason;
                if (_state == SafetyIsolationState.Isolated) return false;
                var previous = _state;
                _state = SafetyIsolationState.Isolated;
                StateChanged?.Invoke(this, new SafetyStateChangedEventArgs(previous, _state, kind, reason));
                return true;
            }

            public bool TryEnterDegraded(SafetyTriggerKind kind, string reason) {
                LastTriggerKind = kind;
                LastTriggerReason = reason;
                if (_state == SafetyIsolationState.Isolated) return false;
                var previous = _state;
                _state = SafetyIsolationState.Degraded;
                StateChanged?.Invoke(this, new SafetyStateChangedEventArgs(previous, _state, kind, reason));
                return true;
            }

            public bool TryRecoverFromDegraded(string reason) {
                if (_state != SafetyIsolationState.Degraded) return false;
                var previous = _state;
                _state = SafetyIsolationState.Normal;
                StateChanged?.Invoke(this, new SafetyStateChangedEventArgs(previous, _state, SafetyTriggerKind.HealthRecovered, reason));
                return true;
            }

            public bool TryResetIsolation(string reason, CancellationToken ct = default) {
                if (_state != SafetyIsolationState.Isolated) return false;
                var previous = _state;
                _state = SafetyIsolationState.Normal;
                StateChanged?.Invoke(this, new SafetyStateChangedEventArgs(previous, _state, SafetyTriggerKind.ResetButton, reason));
                return true;
            }

            public void SetState(SafetyIsolationState state) => _state = state;
        }

        private sealed class FakeRealtimeNotifier : IRealtimeNotifier {
            public List<object> Payloads { get; } = new();

            public ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default) {
                Payloads.Add(payload);
                return ValueTask.CompletedTask;
            }
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

        private sealed class FakeRestarter : IApplicationRestarter {
            private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public List<string> Calls { get; } = new();

            public Task RestartAsync(string reason, CancellationToken ct = default) {
                Calls.Add(reason);
                _tcs.TrySetResult(true);
                return Task.CompletedTask;
            }

            public Task WaitForRestartAsync() => _tcs.Task;
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
