using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {
    /// <summary>
    /// 控制器状态响应数据传输对象。
    /// </summary>
    public sealed record class ControllerResponseDto {
        /// <summary>
        /// 控制器发现的轴数量。
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "轴数量不能为负数")]
        public int AxisCount { get; init; }

        /// <summary>
        /// 当前错误码，0 表示正常，非 0 表示控制器或总线故障。
        /// </summary>
        public int ErrorCode { get; init; }

        /// <summary>
        /// 最后一次初始化是否成功。
        /// </summary>
        public bool Initialized { get; init; }

        /// <summary>
        /// 所有轴的PPR（每转脉冲数）值数组，去重后返回。
        /// 如果所有轴都是同一个PPR则只返回一个元素，如果有不同的则返回多个不重复的。
        /// </summary>
        public int[]? Pps { get; init; }
    }
}
