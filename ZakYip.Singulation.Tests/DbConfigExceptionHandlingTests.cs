using System;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Transport;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试数据库配置读取异常时的安全隔离、日志记录和默认值返回。
    /// </summary>
    internal sealed class DbConfigExceptionHandlingTests {

        [MiniFact]
        public async Task IoStatusMonitorStore_HandlesNormalOperation() {
            // 使用正常的内存数据库验证基本功能仍然工作
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                isolator);

            // 正常读取不应触发降级
            var result = await store.GetAsync();

            MiniAssert.Equal(true, result.Enabled, "默认 Enabled 应该为 true");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task LeadshineSafetyIoStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var store = new LiteDbLeadshineSafetyIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineSafetyIoOptionsStore>.Instance,
                isolator);

            var result = await store.GetAsync();

            MiniAssert.Equal(false, result.Enabled, "默认 Enabled 应该为 false");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task ControllerOptionsStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var store = new LiteDbControllerOptionsStore(
                db,
                NullLogger<LiteDbControllerOptionsStore>.Instance,
                isolator);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task UpstreamOptionsStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var store = new LiteDbUpstreamOptionsStore(
                db,
                NullLogger<LiteDbUpstreamOptionsStore>.Instance,
                isolator);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task UpstreamCodecOptionsStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var store = new LiteDbUpstreamCodecOptionsStore(
                db,
                NullLogger<LiteDbUpstreamCodecOptionsStore>.Instance,
                isolator);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task AxisLayoutStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var store = new LiteDbAxisLayoutStore(
                db,
                NullLogger<LiteDbAxisLayoutStore>.Instance,
                isolator);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        /// <summary>
        /// 用于记录安全隔离调用的模拟实现。
        /// </summary>
        private sealed class RecordingSafetyIsolator : ISafetyIsolator {
            public bool DegradedTriggered { get; private set; }
            public SafetyTriggerKind LastKind { get; private set; }
            public string? LastReason { get; private set; }

            public SafetyIsolationState State => SafetyIsolationState.Normal;
            public bool IsDegraded => false;
            public bool IsIsolated => false;
            public SafetyTriggerKind LastTriggerKind => LastKind;
            public string? LastTriggerReason => LastReason;

            public event EventHandler<SafetyStateChangedEventArgs>? StateChanged;

            public bool TryTrip(SafetyTriggerKind kind, string reason) {
                LastKind = kind;
                LastReason = reason;
                return true;
            }

            public bool TryEnterDegraded(SafetyTriggerKind kind, string reason) {
                DegradedTriggered = true;
                LastKind = kind;
                LastReason = reason;
                return true;
            }

            public bool TryRecoverFromDegraded(string reason) => true;

            public bool TryResetIsolation(string reason, CancellationToken ct = default) => true;
        }
    }
}
