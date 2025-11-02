# ZakYip.Singulation 项目总览

## 🎯 最新更新（2025-11-02）

### ✅ 2025-11-02 重要Bug修复和功能改进

本次更新修复了多个关键问题，提升了系统的可靠性和准确性：

#### 1. **速度联动使用目标速度（仅远程模式）** ⚡
- **说明**：速度联动服务使用目标速度(`TargetSpeedsMmps`)进行判断
- **原因**：需要基于上游下发的目标速度来触发IO联动，而不是实际反馈速度
- **影响**：当上游将目标速度设置为0时，IO联动立即触发，无需等待轴实际停止
- **模式限制**：**速度联动仅在远程模式下生效，本地模式下不触发**
- **相关文件**：`SpeedLinkageService.cs`

#### 2. **实时速度反馈API** 📊
- **验证**：确认`LastFeedbackMmps`在`PingAsync`方法中正确更新
- **影响**：`GET /api/Axes/axes` API能够正确返回轴的实时反馈速度
- **相关字段**：`FeedbackLinearMmps` (mm/s)

#### 3. **轴使能/失能状态检查** 🔍
- **改进**：在使能/失能轴之前，先读取当前ControlWord状态
- **目的**：避免重复操作，提高状态机转换的可靠性
- **实现**：
  - `EnableAsync`：执行前读取并记录当前ControlWord值
  - `DisableAsync`：执行前读取并记录当前ControlWord值
- **相关文件**：`LeadshineLtdmcAxisDrive.cs`

#### 4. **LeadshineProtocolMap注释完善** 📝
- **改进**：为所有协议映射字段添加详细的XML文档注释
- **覆盖范围**：
  - `BitLen` 类：所有位宽常量
  - `ControlWord` 类：所有控制字命令及其位定义
  - `Mode` 类：所有操作模式值
  - `DelayMs` 类：所有延时参数及其用途
- **影响**：提高代码可读性和可维护性
- **相关文件**：`LeadshineProtocolMap.cs`

#### 5. **运行预警时间持久化修复** 🐛
- **问题**：`RunningWarningSeconds`（运行预警秒数）设置后无法保存到数据库
- **原因**：数据库文档实体和映射方法中缺少该字段
- **修复**：
  - 在`CabinetIndicatorPointDoc`中添加`RunningWarningSeconds`属性
  - 更新`ToOptions`和`ToDoc`映射方法包含该字段
- **影响**：运行预警时间现在能够正确保存和读取
- **相关文件**：
  - `LeadshineCabinetIoOptionsDoc.cs`
  - `ConfigMappings.cs`

---

### ✅ 速度联动配置功能

**核心功能**：新增基于轴速度变化自动控制IO端口的速度联动配置功能

#### 1. 功能特性 ✅
- **多组联动**：支持配置多个独立的速度联动组
- **灵活配置**：每组可包含多个轴ID和多个IO端口
- **自动触发**：
  - 当组内所有轴速度从非0降到0时，自动将指定IO设置为配置的电平
  - 当组内所有轴速度从0提升到非0时，自动将指定IO设置为相反电平
- **实时监控**：后台服务每100ms检查一次轴速度状态
- **持久化存储**：配置保存在LiteDB数据库中
- **REST API**：提供完整的查询、更新、删除配置接口

#### 2. 使用示例

**配置示例**：
```json
PUT /api/io-linkage/speed/configs
{
  "enabled": true,
  "linkageGroups": [
    {
      "axisIds": [1001, 1002],
      "ioPoints": [
        { "bitNumber": 3, "levelWhenStopped": 0 },
        { "bitNumber": 4, "levelWhenStopped": 0 }
      ]
    },
    {
      "axisIds": [1003, 1004],
      "ioPoints": [
        { "bitNumber": 5, "levelWhenStopped": 0 },
        { "bitNumber": 6, "levelWhenStopped": 0 }
      ]
    }
  ]
}
```

**工作原理**：
- 第一组：当轴1001和1002都停止时，IO 3和4设为高电平；当任一轴运动时，IO 3和4设为低电平
- 第二组：当轴1003和1004都停止时，IO 5和6设为高电平；当任一轴运动时，IO 5和6设为低电平

#### 3. API 端点
- `GET /api/io-linkage/speed/configs` - 获取速度联动配置
- `PUT /api/io-linkage/speed/configs` - 更新速度联动配置
- `DELETE /api/io-linkage/speed/configs` - 删除速度联动配置

#### 4. 技术实现
- **配置模型**：`SpeedLinkageOptions`, `SpeedLinkageGroup`, `SpeedLinkageIoPoint`
- **存储层**：`LiteDbSpeedLinkageOptionsStore` - 基于LiteDB的持久化存储
- **服务层**：`SpeedLinkageService` - 后台服务监控轴速度变化
- **控制器**：`SpeedLinkageController` - REST API控制器
- **单元测试**：完整的配置和存储测试覆盖

---

## 项目当前状态

### 📊 代码统计
- **总项目数**：9个
- **总源文件数**：~245个 (.cs, .xaml, .csproj)
- **代码行数**：~26,000行
- **编译状态**：✅ 成功（仅警告来自代码分析器建议）
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

### 📈 项目完成度：约 87%

#### ✅ 已完成的核心功能
1. **核心控制层** (100%)：轴驱动、控制器聚合、事件系统、速度规划
2. **安全管理** (100%)：安全管线、隔离器、物理按键集成、远程/本地模式切换
3. **REST API** (100%)：完整的轴管理、安全控制、上游通信、IO联动API，含完整中文文档
4. **SignalR 实时推送** (100%)：事件Hub、实时通知、队列管理
5. **雷赛驱动** (100%)：LTDMC 总线适配、轴驱动、协议映射
6. **持久化** (100%)：LiteDB 存储、配置管理、对象映射
7. **后台服务** (100%)：心跳、日志泵、传输事件泵、IO联动服务、速度联动服务
8. **IO 联动** (100%)：系统状态联动、速度联动（新增）
9. **文档** (95%)：API文档、架构设计、运维指南
10. **MAUI 客户端** (80%)：基础功能完成，需要完善UI和用户体验

#### ⚠️ 待完善的部分
- **测试覆盖** (50%)：有基础单元测试和速度联动测试，需要更多集成测试
- **部署运维** (30%)：缺少容器化、CI/CD、监控告警
- **MAUI应用** (80%)：需要完善应用图标、深色主题等

---

## 接下来的优化方向

### 🚀 短期优化（1-2周）

#### 1. 代码质量优化
- [ ] 评估更多 DTO 类转换为 record class 的机会
- [ ] 识别可以转换为 readonly struct 的小型值对象
- [ ] 启用并配置代码分析器规则（处理 CA 警告）
- [ ] 统一异常处理策略
- [ ] 优化日志记录规范

#### 2. 测试覆盖率提升
- [x] 速度联动功能单元测试
- [ ] Infrastructure层单元测试扩展
- [ ] Controllers集成测试
- [ ] Safety Pipeline端到端测试
- [ ] 性能基准测试扩展

#### 3. 功能增强
- [x] 速度联动配置功能
- [ ] IO联动配置UI界面
- [ ] 速度联动配置UI界面
- [ ] 实时监控仪表板优化
- [ ] 历史数据查询功能

### 🌟 中期规划（2-4周）

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

### 🎯 长期规划（1-3个月）

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

## 可优化的功能

### 性能优化
1. **轴速度监控频率**：当前100ms轮询，可考虑改为事件驱动模式
2. **IO写入批处理**：当多个联动组同时触发时，可批量写入IO以减少硬件调用
3. **缓存优化**：为频繁访问的配置添加内存缓存
4. **异步并发优化**：某些IO操作可以并行执行以提高响应速度

### 功能增强
1. **速度联动延迟触发**：添加延迟配置，避免瞬间速度波动导致的误触发
2. **速度阈值配置**：支持配置速度阈值而非固定的0值判断
3. **联动组优先级**：支持配置联动组的优先级和互斥关系
4. **IO联动历史记录**：记录IO联动触发历史，便于故障排查
5. **配置热重载**：配置更新无需等待下次检查周期即可生效

### 用户体验
1. **配置验证增强**：添加更详细的配置验证，提供友好的错误提示
2. **配置导入导出**：支持配置的JSON文件导入导出
3. **可视化配置界面**：在MAUI应用中提供图形化配置界面
4. **实时状态监控**：显示各联动组的当前状态和触发历史

### 可靠性
1. **异常恢复机制**：IO写入失败时的重试策略
2. **配置版本管理**：支持配置回滚到历史版本
3. **健康检查**：为速度联动服务添加专门的健康检查端点
4. **监控告警**：联动服务异常时发送告警通知

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

---

## 许可证
（待定）

## 贡献指南

欢迎提交问题和拉取请求！在贡献代码时，请遵循以下准则：

### 代码规范
1. **优先使用枚举**：能使用枚举的地方尽量使用枚举代替int/string
2. **使用不可变类型**：配置和DTO优先使用record class
3. **中文注释和文档**：所有注释和文档必须使用中文
4. **代码变更必须同步更新文档**
5. **提交信息请使用中文**

### 测试要求
- 新功能必须包含单元测试
- 修复bug必须包含回归测试
- 测试覆盖率应保持在合理水平

### 提交流程
1. Fork 项目
2. 创建功能分支
3. 提交代码和测试
4. 确保所有测试通过
5. 提交 Pull Request
