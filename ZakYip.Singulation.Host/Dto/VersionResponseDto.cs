namespace ZakYip.Singulation.Host.Dto {
    /// <summary>
    /// 版本信息响应DTO
    /// </summary>
    public sealed record class VersionResponseDto {
        /// <summary>
        /// 当前系统版本号
        /// </summary>
        public required string Version { get; init; }

        /// <summary>
        /// 轴的驱动厂商名称
        /// </summary>
        public required string AxisVendor { get; init; }

        /// <summary>
        /// 上游数据的厂商名称
        /// </summary>
        public required string UpstreamVendor { get; init; }
    }
}
