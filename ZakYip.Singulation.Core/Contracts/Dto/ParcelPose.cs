using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 表示视觉检测的单件位姿信息（位置端口）。
    /// </summary>
    /// <param name="CenterXmm">包裹中心 X 坐标（mm）。</param>
    /// <param name="CenterYmm">包裹中心 Y 坐标（mm）。</param>
    /// <param name="LengthMm">包裹长度（mm）。</param>
    /// <param name="WidthMm">包裹宽度（mm）。</param>
    /// <param name="AngleDeg">包裹角度（度）。</param>
    public readonly record struct ParcelPose(
        float CenterXmm, float CenterYmm, float LengthMm, float WidthMm, float AngleDeg);
}