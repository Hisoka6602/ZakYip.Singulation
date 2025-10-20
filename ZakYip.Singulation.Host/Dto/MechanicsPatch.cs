namespace ZakYip.Singulation.Host.Dto {

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
