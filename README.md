```
ZakYip.Singulation.sln

ZakYip.Singulation.Core/
├─ ZakYip.Singulation.Core.csproj
├─ ISpeedPlanner.cs
├─ Contracts/
│  ├─ Dto/
│  │  ├─ ConveyorTopology.cs
│  │  ├─ PlannerParams.cs
│  │  └─ SpeedSet.cs
│  └─ ValueObjects/
│     ├─ AxisId.cs
│     └─ AxisRpm.cs
└─ Enums/
   ├─ PlannerStatus.cs
   ├─ SourceFlags.cs
   └─ SpeedUnit.cs

ZakYip.Singulation.Protocol/
├─ ZakYip.Singulation.Protocol.csproj
├─ IUpstreamCodec.cs
└─ Enums/
   ├─ CodecFlags.cs
   └─ CodecResult.cs

ZakYip.Singulation.Transport/
├─ ZakYip.Singulation.Transport.csproj
├─ IByteTransport.cs
├─ IUpstreamReceiver.cs
└─ Enums/
   └─ TransportStatus.cs

ZakYip.Singulation.Drivers/
├─ ZakYip.Singulation.Drivers.csproj
├─ IAxisDrive.cs
├─ IDriveRegistry.cs
└─ Enums/
   └─ DriverStatus.cs

ZakYip.Singulation.Host/
├─ ZakYip.Singulation.Host.csproj
├─ Program.cs
├─ Worker.cs
├─ IRuntimeStatusProvider.cs
├─ Transports/
│  └─ RuntimeStatus.cs
├─ appsettings.json
├─ appsettings.Development.json
└─ Properties/
   └─ launchSettings.json



```

```mermaid
graph TD

    subgraph Layer0["Domain Core (最底层)"]
        Core["ZakYip.Singulation.Core"]
    end

    subgraph Layer1["Middle Layers"]
        Protocol["ZakYip.Singulation.Protocol"]
        Transport["ZakYip.Singulation.Transport"]
        Drivers["ZakYip.Singulation.Drivers"]
    end

    subgraph Layer2["Host (最顶层)"]
        Host["ZakYip.Singulation.Host"]
    end

    %% 依赖关系
    Protocol --> Core
    Transport --> Core
    Transport --> Protocol
    Drivers --> Core
    Host --> Core
    Host --> Protocol
    Host --> Transport
    Host --> Drivers


```


