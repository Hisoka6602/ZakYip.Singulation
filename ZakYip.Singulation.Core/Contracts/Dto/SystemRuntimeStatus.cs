using System;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 系统运行状态——单一事实来源（Single Source of Truth）。
    /// 对外 API、内部监控、前端显示均依赖该模型。
    /// </summary>
    public sealed record class SystemRuntimeStatus {
        /// <summary>系统启动的 UTC 时间。</summary>
        public DateTime UptimeUtc { get; init; } = DateTime.UtcNow;

        /// <summary>传输组件的状态列表。</summary>
        public List<TransportStatusItem> Transports { get; init; } = new();

        /// <summary>上游心跳时间（UTC）。</summary>
        public DateTime? UpstreamHeartbeatUtc { get; init; }

        /// <summary>上游近 1 分钟吞吐（fps/pps 等）。</summary>
        public double? UpstreamFps { get; init; }

        /// <summary>当前识别到的轴数量。</summary>
        public int AxisCount { get; init; }

        /// <summary>控制器是否在线。</summary>
        public bool ControllerOnline { get; init; }

        /// <summary>控制器厂商标识。</summary>
        public string? ControllerVendor { get; init; }

        /// <summary>控制器 IP 地址。</summary>
        public string? ControllerIp { get; init; }

        /// <summary>规划器状态描述（若未接入则为空）。</summary>
        public string? PlannerState { get; init; }

        /// <summary>规划器延迟 P50（毫秒）。</summary>
        public double? PlannerLatencyMsP50 { get; init; }

        /// <summary>规划器延迟 P95（毫秒）。</summary>
        public double? PlannerLatencyMsP95 { get; init; }

        /// <summary>扩展信息键值对（用于前端展示）。</summary>
        public Dictionary<string, string>? Extras { get; init; }
    }
}
