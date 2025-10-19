namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 安全控制命令通用请求体。
    /// </summary>
    public sealed class SafetyCommandRequestDto {
        /// <summary>附带原因说明。</summary>
        public string? Reason { get; set; }
    }
}
