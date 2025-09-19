using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers {

    public interface IAxisDrive : IAsyncDisposable {
        AxisId Axis { get; }
        DriverStatus Status { get; }

        ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default);

        ValueTask StopAsync(CancellationToken ct = default);

        ValueTask<bool> PingAsync(CancellationToken ct = default);
    }
}