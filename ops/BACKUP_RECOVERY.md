# ZakYip.Singulation 备份恢复流程

## 文档概述

本文档详细说明 ZakYip.Singulation 系统的数据备份、恢复流程、自动化脚本和最佳实践。

**文档版本**：1.0  
**最后更新**：2025-10-19  
**适用版本**：ZakYip.Singulation v1.0.0+

## 目录

1. [备份策略](#1-备份策略)
2. [备份内容](#2-备份内容)
3. [手动备份](#3-手动备份)
4. [自动化备份](#4-自动化备份)
5. [数据恢复](#5-数据恢复)
6. [灾难恢复](#6-灾难恢复)
7. [验证与测试](#7-验证与测试)
8. [常见问题](#8-常见问题)

---

## 1. 备份策略

### 1.1 备份类型

| 备份类型 | 频率 | 保留期 | 存储位置 | 优先级 |
|----------|------|--------|----------|--------|
| **全量备份** | 每日 02:00 | 7 天 | 本地 + 远程 | 高 |
| **增量备份** | 每 4 小时 | 24 小时 | 本地 | 中 |
| **配置备份** | 每次变更 | 30 天 | 本地 + Git | 高 |
| **日志归档** | 每周日 | 90 天 | 远程存储 | 低 |
| **完整系统镜像** | 每月 | 6 个月 | 远程存储 | 中 |

### 1.2 备份策略矩阵

#### 1.2.1 生产环境备份策略

**3-2-1 原则**：
- **3 份副本**：1 份生产 + 2 份备份
- **2 种介质**：本地磁盘 + 远程存储（云存储/NAS）
- **1 份离线**：至少 1 份离线或异地备份

**实施方案**：
```
生产数据
├── 副本1：本地备份（每日全量 + 每4小时增量）
├── 副本2：远程NAS（每日同步）
└── 副本3：云存储（每周上传）
```

#### 1.2.2 开发测试环境

**简化策略**：
- 每日一次全量备份
- 保留最近 3 天
- 仅本地存储

### 1.3 RTO 和 RPO 目标

| 环境 | RTO（恢复时间目标） | RPO（恢复点目标） |
|------|---------------------|-------------------|
| **生产环境** | < 30 分钟 | < 4 小时 |
| **测试环境** | < 2 小时 | < 24 小时 |
| **开发环境** | < 4 小时 | < 48 小时 |

---

## 2. 备份内容

### 2.1 必须备份的内容

#### 2.1.1 数据库文件

| 文件 | 位置 | 大小（估计） | 重要性 |
|------|------|--------------|--------|
| singulation.db | data/ | 10-100 MB | ⭐⭐⭐⭐⭐ |
| singulation-log.log | data/ | 可忽略 | ⭐ |

#### 2.1.2 配置文件

| 文件 | 位置 | 重要性 |
|------|------|--------|
| appsettings.json | 根目录 | ⭐⭐⭐⭐⭐ |
| appsettings.Production.json | 根目录 | ⭐⭐⭐⭐⭐ |
| appsettings.Development.json | 根目录 | ⭐⭐ |
| certificate.pfx | certs/ | ⭐⭐⭐⭐ |

#### 2.1.3 证书和密钥

| 内容 | 位置 | 重要性 |
|------|------|--------|
| SSL/TLS 证书 | certs/ | ⭐⭐⭐⭐⭐ |
| 私钥文件 | certs/ | ⭐⭐⭐⭐⭐ |

### 2.2 可选备份的内容

| 内容 | 位置 | 建议 |
|------|------|------|
| 应用日志 | logs/ | 每周归档一次 |
| 临时文件 | temp/ | 无需备份 |
| 构建产物 | bin/, obj/ | 无需备份（可重新构建） |

### 2.3 备份文件结构

```
backup/
└── ZakYip.Singulation/
    └── 20251019_020000/          # 时间戳
        ├── data/                  # 数据库文件
        │   └── singulation.db
        ├── config/                # 配置文件
        │   ├── appsettings.json
        │   └── appsettings.Production.json
        ├── certs/                 # 证书文件
        │   └── certificate.pfx
        ├── logs/                  # 日志归档（可选）
        │   └── app-2025-10-19.log
        └── metadata.json          # 备份元信息
```

**metadata.json** 示例：
```json
{
  "backupTime": "2025-10-19T02:00:00Z",
  "version": "1.0.0",
  "backupType": "full",
  "size": 52428800,
  "files": [
    "data/singulation.db",
    "config/appsettings.json"
  ],
  "checksum": {
    "algorithm": "SHA256",
    "value": "abc123..."
  }
}
```

---

## 3. 手动备份

### 3.1 Windows 环境手动备份

#### 3.1.1 基础备份脚本

**backup-manual.ps1**：

```powershell
# 手动备份脚本
param(
    [string]$SourceDir = "C:\ZakYip.Singulation",
    [string]$BackupRoot = "C:\Backups\ZakYip.Singulation"
)

# 创建备份目录
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $BackupRoot $timestamp
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

Write-Host "开始备份到: $BackupDir" -ForegroundColor Green

try {
    # 1. 备份数据库
    Write-Host "备份数据库..." -ForegroundColor Yellow
    $dataBackupDir = Join-Path $BackupDir "data"
    New-Item -ItemType Directory -Path $dataBackupDir -Force | Out-Null
    Copy-Item -Path "$SourceDir\data\*.db" -Destination $dataBackupDir -Force
    
    # 2. 备份配置文件
    Write-Host "备份配置文件..." -ForegroundColor Yellow
    $configBackupDir = Join-Path $BackupDir "config"
    New-Item -ItemType Directory -Path $configBackupDir -Force | Out-Null
    Copy-Item -Path "$SourceDir\appsettings*.json" -Destination $configBackupDir -Force
    
    # 3. 备份证书（如果存在）
    if (Test-Path "$SourceDir\certs") {
        Write-Host "备份证书..." -ForegroundColor Yellow
        $certsBackupDir = Join-Path $BackupDir "certs"
        New-Item -ItemType Directory -Path $certsBackupDir -Force | Out-Null
        Copy-Item -Path "$SourceDir\certs\*" -Destination $certsBackupDir -Recurse -Force
    }
    
    # 4. 生成备份元信息
    $metadata = @{
        backupTime = (Get-Date).ToUniversalTime().ToString("o")
        version = "1.0.0"
        backupType = "manual"
        sourceDir = $SourceDir
    } | ConvertTo-Json
    
    $metadata | Out-File -FilePath (Join-Path $BackupDir "metadata.json") -Encoding UTF8
    
    # 5. 计算备份大小
    $backupSize = (Get-ChildItem $BackupDir -Recurse | Measure-Object -Property Length -Sum).Sum
    $backupSizeMB = [math]::Round($backupSize / 1MB, 2)
    
    Write-Host "✅ 备份完成！" -ForegroundColor Green
    Write-Host "备份位置: $BackupDir" -ForegroundColor Cyan
    Write-Host "备份大小: $backupSizeMB MB" -ForegroundColor Cyan
    
    # 6. 清理旧备份（保留最近 7 天）
    Write-Host "清理旧备份..." -ForegroundColor Yellow
    Get-ChildItem $BackupRoot -Directory | 
        Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } | 
        Remove-Item -Recurse -Force
    
    Write-Host "✅ 旧备份已清理" -ForegroundColor Green
    
} catch {
    Write-Host "❌ 备份失败: $_" -ForegroundColor Red
    exit 1
}
```

**使用方法**：

```powershell
# 使用默认路径
.\backup-manual.ps1

# 自定义路径
.\backup-manual.ps1 -SourceDir "D:\Apps\ZakYip.Singulation" -BackupRoot "E:\Backups"
```

#### 3.1.2 压缩备份脚本

**backup-compressed.ps1**：

```powershell
# 压缩备份脚本
param(
    [string]$SourceDir = "C:\ZakYip.Singulation",
    [string]$BackupRoot = "C:\Backups\ZakYip.Singulation"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$TempBackupDir = Join-Path $env:TEMP "zakyip_backup_$timestamp"
$ZipFile = Join-Path $BackupRoot "backup_$timestamp.zip"

Write-Host "开始创建压缩备份..." -ForegroundColor Green

try {
    # 1. 创建临时备份目录
    New-Item -ItemType Directory -Path $TempBackupDir -Force | Out-Null
    
    # 2. 复制文件
    Copy-Item -Path "$SourceDir\data" -Destination "$TempBackupDir\data" -Recurse -Force
    Copy-Item -Path "$SourceDir\appsettings*.json" -Destination $TempBackupDir -Force
    if (Test-Path "$SourceDir\certs") {
        Copy-Item -Path "$SourceDir\certs" -Destination "$TempBackupDir\certs" -Recurse -Force
    }
    
    # 3. 创建压缩文件
    Write-Host "压缩备份文件..." -ForegroundColor Yellow
    Compress-Archive -Path "$TempBackupDir\*" -DestinationPath $ZipFile -CompressionLevel Optimal
    
    # 4. 清理临时目录
    Remove-Item -Path $TempBackupDir -Recurse -Force
    
    $zipSize = (Get-Item $ZipFile).Length / 1MB
    Write-Host "✅ 压缩备份完成！" -ForegroundColor Green
    Write-Host "备份文件: $ZipFile" -ForegroundColor Cyan
    Write-Host "文件大小: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Cyan
    
    # 5. 清理旧备份
    Get-ChildItem $BackupRoot -Filter "backup_*.zip" | 
        Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | 
        Remove-Item -Force
        
} catch {
    Write-Host "❌ 备份失败: $_" -ForegroundColor Red
    if (Test-Path $TempBackupDir) {
        Remove-Item -Path $TempBackupDir -Recurse -Force
    }
    exit 1
}
```

### 3.2 Linux 环境手动备份

#### 3.2.1 基础备份脚本

**backup-manual.sh**：

```bash
#!/bin/bash

# 手动备份脚本
SOURCE_DIR="/app"
BACKUP_ROOT="/backups/zakyip-singulation"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="$BACKUP_ROOT/$TIMESTAMP"

echo "开始备份到: $BACKUP_DIR"

# 创建备份目录
mkdir -p "$BACKUP_DIR"/{data,config,certs}

# 1. 备份数据库
echo "备份数据库..."
cp -r "$SOURCE_DIR/data"/*.db "$BACKUP_DIR/data/" 2>/dev/null || true

# 2. 备份配置
echo "备份配置文件..."
cp "$SOURCE_DIR"/appsettings*.json "$BACKUP_DIR/config/" 2>/dev/null || true

# 3. 备份证书
if [ -d "$SOURCE_DIR/certs" ]; then
    echo "备份证书..."
    cp -r "$SOURCE_DIR/certs"/* "$BACKUP_DIR/certs/" 2>/dev/null || true
fi

# 4. 生成元信息
cat > "$BACKUP_DIR/metadata.json" <<EOF
{
  "backupTime": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "version": "1.0.0",
  "backupType": "manual",
  "sourceDir": "$SOURCE_DIR"
}
EOF

# 5. 计算备份大小
BACKUP_SIZE=$(du -sh "$BACKUP_DIR" | cut -f1)
echo "✅ 备份完成！"
echo "备份位置: $BACKUP_DIR"
echo "备份大小: $BACKUP_SIZE"

# 6. 清理旧备份（保留 7 天）
echo "清理旧备份..."
find "$BACKUP_ROOT" -type d -mtime +7 -exec rm -rf {} + 2>/dev/null || true
echo "✅ 旧备份已清理"
```

**使用方法**：

```bash
# 赋予执行权限
chmod +x backup-manual.sh

# 执行备份
./backup-manual.sh
```

#### 3.2.2 压缩备份脚本

**backup-compressed.sh**：

```bash
#!/bin/bash

SOURCE_DIR="/app"
BACKUP_ROOT="/backups/zakyip-singulation"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
TEMP_DIR="/tmp/zakyip_backup_$TIMESTAMP"
TAR_FILE="$BACKUP_ROOT/backup_$TIMESTAMP.tar.gz"

echo "开始创建压缩备份..."

# 创建临时目录
mkdir -p "$TEMP_DIR"

# 复制文件
cp -r "$SOURCE_DIR/data" "$TEMP_DIR/" 2>/dev/null || true
cp "$SOURCE_DIR"/appsettings*.json "$TEMP_DIR/" 2>/dev/null || true
[ -d "$SOURCE_DIR/certs" ] && cp -r "$SOURCE_DIR/certs" "$TEMP_DIR/" 2>/dev/null || true

# 创建压缩文件
echo "压缩备份文件..."
tar -czf "$TAR_FILE" -C "$TEMP_DIR" .

# 清理临时目录
rm -rf "$TEMP_DIR"

TAR_SIZE=$(du -h "$TAR_FILE" | cut -f1)
echo "✅ 压缩备份完成！"
echo "备份文件: $TAR_FILE"
echo "文件大小: $TAR_SIZE"

# 清理旧备份（保留 30 天）
find "$BACKUP_ROOT" -name "backup_*.tar.gz" -mtime +30 -delete 2>/dev/null || true
```

---

## 4. 自动化备份

### 4.1 Windows 计划任务

#### 4.1.1 创建计划任务

```powershell
# 创建每日凌晨 2 点的备份任务
$action = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument '-NoProfile -ExecutionPolicy Bypass -File "C:\ZakYip.Singulation\ops\backup-auto.ps1"'

$trigger = New-ScheduledTaskTrigger -Daily -At 2am

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 5)

Register-ScheduledTask `
    -TaskName "ZakYip Singulation Daily Backup" `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description "每日自动备份 ZakYip Singulation 数据"
```

#### 4.1.2 创建增量备份任务

```powershell
# 创建每 4 小时的增量备份任务
$action = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument '-NoProfile -ExecutionPolicy Bypass -File "C:\ZakYip.Singulation\ops\backup-incremental.ps1"'

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Hours 4) -RepetitionDuration ([TimeSpan]::MaxValue)

Register-ScheduledTask `
    -TaskName "ZakYip Singulation Incremental Backup" `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Description "每 4 小时增量备份"
```

### 4.2 Linux Cron 任务

#### 4.2.1 配置 Cron

```bash
# 编辑 crontab
crontab -e

# 添加以下任务

# 每日凌晨 2 点全量备份
0 2 * * * /opt/zakyip-singulation/backup-auto.sh >> /var/log/zakyip-backup.log 2>&1

# 每 4 小时增量备份
0 */4 * * * /opt/zakyip-singulation/backup-incremental.sh >> /var/log/zakyip-backup.log 2>&1

# 每周日凌晨 3 点日志归档
0 3 * * 0 /opt/zakyip-singulation/archive-logs.sh >> /var/log/zakyip-archive.log 2>&1
```

### 4.3 Docker 环境自动备份

#### 4.3.1 使用 Docker Cron 容器

**docker-compose.yml** 添加备份服务：

```yaml
services:
  backup:
    image: alpine:latest
    container_name: singulation-backup
    volumes:
      - ./data:/data:ro
      - ./config:/config:ro
      - /backups:/backups
      - ./scripts:/scripts:ro
    environment:
      - BACKUP_SCHEDULE=0 2 * * *
      - BACKUP_RETENTION_DAYS=7
    command: >
      sh -c "
      apk add --no-cache dcron &&
      echo '0 2 * * * /scripts/backup-docker.sh' > /etc/crontabs/root &&
      crond -f -l 2
      "
```

#### 4.3.2 Docker 备份脚本

**backup-docker.sh**：

```bash
#!/bin/sh

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/zakyip-$TIMESTAMP"

mkdir -p "$BACKUP_DIR"

# 备份挂载卷
cp -r /data "$BACKUP_DIR/"
cp -r /config "$BACKUP_DIR/"

# 压缩备份
cd /backups
tar -czf "zakyip-$TIMESTAMP.tar.gz" "zakyip-$TIMESTAMP"
rm -rf "zakyip-$TIMESTAMP"

# 清理旧备份
find /backups -name "zakyip-*.tar.gz" -mtime +7 -delete

echo "✅ Docker 备份完成: zakyip-$TIMESTAMP.tar.gz"
```

### 4.4 备份到远程存储

#### 4.4.1 备份到网络共享（Windows）

```powershell
# 映射网络驱动器
$networkPath = "\\nas-server\backups"
$credential = Get-Credential
New-PSDrive -Name "Z" -PSProvider FileSystem -Root $networkPath -Credential $credential -Persist

# 同步备份
robocopy "C:\Backups\ZakYip.Singulation" "Z:\ZakYip.Singulation" /MIR /R:3 /W:5 /LOG:"C:\Logs\backup-sync.log"
```

#### 4.4.2 备份到云存储（AWS S3）

```bash
#!/bin/bash

# 安装 AWS CLI
# apt install awscli

# 配置 AWS 凭证
# aws configure

BACKUP_FILE="/backups/backup_$(date +%Y%m%d_%H%M%S).tar.gz"
S3_BUCKET="s3://my-backup-bucket/zakyip-singulation/"

# 创建备份
tar -czf "$BACKUP_FILE" -C /app data config

# 上传到 S3
aws s3 cp "$BACKUP_FILE" "$S3_BUCKET"

# 删除本地备份（可选）
rm "$BACKUP_FILE"

# 清理 S3 旧备份（保留 30 天）
aws s3 ls "$S3_BUCKET" | while read -r line; do
    file_date=$(echo "$line" | awk '{print $1}')
    file_name=$(echo "$line" | awk '{print $4}')
    if [ -n "$file_name" ]; then
        days_old=$(( ($(date +%s) - $(date -d "$file_date" +%s)) / 86400 ))
        if [ $days_old -gt 30 ]; then
            aws s3 rm "$S3_BUCKET$file_name"
        fi
    fi
done
```

---

## 5. 数据恢复

### 5.1 Windows 环境恢复

#### 5.1.1 标准恢复流程

**restore.ps1**：

```powershell
# 数据恢复脚本
param(
    [Parameter(Mandatory=$true)]
    [string]$BackupDir,
    [string]$TargetDir = "C:\ZakYip.Singulation"
)

Write-Host "开始从备份恢复数据..." -ForegroundColor Green
Write-Host "备份源: $BackupDir" -ForegroundColor Cyan
Write-Host "恢复目标: $TargetDir" -ForegroundColor Cyan

# 确认操作
$confirm = Read-Host "此操作将覆盖现有数据，是否继续？(yes/no)"
if ($confirm -ne "yes") {
    Write-Host "操作已取消" -ForegroundColor Yellow
    exit 0
}

try {
    # 1. 停止服务
    Write-Host "停止服务..." -ForegroundColor Yellow
    Stop-Service ZakYipSingulation -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    
    # 2. 备份当前数据（以防万一）
    $emergencyBackup = "C:\Temp\emergency_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Write-Host "创建紧急备份到: $emergencyBackup" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $emergencyBackup -Force | Out-Null
    Copy-Item -Path "$TargetDir\data" -Destination "$emergencyBackup\data" -Recurse -Force -ErrorAction SilentlyContinue
    
    # 3. 恢复数据库
    Write-Host "恢复数据库..." -ForegroundColor Yellow
    if (Test-Path "$BackupDir\data") {
        Copy-Item -Path "$BackupDir\data\*" -Destination "$TargetDir\data\" -Force
    }
    
    # 4. 恢复配置
    Write-Host "恢复配置文件..." -ForegroundColor Yellow
    if (Test-Path "$BackupDir\config") {
        Copy-Item -Path "$BackupDir\config\*" -Destination $TargetDir -Force
    } elseif (Test-Path "$BackupDir\appsettings.json") {
        Copy-Item -Path "$BackupDir\appsettings*.json" -Destination $TargetDir -Force
    }
    
    # 5. 恢复证书
    if (Test-Path "$BackupDir\certs") {
        Write-Host "恢复证书..." -ForegroundColor Yellow
        Copy-Item -Path "$BackupDir\certs\*" -Destination "$TargetDir\certs\" -Recurse -Force
    }
    
    # 6. 启动服务
    Write-Host "启动服务..." -ForegroundColor Yellow
    Start-Service ZakYipSingulation
    Start-Sleep -Seconds 5
    
    # 7. 验证恢复
    $service = Get-Service ZakYipSingulation
    if ($service.Status -eq 'Running') {
        Write-Host "✅ 恢复成功！服务正在运行" -ForegroundColor Green
        
        # 测试 API
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5005/swagger" -TimeoutSec 10 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ API 验证成功" -ForegroundColor Green
            }
        } catch {
            Write-Host "⚠️ API 验证失败，请手动检查" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ 服务启动失败，请查看日志" -ForegroundColor Red
        Write-Host "紧急备份位于: $emergencyBackup" -ForegroundColor Yellow
        exit 1
    }
    
} catch {
    Write-Host "❌ 恢复失败: $_" -ForegroundColor Red
    Write-Host "正在尝试恢复到紧急备份..." -ForegroundColor Yellow
    
    if (Test-Path $emergencyBackup) {
        Copy-Item -Path "$emergencyBackup\data\*" -Destination "$TargetDir\data\" -Force
        Start-Service ZakYipSingulation
    }
    
    exit 1
}
```

**使用方法**：

```powershell
# 从最近备份恢复
$latestBackup = Get-ChildItem "C:\Backups\ZakYip.Singulation" -Directory | Sort-Object Name -Descending | Select-Object -First 1
.\restore.ps1 -BackupDir $latestBackup.FullName

# 从指定备份恢复
.\restore.ps1 -BackupDir "C:\Backups\ZakYip.Singulation\20251019_020000"
```

### 5.2 Linux 环境恢复

#### 5.2.1 标准恢复流程

**restore.sh**：

```bash
#!/bin/bash

# 数据恢复脚本
BACKUP_DIR=$1
TARGET_DIR="/app"

if [ -z "$BACKUP_DIR" ]; then
    echo "用法: $0 <备份目录>"
    exit 1
fi

echo "开始从备份恢复数据..."
echo "备份源: $BACKUP_DIR"
echo "恢复目标: $TARGET_DIR"

# 确认操作
read -p "此操作将覆盖现有数据，是否继续？(yes/no): " confirm
if [ "$confirm" != "yes" ]; then
    echo "操作已取消"
    exit 0
fi

# 1. 停止服务
echo "停止服务..."
systemctl stop zakyip-singulation 2>/dev/null || docker-compose stop 2>/dev/null

# 2. 创建紧急备份
EMERGENCY_BACKUP="/tmp/emergency_backup_$(date +%Y%m%d_%H%M%S)"
echo "创建紧急备份到: $EMERGENCY_BACKUP"
mkdir -p "$EMERGENCY_BACKUP"
cp -r "$TARGET_DIR/data" "$EMERGENCY_BACKUP/" 2>/dev/null || true

# 3. 恢复数据
echo "恢复数据库..."
[ -d "$BACKUP_DIR/data" ] && cp -r "$BACKUP_DIR/data"/* "$TARGET_DIR/data/" 2>/dev/null

echo "恢复配置..."
[ -d "$BACKUP_DIR/config" ] && cp "$BACKUP_DIR/config"/* "$TARGET_DIR/" 2>/dev/null
[ -f "$BACKUP_DIR/appsettings.json" ] && cp "$BACKUP_DIR"/appsettings*.json "$TARGET_DIR/" 2>/dev/null

echo "恢复证书..."
[ -d "$BACKUP_DIR/certs" ] && cp -r "$BACKUP_DIR/certs"/* "$TARGET_DIR/certs/" 2>/dev/null

# 4. 启动服务
echo "启动服务..."
systemctl start zakyip-singulation 2>/dev/null || docker-compose start 2>/dev/null
sleep 5

# 5. 验证恢复
if systemctl is-active --quiet zakyip-singulation 2>/dev/null || docker ps | grep -q singulation-host; then
    echo "✅ 恢复成功！服务正在运行"
    
    # 测试 API
    if curl -f http://localhost:5005/swagger > /dev/null 2>&1; then
        echo "✅ API 验证成功"
    else
        echo "⚠️ API 验证失败，请手动检查"
    fi
else
    echo "❌ 服务启动失败，请查看日志"
    echo "紧急备份位于: $EMERGENCY_BACKUP"
    exit 1
fi
```

### 5.3 从压缩备份恢复

#### 5.3.1 Windows

```powershell
# 从 ZIP 备份恢复
param([string]$ZipFile)

$TempDir = Join-Path $env:TEMP "zakyip_restore_$(Get-Date -Format 'yyyyMMdd_HHmmss')"

# 解压
Expand-Archive -Path $ZipFile -DestinationPath $TempDir

# 恢复
.\restore.ps1 -BackupDir $TempDir

# 清理临时目录
Remove-Item -Path $TempDir -Recurse -Force
```

#### 5.3.2 Linux

```bash
#!/bin/bash

TAR_FILE=$1
TEMP_DIR="/tmp/zakyip_restore_$(date +%Y%m%d_%H%M%S)"

# 解压
mkdir -p "$TEMP_DIR"
tar -xzf "$TAR_FILE" -C "$TEMP_DIR"

# 恢复
./restore.sh "$TEMP_DIR"

# 清理临时目录
rm -rf "$TEMP_DIR"
```

---

## 6. 灾难恢复

### 6.1 完全系统崩溃恢复

**恢复步骤**：

1. **准备新服务器**
   - 安装操作系统
   - 安装 .NET 8.0 Runtime
   - 配置网络和防火墙

2. **安装应用程序**
   ```powershell
   # 从发布包安装
   Expand-Archive -Path "ZakYip.Singulation-v1.0.0.zip" -DestinationPath "C:\ZakYip.Singulation"
   ```

3. **恢复数据**
   ```powershell
   # 从最近备份恢复
   .\restore.ps1 -BackupDir "\\nas-server\backups\ZakYip.Singulation\latest"
   ```

4. **安装服务**
   ```powershell
   .\ops\install.ps1
   ```

5. **验证系统**
   - 检查服务状态
   - 测试 API 访问
   - 验证控制器连接
   - 测试客户端连接

### 6.2 数据库损坏恢复

**LiteDB 修复流程**：

```powershell
# 1. 停止服务
Stop-Service ZakYipSingulation

# 2. 备份损坏的数据库
Copy-Item "C:\ZakYip.Singulation\data\singulation.db" "C:\Temp\singulation.db.corrupted"

# 3. 尝试使用 LiteDB Shell 修复
# 下载 LiteDB.Shell: https://github.com/mbdavid/LiteDB.Shell
.\LiteDB.Shell.exe "C:\ZakYip.Singulation\data\singulation.db"
# > db.rebuild()

# 4. 如果修复失败，从备份恢复
.\restore.ps1 -BackupDir "C:\Backups\ZakYip.Singulation\latest"
```

### 6.3 配置文件丢失恢复

**从版本控制恢复**：

```bash
# 如果配置文件在 Git 中管理
git checkout -- appsettings.json appsettings.Production.json

# 或从备份恢复
cp /backups/zakyip-singulation/latest/config/appsettings.json /app/
```

### 6.4 证书丢失恢复

**重新生成证书**（测试环境）：

```powershell
dotnet dev-certs https -ep certificate.pfx -p YourPassword
dotnet dev-certs https --trust
```

**从备份恢复**（生产环境）：

```powershell
Copy-Item "\\nas-server\secure\certs\certificate.pfx" "C:\ZakYip.Singulation\certs\"
```

---

## 7. 验证与测试

### 7.1 备份完整性验证

#### 7.1.1 校验和验证

```powershell
# 生成校验和
$files = Get-ChildItem "C:\Backups\ZakYip.Singulation\20251019_020000" -Recurse -File
$checksums = @{}

foreach ($file in $files) {
    $hash = Get-FileHash -Path $file.FullName -Algorithm SHA256
    $checksums[$file.FullName] = $hash.Hash
}

$checksums | ConvertTo-Json | Out-File "checksum.json"

# 验证校验和
$savedChecksums = Get-Content "checksum.json" | ConvertFrom-Json
foreach ($file in $files) {
    $currentHash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash
    if ($currentHash -ne $savedChecksums.$($file.FullName)) {
        Write-Host "❌ 文件已损坏: $($file.FullName)" -ForegroundColor Red
    }
}
```

### 7.2 恢复演练

**定期恢复测试**（每季度）：

1. **准备测试环境**
   - 独立的测试服务器
   - 不影响生产环境

2. **执行恢复测试**
   ```powershell
   # 在测试环境执行恢复
   .\restore.ps1 -BackupDir "\\nas-server\backups\latest" -TargetDir "D:\Test\ZakYip.Singulation"
   ```

3. **功能验证**
   - 服务能否正常启动
   - API 能否正常访问
   - 数据是否完整
   - 配置是否正确

4. **记录测试结果**
   - 恢复耗时
   - 遇到的问题
   - 改进建议

### 7.3 RTO/RPO 测试

**测试 RTO**（恢复时间目标）：

```powershell
# 记录开始时间
$startTime = Get-Date

# 执行恢复
.\restore.ps1 -BackupDir $backupDir

# 记录结束时间
$endTime = Get-Date
$duration = ($endTime - $startTime).TotalMinutes

Write-Host "恢复耗时: $duration 分钟"
Write-Host "RTO 目标: 30 分钟"

if ($duration -le 30) {
    Write-Host "✅ 符合 RTO 目标" -ForegroundColor Green
} else {
    Write-Host "❌ 超出 RTO 目标，需要优化" -ForegroundColor Red
}
```

---

## 8. 常见问题

### Q1: 备份文件占用空间太大怎么办？

**解决方案**：
1. 使用压缩备份
2. 排除不必要的文件（日志文件）
3. 缩短备份保留期
4. 使用增量备份

### Q2: 自动备份失败怎么办？

**排查步骤**：
1. 检查计划任务是否启用
2. 查看任务执行日志
3. 验证脚本路径是否正确
4. 确认有足够的磁盘空间和权限

### Q3: 恢复后服务无法启动？

**排查步骤**：
1. 查看服务日志
2. 检查配置文件格式
3. 验证数据库文件完整性
4. 确认文件权限正确

### Q4: 如何验证备份是否可用？

**验证方法**：
1. 定期执行恢复演练
2. 使用校验和验证文件完整性
3. 在测试环境恢复并验证功能

### Q5: 备份到云存储是否安全？

**安全建议**：
1. 使用加密传输（HTTPS/TLS）
2. 启用静态数据加密
3. 使用强密码和多因素认证
4. 定期审计访问日志

---

## 附录

### A. 备份脚本清单

| 脚本名称 | 平台 | 用途 |
|----------|------|------|
| backup-manual.ps1 | Windows | 手动全量备份 |
| backup-compressed.ps1 | Windows | 压缩备份 |
| backup-auto.ps1 | Windows | 自动化全量备份 |
| backup-incremental.ps1 | Windows | 增量备份 |
| restore.ps1 | Windows | 数据恢复 |
| backup-manual.sh | Linux | 手动全量备份 |
| backup-compressed.sh | Linux | 压缩备份 |
| backup-docker.sh | Docker | 容器环境备份 |
| restore.sh | Linux | 数据恢复 |

### B. 备份检查清单

**每日检查**：
- [ ] 备份任务是否执行成功
- [ ] 备份文件是否生成
- [ ] 磁盘空间是否充足

**每周检查**：
- [ ] 备份文件完整性验证
- [ ] 旧备份是否正确清理
- [ ] 远程备份是否同步成功

**每月检查**：
- [ ] 执行恢复演练
- [ ] 审查备份策略
- [ ] 更新文档

### C. 联系方式

遇到备份恢复问题，请联系：
- **技术支持**：support@example.com
- **紧急热线**：+86 xxx-xxxx-xxxx

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 运维团队
