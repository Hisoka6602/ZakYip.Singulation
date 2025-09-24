using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Enums;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>
    /// 对外暴露的“轴”只读资源快照。
    /// 由控制器在运行时生成，作为 REST API 的响应模型。
    /// <para>
    /// 仅包含当前可从 IAxisDrive 获取到的字段；后续驱动若增加更多只读属性，
    /// 可在此 DTO 中扩展对应字段。
    /// </para>
    /// </summary>
    public record AxisResourceDto {
        /// <summary>
        /// 轴 ID（字符串形式）。
        /// <para>
        /// 通常为逻辑编号转换后的 ID，例如 1001、1002。
        /// </para>
        /// </summary>
        public required string AxisId { get; init; }

        /// <summary>
        /// 当前驱动状态。
        /// <para>
        /// 反映了驱动的生命周期与健康度。
        /// </para>
        /// </summary>
        public DriverStatus Status { get; init; }

        /// <summary>
        /// 最近一次下发的目标线速度 (mm/s)。
        /// <para>
        /// 若驱动未下发过速度命令，则该值为 <c>null</c>。
        /// </para>
        /// </summary>
        public double? TargetLinearMmps { get; init; }

        /// <summary>
        /// 最近一次反馈的实际线速度 (mm/s)。
        /// <para>
        /// 若驱动尚未上报速度反馈，则该值为 <c>null</c>。
        /// </para>
        /// </summary>
        public double? FeedbackLinearMmps { get; init; }

        /// <summary>
        /// 驱动是否已使能。
        /// <para>
        /// true 表示当前驱动已处于“使能”状态，可接受速度命令；
        /// false 表示处于未使能/禁用状态；
        /// null 表示驱动未上报此状态。
        /// </para>
        /// </summary>
        public bool? Enabled { get; init; }

        /// <summary>
        /// 最近错误码。
        /// <para>
        /// - 0 表示正常；
        /// - 正数通常为厂商 SDK 返回的错误码；
        /// - -1 表示逻辑/参数错误或本地异常；
        /// - -2 表示通信掉线/设备不响应。
        /// <br/>
        /// 若驱动未上报错误，则该值为 <c>null</c>。
        /// </para>
        /// </summary>
        public int? LastErrorCode { get; init; }

        /// <summary>
        /// 最近错误的详细信息（人类可读）。
        /// <para>
        /// 若无错误，值为 <c>null</c>。
        /// </para>
        /// </summary>
        public string? LastErrorMessage { get; init; }
    }
}