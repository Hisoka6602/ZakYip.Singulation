using ZakYip.Singulation.Tests.TestHelpers;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Cabinet;

namespace ZakYip.Singulation.Tests {

    internal sealed class FrameGuardTests {

        [MiniFact]
        public async Task InitializeAsync_WithHeartbeatPortZero_SkipsHeartbeatMonitoringAsync() {
            var safety = new FakeSafetyPipeline();
            var hub = new FakeUpstreamFrameHub();
            var optionsStore = new FakeUpstreamOptionsStore(heartbeatPort: 0);
            var frameGuardOptions = Options.Create(new FrameGuardOptions());

            var guard = new FrameGuard(
                NullLogger<FrameGuard>.Instance,
                safety,
                frameGuardOptions,
                hub,
                optionsStore,
                FakeSystemClock.CreateDefault());

            var result = await guard.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            MiniAssert.True(result, "InitializeAsync should return true");
            MiniAssert.Equal(0, hub.HeartbeatSubscriptionCount, "Should not subscribe to heartbeat when port is 0");
        }

        [MiniFact]
        public async Task InitializeAsync_WithNonZeroHeartbeatPort_EnablesHeartbeatMonitoringAsync() {
            var safety = new FakeSafetyPipeline();
            var hub = new FakeUpstreamFrameHub();
            var optionsStore = new FakeUpstreamOptionsStore(heartbeatPort: 5003);
            var frameGuardOptions = Options.Create(new FrameGuardOptions());

            var guard = new FrameGuard(
                NullLogger<FrameGuard>.Instance,
                safety,
                frameGuardOptions,
                hub,
                optionsStore,
                FakeSystemClock.CreateDefault());

            var result = await guard.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

            MiniAssert.True(result, "InitializeAsync should return true");
            MiniAssert.Equal(1, hub.HeartbeatSubscriptionCount, "Should subscribe to heartbeat when port is non-zero");
        }

        private sealed class FakeUpstreamOptionsStore : IUpstreamOptionsStore {
            private readonly UpstreamOptions _options;

            public FakeUpstreamOptionsStore(int heartbeatPort) {
                _options = new UpstreamOptions {
                    HeartbeatPort = heartbeatPort,
                    SpeedPort = 5001,
                    PositionPort = 5002
                };
            }

            public Task<UpstreamOptions> GetAsync(CancellationToken ct = default) 
                => Task.FromResult(_options);

            public Task SaveAsync(UpstreamOptions dto, CancellationToken ct = default) 
                => Task.CompletedTask;

            public Task DeleteAsync(CancellationToken ct = default) 
                => Task.CompletedTask;
        }

        private sealed class FakeUpstreamFrameHub : IUpstreamFrameHub {
            public int HeartbeatSubscriptionCount { get; private set; }

            public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) SubscribeHeartbeat(int capacity = 256) {
                HeartbeatSubscriptionCount++;
                var channel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
                return (channel.Reader, new DummyDisposable());
            }

            public void PublishHeartbeat(ReadOnlyMemory<byte> payload) { }

            public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) SubscribeSpeed(int capacity = 256) {
                var channel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
                return (channel.Reader, new DummyDisposable());
            }

            public void PublishSpeed(ReadOnlyMemory<byte> payload) { }

            public (ChannelReader<ReadOnlyMemory<byte>> reader, IDisposable unsubscribe) SubscribePosition(int capacity = 256) {
                var channel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
                return (channel.Reader, new DummyDisposable());
            }

            public void PublishPosition(ReadOnlyMemory<byte> payload) { }

            private class DummyDisposable : IDisposable {
                public void Dispose() { }
            }
        }

        private sealed class FakeSafetyPipeline : ICabinetPipeline {
            public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;
            public event EventHandler<CabinetTriggerEventArgs>? StartRequested;
            public event EventHandler<CabinetTriggerEventArgs>? StopRequested;
            public event EventHandler<CabinetTriggerEventArgs>? ResetRequested;

            public CabinetIsolationState State => CabinetIsolationState.Normal;
            public bool IsRemoteMode => false;

            public bool TryTrip(CabinetTriggerKind kind, string reason) => false;
            public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) => false;
            public bool TryRecoverFromDegraded(string reason) => false;
            public bool TryResetIsolation(string reason, CancellationToken ct = default) => false;
            public void RequestStart(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false) { }
            public void RequestStop(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false) { }
            public void RequestReset(CabinetTriggerKind kind, string? reason = null, bool triggeredByIo = false) { }
        }
    }
}
