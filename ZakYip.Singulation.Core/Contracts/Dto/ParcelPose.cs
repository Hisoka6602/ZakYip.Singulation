namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 表示视觉检测的单件位姿信息（位置端口）。
    /// </summary>
    public readonly record struct ParcelPose {
        /// <summary>包裹中心 X 坐标（mm）。</summary>
        public float CenterXmm { get; init; }

        /// <summary>包裹中心 Y 坐标（mm）。</summary>
        public float CenterYmm { get; init; }

        /// <summary>包裹长度（mm）。</summary>
        public float LengthMm { get; init; }

        /// <summary>包裹宽度（mm）。</summary>
        public float WidthMm { get; init; }

        /// <summary>包裹角度（度）。</summary>
        public float AngleDeg { get; init; }

        /// <summary>
        /// 通过参数构造位姿信息。
        /// </summary>
        public ParcelPose(float centerXmm, float centerYmm, float lengthMm, float widthMm, float angleDeg) {
            CenterXmm = centerXmm;
            CenterYmm = centerYmm;
            LengthMm = lengthMm;
            WidthMm = widthMm;
            AngleDeg = angleDeg;
        }
    }
}
