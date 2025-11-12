# Loki 日志聚合快速启动指南

## 📋 概述

本指南帮助您快速部署 Grafana Loki 日志聚合系统，实现 ZakYip.Singulation 应用的集中式日志管理。

## 🎯 架构说明

```
┌─────────────────────┐
│  Singulation 应用   │
│  生成日志文件       │
│  logs/*.log,*.json  │
└──────────┬──────────┘
           │
           │ 文件系统
           ▼
┌─────────────────────┐
│     Promtail        │
│  日志采集和推送     │
└──────────┬──────────┘
           │
           │ HTTP Push
           ▼
┌─────────────────────┐
│       Loki          │
│  日志存储和索引     │
└──────────┬──────────┘
           │
           │ LogQL 查询
           ▼
┌─────────────────────┐
│      Grafana        │
│  日志查询和可视化   │
└─────────────────────┘
```

## 🚀 快速启动

### 1. 确保应用正在运行并生成日志

```bash
# 启动应用
cd ZakYip.Singulation.Host
dotnet run
```

应用会在 `logs/` 目录生成以下日志文件：
- `all-{date}.log` - 所有日志（传统格式）
- `structured-{date}.json` - 结构化JSON日志（推荐用于聚合）
- `error-{date}.log` - 错误日志
- 其他专用日志文件

### 2. 启动监控栈（包含 Loki）

```bash
# 在项目根目录执行
docker-compose -f docker-compose.monitoring-with-loki.yml up -d
```

这将启动以下服务：
- **Loki**: http://localhost:3100（日志聚合）
- **Promtail**: 日志采集器（后台运行）
- **Prometheus**: http://localhost:9090（指标监控）
- **Grafana**: http://localhost:3000（可视化）

### 3. 访问 Grafana

1. 打开浏览器访问 http://localhost:3000
2. 使用默认凭据登录：
   - 用户名: `admin`
   - 密码: `admin`
3. 首次登录后建议修改密码

### 4. 验证 Loki 数据源

1. 在 Grafana 中导航到 **Configuration** → **Data Sources**
2. 确认 **Loki** 数据源已自动配置
3. 点击 **Test** 按钮验证连接

### 5. 查询日志

#### 使用 Grafana Explore

1. 导航到 **Explore**（侧边栏的指南针图标）
2. 选择 **Loki** 数据源
3. 尝试以下查询：

```logql
# 查询所有日志
{app="singulation"}

# 查询错误日志
{app="singulation", level="ERROR"}

# 查询特定组件的日志
{app="singulation", component="transport-pump"}

# 查询包含特定文本的日志
{app="singulation"} |= "exception"

# 统计错误率（每分钟错误数）
rate({app="singulation", level="ERROR"}[1m])

# 查询特定 Logger 的日志
{app="singulation", logger=~".*AxisController.*"}
```

#### 使用 LogQL 高级查询

```logql
# 多条件过滤
{app="singulation"} 
  |= "error" 
  |~ "timeout|exception"
  != "test"

# 按时间聚合
sum(rate({app="singulation"}[5m])) by (level)

# 日志模式检测
{app="singulation"} 
  | pattern `<_> | <level> | <logger> | <message>`
  | level != "INFO"

# 提取字段并过滤
{app="singulation"} 
  | json 
  | level="ERROR" 
  | message =~ ".*timeout.*"
```

## 📊 创建日志仪表盘

### 1. 创建新仪表盘

1. 点击 **+** → **Dashboard**
2. 添加新面板

### 2. 常用面板示例

#### 面板 1: 日志流

- **查询**: `{app="singulation"}`
- **可视化类型**: Logs
- **说明**: 实时日志流，显示最新日志

#### 面板 2: 错误率趋势

- **查询**: `rate({app="singulation", level="ERROR"}[5m])`
- **可视化类型**: Time series
- **说明**: 每5分钟错误数趋势

#### 面板 3: 日志级别分布

- **查询**: `sum(count_over_time({app="singulation"}[1h])) by (level)`
- **可视化类型**: Pie chart
- **说明**: 过去1小时各级别日志占比

#### 面板 4: 组件日志量

- **查询**: `sum(rate({app="singulation"}[5m])) by (component)`
- **可视化类型**: Bar chart
- **说明**: 各组件日志产生速率

#### 面板 5: 错误日志详情

- **查询**: `{app="singulation", level="ERROR"}`
- **可视化类型**: Table
- **说明**: 显示错误日志的详细信息

### 3. 添加变量

创建仪表盘变量以实现动态过滤：

1. **Dashboard Settings** → **Variables** → **Add variable**
2. 添加以下变量：
   - `level`: 日志级别（ERROR, WARN, INFO, DEBUG）
   - `logger`: Logger 名称
   - `component`: 组件名称

查询示例使用变量：
```logql
{app="singulation", level="$level", logger=~".*$logger.*"}
```

## 🔍 故障排查

### 问题：Promtail 无法读取日志文件

**症状**：Grafana 中查询不到日志

**解决方案**：

1. **检查日志文件路径**

```bash
# 查看日志文件是否存在
ls -la ZakYip.Singulation.Host/logs/

# 检查 Promtail 容器日志
docker logs singulation-promtail
```

2. **调整 Docker Compose 中的卷挂载**

编辑 `docker-compose.monitoring-with-loki.yml`：

```yaml
# Windows 路径示例
- //c/Projects/ZakYip.Singulation/ZakYip.Singulation.Host/logs:/var/log/singulation:ro

# Linux 路径示例
- /home/user/ZakYip.Singulation/ZakYip.Singulation.Host/logs:/var/log/singulation:ro
```

3. **检查文件权限**

```bash
# Linux: 确保 Promtail 容器可以读取日志文件
chmod -R 755 ZakYip.Singulation.Host/logs/
```

### 问题：Loki 查询慢

**解决方案**：

1. **限制查询时间范围**：避免查询超过7天的日志
2. **使用标签过滤**：优先使用标签过滤（`{app="singulation"}`），再使用文本过滤
3. **避免复杂的正则表达式**：简化查询条件
4. **增加 Loki 资源**：编辑 `docker-compose.monitoring-with-loki.yml`

```yaml
loki:
  # ... 其他配置 ...
  deploy:
    resources:
      limits:
        memory: 2G
      reservations:
        memory: 1G
```

### 问题：日志数据丢失

**可能原因**：
1. Loki 存储空间不足
2. 日志超过保留期限被删除（默认30天）
3. Promtail 采集出错

**检查步骤**：

```bash
# 检查 Loki 磁盘使用
docker exec singulation-loki du -sh /loki

# 检查 Loki 日志
docker logs singulation-loki | grep -i error

# 检查 Promtail 日志
docker logs singulation-promtail | grep -i error
```

## 📈 性能优化

### 1. Promtail 优化

编辑 `monitoring/promtail/promtail-config.yml`：

```yaml
# 批量发送配置
clients:
  - url: http://loki:3100/loki/api/v1/push
    batch_wait: 1s
    batch_size: 102400  # 100KB
    max_retries: 10
    timeout: 10s
```

### 2. Loki 优化

编辑 `monitoring/loki/loki-config.yml`：

```yaml
limits_config:
  # 增加入库速率限制
  ingestion_rate_mb: 20
  ingestion_burst_size_mb: 40
  
  # 增加并发查询数
  max_concurrent_tail_requests: 20
```

### 3. 定期清理旧数据

```bash
# 手动清理30天前的数据
docker exec singulation-loki rm -rf /loki/chunks/*
docker restart singulation-loki
```

## 🔐 生产环境建议

### 1. 启用认证

编辑 `monitoring/loki/loki-config.yml`：

```yaml
auth_enabled: true

# 添加租户配置
# ...
```

### 2. 配置外部存储

使用 S3 或其他对象存储替代本地文件系统：

```yaml
storage_config:
  aws:
    s3: s3://region/bucket
    access_key_id: YOUR_KEY
    secret_access_key: YOUR_SECRET
```

### 3. 启用 HTTPS

在 Grafana 和 Loki 前添加反向代理（Nginx/Traefik）并配置 SSL 证书。

### 4. 配置告警

创建告警规则，当日志中出现关键错误时发送通知：

```yaml
# Prometheus 告警规则示例
groups:
  - name: logs
    rules:
      - alert: HighErrorRate
        expr: rate({app="singulation", level="ERROR"}[5m]) > 5
        for: 5m
        annotations:
          summary: "日志错误率过高"
          description: "错误日志速率超过 5/s，持续 5 分钟"
```

## 🎓 学习资源

- [LogQL 查询语法](https://grafana.com/docs/loki/latest/logql/)
- [Promtail 配置文档](https://grafana.com/docs/loki/latest/clients/promtail/configuration/)
- [Loki 最佳实践](https://grafana.com/docs/loki/latest/best-practices/)
- [Grafana 仪表盘示例](https://grafana.com/grafana/dashboards/)

## 📞 技术支持

如遇问题，请检查：
1. 应用日志：`logs/` 目录
2. Loki 日志：`docker logs singulation-loki`
3. Promtail 日志：`docker logs singulation-promtail`
4. Grafana 日志：`docker logs singulation-grafana`

## 🔄 维护命令

```bash
# 查看所有容器状态
docker-compose -f docker-compose.monitoring-with-loki.yml ps

# 重启特定服务
docker-compose -f docker-compose.monitoring-with-loki.yml restart loki

# 查看服务日志
docker-compose -f docker-compose.monitoring-with-loki.yml logs -f loki

# 停止所有服务
docker-compose -f docker-compose.monitoring-with-loki.yml down

# 停止并删除数据（危险！）
docker-compose -f docker-compose.monitoring-with-loki.yml down -v

# 更新服务
docker-compose -f docker-compose.monitoring-with-loki.yml pull
docker-compose -f docker-compose.monitoring-with-loki.yml up -d
```
