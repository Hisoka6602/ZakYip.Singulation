using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Drivers.Enums;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>
    /// 轴状态响应数据传输对象，表示单个轴的只读资源快照。
    /// 由控制器在运行时生成，作为 REST API 的响应模型。
    /// </summary>
    public sealed record class AxisResponseDto {
        /// <summary>
        /// 轴标识符，通常为逻辑编号转换后的字符串形式，例如 "1001"、"1002"。
        /// </summary>
        [Required(ErrorMessage = "轴标识符不能为空")]
        public required string AxisId { get; init; }

        /// <summary>
        /// 当前驱动状态，反映驱动的生命周期与健康度。
        /// </summary>
        public DriverStatus Status { get; init; }

        /// <summary>
        /// 最近一次下发的目标线速度，单位为毫米每秒（mm/s）。
        /// 若驱动未下发过速度命令，则该值为 null。
        /// </summary>
        public double? TargetLinearMmps { get; init; }

        /// <summary>
        /// 最近一次反馈的实际线速度，单位为毫米每秒（mm/s）。
        /// 若驱动尚未上报速度反馈，则该值为 null。
        /// </summary>
        public double? FeedbackLinearMmps { get; init; }

        /// <summary>
        /// 驱动是否已使能。
        /// true 表示驱动已处于使能状态，可接受速度命令；
        /// false 表示处于未使能或禁用状态；
        /// null 表示驱动未上报此状态。
        /// </summary>
        public bool? Enabled { get; init; }

        /// <summary>
        /// 最近错误码。
        /// 0 表示正常；
        /// 正数通常为厂商 SDK 返回的错误码；
        /// -1 表示逻辑或参数错误或本地异常；
        /// -2 表示通信掉线或设备不响应。
        /// 若驱动未上报错误，则该值为 null。
        /// </summary>
        public int? LastErrorCode { get; init; }

        /// <summary>
        /// 最近错误的详细信息，提供人类可读的错误描述。
        /// 若无错误，值为 null。
        /// </summary>
        public string? LastErrorMessage { get; init; }

        /// <summary>
        /// 最大线速度限制，单位为毫米每秒（mm/s）。
        /// 若驱动未配置或不可用，则该值为 null。
        /// </summary>
        public double? MaxLinearMmps { get; init; }

        /// <summary>
        /// 最大线加速度限制，单位为毫米每平方秒（mm/s²）。
        /// 若驱动未配置或不可用，则该值为 null。
        /// </summary>
        public double? MaxAccelMmps2 { get; init; }

        /// <summary>
        /// 最大线减速度限制，单位为毫米每平方秒（mm/s²）。
        /// 若驱动未配置或不可用，则该值为 null。
        /// </summary>
        public double? MaxDecelMmps2 { get; init; }
    }
}
