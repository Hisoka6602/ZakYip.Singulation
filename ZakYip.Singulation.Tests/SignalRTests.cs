using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Host.SignalR;
using ZakYip.Singulation.Host.SignalR.Hubs;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// SignalR 功能测试集。
    /// </summary>
    internal sealed class SignalRTests {

        [MiniFact]
        public void MessageEnvelope_Reset_ClearsAllFields() {
            var envelope = new MessageEnvelope {
                Version = 2,
                Type = "TestType",
                Timestamp = DateTimeOffset.UtcNow,
                Channel = "/test",
                Data = new { test = "data" },
                TraceId = "trace-123",
                Sequence = 42
            };

            envelope.Reset();

            MiniAssert.Equal(1, envelope.Version, "Version should reset to 1");
            MiniAssert.Null(envelope.Type, "Type should reset to null");
            MiniAssert.Equal(default(DateTimeOffset), envelope.Timestamp, "Timestamp should reset to default");
            MiniAssert.Equal(string.Empty, envelope.Channel, "Channel should reset to empty string");
            MiniAssert.Null(envelope.TraceId, "TraceId should reset to null");
            MiniAssert.Equal(0, envelope.Sequence, "Sequence should reset to 0");
        }

        [MiniFact]
        public void MessageEnvelopePoolPolicy_Create_ReturnsNewInstance() {
            var policy = new MessageEnvelopePoolPolicy();
            var envelope = policy.Create();

            MiniAssert.NotNull(envelope, "Create should return a new instance");
            MiniAssert.Equal(1, envelope.Version, "Default version should be 1");
        }

        [MiniFact]
        public void MessageEnvelopePoolPolicy_Return_ResetsObject() {
            var policy = new MessageEnvelopePoolPolicy();
            var envelope = new MessageEnvelope {
                Type = "TestType",
                Channel = "/test",
                Sequence = 42
            };

            var result = policy.Return(envelope);

            MiniAssert.True(result, "Return should succeed");
            MiniAssert.Null(envelope.Type, "Type should be reset after return");
            MiniAssert.Equal(string.Empty, envelope.Channel, "Channel should be reset after return");
            MiniAssert.Equal(0, envelope.Sequence, "Sequence should be reset after return");
        }

        [MiniFact]
        public void SignalRQueueItem_Constructor_SetsProperties() {
            var payload = new { test = "data" };
            var item = new SignalRQueueItem("/test", payload);

            MiniAssert.Equal("/test", item.Channel, "Channel should be set");
            MiniAssert.Equal(payload, item.Payload, "Payload should be set");
        }

        [MiniFact]
        public async Task EventsHub_Ping_ReturnsCompletedTask() {
            var hub = new EventsHub();
            var task = hub.Ping();

            MiniAssert.NotNull(task, "Ping should return a task");
            MiniAssert.True(task.IsCompleted, "Ping should return a completed task");
            await task; // Should not throw
        }

        [MiniFact]
        public void RealtimeDispatchService_GetStatistics_ReturnsInitialValues() {
            var channel = Channel.CreateBounded<SignalRQueueItem>(10);
            var hub = CreateMockHubContext();
            var logger = CreateMockLogger();

            var service = new RealtimeDispatchService(channel, hub, logger);
            var stats = service.GetStatistics();

            MiniAssert.Equal(0L, stats.Processed, "Initial processed count should be 0");
            MiniAssert.Equal(0L, stats.Dropped, "Initial dropped count should be 0");
            MiniAssert.Equal(0L, stats.Failed, "Initial failed count should be 0");
        }

        [MiniFact]
        public async Task SignalRHealthCheck_CheckHealthAsync_ReturnsHealthyWhenQueueLow() {
            var channel = Channel.CreateBounded<SignalRQueueItem>(50000);
            var hub = CreateMockHubContext();
            var logger = CreateMockLogger();
            var service = new RealtimeDispatchService(channel, hub, logger);
            var healthCheck = new SignalRHealthCheck(hub, channel, service);

            var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

            MiniAssert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, 
                result.Status, "Health check should return Healthy when queue is low");
        }

        [MiniFact]
        public async Task SignalRHealthCheck_CheckHealthAsync_ReturnsDegradedWhenQueueHigh() {
            var channel = Channel.CreateBounded<SignalRQueueItem>(50000);
            // Fill the queue to > 40000
            for (int i = 0; i < 45000; i++) {
                await channel.Writer.WriteAsync(new SignalRQueueItem("/test", new { }));
            }

            var hub = CreateMockHubContext();
            var logger = CreateMockLogger();
            var service = new RealtimeDispatchService(channel, hub, logger);
            var healthCheck = new SignalRHealthCheck(hub, channel, service);

            var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

            MiniAssert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, 
                result.Status, "Health check should return Degraded when queue is high");
        }

        // Mock helpers - intentionally simple for these focused unit tests
        private static IHubContext<EventsHub> CreateMockHubContext() {
            return new MockHubContext();
        }

        private static ILogger<RealtimeDispatchService> CreateMockLogger() {
            return new MockLogger();
        }

        // Simple mock implementations
        private sealed class MockHubContext : IHubContext<EventsHub> {
            public IHubClients Clients => new MockHubClients();
            public IGroupManager Groups => new MockGroupManager();
        }

        private sealed class MockHubClients : IHubClients {
            public IClientProxy All => new MockClientProxy();
            public IClientProxy AllExcept(System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new MockClientProxy();
            public IClientProxy Client(string connectionId) => new MockClientProxy();
            public IClientProxy Clients(System.Collections.Generic.IReadOnlyList<string> connectionIds) => new MockClientProxy();
            public IClientProxy Group(string groupName) => new MockClientProxy();
            public IClientProxy GroupExcept(string groupName, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new MockClientProxy();
            public IClientProxy Groups(System.Collections.Generic.IReadOnlyList<string> groupNames) => new MockClientProxy();
            public IClientProxy User(string userId) => new MockClientProxy();
            public IClientProxy Users(System.Collections.Generic.IReadOnlyList<string> userIds) => new MockClientProxy();
        }

        private sealed class MockClientProxy : IClientProxy {
            public Task SendCoreAsync(string method, object?[] args, System.Threading.CancellationToken cancellationToken = default) {
                return Task.CompletedTask;
            }
        }

        private sealed class MockGroupManager : IGroupManager {
            public Task AddToGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default) {
                return Task.CompletedTask;
            }

            public Task RemoveFromGroupAsync(string connectionId, string groupName, System.Threading.CancellationToken cancellationToken = default) {
                return Task.CompletedTask;
            }
        }

        private sealed class MockLogger : ILogger<RealtimeDispatchService> {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
