# ZakYip.Singulation 运维手册

## 文档概述

本运维手册为 ZakYip.Singulation 工业运动控制系统的完整运维指南，涵盖部署、配置、监控、故障排查等运维工作的全流程。

**文档版本**：1.0  
**最后更新**：2025-10-19  
**适用版本**：ZakYip.Singulation v1.0.0+

## 目录

1. [部署手册](#1-部署手册)
2. [配置指南](#2-配置指南)
3. [日常运维](#3-日常运维)
4. [监控告警](#4-监控告警)
5. [备份恢复](#5-备份恢复)
6. [故障排查](#6-故障排查)
7. [应急响应](#7-应急响应)
8. [安全加固](#8-安全加固)

## 相关文档

- **[配置指南](CONFIGURATION_GUIDE.md)** - 详细的参数配置说明和调优建议
- **[备份恢复流程](BACKUP_RECOVERY.md)** - 数据备份和恢复的详细步骤
- **[应急预案](EMERGENCY_RESPONSE.md)** - 各类紧急情况的处理预案
- **[故障排查手册](../docs/TROUBLESHOOTING.md)** - 常见问题的诊断和解决方案
- **[部署运维手册](../docs/DEPLOYMENT.md)** - 详细的部署步骤和环境配置

---

## 1. 部署手册

### 1.1 环境要求

#### 1.1.1 Host 服务器硬件要求

| 配置项 | 最低要求 | 推荐配置 | 说明 |
|--------|----------|----------|------|
| CPU | 2 核 2.0GHz | 4 核 3.0GHz 及以上 | Intel/AMD x64 架构 |
| 内存 | 2GB | 8GB 及以上 | 用于服务运行和缓存 |
| 硬盘 | 10GB | 50GB SSD | 用于系统、日志、数据库 |
| 网卡 | 100Mbps | 1000Mbps | 需支持 UDP 广播 |

#### 1.1.2 操作系统要求

**Windows 服务器**：
- Windows 10 专业版/企业版（64位）
- Windows 11 专业版/企业版（64位）
- Windows Server 2019/2022（推荐）

**Linux 服务器**（容器部署）：
- Ubuntu 20.04/22.04 LTS
- Debian 11/12
- CentOS 8 Stream/Rocky Linux 8
- 支持 Docker 和 .NET 8.0 Runtime

#### 1.1.3 软件依赖

**必需组件**：
- ✅ .NET 8.0 Runtime (ASP.NET Core Runtime)
- ✅ 雷赛 LTDMC 驱动程序（如使用雷赛控制卡）

**可选组件**：
- Docker 24.0+ 和 Docker Compose 2.0+（用于容器部署）
- NSSM 2.24+（用于 Windows 服务管理）
- Nginx/IIS（用于反向代理）

#### 1.1.4 网络要求

| 端口 | 协议 | 用途 | 必需 |
|------|------|------|------|
| 5005 | TCP | HTTP API 服务 | ✅ |
| 5006 | TCP | HTTPS API 服务 | ⭐ |
| 18888 | UDP | 服务自动发现 | ✅ |

**防火墙规则**：
```powershell
# Windows 防火墙
New-NetFirewallRule -DisplayName "ZakYip HTTP" -Direction Inbound -LocalPort 5005 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "ZakYip HTTPS" -Direction Inbound -LocalPort 5006 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "ZakYip UDP Discovery" -Direction Inbound -LocalPort 18888 -Protocol UDP -Action Allow
```

### 1.2 快速部署

#### 1.2.1 Windows 标准部署

**步骤 1：安装 .NET 8.0 Runtime**

```powershell
# 下载并安装
# https://dotnet.microsoft.com/download/dotnet/8.0
# 选择 "ASP.NET Core Runtime 8.0.x - Windows Hosting Bundle"

# 验证安装
dotnet --list-runtimes
```

**步骤 2：下载并解压发布包**

```powershell
# 创建部署目录
New-Item -ItemType Directory -Path "C:\ZakYip.Singulation" -Force

# 下载最新版本
# https://github.com/Hisoka6602/ZakYip.Singulation/releases

# 解压到目标目录
Expand-Archive -Path "ZakYip.Singulation-v1.0.0.zip" -DestinationPath "C:\ZakYip.Singulation"
```

**步骤 3：配置服务**

编辑 `C:\ZakYip.Singulation\appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "ZakYip.Singulation": "Information"
    }
  },
  "KestrelUrl": "http://0.0.0.0:5005",
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Service",
    "HttpPort": 5005
  }
}
```

**步骤 4：安装为 Windows 服务（使用 NSSM）**

```powershell
# 下载 NSSM: https://nssm.cc/download
# 解压到 C:\NSSM

# 以管理员身份运行
cd C:\NSSM\win64

# 安装服务
.\nssm.exe install ZakYipSingulation "C:\ZakYip.Singulation\ZakYip.Singulation.Host.exe"

# 配置服务
.\nssm.exe set ZakYipSingulation AppDirectory "C:\ZakYip.Singulation"
.\nssm.exe set ZakYipSingulation DisplayName "ZakYip Singulation Service"
.\nssm.exe set ZakYipSingulation Description "工业运动控制系统主机服务"
.\nssm.exe set ZakYipSingulation Start SERVICE_AUTO_START
.\nssm.exe set ZakYipSingulation AppStdout "C:\ZakYip.Singulation\logs\stdout.log"
.\nssm.exe set ZakYipSingulation AppStderr "C:\ZakYip.Singulation\logs\stderr.log"

# 设置服务重启策略
.\nssm.exe set ZakYipSingulation AppExit Default Restart
.\nssm.exe set ZakYipSingulation AppRestartDelay 10000
```

**步骤 5：启动服务**

```powershell
# 启动服务
net start ZakYipSingulation

# 或使用 NSSM
.\nssm.exe start ZakYipSingulation

# 检查服务状态
Get-Service ZakYipSingulation
```

**步骤 6：验证部署**

```powershell
# 检查服务是否监听端口
netstat -ano | findstr :5005

# 访问 Swagger 文档
Start-Process "http://localhost:5005/swagger"

# 测试 API
Invoke-WebRequest -Uri "http://localhost:5005/api/axes/axes" -Method GET
```

#### 1.2.2 Docker 容器部署（推荐）

**步骤 1：准备环境**

```bash
# 安装 Docker
curl -fsSL https://get.docker.com | sh

# 安装 Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# 验证安装
docker --version
docker-compose --version
```

**步骤 2：创建 docker-compose.yml**

```yaml
version: '3.8'

services:
  singulation-host:
    image: zakyip/singulation:latest
    container_name: singulation-host
    restart: unless-stopped
    ports:
      - "5005:5005"       # HTTP API
      - "18888:18888/udp" # UDP Discovery
    volumes:
      - ./data:/app/data             # 数据持久化
      - ./logs:/app/logs             # 日志持久化
      - ./config:/app/config         # 配置文件
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - TZ=Asia/Shanghai
      - KestrelUrl=http://0.0.0.0:5005
    networks:
      - singulation-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "5"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5005/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

networks:
  singulation-net:
    driver: bridge
```

**步骤 3：启动容器**

```bash
# 创建必要的目录
mkdir -p data logs config

# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f

# 检查容器状态
docker-compose ps
```

**步骤 4：验证部署**

```bash
# 检查容器运行状态
docker ps | grep singulation

# 测试 API
curl http://localhost:5005/swagger

# 查看实时日志
docker logs -f singulation-host
```

#### 1.2.3 使用安装脚本部署

项目提供了自动化安装脚本：

**Windows PowerShell**：
```powershell
# 进入 ops 目录
cd ops

# 运行自检
.\selfcheck.ps1

# 安装服务（需要管理员权限）
.\install.ps1
```

**Linux Bash**：
```bash
# 进入 ops 目录
cd ops

# 赋予执行权限
chmod +x *.sh

# 运行自检
./selfcheck.sh

# 安装服务
./install.sh
```

### 1.3 升级流程

#### 1.3.1 Windows 服务升级

```powershell
# 1. 备份当前版本
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\Backups\ZakYip.Singulation.$timestamp"
New-Item -ItemType Directory -Path $backupPath -Force
Copy-Item -Path "C:\ZakYip.Singulation\*" -Destination $backupPath -Recurse

# 2. 停止服务
net stop ZakYipSingulation

# 3. 下载新版本并解压
Expand-Archive -Path "ZakYip.Singulation-v1.1.0.zip" -DestinationPath "C:\Temp\NewVersion"

# 4. 保留配置和数据
Copy-Item "C:\ZakYip.Singulation\appsettings.json" "C:\Temp\appsettings.backup.json"
Copy-Item "C:\ZakYip.Singulation\data" "C:\Temp\data.backup" -Recurse

# 5. 覆盖新版本文件
Remove-Item "C:\ZakYip.Singulation\*" -Recurse -Force
Copy-Item "C:\Temp\NewVersion\*" "C:\ZakYip.Singulation\" -Recurse

# 6. 还原配置和数据
Copy-Item "C:\Temp\appsettings.backup.json" "C:\ZakYip.Singulation\appsettings.json"
Copy-Item "C:\Temp\data.backup\*" "C:\ZakYip.Singulation\data\" -Recurse

# 7. 启动服务
net start ZakYipSingulation

# 8. 验证升级
Start-Sleep -Seconds 5
$response = Invoke-WebRequest -Uri "http://localhost:5005/swagger" -UseBasicParsing
if ($response.StatusCode -eq 200) {
    Write-Host "✅ 升级成功！" -ForegroundColor Green
} else {
    Write-Host "❌ 升级失败，正在回滚..." -ForegroundColor Red
    # 回滚操作
    net stop ZakYipSingulation
    Remove-Item "C:\ZakYip.Singulation\*" -Recurse -Force
    Copy-Item "$backupPath\*" "C:\ZakYip.Singulation\" -Recurse
    net start ZakYipSingulation
}
```

#### 1.3.2 Docker 容器升级

```bash
# 1. 备份数据
timestamp=$(date +%Y%m%d_%H%M%S)
mkdir -p /backups/singulation.$timestamp
cp -r data /backups/singulation.$timestamp/
cp -r config /backups/singulation.$timestamp/

# 2. 拉取新镜像
docker pull zakyip/singulation:1.1.0

# 3. 更新 docker-compose.yml
sed -i 's/:latest/:1.1.0/g' docker-compose.yml

# 4. 停止并删除旧容器
docker-compose down

# 5. 启动新版本
docker-compose up -d

# 6. 验证升级
sleep 10
if curl -f http://localhost:5005/swagger > /dev/null 2>&1; then
    echo "✅ 升级成功！"
else
    echo "❌ 升级失败，正在回滚..."
    # 回滚操作
    docker-compose down
    sed -i 's/:1.1.0/:latest/g' docker-compose.yml
    docker-compose up -d
fi
```

### 1.4 卸载流程

#### 1.4.1 Windows 服务卸载

```powershell
# 使用 ops 脚本卸载
cd ops
.\uninstall.ps1

# 或手动卸载
net stop ZakYipSingulation
sc delete ZakYipSingulation

# 删除服务文件（可选）
Remove-Item "C:\ZakYip.Singulation" -Recurse -Force

# 删除防火墙规则（可选）
Remove-NetFirewallRule -DisplayName "ZakYip*"
```

#### 1.4.2 Docker 容器卸载

```bash
# 停止并删除容器
docker-compose down

# 删除镜像
docker rmi zakyip/singulation:latest

# 删除数据（可选，谨慎操作）
rm -rf data logs config

# 删除 Docker 网络
docker network rm singulation-net
```

---

## 2. 配置指南

详细的配置说明请参见 **[配置指南](CONFIGURATION_GUIDE.md)**。

### 2.1 核心配置项

#### 2.1.1 Kestrel Web 服务器

```json
{
  "KestrelUrl": "http://0.0.0.0:5005"
}
```

#### 2.1.2 UDP 服务发现

```json
{
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Service"
  }
}
```

#### 2.1.3 控制器配置

通过 API 配置，存储在 LiteDB 数据库中：

```json
{
  "vendor": "leadshine",
  "controllerIp": "192.168.1.100",
  "template": {
    "card": 0,
    "port": 0,
    "axisCount": 8
  }
}
```

### 2.2 性能调优建议

参见 **[配置指南 - 性能调优部分](CONFIGURATION_GUIDE.md#3-性能调优)**。

---

## 3. 日常运维

### 3.1 服务管理

#### 3.1.1 Windows 服务

```powershell
# 查看服务状态
Get-Service ZakYipSingulation

# 启动服务
Start-Service ZakYipSingulation

# 停止服务
Stop-Service ZakYipSingulation

# 重启服务
Restart-Service ZakYipSingulation

# 查看服务配置
sc qc ZakYipSingulation

# 查看服务依赖
sc enumdepend ZakYipSingulation
```

#### 3.1.2 Docker 容器

```bash
# 查看容器状态
docker-compose ps
docker ps | grep singulation

# 启动容器
docker-compose start

# 停止容器
docker-compose stop

# 重启容器
docker-compose restart

# 查看容器日志
docker-compose logs -f --tail=100

# 进入容器
docker exec -it singulation-host /bin/bash
```

### 3.2 日志管理

#### 3.2.1 日志位置

- **Windows**：`C:\ZakYip.Singulation\logs\app-YYYY-MM-DD.log`
- **Docker**：`./logs/app-YYYY-MM-DD.log` (挂载卷)
- **stdout/stderr**：通过 `docker logs` 查看

#### 3.2.2 日志查看

```powershell
# Windows - 查看最新 50 行日志
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50

# Windows - 实时查看日志
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Wait

# Docker - 查看最新 100 行日志
docker logs singulation-host --tail 100

# Docker - 实时查看日志
docker logs -f singulation-host
```

#### 3.2.3 日志清理

```powershell
# Windows - 删除 30 天前的日志
$logPath = "C:\ZakYip.Singulation\logs"
Get-ChildItem $logPath -Filter "*.log" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | 
    Remove-Item -Force

# Docker - 日志自动轮转（已在 docker-compose.yml 配置）
# max-size: "10m"
# max-file: "5"
```

### 3.3 数据库维护

#### 3.3.1 LiteDB 维护

```powershell
# 检查数据库大小
Get-Item "C:\ZakYip.Singulation\data\singulation.db" | Select-Object Name, Length

# 备份数据库
Copy-Item "C:\ZakYip.Singulation\data\singulation.db" "C:\Backups\singulation.db.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
```

#### 3.3.2 数据库压缩

```csharp
// 定期执行数据库压缩（通过 API 或维护脚本）
using var db = new LiteDatabase("data/singulation.db");
db.Rebuild();
```

### 3.4 定期维护任务

| 频率 | 任务 | 说明 |
|------|------|------|
| **每日** | 查看服务状态 | 确认服务正常运行 |
| **每日** | 检查错误日志 | 查看是否有异常错误 |
| **每周** | 检查磁盘空间 | 确保有足够空间 |
| **每周** | 清理旧日志 | 删除 30 天前的日志 |
| **每月** | 数据库备份 | 完整备份数据库 |
| **每月** | 性能审查 | 查看性能指标趋势 |
| **每季度** | 安全审计 | 检查安全漏洞 |
| **每季度** | 文档更新 | 更新运维文档 |

---

## 4. 监控告警

### 4.1 Prometheus + Grafana 监控系统（推荐）

**🎯 完整的 APM 和告警系统已集成**

系统现已集成 Prometheus + Grafana 监控栈，提供：
- ✅ 实时指标收集和可视化
- ✅ 自动告警和通知
- ✅ 历史数据分析
- ✅ 性能基线建立

**详细文档**：参见 **[监控系统文档](../monitoring/README.md)**

#### 4.1.1 快速启动监控

```bash
# 1. 确保应用正在运行
# 应用会在 http://localhost:5005/metrics 暴露 Prometheus 指标

# 2. 启动 Prometheus + Grafana
cd /path/to/ZakYip.Singulation
docker-compose -f docker-compose.monitoring.yml up -d

# 3. 访问监控面板
# Grafana: http://localhost:3000 (admin/admin)
# Prometheus: http://localhost:9090
```

#### 4.1.2 监控端点

| 端点 | 用途 | 说明 |
|------|------|------|
| `/metrics` | Prometheus 指标 | OpenTelemetry 导出的所有指标 |
| `/health` | 健康检查 | 服务健康状态 |
| `http://localhost:9090` | Prometheus UI | 查询指标和告警 |
| `http://localhost:3000` | Grafana | 可视化仪表盘 |

### 4.2 关键监控指标

#### 4.2.1 业务指标（Singulation 专用）

| 指标名称 | 类型 | 正常范围 | 告警阈值 | 说明 |
|---------|------|---------|---------|------|
| `singulation_frames_processed` | Counter | > 10/s | < 1/s | 已处理的帧总数 |
| `singulation_frames_dropped` | Counter | < 1% | > 5% | 丢弃的帧总数 |
| `singulation_frame_rtt_ms` (P95) | Histogram | < 50ms | > 100ms | 帧往返时间 |
| `singulation_degrade_total` | Counter | 0 | > 1/5m | 系统降级事件 |
| `singulation_axis_fault_total` | Counter | 0 | > 0.5/5m | 轴故障事件 |
| `singulation_heartbeat_timeout_total` | Counter | 0 | > 0.5/5m | 心跳超时 |

#### 4.2.2 .NET 运行时指标

| 指标 | 正常范围 | 告警阈值 | 说明 |
|------|----------|----------|------|
| GC 堆内存 | < 300MB | > 500MB | .NET 堆内存使用 |
| GC 收集频率 | < 5/s | > 10/s | GC 触发频率 |
| 工作集大小 | < 400MB | > 800MB | 进程内存使用 |
| 异常计数 | < 10/m | > 50/m | 异常抛出频率 |

#### 4.2.3 HTTP 性能指标

| 指标 | 正常范围 | 告警阈值 | 说明 |
|------|----------|----------|------|
| 请求延迟 (P95) | < 200ms | > 1000ms | API 响应时间 |
| 5xx 错误率 | < 0.1% | > 1% | 服务器错误率 |
| 请求速率 | 10-100/s | < 1/s 或 > 500/s | 请求吞吐量 |

#### 4.2.4 系统资源指标

| 指标 | 正常范围 | 告警阈值 | 说明 |
|------|----------|----------|------|
| CPU 使用率 | < 40% | > 80% | 持续 5 分钟 |
| 磁盘使用率 | < 70% | > 90% | 剩余空间不足 |
| 网络延迟 | < 10ms | > 100ms | 局域网延迟 |

### 4.3 自动告警规则

系统预配置了 12 个告警规则，覆盖关键场景：

#### 4.3.1 Critical 级别告警

| 告警名称 | 触发条件 | 持续时间 | 响应 |
|---------|---------|---------|------|
| **ServiceDown** | 服务不可用 | 1分钟 | 立即响应 |
| **AxisFaultDetected** | 轴故障 > 0.5/s | 2分钟 | 立即响应 |

#### 4.3.2 Warning 级别告警

| 告警名称 | 触发条件 | 持续时间 | 响应 |
|---------|---------|---------|------|
| **HighMemoryUsage** | 内存 > 500MB | 5分钟 | 15分钟内 |
| **HighGCPressure** | GC > 10/s | 5分钟 | 15分钟内 |
| **HighFrameDropRate** | 帧丢失 > 5/s | 2分钟 | 15分钟内 |
| **FrequentDegradation** | 降级 > 1/s | 3分钟 | 15分钟内 |
| **HeartbeatTimeouts** | 超时 > 0.5/s | 3分钟 | 15分钟内 |
| **HighFrameLatency** | P95 RTT > 100ms | 5分钟 | 30分钟内 |
| **HighHttpErrorRate** | 5xx > 5/s | 2分钟 | 15分钟内 |
| **HighHttpLatency** | P95 > 1s | 5分钟 | 30分钟内 |

**查看告警状态**: http://localhost:9090/alerts

### 4.4 传统监控工具（备选）

#### 4.4.1 Windows Performance Monitor

```powershell
# 启动性能监视器
perfmon

# 添加监控计数器
# - .NET CLR Memory -> Gen 0/1/2 Collections
# - Process -> CPU Usage, Private Bytes
# - ASP.NET Core -> Requests/Sec
```

#### 4.4.2 Docker 监控

```bash
# 实时查看资源使用
docker stats singulation-host

# 查看容器事件
docker events --filter 'container=singulation-host'

# 查看容器详情
docker inspect singulation-host
```

### 4.5 告警配置

#### 4.5.1 告警级别定义

| 级别 | 响应时间 | 通知方式 | 示例 |
|------|----------|----------|------|
| **Critical** | 立即 | 电话、短信、企业微信 | 服务停止、轴故障 |
| **Error** | 15分钟内 | 邮件、企业微信 | API 错误率高、内存泄漏 |
| **Warning** | 1小时内 | 邮件 | 磁盘空间不足、性能下降 |
| **Info** | 记录不通知 | 日志 | 正常启停、配置变更 |

#### 4.5.2 告警通知集成（可选）

可以配置 Alertmanager 集成企业通知渠道：

```yaml
# alertmanager.yml 示例
route:
  receiver: 'default'
  group_by: ['alertname', 'severity']
  group_wait: 10s
  group_interval: 5m
  repeat_interval: 4h

receivers:
  - name: 'default'
    webhook_configs:
      - url: 'http://your-webhook-url'  # 企业微信/钉钉 Webhook
    email_configs:
      - to: 'admin@example.com'
        from: 'alert@example.com'
        smarthost: 'smtp.example.com:587'
```

#### 4.5.3 传统告警脚本示例

```powershell
# check_service_health.ps1
$service = Get-Service ZakYipSingulation
$emailParams = @{
    From = "monitor@example.com"
    To = "admin@example.com"
    SmtpServer = "smtp.example.com"
}

if ($service.Status -ne 'Running') {
    Send-MailMessage @emailParams `
        -Subject "[CRITICAL] ZakYip Service Down" `
        -Body "Service stopped at $(Get-Date). Please investigate immediately."
    
    # 尝试重启服务
    Start-Service ZakYipSingulation -ErrorAction SilentlyContinue
    
    Start-Sleep -Seconds 10
    if ((Get-Service ZakYipSingulation).Status -ne 'Running') {
        Send-MailMessage @emailParams `
            -Subject "[CRITICAL] ZakYip Service Restart Failed" `
            -Body "Failed to restart service. Manual intervention required."
    }
}
```

### 4.6 性能基线建立

使用 Prometheus 数据建立性能基线：

```promql
# 查询过去 7 天的 P95 帧 RTT
histogram_quantile(0.95, 
  rate(singulation_frame_rtt_ms_bucket[7d]))

# 查询平均帧处理速率
rate(singulation_frames_processed_total[7d])

# 查询系统正常运行时间
up{job="singulation-app"}
```

基于这些数据可以：
- 设置更准确的告警阈值
- 识别性能趋势
- 优化系统配置

---

## 5. 备份恢复

详细的备份恢复流程请参见 **[备份恢复流程](BACKUP_RECOVERY.md)**。

### 5.1 备份策略

| 类型 | 频率 | 保留期 | 内容 |
|------|------|--------|------|
| 全量备份 | 每日 | 7 天 | 完整的数据库和配置 |
| 增量备份 | 每4小时 | 24 小时 | 变更的数据 |
| 配置备份 | 每次变更 | 30 天 | 配置文件 |
| 日志归档 | 每周 | 90 天 | 历史日志 |

### 5.2 快速备份

```powershell
# Windows
$BackupDir = "C:\Backups\ZakYip.Singulation\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $BackupDir -Force
Copy-Item "C:\ZakYip.Singulation\data\*" "$BackupDir\data\" -Recurse
Copy-Item "C:\ZakYip.Singulation\appsettings*.json" "$BackupDir\" -Force
```

### 5.3 快速恢复

```powershell
# Windows
net stop ZakYipSingulation
Copy-Item "$BackupDir\data\*" "C:\ZakYip.Singulation\data\" -Recurse -Force
Copy-Item "$BackupDir\appsettings*.json" "C:\ZakYip.Singulation\" -Force
net start ZakYipSingulation
```

---

## 6. 故障排查

详细的故障排查指南请参见 **[故障排查手册](../docs/TROUBLESHOOTING.md)**。

### 6.1 快速诊断清单

遇到问题时，按以下顺序检查：

1. **服务状态检查**
   ```powershell
   Get-Service ZakYipSingulation
   netstat -ano | findstr :5005
   ```

2. **日志检查**
   ```powershell
   Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50
   ```

3. **API 可达性测试**
   ```powershell
   Invoke-WebRequest -Uri "http://localhost:5005/swagger"
   ```

4. **网络连通性测试**
   ```powershell
   Test-NetConnection -ComputerName localhost -Port 5005
   ```

### 6.2 常见问题速查

| 问题 | 可能原因 | 快速解决 |
|------|----------|----------|
| 服务无法启动 | 端口占用、配置错误 | 检查端口、验证配置 |
| API 无响应 | 服务停止、网络问题 | 重启服务、检查防火墙 |
| 客户端连接失败 | UDP 广播被阻止 | 开放 UDP 18888 端口 |
| 轴控制失败 | 控制卡离线 | 检查控制卡连接和IP |
| 内存泄漏 | 事件未取消订阅 | 重启服务、升级版本 |

---

## 7. 应急响应

详细的应急预案请参见 **[应急响应预案](EMERGENCY_RESPONSE.md)**。

### 7.1 应急响应流程

```
发现问题 → 评估影响 → 快速止损 → 根因分析 → 预防措施
    ↓
  通知相关人员
```

### 7.2 常见应急场景

#### 7.2.1 服务中断

**响应步骤**：
1. 确认服务状态
2. 尝试重启服务
3. 如失败，回滚到上一版本
4. 通知相关人员
5. 分析根因

#### 7.2.2 数据丢失

**响应步骤**：
1. 停止服务
2. 评估数据损失程度
3. 从最近备份恢复
4. 验证数据完整性
5. 启动服务

#### 7.2.3 网络中断

**响应步骤**：
1. 确认网络故障范围
2. 检查网络设备状态
3. 联系网络管理员
4. 启用备用网络（如有）
5. 服务自动重连

#### 7.2.4 设备故障

**响应步骤**：
1. 确认设备故障类型
2. 检查设备连接和电源
3. 尝试重启设备
4. 如无法恢复，联系厂商
5. 启用备用设备（如有）

---

## 8. 安全加固

### 8.1 网络安全

#### 8.1.1 防火墙配置

```powershell
# 仅允许特定 IP 访问
New-NetFirewallRule -DisplayName "ZakYip Allow Subnet" `
    -Direction Inbound `
    -LocalPort 5005 `
    -Protocol TCP `
    -RemoteAddress 192.168.1.0/24 `
    -Action Allow
```

#### 8.1.2 HTTPS 配置

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:5006",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "YourCertPassword"
        }
      }
    }
  }
}
```

### 8.2 访问控制

#### 8.2.1 IP 白名单

在 `Program.cs` 中配置：

```csharp
app.Use(async (context, next) =>
{
    var remoteIp = context.Connection.RemoteIpAddress;
    var allowedIps = new[] { "192.168.1.0/24", "10.0.0.0/8" };
    
    if (!IsAllowedIp(remoteIp, allowedIps))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Access Denied");
        return;
    }
    
    await next();
});
```

### 8.3 数据安全

#### 8.3.1 敏感配置加密

```powershell
# 使用 ASP.NET Core User Secrets
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "your_connection_string"
```

#### 8.3.2 数据库加密

```csharp
// LiteDB 支持数据库加密
var connectionString = new ConnectionString
{
    Filename = "data/singulation.db",
    Password = "YourDatabasePassword"
};
using var db = new LiteDatabase(connectionString);
```

---

## 9. 联系与支持

### 9.1 技术支持

- **📧 技术支持邮箱**：support@example.com
- **💬 GitHub Issues**：https://github.com/Hisoka6602/ZakYip.Singulation/issues
- **📚 文档中心**：项目 docs/ 和 ops/ 目录

### 9.2 紧急联系

**生产环境紧急故障**：
- **📞 24/7 热线**：+86 xxx-xxxx-xxxx
- **📱 企业微信群**：扫码加入运维群
- **🚨 值班工程师**：查看当周排班表

### 9.3 培训与认证

- **运维培训**：每季度一次，覆盖部署、监控、故障排查
- **认证考试**：通过后获得运维工程师认证
- **知识库**：内部 Wiki，持续更新最佳实践

---

## 附录

### A. 术语表

| 术语 | 说明 |
|------|------|
| LiteDB | 嵌入式 NoSQL 数据库 |
| SignalR | ASP.NET Core 实时通信框架 |
| LTDMC | 雷赛运动控制卡 |
| UDP Discovery | 基于 UDP 广播的服务发现机制 |
| Kestrel | ASP.NET Core 内置的跨平台 Web 服务器 |

### B. 常用命令速查

**Windows**：
```powershell
# 服务管理
Get-Service ZakYipSingulation
Start-Service ZakYipSingulation
Stop-Service ZakYipSingulation
Restart-Service ZakYipSingulation

# 端口检查
netstat -ano | findstr :5005
Test-NetConnection -ComputerName localhost -Port 5005

# 日志查看
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50 -Wait
```

**Docker**：
```bash
# 容器管理
docker-compose ps
docker-compose start/stop/restart
docker-compose logs -f

# 容器诊断
docker stats singulation-host
docker exec -it singulation-host /bin/bash
docker inspect singulation-host
```

### C. 配置文件模板

完整的配置文件模板请参见 [配置指南](CONFIGURATION_GUIDE.md)。

### D. 更新日志

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| 1.0 | 2025-10-19 | 初始版本，完整运维手册 |

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 运维团队
