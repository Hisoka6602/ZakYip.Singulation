using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Drivers.Abstractions.Ports;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

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

        [MiniFact]
        public async Task ApplySpeedSetOnlyWritesChangedSpeeds() {
            // Arrange: Create controller with 4 axes
            var bus = new FakeBusAdapter(4);
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

            var result = await controller.InitializeAsync("fake", options, 4, CancellationToken.None).ConfigureAwait(false);
            MiniAssert.True(result.Key, "初始化应成功");
            MiniAssert.True(registry.Created.Count == 4, "应创建4根轴驱动");

            // Act 1: First call - all speeds are 300, all axes should be written
            var speedSet1 = new SpeedSet(
                DateTime.UtcNow,
                1,
                new[] { 300, 300, 300, 300 },
                Array.Empty<int>()
            );
            await controller.ApplySpeedSetAsync(speedSet1, CancellationToken.None).ConfigureAwait(false);

            // Assert 1: All 4 axes should have been written to once
            MiniAssert.True(registry.Created[0].WriteSpeedCalls == 1, "轴0应写入1次");
            MiniAssert.True(registry.Created[1].WriteSpeedCalls == 1, "轴1应写入1次");
            MiniAssert.True(registry.Created[2].WriteSpeedCalls == 1, "轴2应写入1次");
            MiniAssert.True(registry.Created[3].WriteSpeedCalls == 1, "轴3应写入1次");

            // Act 2: Second call - only axis 2 changes to 1500, only it should be written
            var speedSet2 = new SpeedSet(
                DateTime.UtcNow,
                2,
                new[] { 300, 300, 1500, 300 },
                Array.Empty<int>()
            );
            await controller.ApplySpeedSetAsync(speedSet2, CancellationToken.None).ConfigureAwait(false);

            // Assert 2: Only axis 2 should have been written to again (total 2 writes)
            MiniAssert.True(registry.Created[0].WriteSpeedCalls == 1, "轴0不应再次写入");
            MiniAssert.True(registry.Created[1].WriteSpeedCalls == 1, "轴1不应再次写入");
            MiniAssert.True(registry.Created[2].WriteSpeedCalls == 2, "轴2应写入2次");
            MiniAssert.True(registry.Created[3].WriteSpeedCalls == 1, "轴3不应再次写入");

            // Act 3: Third call - all axes change to 500
            var speedSet3 = new SpeedSet(
                DateTime.UtcNow,
                3,
                new[] { 500, 500, 500, 500 },
                Array.Empty<int>()
            );
            await controller.ApplySpeedSetAsync(speedSet3, CancellationToken.None).ConfigureAwait(false);

            // Assert 3: All axes should have been written to again
            MiniAssert.True(registry.Created[0].WriteSpeedCalls == 2, "轴0应写入2次");
            MiniAssert.True(registry.Created[1].WriteSpeedCalls == 2, "轴1应写入2次");
            MiniAssert.True(registry.Created[2].WriteSpeedCalls == 3, "轴2应写入3次");
            MiniAssert.True(registry.Created[3].WriteSpeedCalls == 2, "轴3应写入2次");
        }

        [MiniFact]
        public async Task RealtimeSpeedsMmpsReflectsLastFeedbackFromDrives() {
            // Arrange: Create controller with 3 axes
            var bus = new FakeBusAdapter(3);
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

            var result = await controller.InitializeAsync("fake", options, 3, CancellationToken.None).ConfigureAwait(false);
            MiniAssert.True(result.Key, "初始化应成功");
            MiniAssert.True(registry.Created.Count == 3, "应创建3根轴驱动");

            // Act & Assert 1: Initially, all speeds should be null
            var speeds = controller.RealtimeSpeedsMmps;
            MiniAssert.True(speeds.Count == 3, "应有3个速度值");
            MiniAssert.True(speeds[0] == null, "轴0初始速度应为null");
            MiniAssert.True(speeds[1] == null, "轴1初始速度应为null");
            MiniAssert.True(speeds[2] == null, "轴2初始速度应为null");

            // Act 2: Simulate speed feedback for axis 0
            registry.Created[0].SimulateSpeedFeedback(100m);
            speeds = controller.RealtimeSpeedsMmps;
            MiniAssert.True(speeds[0] == 100m, "轴0速度应为100");
            MiniAssert.True(speeds[1] == null, "轴1速度应仍为null");
            MiniAssert.True(speeds[2] == null, "轴2速度应仍为null");

            // Act 3: Simulate speed feedback for axis 1 and 2
            registry.Created[1].SimulateSpeedFeedback(200m);
            registry.Created[2].SimulateSpeedFeedback(300m);
            speeds = controller.RealtimeSpeedsMmps;
            MiniAssert.True(speeds[0] == 100m, "轴0速度应为100");
            MiniAssert.True(speeds[1] == 200m, "轴1速度应为200");
            MiniAssert.True(speeds[2] == 300m, "轴2速度应为300");

            // Act 4: Update speed for axis 0
            registry.Created[0].SimulateSpeedFeedback(150m);
            speeds = controller.RealtimeSpeedsMmps;
            MiniAssert.True(speeds[0] == 150m, "轴0速度应更新为150");
            MiniAssert.True(speeds[1] == 200m, "轴1速度应仍为200");
            MiniAssert.True(speeds[2] == 300m, "轴2速度应仍为300");
        }

        [MiniFact]
        public async Task TargetSpeedsMmpsReflectsLastTargetFromDrives() {
            // Arrange: Create controller with 3 axes
            var bus = new FakeBusAdapter(3);
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

            var result = await controller.InitializeAsync("fake", options, 3, CancellationToken.None).ConfigureAwait(false);
            MiniAssert.True(result.Key, "初始化应成功");
            MiniAssert.True(registry.Created.Count == 3, "应创建3根轴驱动");

            // Act & Assert 1: Initially, all target speeds should be null
            var speeds = controller.TargetSpeedsMmps;
            MiniAssert.True(speeds.Count == 3, "应有3个目标速度值");
            MiniAssert.True(speeds[0] == null, "轴0初始目标速度应为null");
            MiniAssert.True(speeds[1] == null, "轴1初始目标速度应为null");
            MiniAssert.True(speeds[2] == null, "轴2初始目标速度应为null");

            // Act 2: Write speed to axis 0
            await registry.Created[0].WriteSpeedAsync(100m, CancellationToken.None).ConfigureAwait(false);
            speeds = controller.TargetSpeedsMmps;
            MiniAssert.True(speeds[0] == 100m, "轴0目标速度应为100");
            MiniAssert.True(speeds[1] == null, "轴1目标速度应仍为null");
            MiniAssert.True(speeds[2] == null, "轴2目标速度应仍为null");

            // Act 3: Write speed to axis 1 and 2
            await registry.Created[1].WriteSpeedAsync(200m, CancellationToken.None).ConfigureAwait(false);
            await registry.Created[2].WriteSpeedAsync(300m, CancellationToken.None).ConfigureAwait(false);
            speeds = controller.TargetSpeedsMmps;
            MiniAssert.True(speeds[0] == 100m, "轴0目标速度应为100");
            MiniAssert.True(speeds[1] == 200m, "轴1目标速度应为200");
            MiniAssert.True(speeds[2] == 300m, "轴2目标速度应为300");

            // Act 4: Update speed for axis 0
            await registry.Created[0].WriteSpeedAsync(150m, CancellationToken.None).ConfigureAwait(false);
            speeds = controller.TargetSpeedsMmps;
            MiniAssert.True(speeds[0] == 150m, "轴0目标速度应更新为150");
            MiniAssert.True(speeds[1] == 200m, "轴1目标速度应仍为200");
            MiniAssert.True(speeds[2] == 300m, "轴2目标速度应仍为300");
        }

        [MiniFact]
        public async Task ResetLastSpeedsEnsuresSpeedWriteAfterReset() {
            // Arrange: Create controller with 4 axes
            var bus = new FakeBusAdapter(4);
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

            var result = await controller.InitializeAsync("fake", options, 4, CancellationToken.None).ConfigureAwait(false);
            MiniAssert.True(result.Key, "初始化应成功");

            // Act 1: Write initial speed of 300 to all axes
            var speedSet1 = new SpeedSet(
                DateTime.UtcNow,
                1,
                new[] { 300, 300, 300, 300 },
                Array.Empty<int>()
            );
            await controller.ApplySpeedSetAsync(speedSet1, CancellationToken.None).ConfigureAwait(false);

            // Assert 1: All axes should have been written to once
            MiniAssert.True(registry.Created[0].WriteSpeedCalls == 1, "轴0应写入1次");
            MiniAssert.True(registry.Created[1].WriteSpeedCalls == 1, "轴1应写入1次");
            MiniAssert.True(registry.Created[2].WriteSpeedCalls == 1, "轴2应写入1次");
            MiniAssert.True(registry.Created[3].WriteSpeedCalls == 1, "轴3应写入1次");

            // Act 2: Try to write the same speed again - should skip
            var speedSet2 = new SpeedSet(
                DateTime.UtcNow,
                2,
                new[] { 300, 300, 300, 300 },
                Array.Empty<int>()
            );
            await controller.ApplySpeedSetAsync(speedSet2, CancellationToken.None).ConfigureAwait(false);

            // Assert 2: No axes should have been written to again (optimization should skip)
            MiniAssert.True(registry.Created[0].WriteSpeedCalls == 1, "轴0不应再次写入（速度相同）");
            MiniAssert.True(registry.Created[1].WriteSpeedCalls == 1, "轴1不应再次写入（速度相同）");
            MiniAssert.True(registry.Created[2].WriteSpeedCalls == 1, "轴2不应再次写入（速度相同）");
            MiniAssert.True(registry.Created[3].WriteSpeedCalls == 1, "轴3不应再次写入（速度相同）");

            // Act 3: Reset last speeds cache (simulating mode switch)
            controller.ResetLastSpeeds();

            // Act 4: Write the same speed again - should NOT skip after reset
            var speedSet3 = new SpeedSet(
                DateTime.UtcNow,
                3,
                new[] { 300, 300, 300, 300 },
                Array.Empty<int>()
            );
            await controller.ApplySpeedSetAsync(speedSet3, CancellationToken.None).ConfigureAwait(false);

            // Assert 3: All axes should have been written to again (cache was reset)
            MiniAssert.True(registry.Created[0].WriteSpeedCalls == 2, "轴0应写入2次（缓存已重置）");
            MiniAssert.True(registry.Created[1].WriteSpeedCalls == 2, "轴1应写入2次（缓存已重置）");
            MiniAssert.True(registry.Created[2].WriteSpeedCalls == 2, "轴2应写入2次（缓存已重置）");
            MiniAssert.True(registry.Created[3].WriteSpeedCalls == 2, "轴3应写入2次（缓存已重置）");
        }

        [MiniFact]
        public async Task ApplySpeedSetDistributesSpeedsByAxisType() {
            // Arrange: Create controller with 6 axes (3 Main, 3 Eject)
            var bus = new FakeBusAdapter(6);
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

            var result = await controller.InitializeAsync("fake", options, 6, CancellationToken.None).ConfigureAwait(false);
            MiniAssert.True(result.Key, "初始化应成功");
            MiniAssert.True(registry.Created.Count == 6, "应创建6根轴驱动");

            // Set axis types: 0,1,2 = Main, 3,4,5 = Eject
            registry.Created[0].AxisType = AxisType.Main;
            registry.Created[1].AxisType = AxisType.Main;
            registry.Created[2].AxisType = AxisType.Main;
            registry.Created[3].AxisType = AxisType.Eject;
            registry.Created[4].AxisType = AxisType.Eject;
            registry.Created[5].AxisType = AxisType.Eject;

            // Act: Apply speed set with 3 main speeds and 3 eject speeds
            var speedSet = new SpeedSet(
                DateTime.UtcNow,
                1,
                new[] { 100, 200, 300 },  // Main speeds
                new[] { 400, 500, 600 }   // Eject speeds
            );
            await controller.ApplySpeedSetAsync(speedSet, CancellationToken.None).ConfigureAwait(false);

            // Assert: Main axes get main speeds, eject axes get eject speeds
            MiniAssert.True(registry.Created[0].LastTargetMmps == 100m, "Main轴0应为100");
            MiniAssert.True(registry.Created[1].LastTargetMmps == 200m, "Main轴1应为200");
            MiniAssert.True(registry.Created[2].LastTargetMmps == 300m, "Main轴2应为300");
            MiniAssert.True(registry.Created[3].LastTargetMmps == 400m, "Eject轴3应为400");
            MiniAssert.True(registry.Created[4].LastTargetMmps == 500m, "Eject轴4应为500");
            MiniAssert.True(registry.Created[5].LastTargetMmps == 600m, "Eject轴5应为600");
        }

        [MiniFact]
        public async Task AxisOperationsThrowWhenBusNotInitialized() {
            // Arrange: Create controller but DON'T initialize
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

            // Initialize normally first
            var result = await controller.InitializeAsync("fake", options, 2, CancellationToken.None).ConfigureAwait(false);
            MiniAssert.True(result.Key, "初始化应成功");

            // Act: Close the bus to simulate uninitialized state
            await bus.CloseAsync().ConfigureAwait(false);
            MiniAssert.True(!bus.IsInitialized, "总线应处于未初始化状态");

            // Assert: All axis operations should throw InvalidOperationException
            bool stopThrew = false;
            try {
                await controller.StopAllAsync().ConfigureAwait(false);
            } catch (InvalidOperationException ex) {
                stopThrew = ex.Message.Contains("总线未初始化");
            }
            MiniAssert.True(stopThrew, "StopAllAsync 应抛出 InvalidOperationException");

            bool enableThrew = false;
            try {
                await controller.EnableAllAsync().ConfigureAwait(false);
            } catch (InvalidOperationException ex) {
                enableThrew = ex.Message.Contains("总线未初始化");
            }
            MiniAssert.True(enableThrew, "EnableAllAsync 应抛出 InvalidOperationException");

            bool disableThrew = false;
            try {
                await controller.DisableAllAsync().ConfigureAwait(false);
            } catch (InvalidOperationException ex) {
                disableThrew = ex.Message.Contains("总线未初始化");
            }
            MiniAssert.True(disableThrew, "DisableAllAsync 应抛出 InvalidOperationException");

            bool speedThrew = false;
            try {
                await controller.WriteSpeedAllAsync(100m).ConfigureAwait(false);
            } catch (InvalidOperationException ex) {
                speedThrew = ex.Message.Contains("总线未初始化");
            }
            MiniAssert.True(speedThrew, "WriteSpeedAllAsync 应抛出 InvalidOperationException");

            bool accelThrew = false;
            try {
                await controller.SetAccelDecelAllAsync(100m, 100m).ConfigureAwait(false);
            } catch (InvalidOperationException ex) {
                accelThrew = ex.Message.Contains("总线未初始化");
            }
            MiniAssert.True(accelThrew, "SetAccelDecelAllAsync 应抛出 InvalidOperationException");
        }
    }

    internal sealed class FakeDriveRegistry : IDriveRegistry {
        public List<FakeAxisDrive> Created { get; } = new();

        public void Register(string vendor, Func<AxisId, IAxisPort, DriverOptions, IAxisDrive> factory) {
        }

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

        public void Attach(IAxisDrive drive) {
        }

        public void Detach(IAxisDrive drive) {
        }
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
        public int WriteSpeedCalls { get; private set; }
        private decimal? _lastFeedbackMmps;

        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        public AxisId Axis { get; }
        public DriverStatus Status => DriverStatus.Connected;
        public decimal? LastTargetMmps { get; private set; }
        public decimal? LastFeedbackMmps => _lastFeedbackMmps;
        public bool IsEnabled => true;
        public int LastErrorCode => 0;
        public string? LastErrorMessage => null;
        public decimal? MaxLinearMmps => 1000m;
        public decimal? MaxAccelMmps2 => 500m;
        public decimal? MaxDecelMmps2 => 500m;
        public AxisType AxisType { get; set; } = AxisType.Main;
        public int? Ppr => 10000;

        public ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default) {
            LastTargetMmps = rpm.Value;
            WriteSpeedCalls++;
            return ValueTask.CompletedTask;
        }

        public Task WriteSpeedAsync(decimal mmPerSec, CancellationToken ct = default) {
            LastTargetMmps = mmPerSec;
            WriteSpeedCalls++;
            return Task.CompletedTask;
        }

        public Task SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) => Task.CompletedTask;

        public Task SetAccelDecelByLinearAsync(decimal accelMmPerSec, decimal decelMmPerSec,
            CancellationToken ct = default) => Task.CompletedTask;

        public ValueTask StopAsync(CancellationToken ct = default) {
            StopCalls++;
            return ValueTask.CompletedTask;
        }

        public Task EnableAsync(CancellationToken ct = default) => Task.CompletedTask;

        public ValueTask DisableAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

        public Task UpdateLinearLimitsAsync(decimal maxLinearMmps, decimal maxAccelMmps2, decimal maxDecelMmps2, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateMechanicsAsync(decimal rollerDiameterMm, decimal gearRatio, int ppr, CancellationToken ct = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void SimulateSpeedFeedback(decimal speedMmps) {
            _lastFeedbackMmps = speedMmps;
        }
    }
}