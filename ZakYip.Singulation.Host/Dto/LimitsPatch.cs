namespace ZakYip.Singulation.Host.Dto {

    /// <summary>限幅更新片段。</summary>
    public sealed class LimitsPatch {

        /// <summary>最大线速度（mm/s）。</summary>
        public decimal? MaxLinearMmps { get; set; }

        /// <summary>最大线加速度（mm/s²）。</summary>
        public decimal? MaxAccelMmps2 { get; set; }

        /// <summary>最大线减速度（mm/s²）。</summary>
        public decimal? MaxDecelMmps2 { get; set; }
    }
}
