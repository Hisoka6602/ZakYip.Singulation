using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    // Core/Mechanics/KinematicParams.cs
    public readonly record struct KinematicParams(
        double MmPerRev,           // 每转线位移
        decimal GearRatio,         // 电机轴:负载轴
        int DirectionSign = +1,    // 方向极性
        double LinearScale = 1.0   // 标定系数
    );
}