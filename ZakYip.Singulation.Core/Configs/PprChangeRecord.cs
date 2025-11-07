namespace ZakYip.Singulation.Core.Configs {
    /// <summary>
    /// PPR 变化记录实体
    /// </summary>
    public sealed class PprChangeRecord {
        /// <summary>
        /// 记录 ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 轴标识符
        /// </summary>
        public required string AxisId { get; set; }

        /// <summary>
        /// 旧 PPR 值
        /// </summary>
        public int OldPpr { get; set; }

        /// <summary>
        /// 新 PPR 值
        /// </summary>
        public int NewPpr { get; set; }

        /// <summary>
        /// 变化原因
        /// </summary>
        public required string Reason { get; set; }

        /// <summary>
        /// 变化时间
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否异常变化（未经授权或非预期的变化）
        /// </summary>
        public bool IsAnomalous { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Notes { get; set; }
    }
}
