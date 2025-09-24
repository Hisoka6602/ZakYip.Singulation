using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 单轴命令执行结果。
    /// </summary>
    public sealed class AxisCommandResultDto {

        /// <summary>轴标识（字符串）。</summary>
        public string AxisId { get; set; } = default!;

        /// <summary>是否接受并执行成功（异常被吞并转 false）。</summary>
        public bool Accepted { get; set; }

        /// <summary>驱动侧记录的最近错误消息（便于客户端提示）。</summary>
        public string? LastError { get; set; }
    }
}