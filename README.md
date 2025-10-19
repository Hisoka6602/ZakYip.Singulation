# ZakYip.Singulation 项目总览

## 本次更新（2025-10-19）

### MAUI 客户端修复与完善
- **修复编译问题**：解决 `MauiVersion` 未定义错误，将 MAUI 版本固化为 8.0.90（.NET 8 对应的稳定版本）
- **简化目标框架**：仅支持 Android 和 Windows 平台，iOS/MacCatalyst 需在 macOS 上构建
- **修复命名空间冲突**：解决项目命名空间与 `Microsoft.Maui.Hosting.MauiApp` 类型冲突问题
- **完善 MVVM 架构**：
  - 新增 `Services/ApiClient.cs` - HTTP API 客户端，提供控制器查询和安全命令发送功能
  - 新增 `Services/SignalRClientFactory.cs` - SignalR 实时连接工厂
  - 新增 `ViewModels/MainViewModel.cs` - 主页面视图模型，实现刷新控制器、发送安全命令、连接 SignalR
  - 新增 `Converters/InvertedBoolConverter.cs` - 布尔值反转转换器
  - 更新 `MainPage.xaml` - 完整的 UI 界面，包含控制器列表、安全命令发送、SignalR 连接
  - 更新 `MauiProgram.cs` - 注册所有服务和依赖注入
- **添加必要依赖**：
  - `CommunityToolkit.Mvvm` 8.3.2 - MVVM 工具包
  - `Microsoft.AspNetCore.SignalR.Client` 8.0.11 - SignalR 客户端
  - `Microsoft.Extensions.Http` 8.0.1 - HttpClient 扩展
- **构建测试成功**：
  - Debug 构建成功
  - Release 发布成功，生成 APK 和 AAB 部署包

### 之前的更新
- **移除进程重启器**：弃用 `IApplicationRestarter` 与 `ProcessApplicationRestarter`，改为通过 RESTful 的 `DELETE /api/system/session` 释放进程，由外部部署脚本负责重启
- **统一安全命令接口**：`SafetyController` 整合为单一的 `POST /api/safety/commands` 入口，并扩展 `SafetyCommandRequestDto` 增加命令类型字段
- **优化 Swagger**：默认生成 `v1` 文档、加载 XML 注释并保持 RESTful 术语一致，确保在线文档完整可读
- **新增雷赛单元测试**：为 `LeadshineLtdmcBusAdapter` 添加针对底层 `Safe` 包装器的单元测试，验证异常捕获与错误复位逻辑
- **补充 REST API 文档**：在 `docs/API.md` 中列出默认地址、各主要接口与示例调用方式
- **性能细节优化**：底层雷赛总线 `Safe` 方法引入 `ConfigureAwait(false)`，避免多余的上下文切换

## 项目架构

本项目采用分层架构设计，包含以下核心组件：

- **Core** - 领域核心与抽象契约
- **Drivers** - 设备驱动层（雷赛 LTDMC）
- **Infrastructure** - 基础设施层（LiteDB 持久化）
- **Protocol** - 上游协议解析
- **Transport** - 传输管线与事件泵
- **Host** - ASP.NET Core 宿主（REST + SignalR）
- **MauiApp** - .NET MAUI 跨平台客户端
- **Tests** - 单元测试
- **ConsoleDemo** - 控制台演示

## 项目完成度

### ✅ 已完成
1. **核心控制层**：轴驱动、控制器聚合、事件系统、速度规划 - 完全实现
2. **安全管理**：安全管线、隔离器、帧防护、调试序列 - 完全实现
3. **REST API**：
   - 轴管理（GET/PATCH/POST 批量操作）
   - 安全命令统一入口（POST /api/safety/commands）
   - 系统会话管理（DELETE /api/system/session）
   - 上游通信控制
   - 解码器服务
   - Swagger 在线文档（含 XML 注释）
4. **SignalR 实时推送**：事件 Hub、实时通知器、队列管理 - 完全实现
5. **雷赛驱动**：LTDMC 总线适配、轴驱动、协议映射、Safe 包装器 - 完全实现
6. **持久化**：LiteDB 存储、配置管理、对象映射 - 完全实现
7. **后台服务**：心跳、日志泵、传输事件泵、调试工作器、分拣工作器 - 完全实现
8. **单元测试**：雷赛总线适配器测试、安全管线测试 - 基础覆盖
9. **MAUI 客户端**：✅ 
   - 项目结构完整
   - MVVM 架构实现
   - HTTP API 客户端
   - SignalR 连接工厂
   - 完整 UI 界面（控制器列表、安全命令、SignalR 连接）
   - 构建和发布成功（APK/AAB）

### 📊 代码统计
- 总项目数：9 个
- 总源文件数：199+ 个（.cs, .xaml, .csproj）
- 代码行数：约 15,000+ 行

### ⚙️ 技术栈
- **.NET 8.0** - 运行时框架
- **ASP.NET Core** - Web 框架
- **SignalR** - 实时通信
- **.NET MAUI 8.0.90** - 跨平台移动/桌面应用（Android, Windows, iOS, MacCatalyst）
- **LiteDB** - 嵌入式数据库
- **Swagger/OpenAPI** - API 文档
- **CommunityToolkit.Mvvm** - MVVM 工具包
- **雷赛 LTDMC** - 运动控制硬件

## 可继续完善的内容

### 功能增强
1. **MAUI 客户端增强**：
   - 补充应用图标和启动屏设计
   - 添加深色主题支持
   - 实现控制器详情页面
   - 添加轴状态实时监控图表
   - 实现安全事件历史记录查看
   - 添加用户偏好设置（API 地址配置）
   - iOS 和 MacCatalyst 平台构建测试（需 macOS 环境）

2. **SignalR 实时联动**：
   - 在 MAUI 客户端自动连接 SignalR
   - 订阅并显示实时速度变化
   - 订阅并显示安全事件告警
   - 添加实时日志流显示
   - 实现断线重连策略

3. **测试覆盖率提升**：
   - Axis 控制器单元测试
   - Upstream 管线单元测试
   - Safety Pipeline 边界测试
   - 性能基准测试
   - 集成测试套件

4. **安全与审计**：
   - 安全命令审计日志持久化
   - 操作者信息记录
   - 命令原因追溯
   - 访问控制和认证（JWT）

5. **部署与 DevOps**：
   - Docker 容器化配置
   - CI/CD 管道（GitHub Actions）
   - 离线 NuGet 包缓存
   - 自动化发布脚本
   - 健康检查端点

### 性能优化
1. 雷赛总线批量操作优化
2. 事件聚合和批处理
3. 内存池和对象复用
4. 异步 IO 性能调优

### 文档完善
1. 架构设计文档
2. 部署运维手册
3. 开发者指南
4. API 使用示例集
5. 故障排查手册

## 构建与运行

### 前置要求
- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- MAUI 工作负载（用于构建移动应用）
- 雷赛 LTDMC 驱动（用于硬件控制）

### 构建整个解决方案
```bash
# 恢复依赖
dotnet restore

# 构建所有项目
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

### 构建 MAUI 应用

#### Android
```bash
cd ZakYip.Singulation.MauiApp

# 构建 Debug 版本
dotnet build -f net8.0-android

# 发布 Release 版本（默认生成 APK 和 AAB）
dotnet publish -f net8.0-android -c Release

# 仅生成 APK
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk

# 仅生成 AAB（推荐用于 Google Play）
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=aab
```
输出文件：`bin/Release/net8.0-android/publish/*.apk` 和/或 `*.aab`

#### Windows（需在 Windows 上）
```bash
cd ZakYip.Singulation.MauiApp
dotnet build -f net8.0-windows10.0.19041.0
```

#### iOS/MacCatalyst（需在 macOS 上）
```bash
cd ZakYip.Singulation.MauiApp

# 1. 编辑 ZakYip.Singulation.MauiApp.csproj
# 2. 在 <PropertyGroup> 部分，将第 5 行修改为：
#    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('osx'))">net8.0-android;net8.0-ios;net8.0-maccatalyst</TargetFrameworks>

# 3. 构建 iOS 和 MacCatalyst
dotnet build -f net8.0-ios
dotnet build -f net8.0-maccatalyst
```

## 许可证
（待定）

## 贡献指南

欢迎提交问题和拉取请求！在贡献代码时，请遵循以下准则：

### 文档要求
1. **所有 Markdown 文档必须使用中文编写**
   - 所有新增的 `.md` 文件必须使用中文
   - 现有文档如发现英文内容，请翻译为中文

2. **代码变更必须同步更新文档**
   - 添加新功能时，必须在相关文档中说明
   - 修改现有功能时，必须更新对应的文档说明
   - 需要更新的文档包括但不限于：
     - `README.md` - 项目总览和最新更新
     - `docs/API.md` - API 接口变更
     - `docs/MAUIAPP.md` - MAUI 应用功能变更
     - 相关模块的技术文档

3. **提交规范**
   - 提交信息请使用中文
   - 每次提交应包含代码变更和对应的文档更新
   - 在 Pull Request 中明确说明文档的更新内容
