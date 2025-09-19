using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Transport.Enums;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Host.Transports {
    /// <summary>对外状态快照：给 /status 使用。</summary>
    public sealed record RuntimeStatus {
        public required TransportStatus Transport { get; init; }
        public required PlannerStatus Planner { get; init; }
        public required IReadOnlyDictionary<AxisId, DriverStatus> Drivers { get; init; }
        public required double UpstreamFps { get; init; }
        public required TimeSpan LastPlanLatency { get; init; } // 最近一次规划耗时
    }
}