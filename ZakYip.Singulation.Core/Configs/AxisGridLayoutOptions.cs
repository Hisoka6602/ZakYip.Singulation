using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 轴网格布局的单例资源：
    /// - Rows/Cols 描述网格大小
    /// - Placements 描述每个单元格放置的轴
    /// </summary>
    public sealed class AxisGridLayoutOptions {

        /// <summary>网格行数（>=1）。</summary>
        public int Rows { get; init; }

        /// <summary>网格列数（>=1）。</summary>
        public int Cols { get; init; }
    }
}