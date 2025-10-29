using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions.Ports;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Registry {

    /// <summary>
    /// 默认驱动注册表实现，用于管理不同厂商的轴驱动工厂。
    /// </summary>
    public sealed class DefaultDriveRegistry : IDriveRegistry {

        private readonly ConcurrentDictionary<string,
            Func<AxisId, IAxisPort, DriverOptions, IAxisDrive>> _map = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 注册特定厂商的轴驱动工厂。
        /// </summary>
        /// <param name="vendor">厂商名称（不区分大小写）。</param>
        /// <param name="factory">用于创建轴驱动实例的工厂函数。</param>
        public void Register(string vendor, Func<AxisId, IAxisPort, DriverOptions, IAxisDrive> factory)
            => _map[vendor] = factory;

        /// <summary>
        /// 根据厂商名称创建轴驱动实例。
        /// </summary>
        /// <param name="vendor">厂商名称。</param>
        /// <param name="axisId">轴标识。</param>
        /// <param name="port">轴端口。</param>
        /// <param name="opts">驱动选项。</param>
        /// <returns>创建的轴驱动实例。</returns>
        /// <exception cref="KeyNotFoundException">当指定的厂商未注册时抛出。</exception>
        public IAxisDrive Create(string vendor, AxisId axisId, IAxisPort port, DriverOptions opts)
            => _map.TryGetValue(vendor, out var f)
                ? f(axisId, port, opts)
                : throw new KeyNotFoundException($"Unknown driver vendor: {vendor}");
    }
}