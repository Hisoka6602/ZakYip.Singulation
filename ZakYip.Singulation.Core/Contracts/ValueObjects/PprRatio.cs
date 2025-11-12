using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {

    /// <summary>
    /// 表示 PPR（每转脉冲数）比率的值对象。
    /// </summary>
    public readonly record struct PprRatio {
        /// <summary>
        /// 脉冲数的分子部分，用于计算每转脉冲数比率。
        /// </summary>
        public int Numerator { get; init; }

        /// <summary>
        /// 脉冲数的分母部分，用于计算每转脉冲数比率（为 0 时按 1 处理）。
        /// </summary>
        public int Denominator { get; init; }

        /// <summary>
        /// 实际用于换算的浮点数值。
        /// </summary>
        public double Value { get; init; }

        /// <summary>
        /// 指示是否为精确整数比率（无余数）。
        /// </summary>
        public bool IsExact => Denominator != 0 && (Numerator % Denominator) == 0;

        /// <summary>
        /// 使用分子、分母构造 PPR 比率。若分母为 0，则按 1 处理。
        /// </summary>
        public PprRatio(int numerator, int denominator) {
            Numerator = numerator;
            Denominator = denominator;
            Value = denominator != 0 ? (double)numerator / denominator : (double)numerator / 1.0;
        }
    }
}