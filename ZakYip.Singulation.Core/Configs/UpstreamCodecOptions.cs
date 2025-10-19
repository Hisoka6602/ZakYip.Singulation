namespace ZakYip.Singulation.Core.Configs {
    /// <summary>
    /// 上游编解码的轴数量参数，用于初始化分离和疏散段拓扑。
    /// </summary>
    public sealed record class UpstreamCodecOptions {
        /// <summary>主分离段的轴数量。</summary>
        public int MainCount { get; init; } = 28;

        /// <summary>疏散段的轴数量。</summary>
        public int EjectCount { get; init; } = 3;
    }
}
