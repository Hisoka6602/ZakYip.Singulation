using LiteDB;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {
    /// <summary>
    /// 持久化在 LiteDB 中的上游编解码配置文档。
    /// </summary>
    public sealed record class UpstreamCodecOptionsDoc {
        /// <summary>文档主键（固定为 "default"，确保仅有一份配置）。</summary>
        public BsonValue Id { get; set; } = "default";

        /// <summary>主分离段的轴数量。</summary>
        public int MainCount { get; set; }

        /// <summary>疏散段的轴数量。</summary>
        public int EjectCount { get; set; }
    }
}
