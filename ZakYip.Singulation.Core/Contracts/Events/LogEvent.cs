using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>实时日志事件（不包含异常）。</summary>
    public sealed record class LogEvent {
        public required LogKind Kind { get; init; }
        public required string Category { get; init; }   // e.g. transport/speed/protocol/axis
        public required string Message { get; init; }
        public DateTime Utc { get; init; } = DateTime.UtcNow;

        /// <summary>结构化字段，如 axisId/port/channel/codec/frameLen 等。</summary>
        public IReadOnlyDictionary<string, object>? Props { get; init; }
    }
}