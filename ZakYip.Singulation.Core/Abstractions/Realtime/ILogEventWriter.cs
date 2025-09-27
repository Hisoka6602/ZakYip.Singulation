using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Events;

namespace ZakYip.Singulation.Core.Abstractions.Realtime {

    /// <summary>
    /// 非异常实时日志的“写侧”接口：尽量无阻塞、可被高频调用。
    /// </summary>
    public interface ILogEventWriter {

        /// <summary>尝试写入（数据面日志用）。</summary>
        bool TryWrite(LogEvent ev);

        /// <summary>关键日志（非异常）背压写入，不丢。</summary>
        ValueTask WriteAsync(LogEvent ev, CancellationToken ct = default);
    }
}