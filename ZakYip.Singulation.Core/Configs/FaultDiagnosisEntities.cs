namespace ZakYip.Singulation.Core.Configs {
    /// <summary>
    /// 故障诊断记录
    /// </summary>
    public sealed class FaultDiagnosisRecord {
        /// <summary>
        /// 诊断 ID
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 轴标识符（如果故障与特定轴相关）
        /// </summary>
        public string? AxisId { get; init; }

        /// <summary>
        /// 故障类型
        /// </summary>
        public required string FaultType { get; init; }

        /// <summary>
        /// 故障严重程度 (0=Info, 1=Warning, 2=Error, 3=Critical)
        /// </summary>
        public int Severity { get; init; }

        /// <summary>
        /// 故障描述
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        /// 可能原因列表（JSON 序列化）
        /// </summary>
        public string PossibleCausesJson { get; init; } = "[]";

        /// <summary>
        /// 解决建议列表（JSON 序列化）
        /// </summary>
        public string SuggestionsJson { get; init; } = "[]";

        /// <summary>
        /// 诊断时间
        /// </summary>
        public DateTime DiagnosedAt { get; init; }

        /// <summary>
        /// 是否已解决
        /// </summary>
        public bool IsResolved { get; init; }

        /// <summary>
        /// 解决时间
        /// </summary>
        public DateTime? ResolvedAt { get; init; }

        /// <summary>
        /// 相关错误码
        /// </summary>
        public int? ErrorCode { get; init; }

        /// <summary>
        /// 原始错误消息
        /// </summary>
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// 故障知识库条目
    /// </summary>
    public sealed class FaultKnowledgeEntry {
        /// <summary>
        /// 条目 ID
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 错误码或错误模式
        /// </summary>
        public required string ErrorPattern { get; init; }

        /// <summary>
        /// 故障类型
        /// </summary>
        public required string FaultType { get; init; }

        /// <summary>
        /// 故障描述
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        /// 可能原因列表（JSON 序列化）
        /// </summary>
        public string PossibleCausesJson { get; init; } = "[]";

        /// <summary>
        /// 解决建议列表（JSON 序列化）
        /// </summary>
        public string SuggestionsJson { get; init; } = "[]";

        /// <summary>
        /// 严重程度
        /// </summary>
        public int Severity { get; init; }

        /// <summary>
        /// 创建时间
        /// </summary>
        /// <remarks>
        /// 使用 { get; set; } 而非 { get; init; } 是有意为之。
        /// 原因：InitializeKnowledgeBase() 方法在对象创建后批量设置时间戳。
        /// Intentionally mutable for post-initialization timestamp setting in InitializeKnowledgeBase().
        /// </remarks>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        /// <remarks>
        /// 使用 { get; set; } 而非 { get; init; } 是有意为之。
        /// 原因：InitializeKnowledgeBase() 方法在对象创建后批量设置时间戳。
        /// Intentionally mutable for post-initialization timestamp setting in InitializeKnowledgeBase().
        /// </remarks>
        public DateTime UpdatedAt { get; set; }
    }
}
