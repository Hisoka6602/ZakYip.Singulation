namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 故障诊断结果数据传输对象
    /// </summary>
    public sealed record class FaultDiagnosisDto {
        /// <summary>
        /// 诊断 ID
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        /// 轴标识符（如果故障与特定轴相关）
        /// </summary>
        public string? AxisId { get; init; }

        /// <summary>
        /// 故障类型
        /// </summary>
        public required string FaultType { get; init; }

        /// <summary>
        /// 故障严重程度
        /// </summary>
        public FaultSeverity Severity { get; init; }

        /// <summary>
        /// 故障描述
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        /// 可能原因列表
        /// </summary>
        public List<string> PossibleCauses { get; init; } = new();

        /// <summary>
        /// 解决建议列表
        /// </summary>
        public List<string> Suggestions { get; init; } = new();

        /// <summary>
        /// 诊断时间
        /// </summary>
        public DateTime DiagnosedAt { get; init; }

        /// <summary>
        /// 是否已解决
        /// </summary>
        public bool IsResolved { get; init; }

        /// <summary>
        /// 相关错误码
        /// </summary>
        public int? ErrorCode { get; init; }

        /// <summary>
        /// 原始错误消息（来自驱动器）
        /// </summary>
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// 故障严重程度
    /// </summary>
    public enum FaultSeverity {
        /// <summary>信息</summary>
        Info = 0,
        /// <summary>警告</summary>
        Warning = 1,
        /// <summary>错误</summary>
        Error = 2,
        /// <summary>严重</summary>
        Critical = 3
    }
}
