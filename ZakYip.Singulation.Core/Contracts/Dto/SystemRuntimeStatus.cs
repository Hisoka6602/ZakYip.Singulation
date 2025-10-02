using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 系统运行状态——单一事实来源（SSOT）
    /// 对外 API、内部监控、前端显示都用它
    /// </summary>
    public sealed record class SystemRuntimeStatus {
        public DateTime UptimeUtc { get; init; } = DateTime.UtcNow;

        // ---- Transport / 上游 ----
        public List<TransportStatusItem> Transports { get; init; } = new();

        /// <summary>上游心跳时间（UTC）</summary>
        public DateTime? UpstreamHeartbeatUtc { get; init; }

        /// <summary>上游近1分钟吞吐（fps/pps等）</summary>
        public double? UpstreamFps { get; init; }

        // ---- 轴 / 控制器 ----
        public int AxisCount { get; init; }

        public bool ControllerOnline { get; init; }
        public string? ControllerVendor { get; init; }
        public string? ControllerIp { get; init; }

        // ---- 规划器/算法（如果后续接入）----
        public string? PlannerState { get; init; }

        public double? PlannerLatencyMsP50 { get; init; }
        public double? PlannerLatencyMsP95 { get; init; }

        // ---- 扩展信息（便于前端展示）----
        public Dictionary<string, string>? Extras { get; init; }
    }

    public sealed class TransportStatusItem {
        public string Name { get; init; } = string.Empty;     // 如 "upstream.tcp"
        public string Role { get; init; } = "Server";          // Server/Client
        public string Status { get; init; } = "Stopped";       // Stopped/Starting/Running
        public string? Remote { get; init; }                   // "192.168.1.23:5100"
        public DateTime? LastStateChangedUtc { get; init; }
        public long ReceivedBytes { get; init; }
    }
}