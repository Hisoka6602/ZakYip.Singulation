# ZakYip.Singulation 部署运维手册

## 1. 环境要求

### 1.1 Host 服务器要求

**硬件要求**：
- CPU：4 核及以上（推荐 Intel/AMD x64）
- 内存：4GB 及以上（推荐 8GB）
- 硬盘：20GB 可用空间（推荐 SSD）
- 网络：千兆网卡，支持 UDP 广播

**软件要求**：
- 操作系统：Windows 10/11 或 Windows Server 2019/2022
- .NET 8.0 Runtime（ASP.NET Core Runtime）
- 雷赛 LTDMC 驱动程序（如使用雷赛控制卡）
- TCP/IP 网络协议栈

### 1.2 客户端要求

**MAUI 移动端**：
- Android：版本 5.0 (API 21) 及以上
- iOS：版本 11.0 及以上
- Windows：Windows 10 版本 1809 及以上

**Web 客户端**：
- 现代浏览器（Chrome 90+, Edge 90+, Firefox 88+）

### 1.3 网络要求

- Host 和客户端在同一局域网或可路由网络
- UDP 端口 18888 开放（用于服务发现）
- HTTP 端口 5005 开放（可自定义）
- HTTPS 端口 5006 开放（可选）
- 防火墙允许上述端口通信

## 2. 快速部署

### 2.1 Windows 服务部署

#### 步骤 1：下载发布包

从 GitHub Releases 下载最新版本：
```
https://github.com/Hisoka6602/ZakYip.Singulation/releases
```

#### 步骤 2：解压到目标目录

```powershell
# 创建部署目录
mkdir C:\ZakYip.Singulation

# 解压发布包
Expand-Archive -Path ZakYip.Singulation-v1.0.0.zip -DestinationPath C:\ZakYip.Singulation
```

#### 步骤 3：配置 appsettings.json

编辑 `C:\ZakYip.Singulation\appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "KestrelUrl": "http://0.0.0.0:5005",
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Service",
    "HttpPort": 5005,
    "HttpsPort": 5006
  },
  "FrameGuard": {
    "MinIntervalMs": 500,
    "DebounceMs": 200
  }
}
```

#### 步骤 4：安装 Windows 服务

使用管理员权限运行：

```powershell
# 方法 1：使用 sc 命令
sc create ZakYipSingulation binPath="C:\ZakYip.Singulation\ZakYip.Singulation.Host.exe" start=auto

# 方法 2：使用 NSSM (推荐)
# 下载 NSSM: https://nssm.cc/download
nssm install ZakYipSingulation "C:\ZakYip.Singulation\ZakYip.Singulation.Host.exe"
nssm set ZakYipSingulation AppDirectory "C:\ZakYip.Singulation"
nssm set ZakYipSingulation DisplayName "ZakYip Singulation Service"
nssm set ZakYipSingulation Description "工业运动控制系统主机服务"
nssm set ZakYipSingulation Start SERVICE_AUTO_START
```

#### 步骤 5：启动服务

```powershell
# 启动服务
net start ZakYipSingulation

# 或使用 Services 管理器
# Win + R -> services.msc -> 找到 ZakYipSingulation -> 右键启动
```

#### 步骤 6：验证部署

访问以下地址验证服务：
- Swagger 文档：http://localhost:5005/swagger
- 健康检查：http://localhost:5005/health (如已实现)

### 2.2 手动运行（开发/测试）

```powershell
# 进入部署目录
cd C:\ZakYip.Singulation

# 直接运行
.\ZakYip.Singulation.Host.exe

# 或使用 dotnet 运行
dotnet ZakYip.Singulation.Host.dll
```

### 2.3 Docker 容器部署（推荐）

#### 准备 Dockerfile

创建 `Dockerfile`：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5005
EXPOSE 18888/udp

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ZakYip.Singulation.Host/ZakYip.Singulation.Host.csproj", "ZakYip.Singulation.Host/"]
COPY ["ZakYip.Singulation.Core/ZakYip.Singulation.Core.csproj", "ZakYip.Singulation.Core/"]
COPY ["ZakYip.Singulation.Drivers/ZakYip.Singulation.Drivers.csproj", "ZakYip.Singulation.Drivers/"]
COPY ["ZakYip.Singulation.Infrastructure/ZakYip.Singulation.Infrastructure.csproj", "ZakYip.Singulation.Infrastructure/"]
COPY ["ZakYip.Singulation.Protocol/ZakYip.Singulation.Protocol.csproj", "ZakYip.Singulation.Protocol/"]
COPY ["ZakYip.Singulation.Transport/ZakYip.Singulation.Transport.csproj", "ZakYip.Singulation.Transport/"]
RUN dotnet restore "ZakYip.Singulation.Host/ZakYip.Singulation.Host.csproj"

COPY . .
WORKDIR "/src/ZakYip.Singulation.Host"
RUN dotnet build "ZakYip.Singulation.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZakYip.Singulation.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 创建数据和日志目录
RUN mkdir -p /app/data /app/logs

ENTRYPOINT ["dotnet", "ZakYip.Singulation.Host.dll"]
```

#### 构建镜像

```bash
# 构建镜像
docker build -t zakyip/singulation:1.0.0 -t zakyip/singulation:latest .

# 验证镜像
docker images | grep singulation
```

#### 运行容器

```bash
# 使用 docker run
docker run -d \
  --name singulation-host \
  -p 5005:5005 \
  -p 18888:18888/udp \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --restart unless-stopped \
  zakyip/singulation:latest

# 使用 docker-compose (推荐)
```

创建 `docker-compose.yml`：

```yaml
version: '3.8'

services:
  singulation-host:
    image: zakyip/singulation:latest
    container_name: singulation-host
    ports:
      - "5005:5005"
      - "18888:18888/udp"
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
      - ./appsettings.Production.json:/app/appsettings.Production.json
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - TZ=Asia/Shanghai
    restart: unless-stopped
    networks:
      - singulation-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

networks:
  singulation-net:
    driver: bridge
```

启动：

```bash
docker-compose up -d
```

### 2.4 MAUI 客户端部署

#### Android 客户端

1. **从 APK 安装**：
   ```bash
   # 下载 APK
   wget https://github.com/Hisoka6602/ZakYip.Singulation/releases/download/v1.0.0/ZakYip.Singulation.apk
   
   # 安装到设备
   adb install ZakYip.Singulation.apk
   ```

2. **从 Google Play 安装**（如已发布）：
   - 搜索 "ZakYip Singulation"
   - 点击安装

#### iOS 客户端

1. **通过 TestFlight 测试版**：
   - 使用邀请链接加入测试
   - 安装 TestFlight 应用
   - 安装 ZakYip Singulation

2. **通过 App Store**（如已发布）：
   - 搜索 "ZakYip Singulation"
   - 点击获取

#### Windows 客户端

1. **从 MSIX 安装包**：
   ```powershell
   # 双击运行 MSIX 安装包
   .\ZakYip.Singulation.msix
   ```

## 3. 配置详解

### 3.1 Kestrel 配置

```json
{
  "KestrelUrl": "http://0.0.0.0:5005",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5005"
      },
      "Https": {
        "Url": "https://0.0.0.0:5006",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "your_password"
        }
      }
    },
    "Limits": {
      "MaxRequestBodySize": 31457280,
      "KeepAliveTimeout": "00:02:00",
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

### 3.2 UDP 服务发现配置

```json
{
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Service",
    "Version": "1.0.0",
    "HttpPort": 5005,
    "HttpsPort": 5006,
    "SignalRPath": "/hubs/events"
  }
}
```

### 3.3 控制器配置

存储在 LiteDB 中，可通过 API 修改：

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

### 3.4 日志配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "ZakYip.Singulation": "Debug"
    }
  },
  "NLog": {
    "targets": {
      "file": {
        "type": "File",
        "fileName": "logs/app-${shortdate}.log",
        "layout": "${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}"
      }
    },
    "rules": [
      {
        "logger": "*",
        "minLevel": "Info",
        "writeTo": "file"
      }
    ]
  }
}
```

### 3.5 安全管线配置

```json
{
  "FrameGuard": {
    "MinIntervalMs": 500,
    "DebounceMs": 200,
    "MaxRetries": 3
  }
}
```

## 4. 升级与回滚

### 4.1 Windows 服务升级

```powershell
# 1. 停止服务
net stop ZakYipSingulation

# 2. 备份当前版本
xcopy C:\ZakYip.Singulation C:\ZakYip.Singulation.backup\ /E /I

# 3. 覆盖新版本文件
# 保留 appsettings.json 和 data/ 目录
xcopy C:\新版本\ C:\ZakYip.Singulation\ /E /Y

# 4. 启动服务
net start ZakYipSingulation

# 5. 验证
curl http://localhost:5005/swagger
```

### 4.2 Docker 升级

```bash
# 1. 拉取新镜像
docker pull zakyip/singulation:1.1.0

# 2. 停止并删除旧容器
docker-compose down

# 3. 更新 docker-compose.yml 镜像版本
sed -i 's/:1.0.0/:1.1.0/g' docker-compose.yml

# 4. 启动新版本
docker-compose up -d

# 5. 验证
docker logs singulation-host --tail 50
```

### 4.3 回滚操作

**Windows 服务回滚**：
```powershell
# 1. 停止服务
net stop ZakYipSingulation

# 2. 还原备份
xcopy C:\ZakYip.Singulation.backup\ C:\ZakYip.Singulation\ /E /Y

# 3. 启动服务
net start ZakYipSingulation
```

**Docker 回滚**：
```bash
# 方法 1：使用旧镜像版本
docker-compose down
sed -i 's/:1.1.0/:1.0.0/g' docker-compose.yml
docker-compose up -d

# 方法 2：使用镜像 SHA
docker tag zakyip/singulation@sha256:abc123... zakyip/singulation:rollback
docker-compose down
# 修改 docker-compose.yml 使用 :rollback 标签
docker-compose up -d
```

## 5. 备份与恢复

### 5.1 备份内容

**必须备份**：
- LiteDB 数据库文件 (`data/*.db`)
- 配置文件 (`appsettings.json`, `appsettings.Production.json`)
- 日志文件 (`logs/*.log`, 可选)

### 5.2 备份脚本

**Windows PowerShell**：
```powershell
# backup.ps1
$BackupDir = "C:\Backups\ZakYip.Singulation\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
$SourceDir = "C:\ZakYip.Singulation"

# 创建备份目录
New-Item -ItemType Directory -Path $BackupDir -Force

# 备份数据库
Copy-Item "$SourceDir\data\*" "$BackupDir\data\" -Recurse -Force

# 备份配置
Copy-Item "$SourceDir\appsettings*.json" "$BackupDir\" -Force

# 备份日志（可选）
# Copy-Item "$SourceDir\logs\*" "$BackupDir\logs\" -Recurse -Force

Write-Host "Backup completed: $BackupDir"

# 保留最近 7 天的备份
Get-ChildItem "C:\Backups\ZakYip.Singulation\" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } | Remove-Item -Recurse -Force
```

**Linux Bash**：
```bash
#!/bin/bash
# backup.sh

BACKUP_DIR="/backups/zakyip-singulation/$(date +%Y%m%d_%H%M%S)"
SOURCE_DIR="/app"

# 创建备份目录
mkdir -p "$BACKUP_DIR"

# 备份数据库
cp -r "$SOURCE_DIR/data" "$BACKUP_DIR/"

# 备份配置
cp "$SOURCE_DIR/appsettings"*.json "$BACKUP_DIR/"

echo "Backup completed: $BACKUP_DIR"

# 保留最近 7 天的备份
find /backups/zakyip-singulation/ -type d -mtime +7 -exec rm -rf {} \;
```

### 5.3 定时备份

**Windows 计划任务**：
```powershell
# 创建每天凌晨 2 点的备份任务
$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-File C:\ZakYip.Singulation\backup.ps1'
$trigger = New-ScheduledTaskTrigger -Daily -At 2am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "ZakYip Singulation Backup" -Description "每日自动备份"
```

**Linux Cron**：
```bash
# 编辑 crontab
crontab -e

# 添加每天凌晨 2 点的备份任务
0 2 * * * /opt/zakyip-singulation/backup.sh
```

### 5.4 恢复操作

```powershell
# Windows
# 1. 停止服务
net stop ZakYipSingulation

# 2. 恢复数据和配置
$BackupDir = "C:\Backups\ZakYip.Singulation\20250119_020000"
Copy-Item "$BackupDir\data\*" "C:\ZakYip.Singulation\data\" -Recurse -Force
Copy-Item "$BackupDir\appsettings*.json" "C:\ZakYip.Singulation\" -Force

# 3. 启动服务
net start ZakYipSingulation
```

## 6. 监控与告警

### 6.1 健康检查

**健康检查端点**（待实现）：
```bash
# 检查服务健康
curl http://localhost:5005/health

# 预期响应
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "axis_controller": {
      "status": "Healthy",
      "duration": "00:00:00.0098765"
    }
  }
}
```

### 6.2 日志监控

**实时查看日志**：
```powershell
# Windows
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Wait

# Linux/Docker
docker logs -f singulation-host
```

**日志级别调整**：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "ZakYip.Singulation": "Debug"
    }
  }
}
```

### 6.3 性能监控

**Windows 性能计数器**：
- `.NET CLR Memory` - GC 统计
- `Process` - CPU 和内存使用
- `ASP.NET Core` - 请求统计

**Docker 监控**：
```bash
# 实时查看资源使用
docker stats singulation-host

# 详细资源使用
docker inspect singulation-host --format='{{.State.Status}}'
docker inspect singulation-host --format='{{.RestartCount}}'
```

### 6.4 告警设置（推荐）

**Prometheus + Grafana**：
1. 安装 Prometheus exporter for ASP.NET Core
2. 配置 Prometheus 抓取指标
3. 在 Grafana 创建仪表盘
4. 设置告警规则（CPU > 80%, 内存 > 90%, 错误率 > 5%）

**邮件告警脚本**：
```powershell
# check_and_alert.ps1
$service = Get-Service ZakYipSingulation
if ($service.Status -ne 'Running') {
    Send-MailMessage -From 'monitor@example.com' -To 'admin@example.com' `
        -Subject '[Alert] ZakYip Service Down' `
        -Body "Service stopped at $(Get-Date)" `
        -SmtpServer 'smtp.example.com'
}
```

## 7. 安全加固

### 7.1 网络安全

**防火墙规则**：
```powershell
# Windows 防火墙
New-NetFirewallRule -DisplayName "ZakYip Singulation HTTP" -Direction Inbound -LocalPort 5005 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "ZakYip Singulation UDP" -Direction Inbound -LocalPort 18888 -Protocol UDP -Action Allow

# Linux iptables
iptables -A INPUT -p tcp --dport 5005 -j ACCEPT
iptables -A INPUT -p udp --dport 18888 -j ACCEPT
```

**HTTPS 配置**（推荐生产环境）：
1. 申请 SSL 证书（Let's Encrypt 免费）
2. 配置 Kestrel HTTPS 端点
3. 强制 HTTPS 重定向

### 7.2 访问控制

**IP 白名单**（通过中间件）：
```csharp
app.Use(async (context, next) =>
{
    var remoteIp = context.Connection.RemoteIpAddress;
    var allowedIps = new[] { "192.168.1.0/24", "10.0.0.0/8" };
    
    if (!IsAllowed(remoteIp, allowedIps))
    {
        context.Response.StatusCode = 403;
        return;
    }
    
    await next();
});
```

### 7.3 数据加密

- 敏感配置使用 ASP.NET Core Secret Manager
- 数据库连接字符串加密
- API 密钥轮换机制

## 8. 故障排查清单

### 8.1 服务无法启动

**检查步骤**：
1. 查看 Windows 事件日志
2. 检查端口占用：`netstat -ano | findstr 5005`
3. 验证 .NET Runtime 安装：`dotnet --info`
4. 检查文件权限
5. 查看服务日志文件

### 8.2 客户端无法连接

**检查步骤**：
1. Ping 服务器 IP
2. Telnet 端口：`telnet 192.168.1.100 5005`
3. 检查防火墙规则
4. 验证 UDP 广播可达
5. 查看客户端日志

### 8.3 性能问题

**诊断方法**：
1. 检查 CPU/内存使用率
2. 查看慢查询日志
3. 分析 GC 频率和停顿时间
4. 检查网络延迟
5. 查看并发连接数

### 8.4 数据丢失

**恢复步骤**：
1. 停止服务
2. 从最近备份恢复
3. 检查数据完整性
4. 启动服务并验证

## 9. 维护计划

### 9.1 日常维护

- **每日**：查看服务状态和关键日志
- **每周**：检查磁盘空间，清理旧日志
- **每月**：查看性能报告，优化配置

### 9.2 定期任务

**日志清理**：
```powershell
# 删除 30 天前的日志
Get-ChildItem "C:\ZakYip.Singulation\logs\" -Filter "*.log" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | 
    Remove-Item
```

**数据库维护**：
```csharp
// LiteDB 压缩（定期执行）
using var db = new LiteDatabase("data/singulation.db");
db.Rebuild();
```

### 9.3 应急响应流程

1. **发现问题** → 记录现象和时间
2. **初步诊断** → 查看日志和监控
3. **影响评估** → 确定严重程度
4. **紧急修复** → 重启服务或回滚版本
5. **根因分析** → 深入分析问题原因
6. **预防措施** → 更新配置或代码

## 10. 联系与支持

- **技术支持**：support@example.com
- **问题反馈**：https://github.com/Hisoka6602/ZakYip.Singulation/issues
- **文档中心**：https://docs.example.com/zakyip-singulation

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 运维团队
