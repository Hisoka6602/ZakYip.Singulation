# 上线与运维手册

## 📚 完整运维文档

本目录包含 ZakYip.Singulation 系统的部署脚本和完整的运维文档。

### 核心运维文档

| 文档 | 说明 |
|------|------|
| **[运维手册](OPERATIONS_MANUAL.md)** | 完整的运维指南，涵盖部署、配置、监控、故障排查等全流程 |
| **[配置指南](CONFIGURATION_GUIDE.md)** | 详细的配置参数说明、调优建议和最佳实践 |
| **[备份恢复流程](BACKUP_RECOVERY.md)** | 数据备份策略、自动化脚本和恢复流程 |
| **[应急响应预案](EMERGENCY_RESPONSE.md)** | 各类紧急情况的标准处理流程和预案 |

### 补充文档

- [部署运维手册](../docs/DEPLOYMENT.md) - 详细的部署步骤和环境配置
- [故障排查手册](../docs/TROUBLESHOOTING.md) - 常见问题的诊断和解决方案

---

## 🛠️ 部署脚本

### 脚本列表

| 脚本 | 平台 | 功能 |
|------|------|------|
| `install.ps1` | Windows | 发布 Host 并生成运行包，安装 Windows 服务 |
| `install.sh` | Linux | 发布 Host 并生成运行包 |
| `uninstall.ps1` | Windows | 卸载 Windows 服务 |
| `uninstall.sh` | Linux | 卸载说明（提示手动操作） |
| `selfcheck.ps1` | Windows | 执行 `dotnet build` 进行快速自检 |
| `selfcheck.sh` | Linux | 执行 `dotnet build` 进行快速自检 |
| `dryrun.ps1` | Windows | 运行 ConsoleDemo 回归测试 |
| `dryrun.sh` | Linux | 运行 ConsoleDemo 回归测试 |

---

## 🚀 快速开始

### 1. 自检（验证环境）

```powershell
# Windows
.\selfcheck.ps1

# Linux
./selfcheck.sh
```

### 2. 安装部署

```powershell
# Windows（需要管理员权限）
.\install.ps1

# Linux
./install.sh
```

### 3. 回归测试

```powershell
# Windows
.\dryrun.ps1

# Linux
./dryrun.sh
```

验证流程：启动 → 三档 → 停机 → 恢复 → 断连 → 降级 → 恢复

### 4. 卸载服务

```powershell
# Windows（需要管理员权限）
.\uninstall.ps1

# Linux
./uninstall.sh
```

---

## 📖 详细使用指南

### 部署前准备

1. **阅读运维文档**
   - [运维手册 - 环境要求](OPERATIONS_MANUAL.md#11-环境要求)
   - [部署运维手册](../docs/DEPLOYMENT.md)

2. **准备部署环境**
   - 安装 .NET 8.0 Runtime
   - 配置防火墙规则
   - 准备配置文件

3. **执行自检**
   ```powershell
   .\selfcheck.ps1
   ```

### 生产部署流程

**方法 1：使用部署脚本**

```powershell
# 1. 执行自检
.\selfcheck.ps1

# 2. 安装服务
.\install.ps1

# 3. 验证部署
# 访问 http://localhost:5005/swagger

# 4. 运行回归测试
.\dryrun.ps1
```

**方法 2：按照运维手册部署**

详见 [运维手册 - 部署章节](OPERATIONS_MANUAL.md#1-部署手册)

**方法 3：Docker 容器部署**

详见 [运维手册 - Docker 部署](OPERATIONS_MANUAL.md#122-docker-容器部署推荐)

---

## 🔧 日常维护

### 服务管理

```powershell
# 查看服务状态
Get-Service ZakYipSingulation

# 启动服务
Start-Service ZakYipSingulation

# 停止服务
Stop-Service ZakYipSingulation

# 重启服务
Restart-Service ZakYipSingulation
```

### 日志查看

```powershell
# 查看最新日志
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50

# 实时查看日志
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Wait
```

### 备份数据

详见 [备份恢复流程](BACKUP_RECOVERY.md)

```powershell
# 手动备份
.\backup-manual.ps1

# 配置自动备份
# 参见备份恢复流程文档
```

---

## 🚨 应急处理

遇到问题时，请按以下顺序查阅文档：

1. **快速诊断** → [故障排查手册](../docs/TROUBLESHOOTING.md)
2. **应急响应** → [应急响应预案](EMERGENCY_RESPONSE.md)
3. **数据恢复** → [备份恢复流程](BACKUP_RECOVERY.md)

### 常见问题速查

| 问题 | 快速解决 | 详细文档 |
|------|----------|----------|
| 服务无法启动 | 检查端口、查看日志 | [故障排查 - 服务无法启动](../docs/TROUBLESHOOTING.md#21-服务无法启动) |
| 客户端无法连接 | 检查防火墙、UDP 端口 | [故障排查 - 客户端连接问题](../docs/TROUBLESHOOTING.md#22-客户端连接问题) |
| 数据库损坏 | 从备份恢复 | [备份恢复 - 数据库损坏恢复](BACKUP_RECOVERY.md#62-数据库损坏恢复) |
| 性能问题 | 查看资源使用、重启服务 | [故障排查 - 性能问题](../docs/TROUBLESHOOTING.md#24-性能问题) |

---

## 📞 技术支持

- **技术支持邮箱**：support@example.com
- **GitHub Issues**：https://github.com/Hisoka6602/ZakYip.Singulation/issues
- **应急热线**：+86 xxx-xxxx-xxxx（仅限紧急生产故障）

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 运维团队
