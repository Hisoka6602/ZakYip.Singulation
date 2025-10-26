using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// IO 状态查询响应。
    /// </summary>
    public sealed class IoStatusResponseDto {
        /// <summary>
        /// 输入 IO 状态列表。
        /// </summary>
        public List<IoStatusDto> InputIos { get; set; } = new();

        /// <summary>
        /// 输出 IO 状态列表。
        /// </summary>
        public List<IoStatusDto> OutputIos { get; set; } = new();

        /// <summary>
        /// 总 IO 数量。
        /// </summary>
        public int TotalCount => InputIos.Count + OutputIos.Count;

        /// <summary>
        /// 成功读取的 IO 数量。
        /// </summary>
        public int ValidCount { get; set; }

        /// <summary>
        /// 读取失败的 IO 数量。
        /// </summary>
        public int ErrorCount { get; set; }
    }
}
