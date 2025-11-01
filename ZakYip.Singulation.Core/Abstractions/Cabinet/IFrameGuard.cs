using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Core.Abstractions.Cabinet {

    /// <summary>
    /// 速度帧保护器：在下发速度前做滑窗、降级与监控。
    /// </summary>
    public interface IFrameGuard : IAsyncDisposable {
        /// <summary>执行初始化逻辑，如读取配置与拉起后台任务。</summary>
        ValueTask<bool> InitializeAsync(CancellationToken ct);

        /// <summary>评估一帧速度数据并给出处理决策。</summary>
        FrameGuardDecision Evaluate(SpeedSet set);

        /// <summary>报告心跳，保持降级/超时逻辑一致。</summary>
        void ReportHeartbeat();
    }
}
