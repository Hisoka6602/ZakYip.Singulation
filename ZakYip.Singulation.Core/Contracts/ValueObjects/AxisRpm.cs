namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    /// <summary>
    /// 轴转速值对象，统一封装 rpm 数值。
    /// </summary>
    public readonly record struct AxisRpm {
        /// <summary>转速数值（rpm）。</summary>
        public decimal Value { get; init; }

        /// <summary>
        /// 使用数值构造转速值对象。
        /// </summary>
        public AxisRpm(decimal value) {
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString("F2");
    }
}
