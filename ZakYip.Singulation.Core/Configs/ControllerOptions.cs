using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {
    /// <summary>
    /// 控制器模板配置对象，用于持久化控制器的厂商标识与驱动参数。
    /// </summary>
    public sealed record class ControllerOptions {
        /// <summary>
        /// 驱动厂商标识，指定控制器所使用的驱动类型，例如 "leadshine"。
        /// </summary>
        [Required(ErrorMessage = "驱动厂商标识不能为空")]
        [StringLength(50, ErrorMessage = "驱动厂商标识长度不能超过 50 个字符")]
        public required string Vendor { get; init; }

        /// <summary>
        /// 覆盖轴数量的可选值。若未设置，则由总线自动发现轴数量。
        /// </summary>
        [Range(1, 128, ErrorMessage = "覆盖轴数量必须在 1 到 128 之间")]
        public int? OverrideAxisCount { get; init; }

        /// <summary>
        /// 控制器的网络连接 IP 地址，用于建立通信连接。
        /// </summary>
        [Required(ErrorMessage = "控制器连接 IP 不能为空")]
        [StringLength(15, ErrorMessage = "IP 地址长度不能超过 15 个字符")]
        public required string ControllerIp { get; init; } = "192.168.5.11";

        /// <summary>
        /// 驱动参数模板，包含控制器的详细驱动配置信息。
        /// </summary>
        public DriverOptionsTemplateOptions Template { get; init; } = new();
    }
}
