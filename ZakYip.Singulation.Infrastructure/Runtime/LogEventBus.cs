using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Core.Abstractions.Realtime;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    /// <summary>
    /// 日志事件总线：封装一个有界通道，供写侧快速投递，读侧由 LogEventPump 消费。
    /// </summary>
    public sealed class LogEventBus {
        private readonly Channel<LogEvent> _ch;

        public LogEventBus(int capacity = 4096) {
            _ch = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(capacity) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest // 保最新视图
            });
        }

        public ChannelReader<LogEvent> Reader => _ch.Reader;

        public bool TryWrite(LogEvent ev) => _ch.Writer.TryWrite(ev);

        public ValueTask WriteAsync(LogEvent ev, CancellationToken ct = default)
            => _ch.Writer.WriteAsync(ev, ct);
    }
}