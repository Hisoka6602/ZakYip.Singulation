namespace ZakYip.Singulation.Core.Configs {
    /// <summary>
    /// 故障诊断记录
    /// </summary>
    public sealed class FaultDiagnosisRecord {
        /// <summary>
        /// 诊断 ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 轴标识符（如果故障与特定轴相关）
        /// </summary>
        public string? AxisId { get; set; }

        /// <summary>
        /// 故障类型
        /// </summary>
        public required string FaultType { get; set; }

        /// <summary>
        /// 故障严重程度 (0=Info, 1=Warning, 2=Error, 3=Critical)
        /// </summary>
        public int Severity { get; set; }

        /// <summary>
        /// 故障描述
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// 可能原因列表（JSON 序列化）
        /// </summary>
        public string PossibleCausesJson { get; set; } = "[]";

        /// <summary>
        /// 解决建议列表（JSON 序列化）
        /// </summary>
        public string SuggestionsJson { get; set; } = "[]";

        /// <summary>
        /// 诊断时间
        /// </summary>
        public DateTime DiagnosedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否已解决
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// 解决时间
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// 相关错误码
        /// </summary>
        public int? ErrorCode { get; set; }

        /// <summary>
        /// 原始错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 故障知识库条目
    /// </summary>
    public sealed class FaultKnowledgeEntry {
        /// <summary>
        /// 条目 ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 错误码或错误模式
        /// </summary>
        public required string ErrorPattern { get; set; }

        /// <summary>
        /// 故障类型
        /// </summary>
        public required string FaultType { get; set; }

        /// <summary>
        /// 故障描述
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// 可能原因列表（JSON 序列化）
        /// </summary>
        public string PossibleCausesJson { get; set; } = "[]";

        /// <summary>
        /// 解决建议列表（JSON 序列化）
        /// </summary>
        public string SuggestionsJson { get; set; } = "[]";

        /// <summary>
        /// 严重程度
        /// </summary>
        public int Severity { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
