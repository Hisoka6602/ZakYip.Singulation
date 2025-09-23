using Polly;
using System;
using System.Linq;
using System.Text;
using Polly.Retry;
using Polly.CircuitBreaker;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Resilience {

    /// <summary>
    /// 仅用于“观察失败并触发降级/恢复”的管线：
    /// - 不对 I/O 写命令做重试（MaxRetryAttempts=0）
    /// - 连续失败达到阈值 → 打开断路器（降级回调）
    /// - 成功一次 → 关闭断路器（恢复回调）
    /// </summary>
    public static class AxisDegradePolicy {

        public static ResiliencePipeline<short> BuildPdoPipeline(
            int consecutiveFailThreshold,
            Action onDegraded,
            Action onRecovered) {
            // 失败判定：ret!=0 或异常
            var shouldHandle = new PredicateBuilder<short>()
                .HandleResult(ret => ret != 0)
                .Handle<Exception>();

            // 0 次重试：我们只想把 Outcome 喂给断路器，不做重试放大
            var retry = new RetryStrategyOptions<short> {
                ShouldHandle = shouldHandle,
                MaxRetryAttempts = 0
            };

            // 断路器：用“采样窗口内失败率=1.0 且吞吐量≥阈值”来等价“连续 N 次失败”
            // 采样窗口给小一点（比如 2s），把 MinimumThroughput 设成 N
            var breaker = new CircuitBreakerStrategyOptions<short> {
                ShouldHandle = shouldHandle,
                FailureRatio = 1.0,                         // 全失败
                MinimumThroughput = Math.Max(1, consecutiveFailThreshold),
                SamplingDuration = TimeSpan.FromSeconds(2), // 窗口
                BreakDuration = TimeSpan.FromSeconds(2),    // 打开后稍等一会再试
                OnOpened = _ => { onDegraded(); return default; },
                OnClosed = _ => { onRecovered(); return default; }
            };

            return new ResiliencePipelineBuilder<short>()
                //不重试
                .AddCircuitBreaker(breaker)
                .Build();
        }
    }
}