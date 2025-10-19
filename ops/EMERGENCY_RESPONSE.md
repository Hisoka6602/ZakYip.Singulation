# ZakYip.Singulation 应急响应预案

## 文档概述

本应急响应预案针对 ZakYip.Singulation 系统可能发生的各类突发事件，提供标准化的响应流程和处理方案。

**文档版本**：1.0  
**最后更新**：2025-10-19  
**适用版本**：ZakYip.Singulation v1.0.0+

## 目录

1. [应急响应流程](#1-应急响应流程)
2. [服务中断应急处理](#2-服务中断应急处理)
3. [网络故障应急处理](#3-网络故障应急处理)
4. [设备故障应急处理](#4-设备故障应急处理)
5. [数据安全事件处理](#5-数据安全事件处理)
6. [性能问题应急处理](#6-性能问题应急处理)
7. [安全漏洞应急处理](#7-安全漏洞应急处理)
8. [应急联系人](#8-应急联系人)

---

## 1. 应急响应流程

### 1.1 总体响应流程

```
发现问题 → 确认级别 → 启动预案 → 快速止损 → 恢复服务 → 根因分析 → 预防措施
    ↓          ↓           ↓
  报告      通知相关人员    记录过程
```

### 1.2 事件等级分类

| 等级 | 影响范围 | 响应时间 | 通知对象 | 示例 |
|------|----------|----------|----------|------|
| **P0 - 紧急** | 全部服务不可用 | 立即（5分钟内） | 所有相关人员 + 管理层 | 服务完全中断、数据丢失 |
| **P1 - 严重** | 核心功能不可用 | 15分钟内 | 运维团队 + 技术负责人 | 控制器无法连接、API 大量错误 |
| **P2 - 重要** | 部分功能受影响 | 1小时内 | 运维团队 | 性能下降、部分客户端无法连接 |
| **P3 - 一般** | 轻微影响 | 4小时内 | 值班人员 | 日志告警、轻微性能波动 |

### 1.3 响应决策树

```
发现问题
  │
  ├─ 服务是否可访问？
  │   ├─ 否 → P0/P1 → 立即启动应急响应
  │   └─ 是 → 继续
  │
  ├─ 是否影响核心功能？
  │   ├─ 是 → P1/P2 → 快速评估和处理
  │   └─ 否 → 继续
  │
  └─ 是否影响用户体验？
      ├─ 是 → P2/P3 → 计划修复
      └─ 否 → P3/P4 → 记录跟踪
```

### 1.4 标准响应步骤

#### 步骤 1：问题确认（5分钟内）

- **记录时间**：事件发现时间
- **收集信息**：
  - 故障现象描述
  - 影响范围
  - 当前系统状态
- **初步分类**：确定事件等级

#### 步骤 2：通知相关人员（10分钟内）

- **P0/P1**：电话 + 短信 + 企业微信群
- **P2/P3**：企业微信群 + 邮件

#### 步骤 3：快速止损（30分钟内）

- 执行临时解决方案
- 防止问题扩散
- 保护数据安全

#### 步骤 4：恢复服务（1-4小时）

- 根据事件等级执行恢复流程
- 持续监控系统状态
- 定时汇报进展

#### 步骤 5：验证恢复（15-30分钟）

- 功能验证
- 性能验证
- 数据完整性验证

#### 步骤 6：总结报告（24小时内）

- 编写事件报告
- 根因分析
- 改进措施

---

## 2. 服务中断应急处理

### 2.1 服务完全停止

**事件等级**：P0  
**响应时间**：立即

#### 2.1.1 快速诊断

```powershell
# Windows
# 1. 检查服务状态
Get-Service ZakYipSingulation

# 2. 查看最新日志
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 100

# 3. 检查端口占用
netstat -ano | findstr :5005

# 4. 查看系统资源
Get-Process -Name ZakYip.Singulation.Host | Select-Object CPU, WorkingSet64
```

```bash
# Linux/Docker
# 1. 检查容器状态
docker ps -a | grep singulation

# 2. 查看容器日志
docker logs singulation-host --tail 100

# 3. 检查端口监听
netstat -tuln | grep 5005

# 4. 查看系统资源
docker stats singulation-host --no-stream
```

#### 2.1.2 应急处理流程

**方案 A：快速重启（优先尝试）**

```powershell
# Windows
# 1. 重启服务
Restart-Service ZakYipSingulation

# 2. 等待 10 秒
Start-Sleep -Seconds 10

# 3. 验证服务状态
$service = Get-Service ZakYipSingulation
if ($service.Status -eq 'Running') {
    Write-Host "✅ 服务已恢复" -ForegroundColor Green
    
    # 测试 API
    $response = Invoke-WebRequest -Uri "http://localhost:5005/swagger" -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API 正常" -ForegroundColor Green
    }
} else {
    Write-Host "❌ 重启失败，尝试方案 B" -ForegroundColor Red
}
```

**方案 B：清理后重启**

```powershell
# Windows
# 1. 强制停止进程
Get-Process -Name ZakYip.Singulation.Host -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. 清理临时文件
Remove-Item "$env:TEMP\ZakYip*" -Recurse -Force -ErrorAction SilentlyContinue

# 3. 检查端口占用
$process = Get-NetTCPConnection -LocalPort 5005 -ErrorAction SilentlyContinue
if ($process) {
    Stop-Process -Id $process.OwningProcess -Force
}

# 4. 重启服务
Start-Service ZakYipSingulation
```

**方案 C：回滚到上一版本**

```powershell
# 1. 停止服务
Stop-Service ZakYipSingulation

# 2. 备份当前版本
Copy-Item "C:\ZakYip.Singulation" "C:\Temp\ZakYip.Current" -Recurse

# 3. 还原上一版本
Copy-Item "C:\Backups\ZakYip.Previous\*" "C:\ZakYip.Singulation\" -Recurse -Force

# 4. 启动服务
Start-Service ZakYipSingulation
```

### 2.2 API 服务不响应

**事件等级**：P1  
**响应时间**：15分钟内

#### 2.2.1 诊断步骤

```powershell
# 1. 测试 API 端点
$endpoints = @(
    "http://localhost:5005/swagger",
    "http://localhost:5005/api/axes/axes",
    "http://localhost:5005/health"
)

foreach ($endpoint in $endpoints) {
    try {
        $response = Invoke-WebRequest -Uri $endpoint -TimeoutSec 5
        Write-Host "✅ $endpoint - OK" -ForegroundColor Green
    } catch {
        Write-Host "❌ $endpoint - 失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 2. 检查并发连接数
netstat -ano | findstr :5005 | Measure-Object

# 3. 检查内存使用
Get-Process -Name ZakYip.Singulation.Host | Format-Table Name, CPU, WorkingSet, Threads
```

#### 2.2.2 处理方案

**场景 1：请求超时**

```powershell
# 可能原因：性能瓶颈、死锁
# 解决方案：
# 1. 收集性能数据
dotnet-trace collect --process-id <PID> --duration 00:00:30

# 2. 分析慢查询
# 查看日志中的慢请求

# 3. 如问题持续，重启服务
Restart-Service ZakYipSingulation
```

**场景 2：HTTP 500 错误**

```powershell
# 1. 查看详细错误日志
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" | Select-String "Exception|Error" | Select-Object -Last 20

# 2. 检查数据库连接
# 验证 data/singulation.db 文件是否存在和可访问

# 3. 如是数据库问题，从备份恢复
.\ops\restore.ps1 -BackupDir "C:\Backups\ZakYip.Singulation\latest"
```

### 2.3 SignalR 连接失败

**事件等级**：P2  
**响应时间**：1小时内

#### 2.3.1 诊断

```powershell
# 1. 测试 SignalR Hub
$hubUrl = "http://localhost:5005/hubs/events"
Invoke-WebRequest -Uri $hubUrl -Method GET

# 2. 检查 WebSocket 支持
Test-NetConnection -ComputerName localhost -Port 5005

# 3. 查看 SignalR 日志
Get-Content "C:\ZakYip.Singulation\logs\app-*.log" | Select-String "SignalR|Hub"
```

#### 2.3.2 解决方案

```json
// 临时禁用 WebSocket，改用长轮询
{
  "SignalR": {
    "TransportType": "LongPolling"
  }
}
```

---

## 3. 网络故障应急处理

### 3.1 内网中断

**事件等级**：P0（如影响生产）  
**响应时间**：立即

#### 3.1.1 故障确认

```powershell
# 1. 测试网络连通性
Test-NetConnection -ComputerName 192.168.1.1 -Port 80

# 2. 检查网卡状态
Get-NetAdapter

# 3. 查看路由表
Get-NetRoute

# 4. 测试 DNS 解析
nslookup google.com
```

#### 3.1.2 应急处理

**步骤 1：切换到备用网络（如有）**

```powershell
# 启用备用网卡
Enable-NetAdapter -Name "备用网卡"

# 配置静态 IP
New-NetIPAddress -InterfaceAlias "备用网卡" -IPAddress 192.168.2.100 -PrefixLength 24 -DefaultGateway 192.168.2.1
```

**步骤 2：联系网络管理员**

- 报告网络故障
- 提供详细信息（IP、网卡、错误信息）
- 要求紧急修复

**步骤 3：启用离线模式（临时方案）**

```json
// 配置为离线模式
{
  "UdpDiscovery": {
    "Enabled": false
  },
  "OfflineMode": true,
  "LocalControllerIp": "127.0.0.1"
}
```

### 3.2 UDP 广播不可达

**事件等级**：P2  
**响应时间**：1小时内

#### 3.2.1 诊断

```powershell
# 1. 检查防火墙规则
Get-NetFirewallRule | Where-Object { $_.DisplayName -like "*ZakYip*" -or $_.LocalPort -eq 18888 }

# 2. 测试 UDP 端口
Test-NetConnection -ComputerName localhost -Port 18888 -InformationLevel Detailed

# 3. 查看 UDP 监听
netstat -ano | findstr :18888
```

#### 3.2.2 解决方案

**方案 A：添加防火墙规则**

```powershell
New-NetFirewallRule -DisplayName "ZakYip UDP Discovery" `
    -Direction Inbound `
    -LocalPort 18888 `
    -Protocol UDP `
    -Action Allow
```

**方案 B：禁用 UDP 发现，使用手动配置**

```json
{
  "UdpDiscovery": {
    "Enabled": false
  },
  "ManualEndpoints": [
    "http://192.168.1.100:5005",
    "http://192.168.1.101:5005"
  ]
}
```

### 3.3 网络延迟过高

**事件等级**：P2  
**响应时间**：1小时内

#### 3.3.1 诊断

```powershell
# 1. 测试延迟
Test-NetConnection -ComputerName 192.168.1.100 -TraceRoute

# 2. 持续 Ping 测试
ping -t 192.168.1.100

# 3. 检查网络质量
pathping 192.168.1.100
```

#### 3.3.2 优化措施

```json
// 调整超时配置
{
  "SignalR": {
    "KeepAliveInterval": "00:00:30",
    "ClientTimeoutInterval": "00:01:00"
  },
  "AxisController": {
    "CommandTimeoutMs": 10000,
    "ReconnectDelayMs": 10000
  }
}
```

---

## 4. 设备故障应急处理

### 4.1 雷赛控制卡故障

**事件等级**：P0（生产环境）  
**响应时间**：立即

#### 4.1.1 故障诊断

```powershell
# 1. 检查控制卡连接
Test-NetConnection -ComputerName 192.168.1.100 -Port 502

# 2. 查看控制器状态
$response = Invoke-RestMethod -Uri "http://localhost:5005/api/axes/controller/status"
$response | ConvertTo-Json

# 3. 查看轴状态
$axes = Invoke-RestMethod -Uri "http://localhost:5005/api/axes/axes"
$axes | Where-Object { $_.state -ne "Online" }
```

#### 4.1.2 应急处理流程

**步骤 1：快速诊断**

```
硬件检查
├─ 电源指示灯是否正常？
├─ 网口指示灯是否闪烁？
├─ 有无报警灯？
└─ 能否 Ping 通？
```

**步骤 2：分类处理**

**场景 A：电源故障**

```
1. 检查电源线连接
2. 检查保险丝
3. 测量电源电压
4. 更换电源模块（如有备件）
```

**场景 B：网络故障**

```
1. 检查网线连接
2. 更换网线
3. 检查交换机端口
4. 重新配置 IP 地址
```

**场景 C：固件故障**

```
1. 记录错误代码
2. 尝试重启控制卡
3. 如无法恢复，联系厂商技术支持
4. 准备更换控制卡
```

**步骤 3：切换到备用设备（如有）**

```json
// 更新控制器配置
{
  "vendor": "leadshine",
  "controllerIp": "192.168.1.101",  // 备用设备 IP
  "template": {
    "card": 0,
    "port": 0,
    "axisCount": 8
  }
}
```

**步骤 4：通知相关方**

- 生产管理部门
- 设备维护部门
- 厂商技术支持

### 4.2 伺服驱动器故障

**事件等级**：P1  
**响应时间**：15分钟内

#### 4.2.1 故障现象

- 轴无法使能
- 运动异常（抖动、失速）
- 报警灯亮起
- 过热保护

#### 4.2.2 应急处理

**快速检查清单**：

```
□ 检查驱动器电源
□ 检查控制信号连接
□ 检查编码器反馈
□ 检查电机连线
□ 查看驱动器面板错误代码
□ 检查负载是否卡死
```

**安全命令**：

```bash
# 1. 禁用故障轴
POST http://localhost:5005/api/axes/axes/axis1/disable

# 2. 触发急停
POST http://localhost:5005/api/safety/commands
{
  "command": 2,  // EmergencyStop
  "reason": "Servo drive alarm on axis1"
}

# 3. 复位错误
POST http://localhost:5005/api/axes/controller/reset
```

### 4.3 服务器硬件故障

**事件等级**：P0  
**响应时间**：立即

#### 4.3.1 故障类型

| 故障类型 | 现象 | 应急处理 |
|----------|------|----------|
| **内存故障** | 系统频繁重启、蓝屏 | 更换内存条 |
| **硬盘故障** | 读写错误、慢速响应 | 从备份恢复到新硬盘 |
| **CPU 过热** | 自动关机、性能下降 | 检查散热器、降频运行 |
| **电源故障** | 无法开机、意外断电 | 更换电源 |

#### 4.3.2 快速迁移流程

**准备备用服务器**：

```powershell
# 1. 在备用服务器上安装应用
Expand-Archive -Path "ZakYip.Singulation-v1.0.0.zip" -DestinationPath "C:\ZakYip.Singulation"

# 2. 从网络备份恢复数据
.\ops\restore.ps1 -BackupDir "\\nas-server\backups\ZakYip.Singulation\latest"

# 3. 安装服务
.\ops\install.ps1

# 4. 更新 IP 地址（如需要）
# 修改 appsettings.json 中的配置

# 5. 启动服务
Start-Service ZakYipSingulation

# 6. 通知客户端切换到新服务器
# 更新 DNS 记录或通知用户手动更改配置
```

---

## 5. 数据安全事件处理

### 5.1 数据库损坏

**事件等级**：P0  
**响应时间**：立即

#### 5.1.1 故障确认

```powershell
# 1. 尝试打开数据库
try {
    $db = New-Object LiteDB.LiteDatabase("data/singulation.db")
    $db.Dispose()
    Write-Host "✅ 数据库正常" -ForegroundColor Green
} catch {
    Write-Host "❌ 数据库损坏: $($_.Exception.Message)" -ForegroundColor Red
}

# 2. 检查文件大小
Get-Item "C:\ZakYip.Singulation\data\singulation.db" | Select-Object Length, CreationTime, LastWriteTime
```

#### 5.1.2 恢复流程

**方案 A：尝试修复**

```powershell
# 1. 停止服务
Stop-Service ZakYipSingulation

# 2. 备份损坏的数据库
Copy-Item "C:\ZakYip.Singulation\data\singulation.db" "C:\Temp\singulation.db.corrupted"

# 3. 使用 LiteDB Shell 修复
# 下载 LiteDB.Shell
.\LiteDB.Shell.exe "C:\ZakYip.Singulation\data\singulation.db"
# 在 Shell 中执行：
# > db.rebuild()

# 4. 启动服务并验证
Start-Service ZakYipSingulation
```

**方案 B：从备份恢复**

```powershell
# 1. 停止服务
Stop-Service ZakYipSingulation

# 2. 选择最近的备份
$backups = Get-ChildItem "C:\Backups\ZakYip.Singulation" -Directory | Sort-Object Name -Descending
$latestBackup = $backups[0]

Write-Host "使用备份: $($latestBackup.FullName)" -ForegroundColor Yellow

# 3. 恢复数据
.\ops\restore.ps1 -BackupDir $latestBackup.FullName

# 4. 启动服务
Start-Service ZakYipSingulation
```

### 5.2 数据意外删除

**事件等级**：P1  
**响应时间**：15分钟内

#### 5.2.1 数据恢复

```powershell
# 1. 立即停止服务，防止覆盖
Stop-Service ZakYipSingulation

# 2. 从最近备份恢复
$latestBackup = Get-ChildItem "C:\Backups\ZakYip.Singulation" -Directory | Sort-Object Name -Descending | Select-Object -First 1

# 3. 恢复数据库
Copy-Item "$($latestBackup.FullName)\data\singulation.db" "C:\ZakYip.Singulation\data\singulation.db" -Force

# 4. 启动服务
Start-Service ZakYipSingulation

# 5. 验证数据
$response = Invoke-RestMethod -Uri "http://localhost:5005/api/axes/axes"
Write-Host "恢复的轴数量: $($response.Count)" -ForegroundColor Cyan
```

### 5.3 配置文件丢失

**事件等级**：P1  
**响应时间**：15分钟内

#### 5.3.1 恢复配置

```powershell
# 方案 A：从备份恢复
Copy-Item "C:\Backups\ZakYip.Singulation\latest\config\appsettings.json" "C:\ZakYip.Singulation\"

# 方案 B：从版本控制恢复
git checkout -- appsettings.json

# 方案 C：使用默认配置
# 从安装包中提取默认配置文件
```

---

## 6. 性能问题应急处理

### 6.1 CPU 使用率过高

**事件等级**：P2  
**响应时间**：1小时内

#### 6.1.1 诊断

```powershell
# 1. 查看 CPU 使用率
Get-Process -Name ZakYip.Singulation.Host | Select-Object CPU, Threads, WorkingSet64

# 2. 分析线程活动
dotnet-trace collect --process-id <PID> --duration 00:00:30

# 3. 查看热点方法
# 使用 PerfView 或 Visual Studio 分析 trace 文件
```

#### 6.1.2 临时缓解

```powershell
# 1. 降低进程优先级
$process = Get-Process -Name ZakYip.Singulation.Host
$process.PriorityClass = "BelowNormal"

# 2. 限制线程数
# 修改配置
{
  "ThreadPool": {
    "MaxWorkerThreads": 100
  }
}

# 3. 重启服务
Restart-Service ZakYipSingulation
```

### 6.2 内存泄漏

**事件等级**：P2  
**响应时间**：1小时内

#### 6.2.1 诊断

```powershell
# 1. 监控内存使用
$process = Get-Process -Name ZakYip.Singulation.Host
while ($true) {
    $mem = [math]::Round($process.WorkingSet64 / 1MB, 2)
    Write-Host "$(Get-Date -Format 'HH:mm:ss') - 内存: $mem MB"
    Start-Sleep -Seconds 60
}

# 2. 采集内存转储
dotnet-dump collect --process-id <PID>

# 3. 分析内存泄漏
dotnet-dump analyze dump_<timestamp>.dmp
# > dumpheap -stat
# > gcroot <object_address>
```

#### 6.2.2 临时缓解

```powershell
# 1. 重启服务释放内存
Restart-Service ZakYipSingulation

# 2. 设置定时重启（临时方案）
$action = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument '-Command "Restart-Service ZakYipSingulation"'
$trigger = New-ScheduledTaskTrigger -Daily -At 3am
Register-ScheduledTask -TaskName "ZakYip Service Restart" -Action $action -Trigger $trigger
```

### 6.3 磁盘空间不足

**事件等级**：P1  
**响应时间**：15分钟内

#### 6.3.1 快速清理

```powershell
# 1. 检查磁盘空间
Get-PSDrive C | Select-Object Used, Free

# 2. 清理旧日志
$logPath = "C:\ZakYip.Singulation\logs"
Get-ChildItem $logPath -Filter "*.log" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } | 
    Remove-Item -Force

# 3. 清理临时文件
Remove-Item "$env:TEMP\ZakYip*" -Recurse -Force -ErrorAction SilentlyContinue

# 4. 清理旧备份
$backupPath = "C:\Backups\ZakYip.Singulation"
Get-ChildItem $backupPath -Directory | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } | 
    Remove-Item -Recurse -Force

# 5. 压缩数据库
# 在服务重启时自动执行
Restart-Service ZakYipSingulation
```

---

## 7. 安全漏洞应急处理

### 7.1 发现安全漏洞

**事件等级**：根据漏洞严重程度（P0-P2）  
**响应时间**：根据等级

#### 7.1.1 漏洞评估

| 严重程度 | 影响 | 响应时间 | 示例 |
|----------|------|----------|------|
| **Critical** | 远程代码执行 | 立即 | 未授权访问、SQL注入 |
| **High** | 数据泄露 | 24小时内 | 敏感信息泄露、权限提升 |
| **Medium** | 功能滥用 | 1周内 | 越权访问、XSS |
| **Low** | 轻微影响 | 1个月内 | 信息泄露、配置问题 |

#### 7.1.2 应急响应

**步骤 1：隔离受影响系统**

```powershell
# 1. 启用 IP 白名单
# 修改配置，仅允许内网访问
{
  "AllowedIPs": ["192.168.1.0/24"]
}

# 2. 临时禁用公网访问
# 添加防火墙规则，阻止外网访问
New-NetFirewallRule -DisplayName "Block External Access" `
    -Direction Inbound `
    -RemoteAddress Internet `
    -Action Block
```

**步骤 2：评估影响范围**

```powershell
# 1. 审查访问日志
Get-Content "C:\ZakYip.Singulation\logs\access-*.log" | Select-String "suspicious_pattern"

# 2. 检查是否有异常请求
# 查找大量失败的认证尝试、异常 IP 等
```

**步骤 3：应用临时补丁**

```powershell
# 1. 下载安全补丁
Invoke-WebRequest -Uri "https://releases/security-patch.zip" -OutFile "patch.zip"

# 2. 停止服务
Stop-Service ZakYipSingulation

# 3. 应用补丁
Expand-Archive -Path "patch.zip" -DestinationPath "C:\ZakYip.Singulation\" -Force

# 4. 启动服务
Start-Service ZakYipSingulation
```

**步骤 4：通知相关方**

- 安全团队
- 用户（如需要）
- 监管机构（如需要）

### 7.2 遭受攻击

**事件等级**：P0  
**响应时间**：立即

#### 7.2.1 DDoS 攻击

**检测**：

```powershell
# 1. 检查连接数
$connections = netstat -ano | findstr :5005 | Measure-Object
Write-Host "当前连接数: $($connections.Count)"

# 2. 查看 IP 分布
netstat -ano | findstr :5005 | ForEach-Object {
    $parts = $_ -split '\s+'
    $parts[2] -replace ':\d+$', ''
} | Group-Object | Sort-Object Count -Descending | Select-Object -First 10
```

**缓解措施**：

```powershell
# 1. 启用连接限制
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConnectionsPerIP": 10
    }
  }
}

# 2. 启用速率限制
# 使用 ASP.NET Core Rate Limiting

# 3. 联系 ISP 或云服务商启用 DDoS 防护
```

#### 7.2.2 暴力破解攻击

**检测**：

```powershell
# 查找大量失败的认证尝试
Get-Content "C:\ZakYip.Singulation\logs\app-*.log" | 
    Select-String "Authentication failed" | 
    Group-Object { $_ -replace '^.*from (\S+).*', '$1' } | 
    Sort-Object Count -Descending | 
    Select-Object -First 10
```

**缓解措施**：

```powershell
# 1. 临时封禁攻击 IP
$attackerIPs = @("1.2.3.4", "5.6.7.8")
foreach ($ip in $attackerIPs) {
    New-NetFirewallRule -DisplayName "Block $ip" `
        -Direction Inbound `
        -RemoteAddress $ip `
        -Action Block
}

# 2. 启用账户锁定策略
# 3. 强制密码复杂度
# 4. 启用多因素认证（如已实现）
```

---

## 8. 应急联系人

### 8.1 技术团队联系方式

| 角色 | 姓名 | 电话 | 企业微信 | 邮箱 | 职责 |
|------|------|------|----------|------|------|
| **技术负责人** | 张三 | 138-xxxx-1234 | @zhangsan | tech-lead@example.com | 总体技术决策 |
| **运维负责人** | 李四 | 139-xxxx-5678 | @lisi | ops-lead@example.com | 运维协调 |
| **值班工程师（周一、三、五）** | 王五 | 136-xxxx-9012 | @wangwu | oncall1@example.com | 应急响应 |
| **值班工程师（周二、四、六、日）** | 赵六 | 137-xxxx-3456 | @zhaoliu | oncall2@example.com | 应急响应 |
| **数据库专家** | 钱七 | 135-xxxx-7890 | @qianqi | dba@example.com | 数据库问题 |
| **网络工程师** | 孙八 | 134-xxxx-1122 | @sunba | network@example.com | 网络问题 |

### 8.2 外部联系方式

| 服务商 | 联系人 | 电话 | 邮箱 | 服务内容 |
|--------|--------|------|------|----------|
| **雷赛科技** | 技术支持 | 400-xxxx-xxxx | support@leadshine.com | 控制卡技术支持 |
| **云服务商** | 客服 | 400-yyyy-yyyy | cloud-support@example.com | 服务器和网络 |
| **ISP** | 故障申报 | 10086 | - | 网络服务 |

### 8.3 应急通知流程

```
发现问题
    │
    ├─ P0/P1 → 电话通知技术负责人 + 运维负责人 + 值班工程师
    │         同时在企业微信群发送告警
    │
    ├─ P2 → 企业微信群通知值班工程师
    │       抄送技术负责人
    │
    └─ P3 → 邮件通知值班工程师
            记录到问题跟踪系统
```

### 8.4 升级机制

**升级条件**：
- 30分钟内无法解决 P0 问题
- 1小时内无法解决 P1 问题
- 问题影响范围扩大

**升级流程**：
1. 值班工程师 → 技术负责人
2. 技术负责人 → 技术总监
3. 技术总监 → CTO
4. CTO → CEO（特别重大事件）

---

## 附录

### A. 应急工具箱

**Windows**：
```powershell
# 创建应急工具包
$toolbox = "C:\EmergencyToolbox"
New-Item -ItemType Directory -Path $toolbox -Force

# 添加常用工具
# - PerfView
# - dotnet-trace
# - dotnet-dump
# - Process Explorer
# - Wireshark
# - LiteDB.Shell
```

### B. 快速命令参考

**服务管理**：
```powershell
# 查看状态
Get-Service ZakYipSingulation

# 重启服务
Restart-Service ZakYipSingulation

# 查看日志
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50 -Wait
```

**性能诊断**：
```powershell
# CPU 使用率
Get-Process -Name ZakYip.Singulation.Host | Select-Object CPU

# 内存使用
Get-Process -Name ZakYip.Singulation.Host | Select-Object WorkingSet64

# 网络连接
netstat -ano | findstr :5005
```

### C. 应急演练计划

**演练频率**：每季度一次

**演练场景**：
1. 服务中断恢复演练
2. 数据库损坏恢复演练
3. 网络故障切换演练
4. 安全事件响应演练

**演练记录模板**：
```markdown
## 演练报告

**日期**：2025-10-19  
**场景**：服务中断恢复演练  
**参与人员**：张三、李四、王五

### 演练过程
1. 模拟服务停止
2. 执行诊断步骤
3. 执行恢复流程
4. 验证恢复结果

### 发现问题
- 备份文件权限不足
- 恢复脚本路径错误

### 改进措施
- 修正备份权限设置
- 更新恢复脚本
- 增加自动化验证

### 总结
演练整体顺利，发现并修复了2个潜在问题。
```

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 运维团队  
**审核**：技术负责人、运维负责人
