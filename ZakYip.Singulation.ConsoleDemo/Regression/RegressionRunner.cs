using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Host.Runtime;
using ZakYip.Singulation.Host.Safety;
using ZakYip.Singulation.Infrastructure.Safety;
using ZakYip.Singulation.Infrastructure.Telemetry;

namespace ZakYip.Singulation.ConsoleDemo.Regression {

    internal static class RegressionRunner {
        public static async Task RunAsync() {
            using var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Information))
                .ConfigureServices((_, services) => {
                    services.AddLogging(b => b.AddSimpleConsole(options => {
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    }));

                    services.Configure<FrameGuardOptions>(opt => {
                        opt.SequenceWindow = 8;
                        opt.HeartbeatTimeout = TimeSpan.FromSeconds(2);
                        opt.DegradeScale = 0.5m;
                    });

                    services.AddSingleton<IRealtimeNotifier, ConsoleRealtimeNotifier>();
                    services.AddSingleton<IAxisEventAggregator, AxisEventAggregator>();
                    services.AddSingleton<IAxisController, FakeAxisController>();
                    services.AddSingleton<IUpstreamFrameHub, UpstreamFrameHub>();
                    services.AddSingleton<IUpstreamOptionsStore, FakeUpstreamOptionsStore>();
                    services.AddSingleton<ISafetyIsolator, SafetyIsolator>();
                    services.AddSingleton<LoopbackSafetyIoModule>();
                    services.AddSingleton<ISafetyIoModule>(sp => sp.GetRequiredService<LoopbackSafetyIoModule>());
                    services.AddSingleton<FrameGuard>();
                    services.AddSingleton<IFrameGuard>(sp => sp.GetRequiredService<FrameGuard>());
                    services.AddSingleton<SafetyPipeline>();
                    services.AddSingleton<ISafetyPipeline>(sp => sp.GetRequiredService<SafetyPipeline>());
                    services.AddHostedService(sp => sp.GetRequiredService<SafetyPipeline>());
                })
                .Build();

            await host.StartAsync();

            var services = host.Services;
            var pipeline = services.GetRequiredService<ISafetyPipeline>();
            var io = services.GetRequiredService<LoopbackSafetyIoModule>();
            var guard = services.GetRequiredService<IFrameGuard>();
            var controller = (FakeAxisController)services.GetRequiredService<IAxisController>();
            var hub = services.GetRequiredService<IUpstreamFrameHub>();

            Console.WriteLine("[Regression] Initializing FrameGuard...");
            await guard.InitializeAsync(CancellationToken.None);

            using var heartbeatCts = new CancellationTokenSource();
            var heartbeatTask = PumpHeartbeatAsync(hub, heartbeatCts.Token);

            Console.WriteLine("[Regression] Start sequence");
            io.TriggerStart("regression");
            await Task.Delay(500);

            var speeds = new[] { 600, 900, 1200 };
            for (var i = 0; i < speeds.Length; i++) {
                await ApplyFrameAsync(guard, controller, i + 1, speeds[i]);
                await Task.Delay(200);
            }

            Console.WriteLine("[Regression] Stop request");
            io.TriggerStop("regression stop");
            await Task.Delay(500);

            Console.WriteLine("[Regression] Reset and restart");
            io.TriggerReset("regression reset");
            await Task.Delay(200);
            io.TriggerStart("regression restart");
            await Task.Delay(400);

            Console.WriteLine("[Regression] Simulate disconnect");
            pipeline.TryTrip(SafetyTriggerKind.AxisDisconnected, "regression disconnect");
            await Task.Delay(500);
            io.TriggerReset("after disconnect");
            await Task.Delay(200);

            Console.WriteLine("[Regression] Simulate degrade via heartbeat timeout");
            heartbeatCts.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(3));
            await ApplyFrameAsync(guard, controller, 10, 1500);

            Console.WriteLine("[Regression] Heartbeat recovery");
            using var resumeCts = new CancellationTokenSource();
            var resumeTask = PumpHeartbeatAsync(hub, resumeCts.Token);
            await Task.Delay(1000);
            pipeline.TryRecoverFromDegraded("heartbeat resumed");
            await ApplyFrameAsync(guard, controller, 11, 1500);
            resumeCts.Cancel();
            await resumeTask;

            Console.WriteLine("[Regression] Scenario complete");
            await host.StopAsync();
            await heartbeatTask;
        }

        private static async Task PumpHeartbeatAsync(IUpstreamFrameHub hub, CancellationToken ct) {
            try {
                while (!ct.IsCancellationRequested) {
                    hub.PublishHeartbeat(new byte[] { 0x01 });
                    await Task.Delay(500, ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        private static async Task ApplyFrameAsync(IFrameGuard guard, FakeAxisController controller, int sequence, int speed) {
            var set = new SpeedSet(DateTime.UtcNow, sequence, new[] { speed }, Array.Empty<int>());
            var decision = guard.Evaluate(set);
            if (!decision.ShouldApply) {
                Console.WriteLine($"[FrameGuard] Drop seq={sequence} reason={decision.Reason}");
                return;
            }

            controller.Record(decision.Output, decision.DegradedApplied);
            SingulationMetrics.Instance.FrameProcessedCounter.Add(1,
                new KeyValuePair<string, object?>("scenario", "regression"));
        }

        private sealed class ConsoleRealtimeNotifier : IRealtimeNotifier {
            public ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default) {
                Console.WriteLine($"[Realtime] {channel}: {System.Text.Json.JsonSerializer.Serialize(payload)}");
                return ValueTask.CompletedTask;
            }
        }

        private sealed class FakeAxisController : IAxisController {
            private readonly List<string> _log = new();

            public IBusAdapter Bus { get; } = new FakeBusAdapter();

            public IReadOnlyList<IAxisDrive> Drives => Array.Empty<IAxisDrive>();

            public event EventHandler<string>? ControllerFaulted;

            public Task<KeyValuePair<bool, string>> InitializeAsync(string vendor, DriverOptions template, int? overrideAxisCount = null, CancellationToken ct = default)
                => Task.FromResult(new KeyValuePair<bool, string>(true, "fake initialized"));

            public Task EnableAllAsync(CancellationToken ct = default) {
                Console.WriteLine("[Axis] Enable all");
                return Task.CompletedTask;
            }

            public Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default) => Task.CompletedTask;

            public Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default) {
                Console.WriteLine($"[Axis] Write speed all: {mmPerSec} mm/s");
                return Task.CompletedTask;
            }

            public Task StopAllAsync(CancellationToken ct = default) {
                Console.WriteLine("[Axis] Stop all");
                return Task.CompletedTask;
            }

            public Task DisposeAllAsync(CancellationToken ct = default) => Task.CompletedTask;

            public Task ApplySpeedSetAsync(SpeedSet set, CancellationToken ct = default) {
                Record(set, false);
                return Task.CompletedTask;
            }

            public void Record(SpeedSet set, bool degraded) {
                var main = string.Join(',', set.MainMmps);
                var tag = degraded ? "(degraded)" : string.Empty;
                var message = $"seq={set.Sequence} main=[{main}] {tag}";
                _log.Add(message);
                Console.WriteLine($"[Axis] Apply {message}");
            }

            private sealed class FakeBusAdapter : IBusAdapter {
                public bool IsInitialized { get; private set; }

                public Task<KeyValuePair<bool, string>> InitializeAsync(CancellationToken ct = default) {
                    IsInitialized = true;
                    return Task.FromResult(new KeyValuePair<bool, string>(true, "ok"));
                }

                public Task CloseAsync(CancellationToken ct = default) => Task.CompletedTask;

                public Task<int> GetAxisCountAsync(CancellationToken ct = default) => Task.FromResult(1);

                public Task<int> GetErrorCodeAsync(CancellationToken ct = default) => Task.FromResult(0);

                public Task ResetAsync(CancellationToken ct = default) => Task.CompletedTask;

                public Task WarmResetAsync(CancellationToken ct = default) => Task.CompletedTask;

                public ushort TranslateNodeId(ushort logicalNodeId) => logicalNodeId;

                public bool ShouldReverse(ushort logicalNodeId) => false;
            }
        }

        private sealed class FakeUpstreamOptionsStore : IUpstreamOptionsStore {
            private readonly UpstreamOptions _options = new() {
                HeartbeatPort = 5003, // Non-zero to enable heartbeat monitoring in regression tests
                SpeedPort = 5001,
                PositionPort = 5002
            };

            public Task<UpstreamOptions> GetAsync(CancellationToken ct = default) 
                => Task.FromResult(_options);

            public Task SaveAsync(UpstreamOptions dto, CancellationToken ct = default) {
                // No-op for tests
                return Task.CompletedTask;
            }

            public Task DeleteAsync(CancellationToken ct = default) {
                // No-op for tests
                return Task.CompletedTask;
            }
        }
    }
}
