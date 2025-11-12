# 日志磁盘空间优化指南 / Log Disk Space Optimization Guide

## 概述 / Overview

本文档说明如何配置日志系统以防止生成过多的重复日志，避免占用大量硬盘空间。
This document explains how to configure the logging system to prevent excessive duplicate logs and disk space consumption.

## 问题 / Problem

- **高频日志泛滥**：某些组件（如心跳、UDP广播、实时数据推送）每秒可能产生数百条日志
- **重复日志占用空间**：相同或相似的日志消息重复记录
- **日志文件快速增长**：未经优化的日志配置可能导致每天数GB的日志文件
- **存储空间不足**：长期累积的日志文件占用大量磁盘空间

## 解决方案 / Solutions

### 1. 多层日志采样策略 / Multi-Layer Log Sampling Strategy

#### 1.1 应用层采样 (Application-Level Sampling)

使用 `LogSampler` 在代码中实现智能采样：

```csharp
using ZakYip.Singulation.Infrastructure.Logging;

public class MyService : BackgroundService
{
    private readonly ILogger<MyService> _logger;
    private readonly LogSampler _logSampler = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 高频操作
            DoHighFrequencyWork();
            
            // 每100次记录一次日志
            if (_logSampler.ShouldLog("HighFreqOperation", 100))
            {
                _logger.LogDebug("高频操作已执行 {Count} 次", _logSampler.GetCount("HighFreqOperation"));
            }
        }
    }
}
```

**已应用采样的服务：**
- `HeartbeatWorker` - 每100次心跳记录一次
- `UdpDiscoveryService` - 每20次广播记录一次
- `RealtimeAxisDataService` - 数据推送失败时每100次记录一次

#### 1.2 NLog层采样 (NLog-Level Sampling)

在 `nlog.config` 中配置 `LimitingWrapper` 进行二次采样：

```xml
<!-- 每秒最多记录5条日志 -->
<target xsi:type="LimitingWrapper" name="sampled" messageLimitSize="5" timeLimit="00:00:01">
  <target-ref name="actualTarget" />
</target>
```

**当前配置：**
- 高频组件专用日志：每秒最多5条
- 主日志文件：每秒最多100条
- 错误日志：不采样，保留所有错误

### 2. 日志级别优化 / Log Level Optimization

#### 2.1 appsettings.json 配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "ZakYip.Singulation.Infrastructure.Workers.HeartbeatWorker": "Warning",
      "ZakYip.Singulation.Infrastructure.Services.RealtimeAxisDataService": "Warning"
    }
  }
}
```

**说明：**
- `Default: Information` - 生产环境标准级别
- 高频组件设置为 `Warning` - 仅记录警告和错误
- 开发环境可临时降低到 `Debug` 进行调试

#### 2.2 日志级别使用指南

| 级别 | 使用场景 | 生产环境 | 磁盘影响 |
|-----|---------|---------|---------|
| Debug | 详细诊断信息 | 禁用 | 高 |
| Information | 业务关键事件 | 启用 | 中 |
| Warning | 可恢复的问题 | 启用 | 低 |
| Error | 操作失败 | 启用 | 低 |
| Critical | 系统故障 | 启用 | 极低 |

### 3. 日志文件管理 / Log File Management

#### 3.1 文件大小限制

```xml
<!-- nlog.config -->
<variable name="archiveAboveSizeMB" value="10" />
```

- **单个日志文件限制**：10 MB
- **达到限制后**：自动归档并压缩
- **压缩格式**：`.zip`（节省约70%空间）

#### 3.2 日志保留策略

```json
{
  "LogsCleanup": {
    "MainLogRetentionDays": 7,
    "HighFreqLogRetentionDays": 3,
    "ErrorLogRetentionDays": 30
  }
}
```

| 日志类型 | 保留天数 | 说明 |
|---------|---------|------|
| 主日志 (all-*.log) | 7天 | 常规业务日志 |
| 高频日志 (udp-*, transport-*) | 3天 | 快速轮转的专用日志 |
| 错误日志 (error-*.log) | 30天 | 长期保留用于分析 |
| JSON日志 (structured-*.json) | 7天 | 结构化日志，用于聚合 |

#### 3.3 自动清理服务

`LogsCleanupService` 每天凌晨2点自动运行：

```csharp
// 自动删除过期日志
// 压缩归档文件
// 释放磁盘空间
```

### 4. 日志文件组织 / Log File Organization

```
logs/
├── all-20250112.log              (当天主日志)
├── all-20250111.log.zip          (昨天主日志，已压缩)
├── error-20250112.log            (当天错误日志)
├── error-20250111.log.zip        (昨天错误日志，已压缩)
├── udp-20250112.log              (UDP专用日志，3天保留)
├── transport-event-pump-20250112.log  (传输事件日志，3天保留)
├── structured-20250112.json      (结构化日志)
└── nlog-internal.log             (NLog内部日志)
```

### 5. 磁盘空间估算 / Disk Space Estimation

#### 未优化前 (Before Optimization)

```
每秒日志：~500条
每条大小：~200字节
每天大小：500 × 200 × 86400 = ~8.6 GB/天
30天累积：~258 GB
```

#### 优化后 (After Optimization)

```
每秒日志：~50条（采样后）
每条大小：~200字节
每天大小：50 × 200 × 86400 = ~860 MB/天
压缩后：~258 MB/天（压缩率70%）
7天主日志：~1.8 GB
3天高频日志：~0.8 GB
30天错误日志：~1.5 GB
总计：~4.1 GB（减少98%）
```

### 6. 监控和维护 / Monitoring and Maintenance

#### 6.1 日志清理监控

查看日志清理服务输出：

```log
日志清理完成：删除 15 个文件，释放 2.34 MB 空间
下次日志清理时间：2025-01-13 02:00:00，等待 23:45:30
```

#### 6.2 磁盘空间检查

```bash
# Linux
du -sh logs/
df -h

# Windows
dir /s logs
```

#### 6.3 手动清理（紧急情况）

```bash
# 删除3天前的所有日志（保留错误日志）
cd logs
find . -name "*.log" -mtime +3 ! -name "error-*" -delete

# 压缩未压缩的旧日志
find . -name "*.log" -mtime +1 -exec gzip {} \;
```

### 7. 生产环境最佳实践 / Production Best Practices

#### 7.1 日志级别配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning",
      "ZakYip.Singulation.Infrastructure.Workers.HeartbeatWorker": "Warning",
      "ZakYip.Singulation.Infrastructure.Services.RealtimeAxisDataService": "Warning",
      "ZakYip.Singulation.Infrastructure.Services.UdpDiscoveryService": "Information"
    }
  }
}
```

#### 7.2 NLog 控制台输出优化

```xml
<!-- 生产环境只在控制台显示 Warning 及以上 -->
<logger name="*" minlevel="Warning" writeTo="coloredConsole" />
```

#### 7.3 定期审查日志配置

- **每月审查**：检查日志量和磁盘占用
- **调整采样率**：根据实际需求调整
- **更新保留策略**：根据合规要求调整

### 8. 故障排查 / Troubleshooting

#### 8.1 日志量仍然很大

**检查清单：**
1. ✅ 确认应用层采样已启用
2. ✅ 确认 NLog LimitingWrapper 配置正确
3. ✅ 确认高频组件日志级别设置为 Warning
4. ✅ 检查是否有第三方库产生大量日志
5. ✅ 验证日志清理服务是否正常运行

**临时解决方案：**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"  // 临时提高最低级别
    }
  }
}
```

#### 8.2 重要日志丢失

**问题：** 采样率过高导致重要日志被跳过

**解决方案：**
- Error 和 Critical 级别日志从不采样
- 业务关键操作使用 Information 级别（不使用 Debug）
- 调整采样率或使用时间基采样

```csharp
// 对于重要操作，使用更低的采样率
if (_logSampler.ShouldLog("CriticalOperation", 10))  // 每10次记录一次
{
    _logger.LogInformation("关键操作执行");
}
```

### 9. 总结 / Summary

通过以下措施可有效控制日志磁盘占用：

1. ✅ **双层采样**：应用层 + NLog层
2. ✅ **日志级别优化**：高频组件使用 Warning 级别
3. ✅ **文件大小限制**：单文件10MB，自动归档压缩
4. ✅ **保留策略**：主日志7天，高频3天，错误30天
5. ✅ **自动清理**：每天凌晨2点自动运行
6. ✅ **压缩归档**：节省约70%磁盘空间

**预期效果：**
- 日志量减少 90-95%
- 磁盘占用减少 98%
- 保留所有关键信息
- 不影响故障诊断能力

## 参考 / References

- [LOGGING_BEST_PRACTICES.md](./LOGGING_BEST_PRACTICES.md) - 日志记录最佳实践
- [NLog Configuration](https://nlog-project.org/config/) - NLog 官方文档
- LogSampler 实现：`ZakYip.Singulation.Infrastructure/Logging/LogSampler.cs`
- LogsCleanupService 实现：`ZakYip.Singulation.Infrastructure/Workers/LogsCleanupService.cs`
