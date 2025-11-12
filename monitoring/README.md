# ZakYip.Singulation 监控系统

本文档介绍如何使用 Prometheus + Grafana 监控 ZakYip.Singulation 应用的性能和健康状态。

## 📊 监控架构

```
┌─────────────────────┐
│  Singulation 应用   │
│  (Port: 5005)       │
│  /metrics 端点      │
└──────────┬──────────┘
           │
           │ 抓取指标
           ▼
┌─────────────────────┐
│    Prometheus       │
│  (Port: 9090)       │
│  - 指标存储         │
│  - 告警规则         │
└──────────┬──────────┘
           │
           │ 查询数据
           ▼
┌─────────────────────┐
│     Grafana         │
│  (Port: 3000)       │
│  - 仪表盘可视化     │
│  - 告警通知         │
└─────────────────────┘
```

## 🚀 快速启动

### 1. 启动 Singulation 应用

确保 Singulation 应用正在运行并监听 5005 端口：

```bash
# 启动应用
cd ZakYip.Singulation.Host
dotnet run
```

应用会在 `http://localhost:5005/metrics` 暴露 Prometheus 指标。

### 2. 启动监控栈

使用 Docker Compose 启动 Prometheus 和 Grafana：

```bash
# 在项目根目录执行
docker-compose -f docker-compose.monitoring.yml up -d
```

这将启动：
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000

### 3. 访问 Grafana

1. 打开浏览器访问 http://localhost:3000
2. 使用默认凭据登录：
   - 用户名: `admin`
   - 密码: `admin`
3. 首次登录后建议修改密码

### 4. 查看仪表盘

Grafana 会自动加载预配置的仪表盘：
- 导航到 **Dashboards** → **Browse**
- 打开 **Singulation** 文件夹
- 选择 **ZakYip.Singulation 监控面板**

## 📈 监控指标

### 业务指标

| 指标名称 | 类型 | 说明 |
|---------|------|------|
| `singulation_frames_processed` | Counter | 已处理的帧总数 |
| `singulation_frames_dropped` | Counter | 丢弃的帧总数 |
| `singulation_degrade_total` | Counter | 系统降级事件总数 |
| `singulation_axis_fault_total` | Counter | 轴故障事件总数 |
| `singulation_heartbeat_timeout_total` | Counter | 心跳超时总数 |
| `singulation_speed_delta_mmps` | Histogram | 速度差值分布 (mm/s) |
| `singulation_frame_loop_ms` | Histogram | 帧循环处理时间 (ms) |
| `singulation_frame_rtt_ms` | Histogram | 帧往返时间 (ms) |
| `singulation_commissioning_ms` | Histogram | 调试投运周期 (ms) |

### .NET 运行时指标

- GC 收集次数和暂停时间
- 堆内存使用情况
- 线程池状态
- 异常计数

### HTTP 指标

- 请求速率
- 响应时间
- 错误率（按状态码）

## 🚨 告警规则

系统配置了以下告警规则（阈值可根据实际情况调整）：

### 关键告警 (Critical)

- **ServiceDown**: 服务停止响应超过 1 分钟
- **AxisFaultDetected**: 检测到轴故障，频率 > 0.5/s

### 警告告警 (Warning)

- **HighMemoryUsage**: 内存使用超过 500MB 持续 5 分钟
- **HighGCPressure**: GC 触发频率 > 10/s 持续 5 分钟
- **HighFrameDropRate**: 帧丢失率 > 5/s 持续 2 分钟
- **FrequentDegradation**: 降级事件频率 > 1/s 持续 3 分钟
- **HeartbeatTimeouts**: 心跳超时频率 > 0.5/s 持续 3 分钟
- **HighFrameLatency**: P95 帧 RTT > 100ms 持续 5 分钟
- **HighHttpErrorRate**: HTTP 5xx 错误率 > 5/s 持续 2 分钟
- **HighHttpLatency**: P95 HTTP 延迟 > 1s 持续 5 分钟

### 查看告警

1. 访问 Prometheus: http://localhost:9090/alerts
2. 查看所有配置的告警规则及其状态

## 🔧 配置

### 修改抓取间隔

编辑 `monitoring/prometheus/prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'singulation-app'
    scrape_interval: 5s  # 修改为所需的间隔
```

### 调整告警阈值

编辑 `monitoring/prometheus/alerts/singulation_alerts.yml`，修改对应的告警规则。

### 自定义仪表盘

1. 在 Grafana 中修改现有仪表盘
2. 点击 **Save dashboard**
3. 导出 JSON 并保存到 `monitoring/grafana/dashboards/`

## 📦 目录结构

```
monitoring/
├── prometheus/
│   ├── prometheus.yml              # Prometheus 主配置
│   └── alerts/
│       └── singulation_alerts.yml  # 告警规则定义
├── grafana/
│   ├── provisioning/
│   │   ├── datasources/
│   │   │   └── prometheus.yml      # 数据源自动配置
│   │   └── dashboards/
│   │       └── dashboards.yml      # 仪表盘自动加载配置
│   └── dashboards/
│       └── singulation-overview.json  # 主监控仪表盘
```

## 🐳 Docker Compose 命令

```bash
# 启动监控栈
docker-compose -f docker-compose.monitoring.yml up -d

# 查看日志
docker-compose -f docker-compose.monitoring.yml logs -f

# 停止监控栈
docker-compose -f docker-compose.monitoring.yml down

# 停止并删除数据卷（会丢失历史数据）
docker-compose -f docker-compose.monitoring.yml down -v

# 重启单个服务
docker-compose -f docker-compose.monitoring.yml restart prometheus
docker-compose -f docker-compose.monitoring.yml restart grafana
```

## 🔍 故障排查

### 应用指标不显示

1. **检查应用是否运行**: 访问 http://localhost:5005/health
2. **检查 metrics 端点**: 访问 http://localhost:5005/metrics
3. **检查 Prometheus Targets**: 访问 http://localhost:9090/targets
   - 应该显示 `singulation-app` 目标为 **UP** 状态

### Prometheus 无法连接应用

**Windows/Mac Docker Desktop**:
- 使用 `host.docker.internal` 访问宿主机
- 确认配置中的 target 是 `host.docker.internal:5005`

**Linux Docker**:
- 修改 `prometheus.yml` 中的 target 为 `172.17.0.1:5005`
- 或使用 `--network host` 模式运行容器

### Grafana 无法连接 Prometheus

1. 检查 Prometheus 是否运行: `docker ps | grep prometheus`
2. 检查 Grafana 日志: `docker logs singulation-grafana`
3. 验证网络连接: 
   ```bash
   docker exec singulation-grafana ping prometheus
   ```

## 📊 性能基线建议

根据系统实际运行情况，建议设置以下性能基线：

| 指标 | 正常范围 | 警告阈值 | 关键阈值 |
|------|---------|---------|---------|
| 帧处理速率 | > 10/s | < 5/s | < 1/s |
| 帧丢失率 | < 1% | 1-5% | > 5% |
| 帧 RTT (P95) | < 50ms | 50-100ms | > 100ms |
| 内存使用 | < 300MB | 300-500MB | > 500MB |
| GC 频率 | < 5/s | 5-10/s | > 10/s |
| 心跳超时率 | 0 | < 0.1/s | > 0.5/s |

## 🔐 生产环境建议

1. **修改默认密码**: 首次登录 Grafana 后立即修改 admin 密码
2. **启用 HTTPS**: 配置 SSL 证书以加密通信
3. **限制访问**: 使用防火墙限制 Prometheus 和 Grafana 的访问
4. **配置告警通知**: 
   - 集成 Slack、企业微信、钉钉等通知渠道
   - 配置 Alertmanager 进行告警路由和去重
5. **数据备份**: 定期备份 Prometheus 和 Grafana 的数据卷
6. **资源监控**: 监控 Prometheus 和 Grafana 自身的资源使用

## 📚 相关资源

- [Prometheus 文档](https://prometheus.io/docs/)
- [Grafana 文档](https://grafana.com/docs/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [PromQL 查询语法](https://prometheus.io/docs/prometheus/latest/querying/basics/)

## 🤝 技术支持

如遇问题，请查看：
1. 应用日志: `logs/` 目录
2. Prometheus 日志: `docker logs singulation-prometheus`
3. Grafana 日志: `docker logs singulation-grafana`
