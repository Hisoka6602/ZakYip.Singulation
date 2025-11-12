# 日志聚合方案指南

## 📋 概述

本指南介绍 ZakYip.Singulation 系统的日志管理和聚合方案，解决分布式环境下的日志分散、难以分析和性能问题。

## 🎯 问题描述

### 原有问题
1. **日志分散**：分布式环境下日志存储在各个节点，难以集中查看
2. **分析困难**：缺乏统一的日志查询和分析工具
3. **性能影响**：大量日志写入影响系统性能
4. **存储压力**：日志文件增长过快，占用磁盘空间

### 解决方案
1. ✅ **结构化日志**：采用 JSON 格式，便于解析和查询
2. ✅ **日志轮转**：按日期和大小自动归档，压缩旧日志
3. ✅ **日志采样**：高频日志应用采样策略，减少写入量
4. ✅ **分级保留**：不同类型日志采用不同保留期限
5. ✅ **聚合准备**：预留 Loki/ELK 集成接口

## 📁 日志文件分类

### 主要日志文件

| 文件名模式 | 保留期限 | 说明 | 采样策略 |
|-----------|---------|------|---------|
| `all-{date}.log` | 30天 | 所有级别日志（传统格式） | 无 |
| `structured-{date}.json` | 30天 | 结构化JSON日志（聚合） | 无 |
| `error-{date}.log` | 90天 | 错误和异常日志 | 无 |
| `udp-{date}.log` | 7天 | UDP服务发现日志 | 10条/秒 |
| `transport-event-pump-{date}.log` | 7天 | 传输事件日志 | 10条/秒 |
| `io-status-worker-{date}.log` | 7天 | IO状态监控日志 | 10条/秒 |

### 日志轮转策略

- **按日期轮转**：每天凌晨自动创建新文件
- **按大小轮转**：单个文件超过 50MB 自动归档
- **自动压缩**：归档文件自动 gzip 压缩
- **自动清理**：每天凌晨 2 点执行清理任务

## 🔧 配置说明

### NLog 配置 (`nlog.config`)

#### 1. 结构化日志（JSON格式）

```xml
<target xsi:type="File" name="jsonfile"
        fileName="${logDirectory}/structured-${shortdate}.json"
        encoding="utf-8">
  <layout xsi:type="JsonLayout" includeEventProperties="true">
    <attribute name="timestamp" layout="${longdate}" />
    <attribute name="level" layout="${level:uppercase=true}" />
    <attribute name="logger" layout="${logger}" />
    <attribute name="message" layout="${message}" />
    <attribute name="exception" layout="${exception:format=ToString,StackTrace}" />
    <attribute name="machineName" layout="${machinename}" />
    <attribute name="processId" layout="${processid}" />
    <attribute name="threadId" layout="${threadid}" />
  </layout>
</target>
```

**特点**：
- 完整的上下文信息（机器名、进程ID、线程ID）
- 结构化异常信息，包含堆栈跟踪
- 事件属性自动包含
- 易于日志聚合工具解析

#### 2. 日志采样策略

```xml
<target xsi:type="LimitingWrapper" name="udpsampled" 
        messageLimitSize="10" 
        timeLimit="00:00:01">
  <target-ref name="udpfile" />
</target>
```

**策略**：
- 每秒最多记录 10 条相同类别的日志
- 超出部分自动丢弃，避免日志爆炸
- 适用于高频日志（UDP、Transport、IoStatus）

#### 3. 归档和压缩

```xml
<target xsi:type="File" name="allfile"
        archiveEvery="Day"
        archiveNumbering="Date"
        archiveDateFormat="yyyyMMdd"
        maxArchiveFiles="30"
        archiveAboveSize="50000000"
        enableArchiveFileCompression="true" />
```

**特性**：
- 每天自动归档
- 文件超过 50MB 立即归档
- gzip 压缩，节省 70-80% 空间
- 保留最近 30 个归档文件

### 应用配置 (`appsettings.json`)

```json
{
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
}
```

**说明**：
- `Enabled`: 是否启用日志聚合（默认 false）
- `Provider`: 聚合方案提供商（Loki 或 Elasticsearch）
- 预留配置接口，便于未来集成

## 🚀 日志聚合方案

### 方案一：Grafana Loki（推荐）

#### 优势
- ✅ 轻量级，资源占用少
- ✅ 与 Grafana 无缝集成（已部署）
- ✅ 对标签和时间范围查询优化
- ✅ 不需要索引整个日志内容
- ✅ 成本低，适合中小规模部署

#### 部署步骤

1. **启动 Loki 服务**（Docker Compose）

```yaml
# 添加到 docker-compose.monitoring.yml
loki:
  image: grafana/loki:latest
  container_name: singulation-loki
  restart: unless-stopped
  ports:
    - "3100:3100"
  volumes:
    - ./loki/loki-config.yml:/etc/loki/loki-config.yml:ro
    - loki-data:/loki
  command: -config.file=/etc/loki/loki-config.yml
  networks:
    - monitoring
```

2. **配置 Loki**（`monitoring/loki/loki-config.yml`）

```yaml
auth_enabled: false

server:
  http_listen_port: 3100

ingester:
  lifecycler:
    ring:
      kvstore:
        store: inmemory
      replication_factor: 1
  chunk_idle_period: 5m
  chunk_retain_period: 30s

schema_config:
  configs:
    - from: 2024-01-01
      store: boltdb-shipper
      object_store: filesystem
      schema: v11
      index:
        prefix: index_
        period: 24h

storage_config:
  boltdb_shipper:
    active_index_directory: /loki/boltdb-shipper-active
    cache_location: /loki/boltdb-shipper-cache
    shared_store: filesystem
  filesystem:
    directory: /loki/chunks

limits_config:
  enforce_metric_name: false
  reject_old_samples: true
  reject_old_samples_max_age: 168h  # 7天

chunk_store_config:
  max_look_back_period: 720h  # 30天

table_manager:
  retention_deletes_enabled: true
  retention_period: 720h  # 30天
```

3. **安装 NLog.Targets.Loki**

```bash
cd ZakYip.Singulation.Core
dotnet add package NLog.Targets.Loki
```

4. **更新 nlog.config**

```xml
<extensions>
  <add assembly="NLog.Targets.Loki" />
</extensions>

<targets>
  <!-- Loki 目标 -->
  <target xsi:type="loki" 
          name="loki"
          endpoint="http://localhost:3100"
          orderWrites="true"
          compressionLevel="noCompression">
    <label name="app" layout="singulation" />
    <label name="environment" layout="${environment:ASPNETCORE_ENVIRONMENT}" />
    <label name="level" layout="${level:lowercase=true}" />
    <label name="logger" layout="${logger}" />
  </target>
</targets>

<rules>
  <!-- 发送所有日志到 Loki -->
  <logger name="*" minlevel="Info" writeTo="loki" />
</rules>
```

5. **Grafana 配置数据源**

在 Grafana 中添加 Loki 数据源：
- URL: `http://loki:3100`
- 访问模式：Server (default)

6. **查询日志示例**

```logql
# 查询所有错误日志
{app="singulation", level="error"}

# 查询特定 Logger 的日志
{app="singulation", logger=~".*AxisController.*"}

# 统计错误率
rate({app="singulation", level="error"}[5m])
```

### 方案二：ELK Stack（适合大规模）

#### 优势
- ✅ 功能强大，支持复杂查询
- ✅ 全文索引，搜索快速
- ✅ 可视化和仪表盘功能丰富
- ✅ 适合大规模日志分析

#### 部署步骤

1. **启动 ELK 服务**

```yaml
# docker-compose.elk.yml
version: '3.8'
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data

  logstash:
    image: docker.elastic.co/logstash/logstash:8.11.0
    ports:
      - "5044:5044"
    volumes:
      - ./logstash/logstash.conf:/usr/share/logstash/pipeline/logstash.conf:ro

  kibana:
    image: docker.elastic.co/kibana/kibana:8.11.0
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
```

2. **配置 Logstash**（`monitoring/logstash/logstash.conf`）

```conf
input {
  file {
    path => "/var/log/singulation/structured-*.json"
    codec => "json"
    type => "singulation"
  }
}

filter {
  if [type] == "singulation" {
    date {
      match => [ "timestamp", "yyyy-MM-dd HH:mm:ss.SSS" ]
      target => "@timestamp"
    }
  }
}

output {
  elasticsearch {
    hosts => ["elasticsearch:9200"]
    index => "singulation-logs-%{+YYYY.MM.dd}"
  }
}
```

3. **安装 NLog.Targets.ElasticSearch**

```bash
cd ZakYip.Singulation.Core
dotnet add package NLog.Targets.ElasticSearch
```

## 📊 监控指标

### 日志量监控

在 Prometheus 中添加以下指标：

```csharp
// 在 Program.cs 中添加
var logVolumeCounter = Meter.CreateCounter<long>("log_volume_bytes");
var logCountCounter = Meter.CreateCounter<long>("log_count_total");
```

### Grafana 仪表盘

创建以下面板：
- 日志写入速率（条/秒）
- 日志大小增长率（MB/小时）
- 错误日志占比
- 各类型日志分布

## 🔍 故障排查

### 问题：日志文件过大

**解决方案**：
1. 检查是否启用了日志采样
2. 调整采样频率（降低 messageLimitSize）
3. 增加归档阈值（archiveAboveSize）
4. 缩短保留期限

### 问题：磁盘空间不足

**解决方案**：
1. 检查日志清理服务是否正常运行
2. 确认压缩功能已启用
3. 调整保留策略（减少保留天数）
4. 手动清理旧日志：`rm logs/*.log.gz`

### 问题：日志查询慢

**解决方案**：
1. 使用结构化日志（JSON）而非文本日志
2. 部署日志聚合方案（Loki/ELK）
3. 为常用查询创建索引
4. 限制查询时间范围

## 📝 最佳实践

### 1. 日志级别使用

- **Debug**: 开发调试信息，生产环境禁用
- **Info**: 关键业务操作，正常流程
- **Warn**: 警告信息，需要关注但不影响运行
- **Error**: 错误信息，需要处理的异常
- **Fatal**: 致命错误，系统无法继续运行

### 2. 结构化日志

```csharp
// ❌ 避免字符串拼接
_logger.LogInformation("Axis " + axisId + " speed changed to " + speed);

// ✅ 使用结构化参数
_logger.LogInformation("轴 {AxisId} 速度变更为 {Speed}", axisId, speed);
```

### 3. 日志采样

对于高频日志，使用 LogEventBus 进行合并和节流，避免直接调用 ILogger。

### 4. 敏感信息

```csharp
// ❌ 避免记录敏感信息
_logger.LogInformation("User password: {Password}", password);

// ✅ 记录安全信息
_logger.LogInformation("用户 {UserId} 登录成功", userId);
```

## 🔐 安全建议

1. **限制日志访问**：使用文件权限限制日志目录访问
2. **加密传输**：Loki/ELK 连接使用 TLS
3. **脱敏处理**：记录前移除敏感信息
4. **审计日志**：重要操作记录审计日志

## 📚 相关资源

- [NLog 文档](https://nlog-project.org/documentation/)
- [Grafana Loki 文档](https://grafana.com/docs/loki/latest/)
- [Elasticsearch 文档](https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html)
- [日志最佳实践](https://12factor.net/logs)

## 🎯 后续规划

- [ ] 集成 Grafana Loki 或 ELK Stack
- [ ] 实现分布式追踪（OpenTelemetry）
- [ ] 日志异常检测和告警
- [ ] 日志成本优化和归档到对象存储
