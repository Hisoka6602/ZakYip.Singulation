using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 批量命令执行的整体响应数据传输对象。
    /// </summary>
    public sealed record class BatchCommandResponseDto {

        /// <summary>
        /// 各轴的执行结果集合。
        /// </summary>
        [Required(ErrorMessage = "执行结果集合不能为空")]
        public List<AxisCommandResultDto> Results { get; set; } = new();
    }
}
