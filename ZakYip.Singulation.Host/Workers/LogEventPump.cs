using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Host.Runtime;
using ZakYip.Singulation.Core.Contracts.Events;

namespace ZakYip.Singulation.Host.Workers {

    /// <summary>
    /// 日志泵：合并/节流非异常日志，统一落到 NLog（ILogger）与实时出口（SignalR）。
    /// - 合并策略：同 Category+Message 在节流窗口内聚合计数与最新时间；
    /// - 节流窗口：默认 100ms；
    /// - 异常日志不走这里，直接 ILogger 立即写盘（在调用处保持原样）。
    /// </summary>
    public sealed class LogEventPump : BackgroundService {
        private readonly ILogger<LogEventPump> _log;
        private readonly LogEventBus _bus;

        // 合并桶：key = $"{Category}|{Message}"
        private readonly ConcurrentDictionary<string, (LogEvent ev, int count)> _bucket = new();

        // 输出节流：每间隔 flushMs 刷一次
        private readonly int _flushMs = 100;

        private static readonly string[] PropWhitelist =
            { "channel", "axisId", "port", "dropped", "len", "codec", "frameLen" };

        private static IReadOnlyDictionary<string, object> SanitizeProps(
            IReadOnlyDictionary<string, object>? src,
            int maxItems = 12,
            int maxLen = 128) {
            var dst = new Dictionary<string, object>(capacity: Math.Min(maxItems + 2, 16));
            if (src != null) {
                int count = 0;
                foreach (var kv in src) {
                    if (PropWhitelist.Length > 0 && !PropWhitelist.Contains(kv.Key))
                        continue;

                    var v = kv.Value?.ToString() ?? string.Empty;
                    if (v.Length > maxLen) v = v[..maxLen] + "…";
                    dst[kv.Key] = v;

                    if (++count >= maxItems) break;
                }
            }
            return dst;
        }

        public LogEventPump(ILogger<LogEventPump> log, LogEventBus bus) {
            _log = log;
            _bus = bus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var reader = _bus.Reader;

            // 独立刷写循环
            var flushTask = Task.Run(() => FlushLoop(stoppingToken), stoppingToken);

            await foreach (var ev in reader.ReadAllAsync(stoppingToken)) {
                var key = ev.Category + "|" + ev.Message;
                _bucket.AddOrUpdate(key,
                    _ => (ev, 1),
                    (_, old) => (ev, old.count + 1));
            }

            // reader 完成后，做一次最终 flush
            FlushOnce();
            await flushTask;
        }

        private async Task FlushLoop(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                await Task.Delay(_flushMs, ct).ConfigureAwait(false);
                FlushOnce();
            }
        }

        private void FlushOnce() {
            if (_bucket.IsEmpty) return;

            foreach (var kv in _bucket.ToArray()) {
                if (_bucket.TryRemove(kv.Key, out var tuple)) {
                    var ev = tuple.ev;
                    var times = tuple.count;

                    // 1) 处理 props：白名单 + 截断，避免超长行
                    var props = SanitizeProps(ev.Props);
                    // 增补用于路由和常用字段
                    var scopeDict = new Dictionary<string, object>(props) {
                        ["category"] = ev.Category,
                        // 你也可以把 Message 的摘要放进去便于检索
                    };

                    // 2) 把属性放进 Scope，供 NLog 的 ${event-properties:*} 使用
                    using (_log.BeginScope(scopeDict)) {
                        // 3) 精简消息体，不再输出 {@props}，减少行长
                        switch (ev.Kind) {
                            case LogKind.Info:
                                _log.LogInformation("[{cat}] {msg} x{times}",
                                    ev.Category, ev.Message, times);
                                break;

                            case LogKind.Debug:
                                if (_log.IsEnabled(LogLevel.Debug))
                                    _log.LogDebug("[{cat}] {msg} x{times}", ev.Category, ev.Message, times);
                                break;

                            case LogKind.Warn:
                                _log.LogWarning("[{cat}] {msg} x{times}", ev.Category, ev.Message, times);
                                break;
                        }
                    }

                    // 如需推到 SignalR，可以用 props（已截断）：
                    // _notifier.PublishLog(ev.Category, ev.Message, times, scopeDict);
                }
            }
        }
    }
}