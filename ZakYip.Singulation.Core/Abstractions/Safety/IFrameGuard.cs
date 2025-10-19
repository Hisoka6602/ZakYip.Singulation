using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Core.Abstractions.Safety {

    /// <summary>
    /// 速度帧保护器：在下发速度前做滑窗、降级与监控。
    /// </summary>
    public interface IFrameGuard : IAsyncDisposable {
        ValueTask<bool> InitializeAsync(CancellationToken ct);

        FrameGuardDecision Evaluate(SpeedSet set);

        void ReportHeartbeat();
    }

    public readonly record struct FrameGuardDecision(bool ShouldApply, SpeedSet Output, bool DegradedApplied, string? Reason);
}
