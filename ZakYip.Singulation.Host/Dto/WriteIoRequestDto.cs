using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 写入 IO 端口电平请求。
    /// </summary>
    public sealed class WriteIoRequestDto {
        /// <summary>
        /// IO 端口编号。
        /// </summary>
        /// <remarks>
        /// 指定要写入的输出 IO 端口编号，编号从 0 开始。
        /// </remarks>
        [Required(ErrorMessage = "IO 端口编号不能为空")]
        [Range(0, 1023, ErrorMessage = "IO 端口编号必须在 0-1023 之间")]
        public int BitNumber { get; set; }

        /// <summary>
        /// IO 电平状态（High=1 或 Low=0）。
        /// </summary>
        /// <remarks>
        /// 设置输出 IO 的电平状态：High (高电平，1) 或 Low (低电平，0)。
        /// </remarks>
        [Required(ErrorMessage = "IO 状态不能为空")]
        public IoState State { get; set; }
    }
}
