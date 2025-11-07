namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// PPR 变化记录数据传输对象
    /// </summary>
    public sealed record class PprChangeRecordDto {
        /// <summary>
        /// 记录 ID
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        /// 轴标识符
        /// </summary>
        public required string AxisId { get; init; }

        /// <summary>
        /// 旧 PPR 值
        /// </summary>
        public int OldPpr { get; init; }

        /// <summary>
        /// 新 PPR 值
        /// </summary>
        public int NewPpr { get; init; }

        /// <summary>
        /// 变化原因
        /// </summary>
        public required string Reason { get; init; }

        /// <summary>
        /// 变化时间
        /// </summary>
        public DateTime ChangedAt { get; init; }

        /// <summary>
        /// 是否异常变化
        /// </summary>
        public bool IsAnomalous { get; init; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Notes { get; init; }
    }
}
