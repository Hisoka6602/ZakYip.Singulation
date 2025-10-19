namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    /// <summary>
    /// 轴标识值对象，封装逻辑轴编号。
    /// </summary>
    public readonly record struct AxisId {
        /// <summary>轴编号数值。</summary>
        public int Value { get; init; }

        /// <summary>
        /// 使用数值构造轴标识。
        /// </summary>
        public AxisId(int value) {
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString() => Value.ToString();
    }
}
