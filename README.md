```
ZakYip.Singulation-master
├─ ZakYip.Singulation.Core
│  ├─ Configs/PlannerConfig.cs
│  ├─ Contracts/
│  │  ├─ Dto/{ConveyorTopology.cs, PlannerParams.cs, SpeedSet.cs}
│  │  ├─ ISpeedPlanner.cs
│  │  └─ ValueObjects/{AxisId.cs, AxisRpm.cs, KinematicParams.cs, PprRatio.cs}
│  ├─ Enums/{PlannerStatus.cs, SourceFlags.cs, SpeedUnit.cs}
│  ├─ Planning/DefaultSpeedPlanner.cs
│  └─ ZakYip.Singulation.Core.csproj
│
├─ ZakYip.Singulation.Protocol
│  ├─ Abstractions/IUpstreamCodec.cs
│  ├─ Codecs/FastBinaryCodec.cs
│  ├─ Enums/{CodecFlags.cs, CodecResult.cs}
│  ├─ Security/Crc32.cs
│  └─ ZakYip.Singulation.Protocol.csproj
│
├─ ZakYip.Singulation.Transport
│  ├─ Abstractions/{IByteTransport.cs, IUpstreamReceiver.cs}
│  ├─ Enums/TransportStatus.cs
│  ├─ Tcp/
│  │  ├─ TcpByteTransport.cs
│  │  ├─ TcpClientByteTransport/{TcpClientByteTransport.cs, TouchClientByteTransport.cs}
│  │  ├─ TcpServerByteTransport/{TcpServerByteTransport.cs, TouchServerByteTransport.cs}
│  │  ├─ TcpClientOptions.cs
│  │  └─ TcpServerOptions.cs
│  └─ ZakYip.Singulation.Transport.csproj
│
├─ ZakYip.Singulation.Drivers
│  ├─ Abstractions/
│  │  ├─ Events/{AxisDisconnectedEventArgs.cs, AxisErrorEventArgs.cs, AxisSpeedFeedbackEventArgs.cs, DriverNotLoadedEventArgs.cs, EvState.cs}
│  │  ├─ IAxisDrive.cs
│  │  ├─ IDriveRegistry.cs
│  │  └─ Ports/IAxisPort.cs
│  ├─ Common/{AxisCommandQueue.cs, DriverOptions.cs, ProtocolMap.cs, SpanParser.cs}
│  ├─ Enums/DriverStatus.cs
│  ├─ Health/AxisHealthMonitor.cs
│  ├─ Leadshine/{LTDMC.cs, LTDMC.dll, LeadshineLtdmcAxisDrive.cs, LeadshineProtocolMap.cs}
│  ├─ Registry/DefaultDriveRegistry.cs
│  ├─ Resilience/{AxisDegradePolicy.cs, ConsecutiveFailCounter.cs}
│  └─ ZakYip.Singulation.Drivers.csproj
│
├─ ZakYip.Singulation.Host
│  ├─ Program.cs
│  ├─ Runtime/{IRuntimeStatusProvider.cs, RuntimeStatusProvider.cs}
│  ├─ Transports/RuntimeStatus.cs
│  ├─ Workers/SingulationWorker.cs
│  ├─ Worker.cs
│  ├─ appsettings.json (+ Development)
│  └─ ZakYip.Singulation.Host.csproj
│
├─ ZakYip.Singulation.ConsoleDemo
│  ├─ Program.cs
│  └─ ZakYip.Singulation.ConsoleDemo.csproj
│
└─ ZakYip.Singulation.sln

```

```mermaid
---
title: ZakYip.Singulation 项目依赖泳道图
---

%% Mermaid Swimlane
%% 各层作为泳道，箭头表示依赖方向
flowchart LR
    subgraph Core["Core (零依赖根)"]
        C1["ZakYip.Singulation.Core"]
    end

    subgraph Protocol["Protocol"]
        P1["ZakYip.Singulation.Protocol"]
    end

    subgraph Transport["Transport"]
        T1["ZakYip.Singulation.Transport"]
    end

    subgraph Drivers["Drivers"]
        D1["ZakYip.Singulation.Drivers"]
    end

    subgraph Host["Host"]
        H1["ZakYip.Singulation.Host"]
    end

    subgraph ConsoleDemo["ConsoleDemo"]
        DEMO["ZakYip.Singulation.ConsoleDemo"]
    end

    %% 依赖关系
    P1 --> C1
    T1 --> C1
    T1 --> P1
    D1 --> C1
    H1 --> C1
    H1 --> P1
    H1 --> T1
    H1 --> D1
    DEMO --> C1
    DEMO --> D1

```


