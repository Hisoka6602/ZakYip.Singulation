using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Contracts.Dto {

    /// <summary>带区/拓扑定义：N 条轴，对应物理小带与顺序。</summary>
    public sealed class ConveyorTopology {
        public IReadOnlyList<AxisId> Axes { get; init; } // 顺序即物理布局
    }
}