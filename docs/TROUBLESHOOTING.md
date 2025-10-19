# ZakYip.Singulation 故障排查手册

## 1. 快速诊断清单

遇到问题时，按以下顺序快速检查：

### 1.1 服务健康检查

```bash
# 检查服务状态
sc query ZakYipSingulation  # Windows
systemctl status zakyip     # Linux

# 检查端口监听
netstat -ano | findstr 5005      # Windows
netstat -tuln | grep 5005        # Linux

# 测试 API 可达性
curl http://localhost:5005/swagger
```

### 1.2 日志检查

```powershell
# 查看最新日志 (Windows)
Get-Content "C:\ZakYip.Singulation\logs\app-$(Get-Date -Format 'yyyy-MM-dd').log" -Tail 50

# 查看 Docker 日志
docker logs singulation-host --tail 50 --follow

# 查看 Windows 事件日志
Get-EventLog -LogName Application -Source ZakYipSingulation -Newest 10
```

### 1.3 网络连接检查

```bash
# Ping 服务器
ping 192.168.1.100

# 测试端口连通性
telnet 192.168.1.100 5005        # Windows
nc -zv 192.168.1.100 5005        # Linux

# 检查防火墙规则
Get-NetFirewallRule | Where-Object DisplayName -like "*ZakYip*"
```

## 2. 常见问题与解决方案

### 2.1 服务无法启动

#### 问题 1：端口被占用

**症状**：
```
System.IO.IOException: Failed to bind to address http://0.0.0.0:5005
```

**诊断**：
```bash
# 查找占用端口的进程
netstat -ano | findstr :5005
```

**解决方案**：
```powershell
# 方法 1：终止占用进程
taskkill /PID <进程ID> /F

# 方法 2：修改配置文件使用其他端口
# 编辑 appsettings.json
{
  "KestrelUrl": "http://0.0.0.0:5006"
}
```

#### 问题 2：.NET Runtime 缺失

**症状**：
```
The application requires .NET 8.0 runtime
```

**解决方案**：
```bash
# 下载并安装 .NET 8.0 Runtime
# https://dotnet.microsoft.com/download/dotnet/8.0

# 验证安装
dotnet --list-runtimes
```

#### 问题 3：文件权限不足

**症状**：
```
UnauthorizedAccessException: Access to the path 'data\singulation.db' is denied
```

**解决方案**：
```powershell
# 授予服务账户权限
icacls "C:\ZakYip.Singulation" /grant "NT AUTHORITY\NETWORK SERVICE:(OI)(CI)F" /T

# 或以管理员身份运行服务
```

#### 问题 4：配置文件格式错误

**症状**：
```
Unhandled exception. System.Text.Json.JsonException: Invalid JSON
```

**解决方案**：
```bash
# 验证 JSON 格式
jq . appsettings.json  # Linux
python -m json.tool appsettings.json  # Windows/Linux

# 修复格式错误，确保：
# - 括号匹配
# - 逗号正确
# - 字符串使用双引号
# - 没有尾随逗号
```

### 2.2 客户端连接问题

#### 问题 1：UDP 服务发现失败

**症状**：MAUI 客户端无法发现服务

**诊断步骤**：
1. 检查服务器 UDP 广播是否启用
2. 检查客户端和服务器是否在同一网络
3. 检查防火墙是否阻止 UDP 18888 端口

**解决方案**：
```json
// 服务器端 appsettings.json
{
  "UdpDiscovery": {
    "Enabled": true,         // 确保启用
    "BroadcastPort": 18888   // 确保端口正确
  }
}
```

```powershell
# 检查 UDP 端口是否开放
Test-NetConnection -ComputerName 192.168.1.100 -Port 18888

# 添加防火墙规则
New-NetFirewallRule -DisplayName "ZakYip UDP Discovery" -Direction Inbound -LocalPort 18888 -Protocol UDP -Action Allow
```

**临时解决方案**：
手动配置 API 地址，不使用 UDP 发现：
```
Settings -> API 地址: http://192.168.1.100:5005
```

#### 问题 2：SignalR 连接失败

**症状**：
```
Microsoft.AspNetCore.SignalR.Client.HubException: Failed to start SignalR connection
```

**诊断**：
```bash
# 测试 SignalR 端点
curl http://192.168.1.100:5005/hubs/events

# 检查服务器日志
grep "SignalR" /app/logs/app-*.log
```

**解决方案**：
```csharp
// 客户端增加详细日志
_hubConnection = new HubConnectionBuilder()
    .WithUrl($"{_baseUrl}/hubs/events")
    .ConfigureLogging(logging => 
    {
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .WithAutomaticReconnect(new[] { 
        TimeSpan.Zero, 
        TimeSpan.FromSeconds(2), 
        TimeSpan.FromSeconds(10)
    })
    .Build();
```

**常见错误码**：
- `401 Unauthorized`：认证失败（如启用了认证）
- `404 Not Found`：Hub 路径错误
- `500 Internal Server Error`：服务器内部错误，查看日志

#### 问题 3：API 请求超时

**症状**：
```
System.Threading.Tasks.TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout
```

**解决方案**：
```csharp
// 客户端增加超时时间
var client = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl),
    Timeout = TimeSpan.FromSeconds(60)  // 增加到 60 秒
};
```

或在客户端设置中调整超时：
```
Settings -> 超时设置: 60 秒
```

### 2.3 运动控制问题

#### 问题 1：雷赛控制卡初始化失败

**症状**：
```
LeadshineLtdmcBusAdapter: Failed to initialize controller, error code: -1
```

**诊断步骤**：
1. 检查控制卡是否上电
2. 检查网络连接（以太网卡）
3. 验证 IP 地址配置
4. 测试 Ping 通控制卡

**解决方案**：
```bash
# Ping 控制卡
ping 192.168.1.100

# 检查配置
GET http://localhost:5005/api/axes/controller/options

# 更新控制器 IP
PUT http://localhost:5005/api/axes/controller/options
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

#### 问题 2：轴使能失败

**症状**：轴无法使能，状态保持在"离线"或"故障"

**诊断**：
```bash
# 查看轴状态
GET http://localhost:5005/api/axes/axes

# 查看错误码
GET http://localhost:5005/api/axes/axes/axis1
# 响应：
{
  "lastErrorCode": 123,
  "lastErrorMessage": "Servo alarm"
}
```

**解决方案**：
1. 检查伺服驱动器报警灯
2. 检查急停按钮是否按下
3. 检查控制卡与驱动器连线
4. 清除错误并复位：
   ```bash
   POST http://localhost:5005/api/axes/controller/reset
   ```

#### 问题 3：速度设置不生效

**症状**：设置速度后，轴速度未改变

**可能原因**：
1. 安全管线未启动（状态不是 Running）
2. 上游设备持续发送速度命令覆盖
3. 速度超出限制范围

**解决方案**：
```bash
# 1. 检查安全管线状态
GET http://localhost:5005/api/safety/status

# 2. 如果是 Stopped，启动安全管线
POST http://localhost:5005/api/safety/commands
{
  "command": 1,  // Start
  "reason": "Manual start for testing"
}

# 3. 验证速度范围
# 确保速度在 0-2000 mm/s 范围内
POST http://localhost:5005/api/axes/axes/speed
{
  "axisIds": ["axis1"],
  "speedMmps": 100.0
}
```

### 2.4 性能问题

#### 问题 1：API 响应慢

**症状**：API 请求耗时超过 1 秒

**诊断**：
```csharp
// 启用详细计时日志
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();
    _logger.LogInformation("Request {Method} {Path} took {ElapsedMs}ms", 
        context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds);
});
```

**解决方案**：
1. 启用响应压缩（已默认启用）
2. 添加缓存层
3. 优化数据库查询
4. 增加服务器资源（CPU/内存）

#### 问题 2：内存占用持续增长

**症状**：服务运行时间越长，内存占用越高

**诊断**：
```bash
# 查看内存使用
# Windows
Get-Process -Name ZakYip.Singulation.Host | Select-Object WorkingSet64

# Linux
docker stats singulation-host
```

**解决方案**：
1. 检查是否有内存泄漏（取消订阅事件）
2. 调整 GC 设置：
   ```json
   {
     "System.GC.Server": true,
     "System.GC.Concurrent": true
   }
   ```
3. 启用内存池和对象复用
4. 定期重启服务（临时方案）

#### 问题 3：SignalR 消息延迟

**症状**：实时事件延迟 5-10 秒才到达客户端

**可能原因**：
1. 网络延迟或丢包
2. 客户端处理慢
3. 事件聚合延迟过长

**解决方案**：
```csharp
// 调整事件聚合延迟
public class AxisEventAggregator
{
    private const int BatchDelayMs = 100;  // 从 200ms 降到 100ms
    
    public async Task ProcessEventsAsync(CancellationToken ct)
    {
        while (await _eventChannel.Reader.WaitToReadAsync(ct))
        {
            await Task.Delay(BatchDelayMs, ct);  // 更快的批处理
            // ...
        }
    }
}
```

### 2.5 数据问题

#### 问题 1：LiteDB 数据库损坏

**症状**：
```
LiteDB.LiteException: Datafile is corrupted
```

**解决方案**：
```powershell
# 1. 停止服务
net stop ZakYipSingulation

# 2. 备份损坏的数据库
Copy-Item "C:\ZakYip.Singulation\data\singulation.db" "C:\Backups\singulation.db.corrupted"

# 3. 尝试修复
# 使用 LiteDB.Shell 工具
LiteDB.Shell.exe "data\singulation.db"
> db.rebuild()

# 4. 如果修复失败，从备份恢复
Copy-Item "C:\Backups\singulation.db.backup" "C:\ZakYip.Singulation\data\singulation.db"

# 5. 启动服务
net start ZakYipSingulation
```

#### 问题 2：配置丢失

**症状**：重启后配置恢复为默认值

**可能原因**：
1. 配置未保存到数据库
2. 数据库文件权限问题
3. 文件被删除或覆盖

**解决方案**：
```bash
# 检查数据库文件
ls -lh data/singulation.db

# 验证配置是否存在
GET http://localhost:5005/api/axes/controller/options

# 重新配置
PUT http://localhost:5005/api/axes/controller/options
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

## 3. 日志分析

### 3.1 关键日志级别

- **Trace**：最详细，用于深度调试
- **Debug**：调试信息
- **Information**：正常运行信息
- **Warning**：警告，不影响功能
- **Error**：错误，功能受影响
- **Critical**：严重错误，服务中断

### 3.2 常见日志模式

#### 正常启动日志

```
[Info] Application started. Press Ctrl+C to shut down.
[Info] Hosting environment: Production
[Info] Content root path: C:\ZakYip.Singulation\
[Info] Now listening on: http://0.0.0.0:5005
[Info] UDP Discovery Service started on port 18888
[Info] Safety Pipeline initialized
[Info] Axis Controller ready with 8 axes
```

#### 错误日志模式

**数据库连接错误**：
```
[Error] LiteDB connection failed: Unable to open datafile
[Error] at LiteDB.Engine.LiteEngine..ctor(EngineSettings settings)
```

**网络错误**：
```
[Error] TCP listener failed to start: Address already in use
[Error] at System.Net.Sockets.TcpListener.Start()
```

**业务逻辑错误**：
```
[Error] Failed to enable axis 'axis1': Servo alarm
[Error] at ZakYip.Singulation.Drivers.Leadshine.LeadshineLtdmcAxisDrive.EnableAsync()
```

### 3.3 日志查询示例

```powershell
# 查找错误日志
Select-String -Path "C:\ZakYip.Singulation\logs\*.log" -Pattern "\[Error\]" | Select-Object -Last 20

# 查找特定时间段的日志
Get-Content "C:\ZakYip.Singulation\logs\app-2025-10-19.log" | 
    Select-String "2025-10-19 14:" | 
    Select-String "Error|Exception"

# 统计错误数量
(Select-String -Path "C:\ZakYip.Singulation\logs\*.log" -Pattern "\[Error\]").Count
```

## 4. 性能分析工具

### 4.1 Windows Performance Monitor

**监控指标**：
1. `.NET CLR Memory` - GC 和内存使用
2. `Process` - CPU 和内存
3. `ASP.NET Core` - 请求统计

**设置步骤**：
```
1. 运行 perfmon
2. 添加计数器 -> .NET CLR Memory -> 选择进程
3. 监控 Gen 0/1/2 Collections, Allocated Bytes/sec
```

### 4.2 dotnet-trace

**采集性能跟踪**：
```bash
# 安装工具
dotnet tool install --global dotnet-trace

# 找到进程 ID
dotnet-trace ps

# 采集 60 秒的跟踪
dotnet-trace collect --process-id <PID> --duration 00:00:60

# 分析 .nettrace 文件（使用 PerfView 或 Visual Studio）
```

### 4.3 dotnet-counters

**实时监控指标**：
```bash
# 安装工具
dotnet tool install --global dotnet-counters

# 实时监控
dotnet-counters monitor --process-id <PID> --refresh-interval 1

# 监控特定计数器
dotnet-counters monitor --process-id <PID> \
    System.Runtime \
    Microsoft.AspNetCore.Hosting
```

## 5. 紧急修复流程

### 5.1 服务中断响应

**步骤 1：确认影响范围**
- 有多少客户端受影响？
- 影响的核心功能是什么？
- 是否有数据丢失风险？

**步骤 2：快速恢复**
```powershell
# 尝试重启服务
Restart-Service ZakYipSingulation

# 如果失败，检查日志
Get-EventLog -LogName Application -Source ZakYipSingulation -Newest 5

# 回滚到上一个稳定版本
Copy-Item "C:\ZakYip.Singulation.backup\*" "C:\ZakYip.Singulation\" -Recurse -Force
Start-Service ZakYipSingulation
```

**步骤 3：通知相关人员**
- 技术团队
- 运维人员
- 受影响的用户

**步骤 4：根因分析**
- 查看详细日志
- 重现问题
- 找到根本原因

**步骤 5：预防措施**
- 更新监控规则
- 改进代码
- 更新文档

### 5.2 数据恢复流程

**步骤 1：评估数据损坏程度**
```bash
# 检查数据库完整性
LiteDB.Shell.exe "data\singulation.db"
> db.checksum()
```

**步骤 2：从备份恢复**
```powershell
# 停止服务
net stop ZakYipSingulation

# 找到最近的有效备份
$latestBackup = Get-ChildItem "C:\Backups\ZakYip.Singulation\" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1

# 恢复备份
Copy-Item "$($latestBackup.FullName)\data\*" "C:\ZakYip.Singulation\data\" -Force

# 启动服务
net start ZakYipSingulation
```

**步骤 3：验证数据完整性**
```bash
# 验证关键配置
GET http://localhost:5005/api/axes/controller/options

# 验证轴数据
GET http://localhost:5005/api/axes/axes
```

## 6. 监控最佳实践

### 6.1 关键指标

**必须监控**：
- ✅ 服务运行状态（Up/Down）
- ✅ API 响应时间 (< 500ms)
- ✅ CPU 使用率 (< 60%)
- ✅ 内存使用率 (< 80%)
- ✅ 错误率 (< 1%)

**建议监控**：
- ⭐ SignalR 连接数
- ⭐ 轴状态分布
- ⭐ 安全事件频率
- ⭐ 数据库查询时间
- ⭐ GC 暂停时间

### 6.2 告警规则

**Critical（立即处理）**：
- 服务停止
- API 不可用
- 内存使用率 > 95%
- 磁盘空间 < 5%

**Warning（24小时内处理）**：
- CPU 使用率 > 80% 持续 5 分钟
- 内存使用率 > 85%
- 错误率 > 5%
- API 响应时间 > 1 秒

**Info（记录不告警）**：
- 正常启动/停止
- 配置变更
- 客户端连接/断开

## 7. 支持与反馈

### 7.1 获取帮助

**官方渠道**：
- 📧 技术支持邮箱：support@example.com
- 💬 GitHub Issues：https://github.com/Hisoka6602/ZakYip.Singulation/issues
- 📚 文档中心：查看项目 docs/ 目录

### 7.2 提交 Bug 报告

**必需信息**：
1. 问题描述（越详细越好）
2. 复现步骤
3. 预期行为 vs 实际行为
4. 环境信息（操作系统、.NET 版本）
5. 相关日志（最近 100 行）
6. 配置文件（脱敏后）

**Bug 报告模板**：
```markdown
## 问题描述
[简要描述问题]

## 复现步骤
1. 步骤1
2. 步骤2
3. ...

## 预期行为
[描述预期的正常行为]

## 实际行为
[描述实际发生的情况]

## 环境信息
- OS: Windows 11
- .NET: 8.0.1
- 版本: v1.0.0

## 日志
```
[粘贴相关日志]
```

## 配置
```json
[粘贴相关配置，敏感信息用 *** 替代]
```
```

### 7.3 紧急联系

**生产环境紧急故障**：
- 📞 24/7 热线：+86 xxx-xxxx-xxxx
- 📱 企业微信群：扫码加入
- 🚨 值班工程师：查看排班表

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 运维团队
