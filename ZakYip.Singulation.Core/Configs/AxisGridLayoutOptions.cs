using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 轴网格布局配置对象，用于定义多轴系统的物理布局排列方式。
    /// </summary>
    public sealed record class AxisGridLayoutOptions {

        /// <summary>
        /// 网格行数，必须大于等于 1。
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "网格行数必须大于等于 1")]
        public required int Rows { get; init; }

        /// <summary>
        /// 网格列数，必须大于等于 1。
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "网格列数必须大于等于 1")]
        public required int Cols { get; init; }
    }
}