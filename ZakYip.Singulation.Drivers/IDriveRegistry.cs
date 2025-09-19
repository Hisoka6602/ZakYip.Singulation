using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers {

    /// <summary>轴 → 驱动实例映射与拓扑绑定。</summary>
    public interface IDriveRegistry {
        IReadOnlyList<AxisId> Axes { get; }

        bool TryGet(AxisId axis, out IAxisDrive? drive);
    }
}