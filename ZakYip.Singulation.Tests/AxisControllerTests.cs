using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions.Ports;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Enums;

namespace ZakYip.Singulation.Tests {

    internal sealed class AxisControllerTests {

        [MiniFact]
        public async Task StopAllStopsEachDriveAsync() {
            var bus = new FakeBusAdapter(2);
            var registry = new FakeDriveRegistry();
            var aggregator = new FakeAxisEventAggregator();
            var controller = new AxisController(bus, registry, aggregator);
            var options = new DriverOptions {
                Card = 0,
                Port = 0,
                NodeId = 1,
                GearRatio = 1m,
                PulleyPitchDiameterMm = 1m
            };

            var result = await controller.InitializeAsync("fake", options, 2, CancellationToken.None).ConfigureAwait(false);
            MiniAssert.True(result.Key, "初始化应成功");
            await controller.StopAllAsync().ConfigureAwait(false);
            MiniAssert.True(registry.Created.Count == 2, "应创建两根轴驱动");
            MiniAssert.True(registry.Created.TrueForAll(d => d.StopCalls > 0), "所有驱动均应执行停止");
        }
    }

    internal sealed class FakeDriveRegistry : IDriveRegistry {
        public List<FakeAxisDrive> Created { get; } = new();

        public void Register(string vendor, Func<AxisId, IAxisPort, DriverOptions, IAxisDrive> factory) { }

        public IAxisDrive Create(string vendor, AxisId axisId, IAxisPort port, DriverOptions opts) {
            var drive = new FakeAxisDrive(axisId);
            Created.Add(drive);
            return drive;
        }
    }

    internal sealed class FakeAxisEventAggregator : IAxisEventAggregator {
        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;
        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;
        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;
        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;
        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;
        public void Attach(IAxisDrive drive) { }
        public void Detach(IAxisDrive drive) { }
    }

    internal sealed class FakeBusAdapter : IBusAdapter {
        private readonly int _count;
        private bool _initialized;

        public FakeBusAdapter(int count) => _count = count;

        public bool IsInitialized => _initialized;

        public Task<KeyValuePair<bool, string>> InitializeAsync(CancellationToken ct = default) {
            _initialized = true;
            return Task.FromResult(new KeyValuePair<bool, string>(true, "ok"));
        }

        public Task CloseAsync(CancellationToken ct = default) {
            _initialized = false;
            return Task.CompletedTask;
        }

        public Task<int> GetAxisCountAsync(CancellationToken ct = default) => Task.FromResult(_count);

        public Task<int> GetErrorCodeAsync(CancellationToken ct = default) => Task.FromResult(0);

        public Task ResetAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task WarmResetAsync(CancellationToken ct = default) => Task.CompletedTask;

        public ushort TranslateNodeId(ushort logicalNodeId) => logicalNodeId;

        public bool ShouldReverse(ushort logicalNodeId) => false;
    }

    internal sealed class FakeAxisDrive : IAxisDrive {
        public FakeAxisDrive(AxisId axis) => Axis = axis;

        public int StopCalls { get; private set; }

        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;
        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;
        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;
        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;
        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        public AxisId Axis { get; }
        public DriverStatus Status => DriverStatus.Online;
        public decimal? LastTargetMmps { get; private set; }
        public decimal? LastFeedbackMmps => LastTargetMmps;
        public bool IsEnabled => true;
        public int LastErrorCode => 0;
        public string? LastErrorMessage => null;
        public decimal? MaxLinearMmps => 1000m;
        public decimal? MaxAccelMmps2 => 500m;
        public decimal? MaxDecelMmps2 => 500m;

        public ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default) {
            LastTargetMmps = rpm.Value;
            return ValueTask.CompletedTask;
        }

        public Task WriteSpeedAsync(decimal mmPerSec, CancellationToken ct = default) {
            LastTargetMmps = mmPerSec;
            return Task.CompletedTask;
        }

        public Task SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) => Task.CompletedTask;

        public ValueTask SetAccelDecelByLinearAsync(decimal accelMmPerSec, decimal decelMmPerSec, CancellationToken ct = default) => ValueTask.CompletedTask;

        public ValueTask StopAsync(CancellationToken ct = default) {
            StopCalls++;
            return ValueTask.CompletedTask;
        }

        public Task EnableAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateLinearLimitsAsync(decimal maxLinearMmps, decimal maxAccelMmps2, decimal maxDecelMmps2, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateMechanicsAsync(decimal rollerDiameterMm, decimal gearRatio, int ppr, CancellationToken ct = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
