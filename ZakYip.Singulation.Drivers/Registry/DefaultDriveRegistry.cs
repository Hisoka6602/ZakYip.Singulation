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

    public sealed class DefaultDriveRegistry : IDriveRegistry {

        private readonly ConcurrentDictionary<string,
            Func<AxisId, IAxisPort, DriverOptions, IAxisDrive>> _map = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string vendor, Func<AxisId, IAxisPort, DriverOptions, IAxisDrive> factory)
            => _map[vendor] = factory;

        public IAxisDrive Create(string vendor, AxisId axisId, IAxisPort port, DriverOptions opts)
            => _map.TryGetValue(vendor, out var f)
                ? f(axisId, port, opts)
                : throw new KeyNotFoundException($"Unknown driver vendor: {vendor}");
    }
}