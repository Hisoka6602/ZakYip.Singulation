# ZakYip.Singulation 项目总览

## 本次更新

- **移除进程重启器**：弃用 `IApplicationRestarter` 与 `ProcessApplicationRestarter`，改为通过 RESTful 的 `DELETE /api/system/session` 释放进程，由外部部署脚本负责重启。
- **统一安全命令接口**：`SafetyController` 整合为单一的 `POST /api/safety/commands` 入口，并扩展 `SafetyCommandRequestDto` 增加命令类型字段。
- **优化 Swagger**：默认生成 `v1` 文档、加载 XML 注释并保持 RESTful 术语一致，确保在线文档完整可读。
- **新增雷赛单元测试**：为 `LeadshineLtdmcBusAdapter` 添加针对底层 `Safe` 包装器的单元测试，验证异常捕获与错误复位逻辑。
- **补充 REST API 文档**：在 `docs/API.md` 中列出默认地址、各主要接口与示例调用方式。
- **创建跨平台 MAUI 客户端**：新增 `ZakYip.Singulation.MauiApp`，采用 MVVM 架构对接 REST API，并预留 SignalR 连接工厂，支持 Android/iOS/MacCatalyst。
- **性能细节优化**：底层雷赛总线 `Safe` 方法引入 `ConfigureAwait(false)`，避免多余的上下文切换。

## 文件树与功能描述

```text
./
├── README.md                     — 项目说明与变更记录（本文件）
├── ZakYip.Singulation.sln        — 解决方案入口，包含核心、宿主、测试与 MAUI 客户端
├── docs/
│   └── API.md                    — REST API 使用说明（默认地址、请求示例）
├── ZakYip.Singulation.Core/      — 领域核心与抽象契约
│   ├── Abstractions/             — 实时通知、安全隔离等接口定义
│   ├── Configs/                  — 控制器、轴布局、上游通信配置对象
│   ├── Contracts/                — LiteDB 持久化契约与传输事件模型
│   ├── Enums/                    — 控制器、传输、视觉等枚举定义
│   ├── Planning/                 — 速度规划实现
│   └── Utils/                    — 运动学等工具方法
├── ZakYip.Singulation.Drivers/   — 设备驱动层与雷赛适配
│   ├── Abstractions/             — 轴控制与驱动注册接口
│   ├── Common/                   — 控制器聚合、驱动配置等基础实现
│   ├── Leadshine/                — 雷赛 LTDMC 总线/轴实现与协议映射
│   └── Resilience/               — 断路器与故障统计策略
├── ZakYip.Singulation.Host/      — ASP.NET Core 宿主（REST + SignalR）
│   ├── Controllers/              — Axes、Decoder、Safety、SystemSession、Upstream 等 REST 控制器
│   ├── Dto/                      — API 请求/响应模型（含统一 `ApiResponse<T>`）
│   ├── Extensions/               — SignalR 与服务注册扩展
│   ├── Filters/                  — 模型验证过滤器
│   ├── Runtime/                  — 日志事件总线、实时状态服务
│   ├── Safety/                   — 安全隔离器、联动管线与默认调试模块
│   ├── SignalR/                  — 实时推送实现及 Hub
│   ├── SwaggerOptions/           — Swagger/OpenAPI 配置（已启用 XML 注释）
│   ├── Workers/                  — 背景任务：调试、心跳、运输事件等
│   └── Program.cs                — 宿主启动与依赖注入入口
├── ZakYip.Singulation.Infrastructure/ — 基础设施层（数据库、传输实现等）
├── ZakYip.Singulation.Protocol/  — 上游协议解析与第三方厂商实现
├── ZakYip.Singulation.Transport/ — 传输管线与事件泵实现
├── ZakYip.Singulation.Tests/     — 轻量测试框架与场景测试
│   ├── LeadshineBusAdapterTests.cs — 新增雷赛 `Safe` 流程验证
│   └── SafetyPipelineTests.cs      — 安全联动行为测试，已适配无重启器逻辑
├── ZakYip.Singulation.ConsoleDemo/ — 控制台验证脚本与回归执行器
└── ZakYip.Singulation.MauiApp/   — 新增 .NET MAUI MVVM 客户端
    ├── App.xaml(.cs)             — 应用入口，注册 `MainPage`
    ├── MauiProgram.cs            — 服务注册与 CommunityToolkit 集成
    ├── Services/                 — `ApiClient` 与 `SignalRClientFactory`
    ├── ViewModels/               — `MainViewModel`（刷新控制器、发送安全命令）
    ├── Views/                    — `MainPage` UI 布局
    ├── Resources/Styles/         — 颜色与样式资源字典
    └── Platforms/                — Android/iOS/MacCatalyst 平台启动桩代码与清单
```

## 项目完成度

- 核心控制、驱动、宿主服务与测试均可编译运行（需联网恢复 NuGet 包）。
- REST API 已对齐 RESTful 语义，并提供 Swagger 与离线文档。
- 新增 MAUI 客户端提供基础调试入口，SignalR 连接工厂已就绪待后续实现。

## 可继续完善的内容

1. **MAUI 客户端 UI/UX**：补充正式图标、样式以及控制器更多指标的可视化展示。
2. **SignalR 实时联动**：在客户端启动后自动连接并订阅速度/安全事件，展示实时提醒。
3. **测试覆盖率**：为 Axis 控制器与 Upstream 管线扩展单元测试，覆盖异常场景与性能边界。
4. **离线包缓存**：为 CI/CD 环境配置本地 NuGet 镜像，避免无网络时还原失败。
5. **安全命令审计**：扩展安全命令请求记录，持久化原因与下发者信息，满足追溯需求。
