# 监控系统快速启动指南

## 🚀 5 分钟快速上手

### 前置要求

- ✅ Docker 和 Docker Compose 已安装
- ✅ ZakYip.Singulation 应用正在运行（端口 5005）

### 启动步骤

#### 1. 验证应用 metrics 端点

```bash
# 检查应用是否暴露指标
curl http://localhost:5005/metrics

# 应该看到类似以下内容：
# # TYPE singulation_frames_processed counter
# singulation_frames_processed_total 1234
# ...
```

#### 2. 启动监控栈

```bash
# 在项目根目录执行
cd /path/to/ZakYip.Singulation
docker-compose -f docker-compose.monitoring.yml up -d

# 等待服务启动（约 10-20 秒）
docker-compose -f docker-compose.monitoring.yml ps
```

#### 3. 访问监控界面

**Grafana（可视化仪表盘）**
- URL: http://localhost:3000
- 用户名: `admin`
- 密码: `admin`
- 首次登录后会要求修改密码

**Prometheus（指标查询）**
- URL: http://localhost:9090
- 查看 Targets: http://localhost:9090/targets
  - 确认 `singulation-app` 状态为 **UP**
- 查看告警: http://localhost:9090/alerts

#### 4. 查看预配置仪表盘

1. 登录 Grafana
2. 点击左侧菜单 **Dashboards** → **Browse**
3. 打开 **Singulation** 文件夹
4. 点击 **ZakYip.Singulation 监控面板**

## 📊 仪表盘内容

预配置的仪表盘包含以下面板：

1. **服务状态** - 实时服务可用性
2. **帧处理速率** - 每秒处理和丢弃的帧数
3. **帧往返时间 (RTT)** - P50/P95/P99 延迟
4. **系统故障指标** - 降级、轴故障、心跳超时
5. **内存使用情况** - 堆内存和工作集
6. **GC 收集频率** - 垃圾回收统计
7. **HTTP 请求延迟** - API 性能监控

## 🚨 告警测试

### 触发测试告警

```bash
# 停止应用测试 ServiceDown 告警
docker stop singulation-host
# 或
net stop ZakYipSingulation

# 等待 1 分钟，检查告警
# http://localhost:9090/alerts

# 恢复服务
docker start singulation-host
# 或
net start ZakYipSingulation
```

## 🛠️ 常见问题

### Q: Prometheus 显示 target 为 DOWN

**症状**: http://localhost:9090/targets 显示 `singulation-app` 为 DOWN

**解决方案**:

1. 检查应用是否运行:
   ```bash
   curl http://localhost:5005/health
   ```

2. Windows/Mac 用户: 确认使用 `host.docker.internal`
   ```yaml
   # monitoring/prometheus/prometheus.yml
   targets: ['host.docker.internal:5005']
   ```

3. Linux 用户: 修改为 Docker 网关 IP
   ```yaml
   # monitoring/prometheus/prometheus.yml
   targets: ['172.17.0.1:5005']
   ```

### Q: Grafana 无数据显示

**解决方案**:
1. 检查数据源配置: Grafana → Configuration → Data Sources → Prometheus
2. 点击 **Test** 按钮验证连接
3. 如果失败，检查 Prometheus 是否运行: `docker ps | grep prometheus`

### Q: 如何查看更长时间范围的数据？

在 Grafana 仪表盘右上角：
- 点击时间选择器（默认 "Last 1 hour"）
- 选择 "Last 24 hours" 或 "Last 7 days"

## 📈 进阶使用

### 自定义查询

在 Grafana 中添加新面板：
1. 点击 **Add panel**
2. 选择 **Add a new panel**
3. 使用 PromQL 查询，例如：

```promql
# 帧处理成功率
rate(singulation_frames_processed_total[5m]) / 
(rate(singulation_frames_processed_total[5m]) + 
 rate(singulation_frames_dropped_total[5m])) * 100

# 内存增长趋势
deriv(process_runtime_dotnet_gc_heap_size_bytes[10m])
```

### 修改告警阈值

编辑 `monitoring/prometheus/alerts/singulation_alerts.yml`:

```yaml
# 例如：修改帧丢失率告警阈值
- alert: HighFrameDropRate
  expr: rate(singulation_frames_dropped_total[5m]) > 10  # 从 5 改为 10
  for: 2m
```

重新加载配置：
```bash
docker-compose -f docker-compose.monitoring.yml restart prometheus
```

## 🔄 维护命令

```bash
# 查看日志
docker-compose -f docker-compose.monitoring.yml logs -f

# 重启服务
docker-compose -f docker-compose.monitoring.yml restart

# 停止监控栈
docker-compose -f docker-compose.monitoring.yml down

# 清理数据（重新开始）
docker-compose -f docker-compose.monitoring.yml down -v
```

## 📚 更多文档

- **完整文档**: [monitoring/README.md](README.md)
- **运维手册**: [ops/OPERATIONS_MANUAL.md](../ops/OPERATIONS_MANUAL.md)
- **Prometheus 官方文档**: https://prometheus.io/docs/
- **Grafana 官方文档**: https://grafana.com/docs/

## ✅ 验收检查清单

部署完成后，确认以下项目：

- [ ] 应用 metrics 端点可访问: http://localhost:5005/metrics
- [ ] Prometheus UI 可访问: http://localhost:9090
- [ ] Prometheus Targets 显示 UP: http://localhost:9090/targets
- [ ] Grafana 可访问并登录: http://localhost:3000
- [ ] 仪表盘显示数据: Dashboards → Singulation → Overview
- [ ] 告警规则已加载: http://localhost:9090/alerts
- [ ] 修改了 Grafana 默认密码

恭喜！监控系统已成功部署 🎉
