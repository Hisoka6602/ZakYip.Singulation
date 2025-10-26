using System.ComponentModel;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 单个 IO 端口的状态信息。
    /// </summary>
    public sealed class IoStatusDto {
        /// <summary>
        /// IO 端口编号。
        /// </summary>
        public int BitNumber { get; set; }

        /// <summary>
        /// IO 类型（Input 或 Output）。
        /// </summary>
        public IoType Type { get; set; }

        /// <summary>
        /// IO 状态（High=1 或 Low=0）。
        /// </summary>
        public IoState State { get; set; }

        /// <summary>
        /// 读取是否成功。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息（如果读取失败）。
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// IO 类型枚举。
    /// </summary>
    [Description("IO 类型")]
    public enum IoType {
        /// <summary>输入 IO</summary>
        [Description("输入")]
        Input = 0,

        /// <summary>输出 IO</summary>
        [Description("输出")]
        Output = 1
    }

    /// <summary>
    /// IO 状态枚举。
    /// </summary>
    [Description("IO 状态")]
    public enum IoState {
        /// <summary>低电平</summary>
        [Description("低电平")]
        Low = 0,

        /// <summary>高电平</summary>
        [Description("高电平")]
        High = 1
    }
}
