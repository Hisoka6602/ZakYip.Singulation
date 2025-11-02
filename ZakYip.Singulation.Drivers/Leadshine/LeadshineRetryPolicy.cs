using Polly;
using System;
using Polly.Retry;

namespace ZakYip.Singulation.Drivers.Leadshine {

    /// <summary>
    /// 雷赛轴使能/失能重试策略：失败时重试最多3次，无等待时间。
    /// 用于 EnableAsync 和 DisableAsync 操作。
    /// </summary>
    public static class LeadshineRetryPolicy {

        /// <summary>
        /// 创建雷赛使能/失能操作的重试管线。
        /// 重试策略：失败时立即重试，最多3次重试（总共4次尝试），无等待时间。
        /// </summary>
        /// <returns>重试管线。</returns>
        public static ResiliencePipeline BuildEnableDisableRetryPipeline() {
            var retry = new RetryStrategyOptions {
                ShouldHandle = new PredicateBuilder()
                    .Handle<Exception>(),
                MaxRetryAttempts = 3,  // 重试3次（加上初始尝试，总共4次）
                Delay = TimeSpan.Zero, // 无等待时间
                BackoffType = DelayBackoffType.Constant
            };

            return new ResiliencePipelineBuilder()
                .AddRetry(retry)
                .Build();
        }
    }
}
