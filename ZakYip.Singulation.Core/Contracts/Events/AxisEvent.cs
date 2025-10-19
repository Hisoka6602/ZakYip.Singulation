using System;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>
    /// 轴侧事件：以域模型为中心的简洁承载，保持零分配（除异常对象本身）。
    /// </summary>
    public readonly record struct AxisEvent {
        /// <summary>事件来源，建议格式 "axis:&lt;id&gt;"、"driver:&lt;lib&gt;" 或 "controller"。</summary>
        public string Source { get; init; }

        /// <summary>事件类型。</summary>
        public AxisEventType Type { get; init; }

        /// <summary>关联的轴标识（可空：驱动库级/控制器级事件时为空）。</summary>
        public AxisId? AxisId { get; init; }

        /// <summary>文本原因（断线/未加载/自检失败等）。</summary>
        public string? Reason { get; init; }

        /// <summary>异常对象（faulted 等），可空。</summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// 通过参数构造轴事件。
        /// </summary>
        public AxisEvent(string source, AxisEventType type, AxisId? axisId, string? reason, Exception? exception) {
            Source = source;
            Type = type;
            AxisId = axisId;
            Reason = reason;
            Exception = exception;
        }
    }
}
