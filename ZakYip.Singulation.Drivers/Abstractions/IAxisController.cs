using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Common;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>
    /// 轴群编排器：负责创建/持有多根 <see cref="IAxisDrive"/>，并提供“全体/批量”的常用操作。
    /// </summary>
    public interface IAxisController {

        /// <summary>底层总线适配器。</summary>
        IBusAdapter Bus { get; }

        /// <summary>已创建/托管的驱动集合。</summary>
        IReadOnlyList<IAxisDrive> Drives { get; }

        /// <summary>初始化总线并按规则创建 N 根驱动（通过 <see cref="IDriveRegistry"/> 工厂）。</summary>
        Task InitializeAsync(string vendor, DriverOptions template, int? overrideAxisCount = null, CancellationToken ct = default);

        Task EnableAllAsync(CancellationToken ct = default);

        Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default);

        Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default);

        Task StopAllAsync(CancellationToken ct = default);

        Task DisposeAllAsync(CancellationToken ct = default);
    }
}