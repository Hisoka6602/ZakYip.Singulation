using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 单轴命令执行结果数据传输对象。
    /// </summary>
    public sealed record class AxisCommandResultDto {

        /// <summary>
        /// 轴标识符，字符串形式的轴 ID。
        /// </summary>
        [Required(ErrorMessage = "轴标识符不能为空")]
        public string AxisId { get; init; } = default!;

        /// <summary>
        /// 命令是否被接受并执行成功。
        /// 异常被捕获时返回 false。
        /// </summary>
        public bool Accepted { get; init; }

        /// <summary>
        /// 驱动侧记录的最近错误消息，用于向客户端提供错误提示。
        /// </summary>
        public string? LastError { get; init; }
    }
}
