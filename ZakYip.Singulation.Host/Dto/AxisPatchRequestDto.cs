using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 单轴部分更新请求数据传输对象，用于 PATCH /axes/{id} 接口。
    /// 未指定的字段不会被更新。
    /// </summary>
    public sealed record class AxisPatchRequestDto {

        /// <summary>
        /// 加速度，单位为毫米每平方秒（mm/s²）。
        /// </summary>
        [Range(0.001, 1000000, ErrorMessage = "加速度必须在 0.001 到 1000000 之间")]
        public decimal? AccelMmps2 { get; init; }

        /// <summary>
        /// 减速度，单位为毫米每平方秒（mm/s²）。
        /// </summary>
        [Range(0.001, 1000000, ErrorMessage = "减速度必须在 0.001 到 1000000 之间")]
        public decimal? DecelMmps2 { get; init; }

        /// <summary>
        /// 限幅参数更新对象。
        /// </summary>
        public LimitsPatch? Limits { get; init; }

        /// <summary>
        /// 机械参数更新对象。
        /// </summary>
        public MechanicsPatch? Mechanics { get; init; }

        /// <summary>
        /// 轴类型，用于区分分离轴（Main）和疏散轴（Eject）。
        /// </summary>
        public AxisType? AxisType { get; init; }

        /// <summary>
        /// 限幅参数更新片段。
        /// </summary>
        public sealed record class LimitsPatch {

            /// <summary>
            /// 最大线速度，单位为毫米每秒（mm/s）。
            /// </summary>
            [Range(0.001, 100000, ErrorMessage = "最大线速度必须在 0.001 到 100000 之间")]
            public decimal? MaxLinearMmps { get; init; }

            /// <summary>
            /// 最大线加速度，单位为毫米每平方秒（mm/s²）。
            /// </summary>
            [Range(0.001, 1000000, ErrorMessage = "最大线加速度必须在 0.001 到 1000000 之间")]
            public decimal? MaxAccelMmps2 { get; init; }

            /// <summary>
            /// 最大线减速度，单位为毫米每平方秒（mm/s²）。
            /// </summary>
            [Range(0.001, 1000000, ErrorMessage = "最大线减速度必须在 0.001 到 1000000 之间")]
            public decimal? MaxDecelMmps2 { get; init; }
        }

        /// <summary>
        /// 机械参数更新片段。
        /// </summary>
        public sealed record class MechanicsPatch {

            /// <summary>
            /// 滚筒直径，单位为毫米（mm）。
            /// </summary>
            [Range(1, 10000, ErrorMessage = "滚筒直径必须在 1 到 10000 之间")]
            public decimal? RollerDiameterMm { get; init; }

            /// <summary>
            /// 齿轮比，表示电机轴与滚筒轴之间的传动比。
            /// </summary>
            [Range(0.001, 1000, ErrorMessage = "齿轮比必须在 0.001 到 1000 之间")]
            public decimal? GearRatio { get; init; }

            /// <summary>
            /// 每转脉冲数（PPR）。
            /// </summary>
            [Range(1, 1000000, ErrorMessage = "每转脉冲数必须在 1 到 1000000 之间")]
            public int? Ppr { get; init; }
        }
    }
}
