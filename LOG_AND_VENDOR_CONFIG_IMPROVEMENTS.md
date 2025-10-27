# 日志分离和厂商配置改进说明

本文档记录了针对问题陈述中提出的日志分离和厂商配置结构改进的实施情况。

## 改进概述

### 1. 日志分离（问题 1、2、3）

#### 实施的改进：

**1.1 UDP Discovery Service 日志分离**
- 新增专属日志文件：`logs/udp-discovery-{日期}.log`
- 记录内容：非异常日志（Debug、Info、Warn 级别）
- 异常处理：Error 级别的异常仍记录到 `logs/error-{日期}.log`
- 配置位置：`nlog.config` 第 30-38 行（target）、第 77-80 行（rules）

**1.2 TransportEventPump 日志分离**
- 新增专属日志文件：`logs/transport-event-pump-{日期}.log`
- 记录内容：非异常日志（Debug、Info、Warn 级别）
- 异常处理：Error 级别的异常仍记录到 `logs/error-{日期}.log`
- 配置位置：`nlog.config` 第 40-48 行（target）、第 82-85 行（rules）

**1.3 IoStatusWorker 日志分离**
- 新增专属日志文件：`logs/io-status-worker-{日期}.log`
- 记录内容：所有日志（包括异常）
- 异常处理：包含在专属日志文件中（与前两者不同）
- 配置位置：`nlog.config` 第 50-58 行（target）、第 87-89 行（rules）

**特点说明：**
- ✅ 三个组件的日志均已独立到专属文件
- ✅ UDP 和 TransportEventPump 的异常单独记录到 error 文件
- ✅ IoStatusWorker 的所有日志（包括异常）记录到专属文件
- ✅ 所有日志仍然保留在 `all-{日期}.log` 中便于全局查看
- ✅ 所有日志仍然输出到控制台便于实时监控

### 2. 厂商配置结构优化（问题 4）

#### 实施的改进：

**2.1 创建厂商配置目录结构**
```
ZakYip.Singulation.Host/Config/Vendors/
├── Axis/           # 轴控制厂商配置
│   └── leadshine.json
├── Protocol/       # 上游协议厂商配置
│   ├── guiwei.json
│   └── huarary.json
├── Io/             # IO 厂商配置
│   └── leadshine.json
└── README.md       # 配置说明文档
```

**2.2 厂商配置文件**

每个配置文件包含：
- **Vendor**: 厂商名称
- **Category**: 配置类别（Axis/Protocol/Io）
- **Description**: 配置描述
- **Enabled**: 是否启用（Protocol 和 Io 类别）
- 类别特定的配置节点

**示例：Axis/leadshine.json**
- BusConfig: 总线配置（CardNo, PortNo, ControllerIp）
- AxisConfig: 轴配置（最大轴数、加减速度、最大速度）
- Communication: 通信配置（协议、超时、重试次数）
- Advanced: 高级配置（健康监控、弹性配置）

**示例：Protocol/huarary.json**
- Transport: 传输层配置（类型、端口、缓冲区大小、超时）
- Protocol: 协议配置（起止字节、校验类型、编码）
- Features: 功能特性（支持的帧类型、双向通信等）

**示例：Io/leadshine.json**
- InputConfig: 输入配置（起始地址、数量、轮询间隔）
- OutputConfig: 输出配置（起始地址、数量）
- Features: 功能特性（数字/模拟 IO 支持）
- Monitoring: 监控配置（状态广播、广播间隔、SignalR 频道）

**2.3 主配置文件更新**

`appsettings.json` 增加 `Vendors` 节点：
```json
"Vendors": {
  "Axis": {
    "Leadshine": {
      "BusConfig": { /* ... */ }
    }
  },
  "Protocol": {
    "Guiwei": { "Enabled": false },
    "Huarary": { "Enabled": true }
  },
  "Io": {
    "Leadshine": { "Enabled": true }
  }
}
```

同时保留原有的 `LeadshineBus` 配置以保持向后兼容。

**2.4 项目文件更新**

更新 `ZakYip.Singulation.Host.csproj`：
- 添加 `Config/Vendors/**/*.json` 文件的复制规则
- 添加 `Config/Vendors/README.md` 文件的复制规则
- 确保配置文件在构建时复制到输出目录

## 文件清单

### 修改的文件
1. **nlog.config** - 添加三个新的日志目标和对应的路由规则
2. **appsettings.json** - 添加 Vendors 节点结构化厂商配置
3. **ZakYip.Singulation.Host.csproj** - 添加厂商配置文件的复制规则

### 新增的文件
1. **Config/Vendors/Axis/leadshine.json** - 雷赛轴控制配置
2. **Config/Vendors/Protocol/guiwei.json** - 归位协议配置
3. **Config/Vendors/Protocol/huarary.json** - 华雷协议配置
4. **Config/Vendors/Io/leadshine.json** - 雷赛 IO 配置
5. **Config/Vendors/README.md** - 厂商配置目录说明文档
6. **LOGGING_CONFIGURATION.md** - 日志配置详细说明文档

## 优势和好处

### 日志分离的优势
1. **问题定位更快**: 每个组件的日志独立，快速查找特定组件的问题
2. **文件更小**: 分散的日志文件比单一大文件更易于管理和查询
3. **灵活的异常处理**: UDP 和 Transport 的异常集中管理，IoStatusWorker 完整记录
4. **保持全局视图**: 仍然保留 all 和 error 汇总日志文件

### 厂商配置结构的优势
1. **扩展性强**: 支持未来添加更多厂商（西门子、三菱、台达、基恩士等）
2. **分类清晰**: 按 Axis、Protocol、Io 分类，职责明确
3. **配置独立**: 每个厂商的配置文件独立，互不影响
4. **易于维护**: 配置文件小而专注，便于理解和修改
5. **文档完善**: 包含详细的 README 和配置示例
6. **向后兼容**: 保留原有配置结构，平滑迁移

## 使用方法

### 查看日志
```bash
# 查看 UDP 服务发现日志
tail -f logs/udp-discovery-2025-10-27.log

# 查看传输事件泵日志
tail -f logs/transport-event-pump-2025-10-27.log

# 查看 IO 状态监控日志
tail -f logs/io-status-worker-2025-10-27.log

# 查看所有错误
tail -f logs/error-2025-10-27.log
```

### 添加新厂商配置
1. 在对应类别目录下创建 `{vendor}.json` 文件
2. 参考现有配置文件的结构填写配置
3. 在 `appsettings.json` 的 `Vendors` 节点中添加引用（如需要）
4. 重新构建项目

### 调整日志级别
在 `appsettings.json` 中修改：
```json
"Logging": {
  "LogLevel": {
    "ZakYip.Singulation.Host.Services.UdpDiscoveryService": "Debug",
    "ZakYip.Singulation.Host.Workers.TransportEventPump": "Information",
    "ZakYip.Singulation.Host.Workers.IoStatusWorker": "Warning"
  }
}
```

## 验证

### 构建验证
```bash
cd ZakYip.Singulation.Host
dotnet build
```
✅ 构建成功，无错误和警告

### 配置文件验证
```bash
ls bin/Debug/net8.0/Config/Vendors/
```
✅ 所有厂商配置文件已复制到输出目录

### 文件完整性验证
- ✅ nlog.config 包含三个新的日志目标
- ✅ nlog.config 包含对应的日志路由规则
- ✅ appsettings.json 包含 Vendors 节点
- ✅ 所有厂商配置文件格式正确（JSON 有效）

## 后续建议

### 日志方面
1. 根据实际使用情况调整日志级别
2. 考虑添加日志查询工具或日志聚合系统
3. 定期清理过期日志文件（已配置保留 30 天）

### 厂商配置方面
1. 根据需要添加更多厂商配置
2. 考虑实现配置文件的动态加载和热更新
3. 添加配置文件的验证机制
4. 将配置对象映射到强类型类以便在代码中使用

## 相关文档

- [LOGGING_CONFIGURATION.md](ZakYip.Singulation.Host/LOGGING_CONFIGURATION.md) - 日志配置详细说明
- [Config/Vendors/README.md](ZakYip.Singulation.Host/Config/Vendors/README.md) - 厂商配置目录说明
- [VENDOR_STRUCTURE.md](VENDOR_STRUCTURE.md) - 厂商代码结构文档
- [nlog.config](ZakYip.Singulation.Host/nlog.config) - NLog 配置文件
- [appsettings.json](ZakYip.Singulation.Host/appsettings.json) - 应用程序配置

## 总结

本次改进完全满足了问题陈述中的所有要求：

✅ **要求 1**: UDP 相关日志单独一个文件存放（异常除外）
✅ **要求 2**: TransportEventPump 相关日志单独一个文件存放（异常除外）
✅ **要求 3**: IoStatusWorker 相关日志单独一个文件存放
✅ **要求 4**: 调整文件目录和命名，不同厂商的配置/实现分别命名和专属目录

所有改进均已实现、测试和文档化，代码已提交到仓库。
