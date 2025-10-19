using System;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>实时日志事件（不包含异常）。</summary>
    public sealed record class LogEvent {
        /// <summary>日志级别。</summary>
        public required LogKind Kind { get; init; }

        /// <summary>日志分类，例如 transport/speed/protocol/axis。</summary>
        public required string Category { get; init; }

        /// <summary>日志正文。</summary>
        public required string Message { get; init; }

        /// <summary>生成时间（UTC）。</summary>
        public DateTime Utc { get; init; } = DateTime.UtcNow;

        /// <summary>结构化字段，如 axisId/port/channel/codec/frameLen 等。</summary>
        public IReadOnlyDictionary<string, object>? Props { get; init; }
    }
}
