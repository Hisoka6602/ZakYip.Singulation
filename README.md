```
ZakYip.Singulation-master
├─ ZakYip.Singulation.Core
│  ├─ Configs/
│  │  └─ PlannerConfig.cs
│  ├─ Contracts/
│  │  ├─ Dto/
│  │  │  ├─ ConveyorTopology.cs
│  │  │  ├─ PlannerParams.cs
│  │  │  └─ SpeedSet.cs
│  │  └─ ValueObjects/
│  │     ├─ AxisId.cs
│  │     └─ AxisRpm.cs
│  │  └─ ISpeedPlanner.cs
│  ├─ Enums/
│  │  ├─ PlannerStatus.cs
│  │  └─ SourceFlags.cs
│  └─ Planning/
│     └─ DefaultSpeedPlanner.cs
│
├─ ZakYip.Singulation.Protocol
│  ├─ Abstractions/
│  │  └─ IUpstreamCodec.cs
│  ├─ Codecs/
│  │  └─ FastBinaryCodec.cs
│  ├─ Enums/
│  │  ├─ CodecFlags.cs
│  │  └─ CodecResult.cs
│  └─ Security/
│     └─ Crc32.cs
│
├─ ZakYip.Singulation.Transport
│  ├─ Abstractions/
│  │  ├─ IByteTransport.cs
│  │  └─ IUpstreamReceiver.cs
│  ├─ Enums/
│  │  └─ TransportStatus.cs
│  └─ Tcp/
│     ├─ TcpByteTransport.cs
│     ├─ TcpClientOptions.cs
│     ├─ TcpServerOptions.cs
│     ├─ TcpClientByteTransport/
│     │  ├─ TcpClientByteTransport.cs
│     │  └─ TouchClientByteTransport.cs
│     └─ TcpServerByteTransport/
│        ├─ TcpServerByteTransport.cs
│        └─ TouchServerByteTransport.cs
│
├─ ZakYip.Singulation.Drivers
│  ├─ Enums/
│  │  └─ DriverStatus.cs
│  ├─ Leadshine/
│  │  └─ LeadshineAxisDrive.cs
│  ├─ Registry/
│  │  └─ DefaultDriveRegistry.cs
│  ├─ Simulated/
│  │  └─ SimAxisDrive.cs
│  ├─ IAxisDrive.cs
│  └─ IDriveRegistry.cs
│
├─ ZakYip.Singulation.Host
│  ├─ Runtime/
│  │  ├─ IRuntimeStatusProvider.cs
│  │  └─ RuntimeStatusProvider.cs
│  ├─ Transports/
│  │  └─ RuntimeStatus.cs
│  ├─ Workers/
│  │  └─ SingulationWorker.cs
│  ├─ Program.cs
│  ├─ Worker.cs
│  └─ appsettings*.json
│
├─ README.md
└─ ZakYip.Singulation.sln

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


