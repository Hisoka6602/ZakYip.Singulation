# 配置清理说明

## 概述

本文档记录了对 `appsettings.json` 配置文件的清理工作，移除了未使用的配置项以简化配置管理。

## 清理日期

2025-11-12

## 已移除的配置项

### 1. "Urls" 配置项

**移除原因：** 该配置项从未被使用。

**详细说明：**
- ASP.NET Core 的 `"Urls"` 配置项理论上可以被用来配置 Kestrel 监听的 URL
- 但在本项目中，Program.cs 使用 `"KestrelUrl"` 配置项并通过 `webBuilder.UseUrls(url)` 显式设置
- 搜索整个代码库，没有任何地方读取 `"Urls"` 配置项
- `"Urls"` 和 `"KestrelUrl"` 重复，保留 `"KestrelUrl"` 因为它被明确使用

**影响：** 无影响，该配置项从未生效。

### 2. "LogAggregation" 配置节

**移除原因：** 整个配置节及其所有子项从未被使用。

**详细说明：**
- 搜索整个代码库，没有任何 C# 代码引用 `"LogAggregation"` 配置
- 没有对应的配置类（如 `LogAggregationOptions`）
- 没有服务读取或使用这些配置
- Loki 和 Elasticsearch 集成尚未实现
- 这些配置是为未来功能预留的，但目前完全未使用

**移除的子项：**
```json
"LogAggregation": {
  "Enabled": false,
  "Provider": "Loki",
  "Loki": {
    "Endpoint": "http://localhost:3100",
    "Labels": {
      "app": "singulation",
      "environment": "production"
    }
  },
  "Elasticsearch": {
    "Endpoint": "http://localhost:9200",
    "IndexPrefix": "singulation-logs",
    "Username": "",
    "Password": ""
  }
}
```

**影响：** 无影响，这些配置从未被读取或使用。

**未来规划：** 如果需要集成日志聚合系统（Loki/Elasticsearch），应：
1. 创建对应的配置类（如 `LogAggregationOptions`）
2. 实现日志聚合服务
3. 在 Program.cs 中注册服务并绑定配置
4. 重新添加配置项到 appsettings.json

### 3. 注释掉的 "envelope" 示例

**移除原因：** 这是文档性质的注释，不应放在配置文件中。

**详细说明：**
- 这是一个 JSON 格式的示例，用于说明信封格式
- 包含无效的 JSON 语法（如 `"|"` 字符）
- 属于文档内容，应该放在专门的文档文件中，而非配置文件

**移除的内容：**
```json
//信封
/* "envelope": {
  "v": 1,
  "type": "SpeedDecoded",
  "|" "TransportState",
  "|" "Log",
  "|" "...",
  "ts": "2025-10-02T19:xx:xx.fff+08:00",
  "channel": "/vision/speed.decoded",
  "data": { "..." },
  "traceId": "activity id or null",
  "seq": 42
}*/
```

**影响：** 无影响，纯文档内容。

**建议：** 如需保留信封格式说明，应放在以下位置之一：
- `docs/PROTOCOL_FORMAT.md` - 协议格式文档
- `docs/UPSTREAM_PROTOCOL_INTEGRATION_GUIDE.md` - 上游协议集成指南
- 代码注释中的相关类

## 保留的配置项

以下配置项经过验证，确认正在使用，已保留：

### 1. Logging

**用途：** ASP.NET Core 内置日志系统配置

**使用位置：** 
- ASP.NET Core 框架自动读取
- 配置日志级别和输出目标

### 2. LogsCleanup

**用途：** 日志文件清理服务配置

**使用位置：**
- `ZakYip.Singulation.Infrastructure/Workers/LogsCleanupOptions.cs` - 配置类
- `ZakYip.Singulation.Infrastructure/Workers/LogsCleanupService.cs` - 后台服务
- `Program.cs` - 注册服务：`services.Configure<LogsCleanupOptions>(configuration.GetSection("LogsCleanup"))`

**配置项：**
- `MainLogRetentionDays`: 主日志保留天数
- `HighFreqLogRetentionDays`: 高频日志保留天数
- `ErrorLogRetentionDays`: 错误日志保留天数

### 3. KestrelUrl

**用途：** Kestrel HTTP 服务器监听地址配置

**使用位置：**
- `Program.cs` - Kestrel 配置：`webBuilder.UseUrls(url)`
- `Program.cs` - Windows 防火墙管理：提取端口号用于防火墙规则

**配置值：** `http://localhost:5005`

### 4. UdpDiscovery

**用途：** UDP 服务发现配置

**使用位置：**
- `ZakYip.Singulation.Infrastructure/Services/UdpDiscoveryService.cs` - UDP 发现服务
- `ZakYip.Singulation.MauiApp` - MAUI 应用使用 UDP 发现服务端点

**配置项：**
- `Enabled`: 是否启用 UDP 发现
- `BroadcastPort`: UDP 广播端口
- `BroadcastIntervalSeconds`: 广播间隔（秒）
- `ServiceName`: 服务名称
- `HttpPort`: HTTP 端口
- `HttpsPort`: HTTPS 端口（MAUI 应用使用）

### 5. LeadshineBus

**用途：** 雷赛运动控制总线配置

**使用位置：**
- `ZakYip.Singulation.Drivers.Leadshine` - 雷赛驱动器初始化
- 配置雷赛控制卡参数

**配置项：**
- `CardNo`: 控制卡编号
- `PortNo`: 端口号
- `ControllerIp`: 控制器 IP 地址

## 清理后的配置文件

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "LogsCleanup": {
    "MainLogRetentionDays": 2,
    "HighFreqLogRetentionDays": 2,
    "ErrorLogRetentionDays": 2
  },
  "KestrelUrl": "http://localhost:5005",
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Service",
    "HttpPort": 5005,
    "HttpsPort": 5006
  },
  "LeadshineBus": {
    "CardNo": 0,
    "PortNo": 1,
    "ControllerIp": "192.168.1.100"
  }
}
```

## 验证

清理后进行了以下验证：

1. ✅ **编译验证：** 项目成功编译，无错误
   ```bash
   dotnet build ZakYip.Singulation.Host -c Release
   # Build succeeded with 2 warnings (unrelated to configuration)
   ```

2. ✅ **代码搜索验证：** 确认移除的配置项在代码中无引用
   ```bash
   grep -r "Urls" --include="*.cs"           # 无结果（除 UseUrls 方法调用）
   grep -r "LogAggregation" --include="*.cs" # 无结果
   ```

3. ✅ **配置类验证：** 确认保留的配置项都有对应的配置类
   - LogsCleanupOptions ✅
   - UdpDiscoveryOptions ✅
   - LeadshineBusOptions ✅ (隐式使用)

## 影响分析

### 对现有功能的影响

**无影响** - 移除的配置项从未被使用，不会影响任何现有功能。

### 对未来开发的影响

如果未来需要添加日志聚合功能（Loki/Elasticsearch），需要：

1. 创建配置类：
   ```csharp
   public sealed record class LogAggregationOptions {
       public bool Enabled { get; init; }
       public string Provider { get; init; } = "Loki";
       public LokiOptions Loki { get; init; } = new();
       public ElasticsearchOptions Elasticsearch { get; init; } = new();
   }
   ```

2. 实现日志聚合服务

3. 注册服务：
   ```csharp
   services.Configure<LogAggregationOptions>(
       configuration.GetSection("LogAggregation"));
   ```

4. 重新添加配置到 appsettings.json

## 维护建议

### 定期审查配置文件

建议每个季度审查一次配置文件：

1. 检查是否有未使用的配置项
2. 验证所有配置项都有对应的代码引用
3. 更新配置文档

### 添加新配置项的最佳实践

1. **先实现代码，后添加配置**
   - 创建配置类（Options 模式）
   - 实现使用配置的服务
   - 在 Program.cs 中注册
   - 最后添加到 appsettings.json

2. **使用强类型配置**
   - 避免使用 `IConfiguration["Key"]` 直接访问
   - 使用 `IOptions<T>` 模式
   - 提供默认值

3. **文档化配置项**
   - 在配置类中添加 XML 注释
   - 在配置指南中说明用途
   - 提供示例值

### 避免未使用配置的方法

1. **代码审查：** 在 PR 中审查配置文件变更
2. **自动化检测：** 考虑编写工具扫描未引用的配置项
3. **文档同步：** 配置变更时同步更新文档

## 相关文档

- [配置指南](../ops/CONFIGURATION_GUIDE.md)
- [运维手册](../ops/OPERATIONS_MANUAL.md)
- [开发指南](DEVELOPER_GUIDE.md)

## 变更历史

| 日期 | 变更内容 | 负责人 |
|------|---------|--------|
| 2025-11-12 | 初始清理：移除 Urls, LogAggregation, envelope 注释 | GitHub Copilot |

## 附录：配置项搜索脚本

以下脚本可用于检查配置项是否被使用：

```bash
#!/bin/bash
# check_config_usage.sh - 检查配置项使用情况

CONFIG_FILE="ZakYip.Singulation.Host/appsettings.json"
CODE_DIR="."

echo "检查配置项使用情况..."
echo ""

# 提取配置项（简化版本，仅提取第一级键）
CONFIG_KEYS=$(grep -o '"[^"]*"' $CONFIG_FILE | grep -v ":" | sort -u)

for key in $CONFIG_KEYS; do
    key_clean=$(echo $key | tr -d '"')
    echo "检查: $key_clean"
    
    # 搜索代码中的引用
    count=$(grep -r "$key_clean" --include="*.cs" $CODE_DIR 2>/dev/null | wc -l)
    
    if [ $count -eq 0 ]; then
        echo "  ⚠️  未找到引用"
    else
        echo "  ✅ 找到 $count 处引用"
    fi
    echo ""
done
```

使用方法：
```bash
chmod +x check_config_usage.sh
./check_config_usage.sh
```
