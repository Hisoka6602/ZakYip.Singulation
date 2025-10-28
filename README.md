# ZakYip.Singulation 项目总览

## 最新更新（2025-10-28 项目架构重组）

### ✅ Host层架构优化

**核心改进**：重新调整项目分层架构，将业务逻辑从表示层移至基础设施层，实现更清晰的职责分离

#### 1. 架构重组 ✅
- **重组内容**：
  - 将 `Runtime/` 文件夹（6个文件）移至 `Infrastructure/Runtime`
  - 将 `Safety/` 文件夹（7个文件）移至 `Infrastructure/Safety`
  - 将 `Services/` 文件夹（3个文件）移至 `Infrastructure/Services`
  - 将 `Workers/` 文件夹（8个文件）移至 `Infrastructure/Workers`
  - 将领域DTO（IoStatusDto, IoStatusResponseDto）移至 `Core/Contracts/Dto`
  - 将SignalR相关服务（RealtimeDispatchService）移至 `Host/SignalR`
- **修复工作**：
  - 更新所有命名空间引用（`Host.*` → `Infrastructure.*`）
  - 添加必要的NuGet包到Infrastructure层（Microsoft.Extensions.Hosting.Abstractions等）
  - 修复所有编译错误，项目成功构建 ✅

#### 2. 新的项目结构

```
ZakYip.Singulation/
├── Core/                          # 领域核心层
│   ├── Abstractions/             # 抽象接口
│   ├── Contracts/                # 契约和DTO
│   │   └── Dto/                  # 领域数据传输对象
│   ├── Enums/                    # 枚举定义
│   ├── Utils/                    # 工具类
│   └── Planning/                 # 规划算法
│
├── Drivers/                       # 设备驱动层
│   └── Leadshine/                # 雷赛驱动实现
│
├── Infrastructure/                # 基础设施层 ⭐ 重组后
│   ├── Runtime/                  # 运行时管理（从Host移入）
│   ├── Safety/                   # 安全管道（从Host移入）
│   ├── Services/                 # 业务服务（从Host移入）
│   ├── Workers/                  # 后台工作器（从Host移入）
│   ├── Configs/                  # 配置管理
│   ├── Persistence/              # 数据持久化
│   ├── Transport/                # 传输层
│   └── Telemetry/                # 遥测服务
│
├── Host/                          # 表示层 ⭐ 精简后
│   ├── Controllers/              # REST API控制器
│   ├── Dto/                      # API数据传输对象
│   ├── SignalR/                  # SignalR实时通信
│   ├── Extensions/               # 扩展方法
│   ├── Filters/                  # 过滤器
│   ├── SwaggerOptions/           # Swagger配置
│   └── Program.cs                # 应用入口
│
├── Protocol/                      # 协议解析层
├── Transport/                     # 传输管线层
├── Tests/                         # 单元测试
├── MauiApp/                       # MAUI跨平台客户端
├── ConsoleDemo/                   # 控制台演示
└── Benchmarks/                    # 性能基准测试
```

#### 3. 架构改进成果

**分层职责更加清晰**：
- **Host层**：纯粹的表示层，只包含API入口、DTO、SignalR Hubs、Swagger配置
  - 文件数量：从 41个 减少到 ~15个
  - 代码职责：仅负责HTTP请求/响应、实时通信、API文档
- **Infrastructure层**：基础设施实现，包含所有业务服务和后台工作器
  - 新增：Runtime管理、Safety管道、业务Services、后台Workers
  - 代码职责：业务逻辑实现、后台任务、运行时管理
- **Core层**：领域核心，包含抽象、契约和共享DTO
  - 新增：领域DTO（IoStatusDto等）
  - 代码职责：定义领域模型、抽象接口、业务契约

**依赖方向符合Clean Architecture**：
```
Host → Infrastructure → Core
     ↘      ↓          ↓
       Drivers → Core
```

#### 4. 技术亮点

- ✅ **职责分离清晰**：表示层不再包含业务逻辑
- ✅ **可测试性提升**：基础设施层代码更易于单元测试
- ✅ **可维护性增强**：代码组织更加合理，易于定位和修改
- ✅ **符合最佳实践**：遵循DDD和Clean Architecture原则
- ✅ **编译零错误**：所有代码重构完成后成功编译

---

## 最新更新（2025-10-28 项目架构重组）

### ✅ Host层架构优化

**核心改进**：重新调整项目分层架构，将业务逻辑从表示层移至基础设施层，实现更清晰的职责分离

#### 1. 架构重组 ✅
- **重组内容**：
  - 将 `Runtime/` 文件夹（6个文件）移至 `Infrastructure/Runtime`
  - 将 `Safety/` 文件夹（7个文件）移至 `Infrastructure/Safety`
  - 将 `Services/` 文件夹（3个文件）移至 `Infrastructure/Services`
  - 将 `Workers/` 文件夹（8个文件）移至 `Infrastructure/Workers`
  - 将领域DTO（IoStatusDto, IoStatusResponseDto）移至 `Core/Contracts/Dto`
  - 将SignalR相关服务（RealtimeDispatchService）移至 `Host/SignalR`
- **修复工作**：
  - 更新所有命名空间引用（`Host.*` → `Infrastructure.*`）
  - 添加必要的NuGet包到Infrastructure层（Microsoft.Extensions.Hosting.Abstractions等）
  - 修复所有编译错误，项目成功构建 ✅

#### 2. 新的项目结构

```
ZakYip.Singulation/
├── Core/                          # 领域核心层
│   ├── Abstractions/             # 抽象接口
│   ├── Contracts/                # 契约和DTO
│   │   └── Dto/                  # 领域数据传输对象
│   ├── Enums/                    # 枚举定义
│   ├── Utils/                    # 工具类
│   └── Planning/                 # 规划算法
│
├── Drivers/                       # 设备驱动层
│   └── Leadshine/                # 雷赛驱动实现
│
├── Infrastructure/                # 基础设施层 ⭐ 重组后
│   ├── Runtime/                  # 运行时管理（从Host移入）
│   ├── Safety/                   # 安全管道（从Host移入）
│   ├── Services/                 # 业务服务（从Host移入）
│   ├── Workers/                  # 后台工作器（从Host移入）
│   ├── Configs/                  # 配置管理
│   ├── Persistence/              # 数据持久化
│   ├── Transport/                # 传输层
│   └── Telemetry/                # 遥测服务
│
├── Host/                          # 表示层 ⭐ 精简后
│   ├── Controllers/              # REST API控制器
│   ├── Dto/                      # API数据传输对象
│   ├── SignalR/                  # SignalR实时通信
│   ├── Extensions/               # 扩展方法
│   ├── Filters/                  # 过滤器
│   ├── SwaggerOptions/           # Swagger配置
│   └── Program.cs                # 应用入口
│
├── Protocol/                      # 协议解析层
├── Transport/                     # 传输管线层
├── Tests/                         # 单元测试
├── MauiApp/                       # MAUI跨平台客户端
├── ConsoleDemo/                   # 控制台演示
└── Benchmarks/                    # 性能基准测试
```

#### 3. 架构改进成果

**分层职责更加清晰**：
- **Host层**：纯粹的表示层，只包含API入口、DTO、SignalR Hubs、Swagger配置
  - 文件数量：从 41个 减少到 ~15个
  - 代码职责：仅负责HTTP请求/响应、实时通信、API文档
- **Infrastructure层**：基础设施实现，包含所有业务服务和后台工作器
  - 新增：Runtime管理、Safety管道、业务Services、后台Workers
  - 代码职责：业务逻辑实现、后台任务、运行时管理
- **Core层**：领域核心，包含抽象、契约和共享DTO
  - 新增：领域DTO（IoStatusDto等）
  - 代码职责：定义领域模型、抽象接口、业务契约

**依赖方向符合Clean Architecture**：
```
Host → Infrastructure → Core
     ↘      ↓          ↓
       Drivers → Core
```

#### 4. 技术亮点

- ✅ **职责分离清晰**：表示层不再包含业务逻辑
- ✅ **可测试性提升**：基础设施层代码更易于单元测试
- ✅ **可维护性增强**：代码组织更加合理，易于定位和修改
- ✅ **符合最佳实践**：遵循DDD和Clean Architecture原则
- ✅ **编译零错误**：所有代码重构完成后成功编译

---


---

## 项目当前状态

### 📊 代码统计
- **总项目数**：9个
- **总源文件数**：~240个 (.cs, .xaml, .csproj)
- **代码行数**：~25,000行
- **编译状态**：✅ 成功（仅2个警告，来自第三方LTDMC.dll）
- **架构质量**：✅ 符合Clean Architecture和DDD原则

### ⚙️ 技术栈
- **.NET 8.0** - 运行时框架
- **ASP.NET Core** - Web 框架
- **SignalR** - 实时通信
- **.NET MAUI 8.0.90** - 跨平台移动/桌面应用
- **Prism** - MVVM 框架和依赖注入
- **LiteDB** - 嵌入式数据库
- **Swagger/OpenAPI** - API 文档
- **雷赛 LTDMC** - 运动控制硬件

### 📈 项目完成度：约 85%

#### ✅ 已完成的核心功能
1. **核心控制层** (100%)：轴驱动、控制器聚合、事件系统、速度规划
2. **安全管理** (100%)：安全管线、隔离器、物理按键集成、远程/本地模式切换
3. **REST API** (100%)：完整的轴管理、安全控制、上游通信API，含完整中文文档
4. **SignalR 实时推送** (100%)：事件Hub、实时通知、队列管理
5. **雷赛驱动** (100%)：LTDMC 总线适配、轴驱动、协议映射
6. **持久化** (100%)：LiteDB 存储、配置管理、对象映射
7. **后台服务** (100%)：心跳、日志泵、传输事件泵、后台工作器
8. **MAUI 客户端** (80%)：基础功能完成，需要完善UI和用户体验
9. **文档** (98%)：API文档、架构设计、运维指南完整

#### ⚠️ 待完善的部分
- **测试覆盖** (40%)：有基础单元测试，缺少集成测试和性能测试
- **部署运维** (30%)：缺少容器化、CI/CD、监控告警
- **MAUI应用** (80%)：需要完善应用图标、深色主题等

---

## 接下来的优化方向

### 🎯 短期目标（1-2周）

#### 1. 完善文档体系
- ✅ 架构重组文档完成
- [ ] 更新开发者指南，说明新的项目结构
- [ ] 补充架构决策记录（ADR）
- [ ] 完善代码注释覆盖率

#### 2. 测试覆盖率提升
- [ ] Infrastructure层单元测试（新移入的组件）
- [ ] Controllers集成测试
- [ ] Safety Pipeline端到端测试
- [ ] 性能基准测试扩展

#### 3. 代码质量优化
- [ ] 应用代码分析工具（SonarQube）
- [ ] 统一异常处理策略
- [ ] 优化日志记录规范
- [ ] 性能瓶颈分析和优化

### 🚀 中期目标（2-4周）

#### 1. 生产环境准备
- [ ] Docker容器化配置
- [ ] Kubernetes部署配置
- [ ] 健康检查端点完善
- [ ] 配置管理优化（环境变量、配置中心）

#### 2. 监控和运维
- [ ] Prometheus + Grafana监控大盘
- [ ] 日志聚合（ELK或Loki）
- [ ] 告警规则配置
- [ ] APM性能监控集成

#### 3. CI/CD流水线
- [ ] GitHub Actions自动构建
- [ ] 自动化测试执行
- [ ] Docker镜像自动发布
- [ ] 版本管理和发布流程

### 🌟 长期规划（1-3个月）

#### 1. 安全加固
- [ ] JWT Token认证
- [ ] 角色权限管理
- [ ] 审计日志完善
- [ ] API请求频率限制

#### 2. 功能扩展
- [ ] 数据可视化（实时曲线、历史分析）
- [ ] 移动端功能完善
- [ ] 多语言支持
- [ ] 深色主题支持

#### 3. 高可用架构
- [ ] 负载均衡
- [ ] 服务降级和熔断
- [ ] 分布式部署
- [ ] 灾备方案

---

## 构建与运行

### 前置要求
- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- 雷赛 LTDMC 驱动（用于硬件控制）

### 构建整个解决方案
```bash
# 恢复依赖
dotnet restore

# 构建所有项目（除MAUI外）
dotnet build

# 运行测试
dotnet test
```

### 运行 Host 服务
```bash
cd ZakYip.Singulation.Host
dotnet run
```
服务将在 http://localhost:5000 启动，Swagger 文档位于 http://localhost:5000/swagger

---

## 📚 文档资源

### 核心文档
- [架构设计](docs/ARCHITECTURE.md)
- [API 文档](docs/API.md)
- [API 操作说明](docs/API_OPERATIONS.md)
- [安全按键快速入门](docs/SAFETY_QUICK_START.md)
- [安全按键完整指南](docs/SAFETY_BUTTONS.md)

### 运维文档
- [运维手册](ops/OPERATIONS_MANUAL.md)
- [配置指南](ops/CONFIGURATION_GUIDE.md)
- [部署运维手册](docs/DEPLOYMENT.md)
- [故障排查手册](docs/TROUBLESHOOTING.md)
- [备份恢复流程](ops/BACKUP_RECOVERY.md)
- [应急响应预案](ops/EMERGENCY_RESPONSE.md)

### 开发文档
- [开发指南](docs/DEVELOPER_GUIDE.md)
- [MAUI 应用说明](docs/MAUIAPP.md)
- [图标字体指南](docs/ICON_FONT_GUIDE.md)
- [性能优化](docs/PERFORMANCE.md)
- [完整更新历史](docs/CHANGELOG.md)

---

## 许可证
（待定）

## 贡献指南

欢迎提交问题和拉取请求！在贡献代码时，请遵循以下准则：

### 文档要求
1. **所有 Markdown 文档必须使用中文编写**
2. **代码变更必须同步更新文档**
3. **提交信息请使用中文**

详细贡献指南请参见 [CONTRIBUTING.md](CONTRIBUTING.md)
