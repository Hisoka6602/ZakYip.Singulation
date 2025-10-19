# ZakYip.Singulation 总览

## 本次更新

- 将 `SafetyIsolator` 的内部状态与触发类型改为 `int` 支撑的易失读写，满足 `Volatile.Read` 泛型约束并保持线程安全的隔离器行为。
- 为雷赛驱动的 PPR 缓存访问显式使用 `Volatile.Read`，确保静态门闩字段按照易失语义读取，消除编译器对引用访问的警告。
- 调整 README，重新导出完整文件树并更新变更记录，便于快速定位组件职责与当前迭代成果。

## 文件树与功能说明

```text
./
    .gitattributes
    .gitignore
    README.md
    ZakYip.Singulation.sln
    ZakYip.Singulation.ConsoleDemo/
        Program.cs
        ZakYip.Singulation.ConsoleDemo.csproj
        Regression/
            RegressionRunner.cs
    ZakYip.Singulation.Core/
        ZakYip.Singulation.Core.csproj
        Abstractions/
            Realtime/
                IRealtimeNotifier.cs
            Safety/
                FrameGuardDecision.cs
                ICommissioningSequence.cs
                IFrameGuard.cs
                ISafetyIoModule.cs
                ISafetyIsolator.cs
                ISafetyPipeline.cs
        Configs/
            AxisGridLayoutOptions.cs
            ControllerOptions.cs
            DriverOptionsTemplateOptions.cs
            PlannerConfig.cs
            UpstreamCodecOptions.cs
            UpstreamOptions.cs
            Defaults/
                ConfigDefaults.cs
        Contracts/
            IAxisLayoutStore.cs
            IControllerOptionsStore.cs
            ISpeedPlanner.cs
            IUpstreamCodecOptionsStore.cs
            IUpstreamFrameHub.cs
            IUpstreamOptionsStore.cs
            Dto/
                LinearPlannerParams.cs
                ParcelPose.cs
                SpeedSet.cs
                StatusSnapshot.cs
                SystemRuntimeStatus.cs
                TransportStatusItem.cs
                VisionParams.cs
            Events/
                AxisCommandIssuedEventArgs.cs
                AxisDisconnectedEventArgs.cs
                AxisErrorEventArgs.cs
                AxisEvent.cs
                AxisSpeedFeedbackEventArgs.cs
                BytesReceivedEventArgs.cs
                DriverNotLoadedEventArgs.cs
                EvState.cs
                LogEvent.cs
                TransportErrorEventArgs.cs
                TransportEvent.cs
                TransportStateChangedEventArgs.cs
                Safety/
                    SafetyStateChangedEventArgs.cs
                    SafetyTriggerEventArgs.cs
            ValueObjects/
                AxisId.cs
                AxisRpm.cs
                KinematicParams.cs
                PprRatio.cs
        Enums/
            AxisEventType.cs
            ControllerResetType.cs
            LogKind.cs
            PlannerStatus.cs
            SafetyCommand.cs
            SafetyIsolationState.cs
            SafetyTriggerKind.cs
            TransportConnectionState.cs
            TransportEventType.cs
            TransportRole.cs
            VisionAlarm.cs
        Planning/
            DefaultSpeedPlanner.cs
        Utils/
            AxisKinematics.cs
            FileUtils.cs
    ZakYip.Singulation.Drivers/
        ZakYip.Singulation.Drivers.csproj
        Abstractions/
            IAxisController.cs
            IAxisDrive.cs
            IAxisEventAggregator.cs
            IBusAdapter.cs
            IDriveRegistry.cs
            Ports/
                IAxisPort.cs
        Common/
            AxisController.cs
            AxisEventAggregator.cs
            DriverOptions.cs
            SpanParser.cs
        Enums/
            DriverStatus.cs
        Health/
            AxisHealthMonitor.cs
        Leadshine/
            LTDMC.cs
            LTDMC.dll
            LeadshineLtdmcAxisDrive.cs
            LeadshineLtdmcBusAdapter.cs
            LeadshineProtocolMap.cs
        Registry/
            DefaultDriveRegistry.cs
        Resilience/
            AxisDegradePolicy.cs
            ConsecutiveFailCounter.cs
    ZakYip.Singulation.Host/
        Nlog.config
        Program.cs
        ZakYip.Singulation.Host.csproj
        appsettings.Development.json
        appsettings.json
        install.bat
        signalr.ts
        singulation-log.db
        singulation.db
        unstall.bat
        Controllers/
            AdminController.cs
            AxesController.cs
            DecoderController.cs
            UpstreamController.cs
        Dto/
            ApiResponse.cs
            AxisCommandResultDto.cs
            AxisPatchRequestDto.cs
            AxisResponseDto.cs
            BatchCommandResponseDto.cs
            ControllerResetRequestDto.cs
            ControllerResponseDto.cs
            DecodeRequest.cs
            DecodeResult.cs
            SetSpeedRequestDto.cs
            UpstreamConnectionDto.cs
            UpstreamConnectionsDto.cs
        Extensions/
            SignalRSetup.cs
        Filters/
            ValidateModelFilter.cs
        Properties/
            launchSettings.json
        Runtime/
            IRuntimeStatusProvider.cs
            LogEventBus.cs
            PowerGuard.cs
            RealtimeDispatchService.cs
            RuntimeStatusProvider.cs
            UpstreamFrameHub.cs
        Safety/
            DefaultCommissioningSequence.cs
            FrameGuard.cs
            FrameGuardOptions.cs
            LoopbackSafetyIoModule.cs
            SafetyOperation.cs
            SafetyOperationKind.cs
            SafetyPipeline.cs
        SignalR/
            SignalRQueueItem.cs
            SignalRRealtimeNotifier.cs
            Hubs/
                EventsHub.cs
        SwaggerOptions/
            ConfigureSwaggerOptions.cs
            CustomOperationFilter.cs
            EnumSchemaFilter.cs
            HideLongListSchemaFilter.cs
            SwaggerGroupDiscovery.cs
        Workers/
            AxisBootstrapper.cs
            CommissioningCommand.cs
            CommissioningCommandKind.cs
            CommissioningState.cs
            CommissioningWorker.cs
            HeartbeatWorker.cs
            LogEventPump.cs
            LogsCleanupService.cs
            SingulationWorker.cs
            SpeedFrameWorker.cs
            TransportEventPump.cs
    ZakYip.Singulation.Infrastructure/
        ZakYip.Singulation.Infrastructure.csproj
        Configs/
            Entities/
                AxisGridLayoutDoc.cs
                ControllerOptionsDoc.cs
                DriverOptionsTemplateDoc.cs
                UpstreamCodecOptionsDoc.cs
                UpstreamOptionsDoc.cs
            Mappings/
                ConfigMappings.cs
        Persistence/
            LiteDbAxisLayoutStore.cs
            LiteDbControllerOptionsStore.cs
            PersistenceServiceCollectionExtensions.cs
        Safety/
            SafetyIsolator.cs
        Telemetry/
            SingulationMetrics.cs
        Transport/
            LiteDbUpstreamCodecOptionsStore.cs
            LiteDbUpstreamOptionsStore.cs
            TransportPersistenceExtensions.cs
            UpstreamTcpInjection.cs
    ZakYip.Singulation.Protocol/
        ZakYip.Singulation.Protocol.csproj
        Abstractions/
            IUpstreamCodec.cs
        Enums/
            CodecFlags.cs
            CodecResult.cs
            UpstreamCtrl.cs
        Vendors/
            Guiwei/
                GuiweiCodec.cs
                GuiweiControl.cs
                homing_only_tcp.md
            Huarary/
                HuararyCodec.cs
                HuararyControl.cs
                vision_mock_packets.md
    ZakYip.Singulation.Transport/
        ZakYip.Singulation.Transport.csproj
        Abstractions/
            IByteTransport.cs
            IUpstreamReceiver.cs
        Enums/
            TransportStatus.cs
        Tcp/
            TcpClientOptions.cs
            TcpServerOptions.cs
            TcpClientByteTransport/
                TouchClientByteTransport.cs
            TcpServerByteTransport/
                TouchServerByteTransport.cs
    ops/
        README.md
        dryrun.ps1
        dryrun.sh
        install.ps1
        install.sh
        selfcheck.ps1
        selfcheck.sh
        uninstall.ps1
        uninstall.sh
```

### 各文件职责说明

- **README.md**：汇总项目概览、结构说明、当前进度与后续工作。
- **ZakYip.Singulation.sln**：解决方案入口，组织所有项目。

#### ZakYip.Singulation.ConsoleDemo

- `ZakYip.Singulation.ConsoleDemo.csproj`：控制台示例项目配置。
- `Program.cs`：示例主程序，演示驱动初始化、设速、事件订阅与安全收尾。
- `Regression/RegressionRunner.cs`：无硬件回归场景，模拟关键状态机流程。

#### ZakYip.Singulation.Core

- `ZakYip.Singulation.Core.csproj`：核心库项目配置。
- `Abstractions/Realtime/IRealtimeNotifier.cs`：定义实时事件推送接口。
- `Abstractions/Safety/FrameGuardDecision.cs`：描述帧保护决策结构。
- `Abstractions/Safety/ICommissioningSequence.cs`：规定调试上电序列接口。
- `Abstractions/Safety/IFrameGuard.cs`：约束安全帧守卫实现。
- `Abstractions/Safety/ISafetyIoModule.cs`：抽象安全 IO 读取/写入模块。
- `Abstractions/Safety/ISafetyIsolator.cs`：定义隔离器控制接口。
- `Abstractions/Safety/ISafetyPipeline.cs`：声明安全联动主流程接口。
- `Configs/AxisGridLayoutOptions.cs`：轴网格配置模型。
- `Configs/ControllerOptions.cs`：控制器运行参数配置模型。
- `Configs/DriverOptionsTemplateOptions.cs`：驱动模板参数。
- `Configs/PlannerConfig.cs`：速度规划器配置。
- `Configs/UpstreamCodecOptions.cs`：上位协议编解码配置。
- `Configs/UpstreamOptions.cs`：上位连接配置。
- `Configs/Defaults/ConfigDefaults.cs`：提供配置默认值集合。
- `Contracts/IAxisLayoutStore.cs`：轴布局持久化接口。
- `Contracts/IControllerOptionsStore.cs`：控制器配置存储接口。
- `Contracts/ISpeedPlanner.cs`：速度规划器抽象。
- `Contracts/IUpstreamCodecOptionsStore.cs`：编解码配置仓储接口。
- `Contracts/IUpstreamFrameHub.cs`：上位帧转发中心接口。
- `Contracts/IUpstreamOptionsStore.cs`：上位连接配置仓储接口。
- `Contracts/Dto/LinearPlannerParams.cs`：线性规划参数 DTO。
- `Contracts/Dto/ParcelPose.cs`：包裹位姿 DTO。
- `Contracts/Dto/SpeedSet.cs`：速度设定 DTO。
- `Contracts/Dto/StatusSnapshot.cs`：运行快照 DTO。
- `Contracts/Dto/SystemRuntimeStatus.cs`：系统运行状态 DTO。
- `Contracts/Dto/TransportStatusItem.cs`：传输状态 DTO。
- `Contracts/Dto/VisionParams.cs`：视觉参数 DTO。
- `Contracts/Events/AxisCommandIssuedEventArgs.cs`：轴命令事件。
- `Contracts/Events/AxisDisconnectedEventArgs.cs`：轴离线事件。
- `Contracts/Events/AxisErrorEventArgs.cs`：轴错误事件。
- `Contracts/Events/AxisEvent.cs`：轴事件基础定义。
- `Contracts/Events/AxisSpeedFeedbackEventArgs.cs`：速度反馈事件。
- `Contracts/Events/BytesReceivedEventArgs.cs`：原始字节接收事件。
- `Contracts/Events/DriverNotLoadedEventArgs.cs`：驱动加载失败事件。
- `Contracts/Events/EvState.cs`：事件状态包装。
- `Contracts/Events/LogEvent.cs`：日志事件实体。
- `Contracts/Events/Safety/SafetyStateChangedEventArgs.cs`：安全状态变化事件。
- `Contracts/Events/Safety/SafetyTriggerEventArgs.cs`：安全触发事件。
- `Contracts/Events/TransportErrorEventArgs.cs`：通讯错误事件。
- `Contracts/Events/TransportEvent.cs`：通讯事件基类。
- `Contracts/Events/TransportStateChangedEventArgs.cs`：通讯状态改变事件。
- `Contracts/ValueObjects/AxisId.cs`：轴标识值对象。
- `Contracts/ValueObjects/AxisRpm.cs`：轴转速值对象。
- `Contracts/ValueObjects/KinematicParams.cs`：运动学参数值对象。
- `Contracts/ValueObjects/PprRatio.cs`：脉冲与距离换算值对象。
- `Enums/AxisEventType.cs`：轴事件类型枚举。
- `Enums/ControllerResetType.cs`：控制器复位方式枚举。
- `Enums/LogKind.cs`：日志类型枚举。
- `Enums/PlannerStatus.cs`：规划状态枚举。
- `Enums/SafetyCommand.cs`：安全指令枚举。
- `Enums/SafetyIsolationState.cs`：安全隔离状态枚举。
- `Enums/SafetyTriggerKind.cs`：安全触发类型枚举。
- `Enums/TransportConnectionState.cs`：传输连接状态枚举。
- `Enums/TransportEventType.cs`：传输事件类型枚举。
- `Enums/TransportRole.cs`：传输角色枚举。
- `Enums/VisionAlarm.cs`：视觉报警枚举。
- `Planning/DefaultSpeedPlanner.cs`：默认速度规划实现。
- `Utils/AxisKinematics.cs`：运动换算工具。
- `Utils/FileUtils.cs`：文件读写工具。

#### ZakYip.Singulation.Drivers

- `ZakYip.Singulation.Drivers.csproj`：驱动层项目配置。
- `Abstractions/IAxisController.cs`：轴集合控制抽象。
- `Abstractions/IAxisDrive.cs`：单轴驱动抽象。
- `Abstractions/IAxisEventAggregator.cs`：轴事件聚合抽象。
- `Abstractions/IBusAdapter.cs`：总线适配抽象。
- `Abstractions/IDriveRegistry.cs`：驱动注册中心抽象。
- `Abstractions/Ports/IAxisPort.cs`：轴通信端口抽象。
- `Common/AxisController.cs`：基于抽象组合实现的轴控制器。
- `Common/AxisEventAggregator.cs`：轴事件聚合器实现。
- `Common/DriverOptions.cs`：驱动配置实体。
- `Common/SpanParser.cs`：二进制解析工具。
- `Enums/DriverStatus.cs`：驱动状态枚举。
- `Health/AxisHealthMonitor.cs`：轴健康监控实现。
- `Leadshine/LTDMC.cs`：雷赛 SDK 托管封装。
- `Leadshine/LTDMC.dll`：雷赛驱动库原生 DLL。
- `Leadshine/LeadshineLtdmcAxisDrive.cs`：雷赛轴驱动实现。
- `Leadshine/LeadshineLtdmcBusAdapter.cs`：雷赛总线适配器。
- `Leadshine/LeadshineProtocolMap.cs`：雷赛协议映射定义。
- `Registry/DefaultDriveRegistry.cs`：默认驱动注册中心。
- `Resilience/AxisDegradePolicy.cs`：轴降级策略配置。
- `Resilience/ConsecutiveFailCounter.cs`：连续失败统计工具。

#### ZakYip.Singulation.Host

- `ZakYip.Singulation.Host.csproj`：Host API 项目配置。
- `Program.cs`：ASP.NET Core 启动入口。
- `Controllers/AdminController.cs`：运维管理接口。
- `Controllers/AxesController.cs`：轴控制接口。
- `Controllers/DecoderController.cs`：上位协议解析接口。
- `Controllers/UpstreamController.cs`：上位通道管理接口。
- `Dto/ApiResponse.cs`：统一 API 返回包装。
- `Dto/AxisCommandResultDto.cs`：轴命令执行结果。
- `Dto/AxisPatchRequestDto.cs`：轴参数补丁请求。
- `Dto/AxisResponseDto.cs`：轴状态响应。
- `Dto/BatchCommandResponseDto.cs`：批量命令响应。
- `Dto/ControllerResetRequestDto.cs`：控制器复位请求。
- `Dto/ControllerResponseDto.cs`：控制器状态响应。
- `Dto/DecodeRequest.cs`：协议解码请求。
- `Dto/DecodeResult.cs`：协议解码结果。
- `Dto/SetSpeedRequestDto.cs`：设速请求。
- `Dto/UpstreamConnectionDto.cs`：单个上位连接信息。
- `Dto/UpstreamConnectionsDto.cs`：上位连接列表信息。
- `Extensions/SignalRSetup.cs`：SignalR 依赖注入扩展。
- `Filters/ValidateModelFilter.cs`：模型校验过滤器。
- `Properties/launchSettings.json`：本地启动配置。
- `Runtime/IRuntimeStatusProvider.cs`：运行状态提供接口。
- `Runtime/LogEventBus.cs`：日志事件总线实现。
- `Runtime/PowerGuard.cs`：电源防护逻辑。
- `Runtime/RealtimeDispatchService.cs`：实时分发服务。
- `Runtime/RuntimeStatusProvider.cs`：运行状态提供实现。
- `Safety/DefaultCommissioningSequence.cs`：默认调试顺序。
- `Safety/FrameGuard.cs`：速度帧守卫逻辑。
- `Safety/FrameGuardOptions.cs`：帧守卫配置。
- `Safety/LoopbackSafetyIoModule.cs`：环回安全 IO 模块。
- `Safety/SafetyOperation.cs`：安全操作定义。
- `Safety/SafetyOperationKind.cs`：安全操作类型枚举。
- `Safety/SafetyPipeline.cs`：安全联动管线。
- `SignalR/Hubs/EventsHub.cs`：SignalR 事件中心。
- `SignalR/SignalRQueueItem.cs`：SignalR 推送队列项。
- `SignalR/SignalRRealtimeNotifier.cs`：实时通知实现。
- `SwaggerOptions/ConfigureSwaggerOptions.cs`：Swagger 配置注册。
- `SwaggerOptions/CustomOperationFilter.cs`：自定义操作过滤器。
- `SwaggerOptions/EnumSchemaFilter.cs`：枚举 Schema 过滤。
- `SwaggerOptions/HideLongListSchemaFilter.cs`：列表隐藏策略。
- `SwaggerOptions/SwaggerGroupDiscovery.cs`：Swagger 分组发现。
- `Workers/AxisBootstrapper.cs`：轴引导流程。
- `Workers/CommissioningCommand.cs`：调试命令实体。
- `Workers/CommissioningCommandKind.cs`：调试命令类型。
- `Workers/CommissioningState.cs`：调试状态枚举。
- `Workers/CommissioningWorker.cs`：调试状态机。
- `Workers/HeartbeatWorker.cs`：心跳维护。
- `Workers/LogEventPump.cs`：日志转发服务。
- `Workers/LogsCleanupService.cs`：日志清理服务。
- `Workers/SingulationWorker.cs`：分拣主循环。
- `Workers/SpeedFrameWorker.cs`：速度帧处理。
- `Workers/TransportEventPump.cs`：传输事件泵。
- `appsettings.Development.json`：开发环境配置。
- `appsettings.json`：默认配置。
- `install.bat`：Windows 安装脚本。
- `nlog.config`：NLog 配置。
- `signalr.ts`：SignalR 前端脚本。
- `singulation-log.db`：内置日志数据库样本。
- `singulation.db`：默认配置数据库样本。
- `unstall.bat`：卸载脚本。

#### ZakYip.Singulation.Infrastructure

- `ZakYip.Singulation.Infrastructure.csproj`：基础设施层项目配置。
- `Configs/Entities/AxisGridLayoutDoc.cs`：轴布局文档模型。
- `Configs/Entities/ControllerOptionsDoc.cs`：控制器配置文档模型。
- `Configs/Entities/DriverOptionsTemplateDoc.cs`：驱动模板文档模型。
- `Configs/Entities/UpstreamCodecOptionsDoc.cs`：上位编解码配置文档模型。
- `Configs/Entities/UpstreamOptionsDoc.cs`：上位连接配置文档模型。
- `Configs/Mappings/ConfigMappings.cs`：配置映射配置。
- `Persistence/LiteDbAxisLayoutStore.cs`：轴布局 LiteDB 仓储。
- `Persistence/LiteDbControllerOptionsStore.cs`：控制器配置 LiteDB 仓储。
- `Persistence/PersistenceServiceCollectionExtensions.cs`：持久化注入扩展。
- `Persistence/TransportPersistenceExtensions.cs`：传输配置持久化扩展。
- `Safety/SafetyIsolator.cs`：安全隔离器实现。
- `Telemetry/SingulationMetrics.cs`：指标采集实现。
- `Transport/LiteDbUpstreamCodecOptionsStore.cs`：编解码配置持久化。
- `Transport/LiteDbUpstreamOptionsStore.cs`：上位连接配置持久化。
- `Transport/UpstreamTcpInjection.cs`：上位 TCP 注入辅助。

#### ZakYip.Singulation.Protocol

- `ZakYip.Singulation.Protocol.csproj`：协议层项目配置。
- `Abstractions/IUpstreamCodec.cs`：上位协议编解码抽象。
- `Enums/CodecFlags.cs`：协议标记枚举。
- `Enums/CodecResult.cs`：协议解码结果枚举。
- `Enums/UpstreamCtrl.cs`：上位控制指令枚举。
- `Vendors/Guiwei/GuiweiCodec.cs`：柜纬协议编解码实现。
- `Vendors/Guiwei/GuiweiControl.cs`：柜纬上位控制模型。
- `Vendors/Guiwei/homing_only_tcp.md`：柜纬对接文档。
- `Vendors/Huarary/HuararyCodec.cs`：华睿协议编解码实现。
- `Vendors/Huarary/HuararyControl.cs`：华睿控制模型。
- `Vendors/Huarary/vision_mock_packets.md`：华睿视觉报文样例。

#### ZakYip.Singulation.Transport

- `ZakYip.Singulation.Transport.csproj`：传输层项目配置。
- `Abstractions/IByteTransport.cs`：字节传输抽象。
- `Abstractions/IUpstreamReceiver.cs`：上位消息接收抽象。
- `Enums/TransportStatus.cs`：传输状态枚举。
- `Tcp/TcpClientByteTransport/TouchClientByteTransport.cs`：TCP 客户端传输实现。
- `Tcp/TcpClientOptions.cs`：TCP 客户端选项。
- `Tcp/TcpServerByteTransport/TouchServerByteTransport.cs`：TCP 服务端传输实现。
- `Tcp/TcpServerOptions.cs`：TCP 服务端选项。

#### ops

- `README.md`：运维脚本说明。
- `dryrun.ps1`：PowerShell 试跑脚本。
- `dryrun.sh`：Bash 试跑脚本。
- `install.ps1`：PowerShell 安装脚本。
- `install.sh`：Bash 安装脚本。
- `selfcheck.ps1`：PowerShell 自检脚本。
- `selfcheck.sh`：Bash 自检脚本。
- `uninstall.ps1`：PowerShell 卸载脚本。
- `uninstall.sh`：Bash 卸载脚本。

## 项目完成度

- 核心：安全联动、驱动层、速度规划与主业务循环已贯通，可支持基础分拣流程。
- 配套：ConsoleDemo、Host API、运维脚本与指标体系完整，可支撑无硬件回归与部署。
- 待补：部分厂商驱动与安全 IO 仍处于模拟阶段，需要结合真实硬件进一步验证。

## 指标与监控

| 指标 | 说明 |
| --- | --- |
| `singulation_frame_loop_ms` | SpeedFrameWorker 单帧处理耗时 |
| `singulation_frame_rtt_ms` | 帧时间戳到执行的 RTT |
| `singulation_speed_delta_mmps` | 降级缩放带来的速度差 |
| `singulation_degrade_total` | 降级/隔离触发计数（标签区分状态） |
| `singulation_axis_fault_total` | 轴故障触发次数 |
| `singulation_heartbeat_timeout_total` | 心跳超时触发次数 |
| `singulation_commissioning_ms` | 上电顺序机用时 |

## 运维脚本入口

```bash
# 自检
pwsh ops/selfcheck.ps1
# 或
./ops/selfcheck.sh

# 生成发布目录
pwsh ops/install.ps1 -PublishDir publish/host

# 回归试跑
pwsh ops/dryrun.ps1
# 或
./ops/dryrun.sh
```

## 可继续完善

- ConsoleDemo 仍有部分异常信息来源于驱动内部英文描述，可继续评估统一翻译策略。
- 针对 Host API 的日志与前端提示可增补中文本地化，保持运维一致性。
- 引入真实硬件 IO 模块，替换 `LoopbackSafetyIoModule` 并完善自检流程。
- 扩展 `AxisKinematics` 支持更多机构换算与参数校验工具。
- 将 `SingulationMetrics` 输出接入观测平台，完善报警策略与历史留存。
- 为 SignalR 实时通知链路补充单元测试与压力验证，覆盖通道满载与失败回退场景。
