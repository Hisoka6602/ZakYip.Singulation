# ZakYip.Singulation 项目总览

## 🎯 最新更新（2025-10-29 Swagger 可靠性、安全隔离器和健康检查）

### ✅ Swagger 可靠性增强

**核心改进**：确保 Swagger 文档在任何情况下都不会被阻塞，使用统一的安全隔离器模式处理可能的异常

#### 1. 安全隔离器统一应用 ✅
- **功能特性**：
  - 新增 `SafeOperationHelper` 统一安全隔离器辅助类
  - 所有 Swagger 配置操作使用安全隔离器包装
  - XML 注释文件加载失败不会阻塞 Swagger 启动
  - Schema 过滤器、操作过滤器异常被安全捕获
  - 详细的日志记录帮助排查问题
  
- **应用范围**：
  - `ConfigureSwaggerOptions`: XML 注释加载、Schema ID 配置
  - `CustomOperationFilter`: 路由信息提取
  - `EnumSchemaFilter`: 枚举类型描述生成
  - `HideLongListSchemaFilter`: Schema 标题简化

#### 2. 健康检查端点 ✅
- **端点地址**：
  - `GET /health` - 基础健康检查端点
  - 返回 HTTP 200 表示服务健康
  - 可用于 Kubernetes liveness/readiness 探针
  - 可用于负载均衡器健康检查

- **使用方法**：
```bash
# 检查服务健康状态
curl http://localhost:5005/health

# 预期响应：HTTP 200 OK
# 响应体：Healthy
```

#### 3. Swagger 访问地址
- **Swagger UI**：http://localhost:5005/swagger
- **Swagger JSON**：http://localhost:5005/swagger/v1/swagger.json
- **注意**：Swagger 现在在任何情况下都不会被阻塞，即使 XML 文档缺失或过滤器异常

#### 4. 技术亮点

- ✅ **可靠性保障**：任何异常都不会阻塞 Swagger 启动
- ✅ **统一模式**：所有 Swagger 组件使用相同的安全隔离器
- ✅ **详细日志**：异常被捕获并记录，便于排查问题
- ✅ **健康检查**：支持容器编排和负载均衡
- ✅ **向后兼容**：不影响现有功能

---

## 最新更新（2025-10-28 命名规范分析）

### ✅ 快递分拣行业命名规范分析完成

**核心成果**：完成了项目命名规范的全面分析，生成了三份专业文档

#### 📚 新增文档
- **[命名规范快速指南](NAMING_GUIDE_README.md)** ⭐ **推荐先看这个**
  - 10分钟快速了解项目命名质量
  - 清晰的问题优先级和修改建议
  - 详细的行业术语对照表
  
- **[命名问题分析与优化建议](NAMING_ANALYSIS_AND_RECOMMENDATIONS.md)**
  - 深入分析5大类命名问题
  - 按优先级分级的改进方案
  - 具体实施计划和预期收益
  
- **[命名规范标准](NAMING_STANDARDS.md)**
  - 快递分拣和机械控制行业术语标准
  - 详细的命名模式和约定
  - 代码审查清单

#### 📊 主要发现
- ✅ **整体质量良好**：机械控制术语规范，架构清晰
- ⚠️ **2个高优先级问题**：Touch前缀暴露第三方库名称
- 🎯 **建议优化**：Eject术语改为更精确的行业术语（Discharge/Outlet）
- 📈 **项目评分**：⭐⭐⭐⭐ (4/5星)

---

## 最新更新（2025-10-28 上游TCP连接热更新）

### ✅ 上游TCP连接热更新支持

**核心改进**：上游TCP连接现在支持配置热更新，修改连接参数即刻生效，无需重启应用程序

#### 1. 热更新功能 ✅
- **功能特性**：
  - 支持在运行时动态更新上游TCP连接配置
  - 配置变更后自动停止旧连接、创建并启动新连接
  - 零停机时间：新连接创建成功后才会释放旧连接
  - 支持所有连接参数的热更新：Host、Port、Role（Client/Server模式）
  - 三路连接（Speed/Position/Heartbeat）同时热更新
  
- **实现机制**：
  - 新增 `UpstreamTransportManager` 统一管理传输层生命周期
  - 配置更新API（`PUT /api/upstream/configs`）自动触发热更新
  - 线程安全的连接切换机制，避免并发竞态问题
  - 失败回滚：热更新失败时自动恢复到旧连接

#### 2. 使用方法

**通过API更新配置**：
```bash
# 更新上游TCP连接配置（自动触发热更新）
curl -X PUT http://localhost:5000/api/upstream/configs \
  -H "Content-Type: application/json" \
  -d '{
    "host": "192.168.1.100",
    "speedPort": 5001,
    "positionPort": 5002,
    "heartbeatPort": 5003,
    "role": "Client",
    "validateCrc": true
  }'
```

**配置参数说明**：
- `host`: 远程主机地址（仅Client模式使用）
- `speedPort`: 速度通道端口号
- `positionPort`: 位置通道端口号
- `heartbeatPort`: 心跳通道端口号
- `role`: 连接角色，可选值：
  - `Client`: 客户端模式，主动连接远程服务器
  - `Server`: 服务器模式，本地监听等待连接
- `validateCrc`: 是否校验CRC

**热更新流程**：
1. 调用API更新配置
2. 配置保存到LiteDB数据库
3. `UpstreamTransportManager` 自动检测配置变更
4. 创建新的传输连接实例（使用新配置）
5. 启动新连接
6. 停止并释放旧连接
7. 返回操作结果

#### 3. 技术亮点

- ✅ **零停机更新**：新连接建立后才释放旧连接，保证服务连续性
- ✅ **线程安全**：使用锁机制保护并发访问
- ✅ **异常处理**：热更新失败时自动回滚到旧配置
- ✅ **自动化**：配置更新API自动触发热更新，无需手动重启
- ✅ **灵活性**：支持Client/Server模式切换
- ✅ **可观测性**：详细的日志记录，便于排查问题

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
服务将在 http://localhost:5005 启动

**访问地址**：
- **Swagger 文档**：http://localhost:5005/swagger
- **健康检查**：http://localhost:5005/health
- **SignalR Hub**：ws://localhost:5005/hubs/events

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

### 代码规范
- [命名规范快速指南](NAMING_GUIDE_README.md) ⭐ **推荐先看这个**
- [命名问题分析与优化建议](NAMING_ANALYSIS_AND_RECOMMENDATIONS.md)
- [命名规范标准](NAMING_STANDARDS.md)
- [异常处理规范](EXCEPTION_HANDLING_GUIDELINES.md)
- [日志规范](LOGGING_GUIDELINES.md)

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
