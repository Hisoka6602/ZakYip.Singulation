# 日志分离和厂商目录结构优化

本文档说明了日志分离和厂商目录结构的组织方式，使系统日志更易于管理和排查，同时为未来支持多厂商做好准备。

## 日志分离

### 概述

为了更好地管理和分析日志，系统将不同组件的日志分离到独立的文件中，同时保持异常信息集中记录。

### 日志文件结构

```
logs/
├── all-{date}.log                      # 所有日志（完整记录）
├── error-{date}.log                    # 所有错误和异常
├── udp-{date}.log                      # UDP 服务发现日志（非异常）
├── transport-event-pump-{date}.log     # TransportEventPump 日志（非异常）
├── io-status-worker-{date}.log         # IoStatusWorker 日志（所有级别）
└── nlog-internal.log                   # NLog 内部日志
```

### 日志分类规则

#### 1. UDP 服务发现日志（`logs/udp-{date}.log`）

**包含内容：**
- UDP 广播发送记录
- 服务发现启动/停止信息
- 配置参数信息
- Debug、Info、Warn 级别日志

**排除内容：**
- Error 及以上级别的异常（写入 `error-{date}.log`）

**对应组件：**
- `ZakYip.Singulation.Host.Services.UdpDiscoveryService`

**使用场景：**
- 排查 UDP 服务发现问题
- 分析网络广播频率和内容
- 监控客户端发现服务的过程

#### 2. TransportEventPump 日志（`logs/transport-event-pump-{date}.log`）

**包含内容：**
- 传输层事件处理
- 数据接收统计
- 状态变更记录
- 轴事件处理
- Debug、Info、Warn 级别日志

**排除内容：**
- Error 及以上级别的异常（写入 `error-{date}.log`）

**对应组件：**
- `ZakYip.Singulation.Host.Workers.TransportEventPump`

**使用场景：**
- 分析上游数据流
- 排查传输层连接问题
- 监控轴控制事件
- 性能优化和调试

#### 3. IoStatusWorker 日志（`logs/io-status-worker-{date}.log`）

**包含内容：**
- IO 状态轮询记录
- IO 状态变化
- SignalR 广播记录
- 所有级别日志（包括异常）

**对应组件：**
- `ZakYip.Singulation.Host.Workers.IoStatusWorker`

**使用场景：**
- 排查 IO 监控问题
- 分析 IO 状态变化
- 监控广播频率

#### 4. 错误日志（`logs/error-{date}.log`）

**包含内容：**
- 所有组件的 Error、Fatal 级别日志
- 异常堆栈信息
- 系统级错误

**使用场景：**
- 集中排查所有系统异常
- 生产环境错误监控
- 快速定位故障

### NLog 配置示例

```xml
<!-- UDP 服务发现日志（非异常写入专用文件，异常写入错误文件） -->
<logger name="ZakYip.Singulation.Host.Services.UdpDiscoveryService" 
        minlevel="Trace" maxlevel="Warn" 
        writeTo="udpfile" final="true" />
<logger name="ZakYip.Singulation.Host.Services.UdpDiscoveryService" 
        minlevel="Error" 
        writeTo="errorfile,allfile" final="true" />

<!-- TransportEventPump 日志（非异常写入专用文件，异常写入错误文件） -->
<logger name="ZakYip.Singulation.Host.Workers.TransportEventPump" 
        minlevel="Trace" maxlevel="Warn" 
        writeTo="transporteventpumpfile" final="true" />
<logger name="ZakYip.Singulation.Host.Workers.TransportEventPump" 
        minlevel="Error" 
        writeTo="errorfile,allfile" final="true" />

<!-- IoStatusWorker 日志（包括所有级别） -->
<logger name="ZakYip.Singulation.Host.Workers.IoStatusWorker" 
        minlevel="Trace" 
        writeTo="iostatusworkerfile" final="true" />
```

### 日志管理最佳实践

1. **日志保留策略**：所有日志文件自动按日归档，保留最近 30 天
2. **日志级别调整**：生产环境可将控制台输出级别调整为 Info 或 Warn
3. **监控建议**：定期检查 `error-{date}.log` 了解系统异常情况
4. **性能考虑**：分离日志不影响性能，写入操作异步执行

## 厂商目录结构

### 概述

为了支持多厂商硬件和协议，系统采用厂商特定的目录结构来组织配置、实现和数据存储。

### 目录结构

```
ZakYip.Singulation.Infrastructure/
├── Configs/
│   ├── Entities/                           # 通用配置实体
│   │   ├── AxisGridLayoutDoc.cs
│   │   ├── ControllerOptionsDoc.cs
│   │   ├── DriverOptionsTemplateDoc.cs
│   │   ├── IoStatusMonitorOptionsDoc.cs
│   │   ├── UpstreamCodecOptionsDoc.cs
│   │   └── UpstreamOptionsDoc.cs
│   ├── Mappings/                           # 配置映射
│   │   └── ConfigMappings.cs
│   └── Vendors/                            # 厂商特定配置 ⭐
│       └── Leadshine/
│           ├── Entities/
│           │   └── LeadshineSafetyIoOptionsDoc.cs
│           └── Mappings/                   # 预留：厂商特定映射
│
└── Persistence/
    ├── LiteDbAxisLayoutStore.cs            # 通用存储
    ├── LiteDbControllerOptionsStore.cs
    ├── LiteDbIoStatusMonitorOptionsStore.cs
    ├── PersistenceServiceCollectionExtensions.cs
    └── Vendors/                            # 厂商特定存储 ⭐
        └── Leadshine/
            └── LiteDbLeadshineSafetyIoOptionsStore.cs
```

### 命名空间约定

#### 厂商特定配置实体

```csharp
namespace ZakYip.Singulation.Infrastructure.Configs.Vendors.Leadshine.Entities
```

#### 厂商特定存储实现

```csharp
namespace ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine
```

### 添加新厂商支持

#### 步骤 1：创建目录结构

将 `{VendorName}` 替换为实际厂商名称（如 Leadshine、Siemens 等）：

```bash
# 配置实体目录
mkdir -p ZakYip.Singulation.Infrastructure/Configs/Vendors/{VendorName}/Entities
mkdir -p ZakYip.Singulation.Infrastructure/Configs/Vendors/{VendorName}/Mappings

# 持久化存储目录
mkdir -p ZakYip.Singulation.Infrastructure/Persistence/Vendors/{VendorName}
```

#### 步骤 2：创建配置实体

创建厂商特定的配置文档类（将 `{VendorName}` 替换为实际厂商名称）：

```csharp
// ZakYip.Singulation.Infrastructure/Configs/Vendors/{VendorName}/Entities/{VendorName}OptionsDoc.cs
namespace ZakYip.Singulation.Infrastructure.Configs.Vendors.{VendorName}.Entities {
    public sealed class {VendorName}OptionsDoc {
        public string Id { get; set; } = string.Empty;
        // 厂商特定字段...
    }
}
```

#### 步骤 3：创建存储实现

创建厂商特定的 LiteDB 存储（将 `{VendorName}` 替换为实际厂商名称，`{vendorname}` 为小写形式）：

```csharp
// ZakYip.Singulation.Infrastructure/Persistence/Vendors/{VendorName}/LiteDb{VendorName}Store.cs
using ZakYip.Singulation.Infrastructure.Configs.Vendors.{VendorName}.Entities;

namespace ZakYip.Singulation.Infrastructure.Persistence.Vendors.{VendorName} {
    public sealed class LiteDb{VendorName}Store : I{VendorName}Store {
        private readonly ILiteCollection<{VendorName}OptionsDoc> _col;
        
        public LiteDb{VendorName}Store(ILiteDatabase db, ...) {
            // Collection 名称使用小写厂商名，例如 "leadshine_options"
            _col = db.GetCollection<{VendorName}OptionsDoc>("{vendorname}_options");
            // 实现...
        }
    }
}
```

#### 步骤 4：注册服务

在 `PersistenceServiceCollectionExtensions.cs` 中添加注册方法：

```csharp
using ZakYip.Singulation.Infrastructure.Persistence.Vendors.{VendorName};

public static class PersistenceServiceCollectionExtensions {
    public static IServiceCollection AddLiteDb{VendorName}(this IServiceCollection services) {
        services.AddSingleton<I{VendorName}Store, LiteDb{VendorName}Store>();
        return services;
    }
}
```

#### 步骤 5：更新映射

在 `ConfigMappings.cs` 中添加导入和映射方法：

```csharp
using ZakYip.Singulation.Infrastructure.Configs.Vendors.{VendorName}.Entities;

public static class ConfigMappings {
    public static {VendorName}Options ToOptions(this {VendorName}OptionsDoc d) => new() {
        // 映射字段...
    };
    
    public static {VendorName}OptionsDoc ToDoc(this {VendorName}Options o) => new() {
        // 映射字段...
    };
}
```

### 数据库文件组织

#### 当前实现

所有配置共享同一个 LiteDB 数据库文件（`data/singulation.db`），但通过不同的 Collection 名称区分：

- 通用配置：`controller_options`、`axis_layout`、`io_status_monitor_options` 等
- 雷赛厂商：`leadshine_safety_io_options`

#### 未来扩展（可选）

如果需要为每个厂商使用独立数据库文件（将 `{VendorName}` 替换为实际厂商名称，`{vendorname}` 为小写形式）：

```csharp
// 在 PersistenceServiceCollectionExtensions 中
public static IServiceCollection AddLiteDb{VendorName}(
    this IServiceCollection services, 
    string filePath = "data/vendors/{vendorname}/{vendorname}.db") {
    
    services.AddSingleton<ILiteDatabase>(sp => {
        var resolvedPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(AppContext.BaseDirectory, filePath);
        
        var directory = Path.GetDirectoryName(resolvedPath);
        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }
        
        return new LiteDatabase($"Filename={resolvedPath};Mode=Shared");
    });
    
    services.AddSingleton<I{VendorName}Store, LiteDb{VendorName}Store>();
    return services;
}
```

### 兼容性说明

1. **向后兼容**：现有配置和数据库文件位置保持不变，确保平滑升级
2. **代码迁移**：仅代码文件移动到厂商目录，命名空间更新
3. **导入更新**：使用新命名空间的文件需要更新 `using` 声明

### 已支持厂商

| 厂商 | 配置实体 | 持久化存储 | 数据库 Collection |
|------|---------|-----------|------------------|
| Leadshine（雷赛） | `LeadshineSafetyIoOptionsDoc` | `LiteDbLeadshineSafetyIoOptionsStore` | `leadshine_safety_io_options` |

### 相关文档

- [厂商驱动和协议结构](VENDOR_STRUCTURE.md) - 驱动层和协议层的厂商组织方式
- [README](README.md) - 项目整体架构和使用指南

## 总结

本次优化实现了以下目标：

1. **日志分离**：UDP、TransportEventPump、IoStatusWorker 的日志独立存储，便于问题排查
2. **异常集中**：所有异常仍然写入错误日志，确保不遗漏任何问题
3. **厂商隔离**：配置和存储按厂商组织，为多厂商支持奠定基础
4. **结构清晰**：代码组织更合理，易于维护和扩展

这些改进使系统更易于维护、调试和扩展，同时为未来支持更多厂商硬件做好了准备。
