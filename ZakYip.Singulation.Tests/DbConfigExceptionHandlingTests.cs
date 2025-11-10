using System;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts.Events.Cabinet;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Persistence;
using ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine;
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
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbIoStatusMonitorOptionsStore(
                db,
                NullLogger<LiteDbIoStatusMonitorOptionsStore>.Instance,
                isolator,
                cache);

            // 正常读取不应触发降级
            var result = await store.GetAsync();

            MiniAssert.Equal(true, result.Enabled, "默认 Enabled 应该为 true");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task LeadshineSafetyIoStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbLeadshineCabinetIoOptionsStore(
                db,
                NullLogger<LiteDbLeadshineCabinetIoOptionsStore>.Instance,
                isolator,
                cache);

            var result = await store.GetAsync();

            MiniAssert.Equal(false, result.Enabled, "默认 Enabled 应该为 false");
            MiniAssert.False(isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task ControllerOptionsStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var isolator = new RecordingSafetyIsolator();
            var store = new LiteDbControllerOptionsStore(
                db,
                NullLogger<LiteDbControllerOptionsStore>.Instance,
                isolator,
                cache);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task UpstreamOptionsStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbUpstreamOptionsStore(
                db,
                NullLogger<LiteDbUpstreamOptionsStore>.Instance,
                isolator,
                cache);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task UpstreamCodecOptionsStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbUpstreamCodecOptionsStore(
                db,
                NullLogger<LiteDbUpstreamCodecOptionsStore>.Instance,
                isolator,
                cache);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        [MiniFact]
        public async Task AxisLayoutStore_HandlesNormalOperation() {
            using var db = new LiteDatabase(":memory:");
            var isolator = new RecordingSafetyIsolator();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var store = new LiteDbAxisLayoutStore(
                db,
                NullLogger<LiteDbAxisLayoutStore>.Instance,
                isolator,
                cache);

            var result = await store.GetAsync();

            MiniAssert.True(result != null, "应返回配置对象");
            MiniAssert.True(!isolator.DegradedTriggered, "正常读取不应触发降级");
        }

        /// <summary>
        /// 用于记录安全隔离调用的模拟实现。
        /// </summary>
        private sealed class RecordingSafetyIsolator : ICabinetIsolator {
            public bool DegradedTriggered { get; private set; }
            public CabinetTriggerKind LastKind { get; private set; }
            public string? LastReason { get; private set; }

            public CabinetIsolationState State => CabinetIsolationState.Normal;
            public bool IsDegraded => false;
            public bool IsIsolated => false;
            public CabinetTriggerKind LastTriggerKind => LastKind;
            public string? LastTriggerReason => LastReason;

            public event EventHandler<CabinetStateChangedEventArgs>? StateChanged;

            public bool TryTrip(CabinetTriggerKind kind, string reason) {
                LastKind = kind;
                LastReason = reason;
                return true;
            }

            public bool TryEnterDegraded(CabinetTriggerKind kind, string reason) {
                DegradedTriggered = true;
                LastKind = kind;
                LastReason = reason;
                return true;
            }

            public bool TryRecoverFromDegraded(string reason) => true;

            public bool TryResetIsolation(string reason, CancellationToken ct = default) => true;

            // 实现新的安全执行方法
            public bool SafeExecute(Action action, string operationName, Action<Exception>? onError = null) {
                try { action(); return true; } catch (Exception ex) { onError?.Invoke(ex); return false; }
            }

            public T SafeExecute<T>(Func<T> func, string operationName, T defaultValue, Action<Exception>? onError = null) {
                try { return func(); } catch (Exception ex) { onError?.Invoke(ex); return defaultValue; }
            }

            public T? SafeExecuteNullable<T>(Func<T> func, string operationName, Action<Exception>? onError = null) where T : class {
                try { return func(); } catch (Exception ex) { onError?.Invoke(ex); return null; }
            }

            public int SafeExecuteBatch(Action[] actions, string operationName, bool stopOnFirstError = false) {
                int successCount = 0;
                foreach (var action in actions) {
                    try { action(); successCount++; } catch { if (stopOnFirstError) break; }
                }
                return successCount;
            }

            public async Task<bool> SafeExecuteAsync(Func<Task> action, string operationName, Action<Exception>? onError = null) {
                try { await action(); return true; } catch (Exception ex) { onError?.Invoke(ex); return false; }
            }

            public async Task<T> SafeExecuteAsync<T>(Func<Task<T>> func, string operationName, T defaultValue, Action<Exception>? onError = null) {
                try { return await func(); } catch (Exception ex) { onError?.Invoke(ex); return defaultValue; }
            }
        }
    }
}
