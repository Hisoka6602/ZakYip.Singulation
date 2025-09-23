using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {

    public struct PprRatio {
        public int Numerator;       // 原始分子
        public int Denominator;     // 原始分母（为 0 时按 1 处理）
        public double Value;        // 实际用于换算的 double
        public bool IsExact => Denominator != 0 && (Numerator % Denominator) == 0;
    }
}