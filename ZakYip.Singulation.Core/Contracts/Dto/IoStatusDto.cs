using System.ComponentModel;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>
    /// 单个 IO 端口的状态信息。
    /// </summary>
    public sealed record class IoStatusDto {
        /// <summary>
        /// IO 端口编号。
        /// </summary>
        public int BitNumber { get; init; }

        /// <summary>
        /// IO 类型（Input 或 Output）。
        /// </summary>
        public IoType Type { get; init; }

        /// <summary>
        /// IO 状态（High=1 或 Low=0）。
        /// </summary>
        public IoState State { get; init; }

        /// <summary>
        /// 读取是否成功。
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// 错误信息（如果读取失败）。
        /// </summary>
        public string? ErrorMessage { get; init; }
    }
}
