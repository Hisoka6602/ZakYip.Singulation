# 日志分离配置说明

本文档说明 ZakYip.Singulation 项目中日志文件的分离配置，便于不同组件的日志独立查看和管理。

## 日志文件结构

### 通用日志文件

- **logs/all-{日期}.log** - 所有级别的日志（包含所有组件）
- **logs/error-{日期}.log** - 所有错误级别的日志（包含所有组件的异常）

### 专属日志文件

#### 1. UDP Discovery 服务日志
- **文件**: `logs/udp-discovery-{日期}.log`
- **内容**: UDP 服务发现相关的非异常日志（Debug、Info、Warn 级别）
- **异常处理**: 异常日志（Error 级别）仍记录到 `error-{日期}.log`
- **用途**: 追踪 UDP 广播服务的运行状态、广播消息等

#### 2. TransportEventPump 日志
- **文件**: `logs/transport-event-pump-{日期}.log`
- **内容**: 传输事件泵相关的非异常日志（Debug、Info、Warn 级别）
- **异常处理**: 异常日志（Error 级别）仍记录到 `error-{日期}.log`
- **用途**: 追踪传输层事件处理、数据流转、状态变更等

#### 3. IoStatusWorker 日志
- **文件**: `logs/io-status-worker-{日期}.log`
- **内容**: IO 状态监控的所有日志（包括异常）
- **异常处理**: 异常日志也记录在此文件中（与其他组件不同）
- **用途**: 完整追踪 IO 状态监控的所有活动

## 配置位置

日志配置文件位于：
```
ZakYip.Singulation.Host/nlog.config
```

## 日志路由规则

### UDP Discovery Service
```xml
<!-- 非异常日志 → udp-discovery-{日期}.log -->
<logger name="ZakYip.Singulation.Host.Services.UdpDiscoveryService" maxlevel="Warn" writeTo="udpfile" final="false" />

<!-- 异常日志 → error-{日期}.log -->
<logger name="ZakYip.Singulation.Host.Services.UdpDiscoveryService" minlevel="Error" writeTo="errorfile" final="false" />

<!-- 所有日志 → all-{日期}.log 和控制台 -->
<logger name="ZakYip.Singulation.Host.Services.UdpDiscoveryService" minlevel="Trace" writeTo="allfile,coloredConsole" final="true" />
```

### TransportEventPump
```xml
<!-- 非异常日志 → transport-event-pump-{日期}.log -->
<logger name="ZakYip.Singulation.Host.Workers.TransportEventPump" maxlevel="Warn" writeTo="transportpumpfile" final="false" />

<!-- 异常日志 → error-{日期}.log -->
<logger name="ZakYip.Singulation.Host.Workers.TransportEventPump" minlevel="Error" writeTo="errorfile" final="false" />

<!-- 所有日志 → all-{日期}.log 和控制台 -->
<logger name="ZakYip.Singulation.Host.Workers.TransportEventPump" minlevel="Trace" writeTo="allfile,coloredConsole" final="true" />
```

### IoStatusWorker
```xml
<!-- 所有日志（包含异常）→ io-status-worker-{日期}.log -->
<logger name="ZakYip.Singulation.Host.Workers.IoStatusWorker" minlevel="Trace" writeTo="iostatusfile" final="false" />

<!-- 所有日志 → all-{日期}.log 和控制台 -->
<logger name="ZakYip.Singulation.Host.Workers.IoStatusWorker" minlevel="Trace" writeTo="allfile,coloredConsole" final="true" />
```

## 日志格式

### 专属文件（非异常）
```
${longdate}|${level:uppercase=true}|${logger}|${message}
```
示例：
```
2025-10-27 10:50:00.123|INFO|ZakYip.Singulation.Host.Services.UdpDiscoveryService|UDP 服务发现服务启动，端口: 18888
```

### 专属文件（包含异常）
```
${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}
```
示例：
```
2025-10-27 10:50:00.123|ERROR|ZakYip.Singulation.Host.Workers.IoStatusWorker|查询 IO 状态失败 System.Exception: ...
```

## 日志归档

所有日志文件均配置为：
- **归档周期**: 每天
- **归档编号**: 按日期（yyyyMMdd）
- **保留天数**: 30 天
- **编码**: UTF-8

## 查看日志建议

### 查看 UDP 服务发现活动
```bash
tail -f logs/udp-discovery-{今日日期}.log
```

### 查看传输事件处理
```bash
tail -f logs/transport-event-pump-{今日日期}.log
```

### 查看 IO 状态监控
```bash
tail -f logs/io-status-worker-{今日日期}.log
```

### 查看所有错误
```bash
tail -f logs/error-{今日日期}.log
```

### 查看完整日志
```bash
tail -f logs/all-{今日日期}.log
```

## 日志分离的优势

1. **专注性**: 每个组件的日志独立，便于快速定位问题
2. **性能**: 减少单个日志文件的大小，提高查询效率
3. **维护性**: 异常与正常日志分离，便于日志分析和告警
4. **灵活性**: 可以针对不同组件调整日志级别和保留策略

## 注意事项

1. **异常处理差异**: 
   - UDP Discovery Service 和 TransportEventPump 的异常记录在 `error-{日期}.log`
   - IoStatusWorker 的异常同时记录在专属文件和 `error-{日期}.log`

2. **控制台输出**: 所有组件的日志仍然输出到控制台，便于实时监控

3. **all 文件**: 所有组件的日志仍然记录到 `all-{日期}.log`，便于全局查看

4. **日志级别**: 可以在 `appsettings.json` 中调整各组件的日志级别

## 相关文档

- [nlog.config](nlog.config) - NLog 配置文件
- [appsettings.json](appsettings.json) - 应用程序配置
- [ARCHITECTURE.md](../docs/ARCHITECTURE.md) - 系统架构文档
