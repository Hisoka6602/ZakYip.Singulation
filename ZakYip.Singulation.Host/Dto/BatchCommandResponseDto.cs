using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 批量命令的整体响应。
    /// </summary>
    public sealed class BatchCommandResponseDto {

        /// <summary>各轴的执行结果集合。</summary>
        public List<AxisCommandResultDto> Results { get; set; } = new();
    }
}