using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 单轴部分更新请求（PATCH /axes/{id}）。
    /// 未出现的字段不更新。
    /// </summary>
    public sealed class AxisPatchRequestDto {

        /// <summary>加速度（mm/s²）。</summary>
        public decimal? AccelMmps2 { get; set; }

        /// <summary>减速度（mm/s²）。</summary>
        public decimal? DecelMmps2 { get; set; }

        /// <summary>限幅更新。</summary>
        public LimitsPatch? Limits { get; set; }

        /// <summary>机械参数更新。</summary>
        public MechanicsPatch? Mechanics { get; set; }

        /// <summary>限幅更新片段。</summary>
        public sealed class LimitsPatch {

            /// <summary>最大线速度（mm/s）。</summary>
            public decimal? MaxLinearMmps { get; set; }

            /// <summary>最大线加速度（mm/s²）。</summary>
            public decimal? MaxAccelMmps2 { get; set; }

            /// <summary>最大线减速度（mm/s²）。</summary>
            public decimal? MaxDecelMmps2 { get; set; }
        }

        /// <summary>机械参数更新片段。</summary>
        public sealed class MechanicsPatch {

            /// <summary>滚筒直径（mm）。</summary>
            public decimal? RollerDiameterMm { get; set; }

            /// <summary>齿轮比（电机轴:滚筒轴）。</summary>
            public decimal? GearRatio { get; set; }

            /// <summary>每转脉冲数（PPR）。</summary>
            public int? Ppr { get; set; }
        }
    }
}