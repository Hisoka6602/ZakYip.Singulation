using System;
using System.Runtime.CompilerServices;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// 雷赛（Leadshine）驱动器专用单位换算工具类。
    /// <para>
    /// 提供负载侧脉冲频率（pps）与线速度/加速度（mm/s、mm/s²）之间的转换。
    /// 适用于雷赛 LTDMC EtherCAT 驱动器的 0x60FF（TargetVelocity）和 0x606C（ActualVelocity）等寄存器。
    /// </para>
    /// <para>
    /// 核心概念：
    /// - Lpr（Linear per Revolution）：每转线位移（mm/turn），由丝杠导程或滚筒周长决定
    /// - PPR（Pulses Per Revolution）：每转脉冲数
    /// - 负载侧：经过齿轮减速后的输出侧
    /// </para>
    /// </summary>
    public static class LeadshineConversions
    {
        /// <summary>
        /// 计算每转线位移 Lpr（mm/turn）。
        /// <para>丝杠导程优先；若未提供，则使用滚筒直径计算周长。</para>
        /// </summary>
        /// <param name="screwPitchMm">丝杠导程（mm），若大于0则优先使用</param>
        /// <param name="pulleyPitchDiameterMm">滚筒节圆直径（mm）</param>
        /// <returns>每转线位移（mm/turn）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal ComputeLinearPerRevolution(decimal screwPitchMm, decimal pulleyPitchDiameterMm)
        {
            // 若存在丝杠导程，优先采用；否则采用滚筒直径换算周长
            if (screwPitchMm > 0m)
                return screwPitchMm;
            return (decimal)Math.PI * pulleyPitchDiameterMm;
        }

        /// <summary>
        /// 线速度或线加速度 → 负载侧脉冲频率或脉冲加速度。
        /// <para>公式：pps = (value ÷ Lpr) × PPR ÷ gearRatio</para>
        /// <para>适用于速度（mm/s → pps）和加速度（mm/s² → pps²）转换。</para>
        /// </summary>
        /// <param name="linearValue">线速度（mm/s）或线加速度（mm/s²）</param>
        /// <param name="lprMm">每转线位移 Lpr（mm/turn）</param>
        /// <param name="ppr">每转脉冲数 PPR</param>
        /// <param name="gearRatio">齿轮比（电机:负载）</param>
        /// <returns>负载侧脉冲频率（pps）或脉冲加速度（pps²）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal LinearToLoadPps(decimal linearValue, decimal lprMm, int ppr, decimal gearRatio)
        {
            if (linearValue == 0m || lprMm <= 0m || ppr <= 0 || gearRatio <= 0m)
                return 0m;
            var revPerSecLoad = linearValue / lprMm;
            var ppsLoad = revPerSecLoad * ppr / gearRatio;
            return ppsLoad;
        }

        /// <summary>
        /// 负载侧脉冲频率或脉冲加速度 → 线速度或线加速度。
        /// <para>公式：mm/s = (pps ÷ PPR) × Lpr</para>
        /// <para>适用于速度（pps → mm/s）和加速度（pps² → mm/s²）转换。</para>
        /// </summary>
        /// <param name="ppsLoadValue">负载侧脉冲频率（pps）或脉冲加速度（pps²）</param>
        /// <param name="ppr">每转脉冲数 PPR</param>
        /// <param name="lprMm">每转线位移 Lpr（mm/turn）</param>
        /// <returns>线速度（mm/s）或线加速度（mm/s²）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal LoadPpsToLinear(decimal ppsLoadValue, int ppr, decimal lprMm)
        {
            if (ppr <= 0 || lprMm <= 0m)
                return 0m;
            var revPerSecLoad = ppsLoadValue / ppr;
            var linearValue = revPerSecLoad * lprMm;
            return linearValue;
        }

        // ---- 便捷重载：明确命名的速度转换方法 ----

        /// <summary>
        /// 线速度（mm/s）→ 负载侧脉冲频率（pps）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal MmpsToLoadPps(decimal mmps, decimal lprMm, int ppr, decimal gearRatio)
            => LinearToLoadPps(mmps, lprMm, ppr, gearRatio);

        /// <summary>
        /// 负载侧脉冲频率（pps）→ 线速度（mm/s）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal LoadPpsToMmps(int ppsLoad, int ppr, decimal lprMm)
            => LoadPpsToLinear(ppsLoad, ppr, lprMm);

        // ---- 便捷重载：明确命名的加速度转换方法 ----

        /// <summary>
        /// 线加速度（mm/s²）→ 负载侧脉冲加速度（pps²）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Mmps2ToLoadPps2(decimal mmps2, decimal lprMm, int ppr, decimal gearRatio)
            => LinearToLoadPps(mmps2, lprMm, ppr, gearRatio);

        /// <summary>
        /// 负载侧脉冲加速度（pps²）→ 线加速度（mm/s²）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal LoadPps2ToMmps2(uint pps2Load, int ppr, decimal lprMm)
            => LoadPpsToLinear(pps2Load, ppr, lprMm);
    }
}
