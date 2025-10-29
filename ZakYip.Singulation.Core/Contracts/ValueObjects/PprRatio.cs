using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {

    /// <summary>
    /// 表示 PPR（每转脉冲数）比率的值对象。
    /// </summary>
    public struct PprRatio {
        /// <summary>
        /// 原始分子。
        /// </summary>
        public int Numerator;

        /// <summary>
        /// 原始分母（为 0 时按 1 处理）。
        /// </summary>
        public int Denominator;

        /// <summary>
        /// 实际用于换算的浮点数值。
        /// </summary>
        public double Value;

        /// <summary>
        /// 指示是否为精确整数比率（无余数）。
        /// </summary>
        public bool IsExact => Denominator != 0 && (Numerator % Denominator) == 0;
    }
}