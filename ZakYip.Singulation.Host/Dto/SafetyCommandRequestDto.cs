namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 安全控制命令通用请求体。
    /// </summary>
    public sealed class SafetyCommandRequestDto {
        /// <summary>指示要执行的命令类型。</summary>
        public ZakYip.Singulation.Core.Enums.SafetyCommand Command { get; set; }

        /// <summary>附带原因说明。</summary>
        public string? Reason { get; set; }
    }
}
