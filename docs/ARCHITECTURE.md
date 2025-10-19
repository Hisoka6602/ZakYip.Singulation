# ZakYip.Singulation 架构设计文档

## 1. 系统概述

ZakYip.Singulation 是一个工业自动化运动控制系统，用于管理多轴运动控制器，支持实时监控、安全管理和远程控制。系统采用分层架构设计，提供 REST API、SignalR 实时通信和跨平台移动客户端。

### 1.1 核心特性

- **多轴运动控制**：支持雷赛 LTDMC 运动控制器，可扩展其他品牌
- **实时通信**：基于 SignalR 的实时事件推送和状态监控
- **安全管理**：完整的安全管线、隔离器和帧防护机制
- **远程管理**：RESTful API 和跨平台 MAUI 客户端
- **自动发现**：UDP 广播服务发现，零配置连接
- **高性能**：异步 IO、内存池、事件聚合优化

### 1.2 技术栈

- **.NET 8.0**：运行时框架
- **ASP.NET Core**：Web 框架和 REST API
- **SignalR**：实时双向通信
- **.NET MAUI 8.0**：跨平台移动/桌面客户端
- **LiteDB**：嵌入式 NoSQL 数据库
- **Prism + DryIoc**：MVVM 框架和依赖注入
- **NLog**：日志框架
- **Swagger/OpenAPI**：API 文档

## 2. 系统架构

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                        客户端层 (Client Layer)                  │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │  MAUI 移动客户端  │  │  Web 浏览器       │  │  第三方应用  │  │
│  │  (Android/iOS)   │  │  (Swagger UI)    │  │  (REST API)  │  │
│  └──────────────────┘  └──────────────────┘  └──────────────┘  │
└───────────────┬─────────────────┬──────────────────┬───────────┘
                │                 │                  │
                │ UDP 发现        │ HTTP/WebSocket   │ REST API
                │ SignalR         │                  │
                ▼                 ▼                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                       服务层 (Service Layer)                     │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │  REST API       │  │  SignalR Hub     │  │  UDP 广播服务 │  │
│  │  Controllers    │  │  (EventsHub)     │  │  (Discovery)  │  │
│  └─────────────────┘  └──────────────────┘  └───────────────┘  │
└───────────────┬─────────────────┬──────────────────┬───────────┘
                │                 │                  │
                ▼                 ▼                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                      业务层 (Business Layer)                     │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │  轴控制器       │  │  安全管线        │  │  上游协议     │  │
│  │  AxisController │  │  SafetyPipeline  │  │  Upstream     │  │
│  └─────────────────┘  └──────────────────┘  └───────────────┘  │
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │  事件聚合器     │  │  解码器服务      │  │  速度规划器   │  │
│  │  EventAggregator│  │  DecoderService  │  │  SpeedPlanner │  │
│  └─────────────────┘  └──────────────────┘  └───────────────┘  │
└───────────────┬─────────────────┬──────────────────┬───────────┘
                │                 │                  │
                ▼                 ▼                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                   基础设施层 (Infrastructure Layer)              │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │  LiteDB 存储    │  │  TCP 传输管线    │  │  日志服务     │  │
│  │  (Persistence)  │  │  (Transport)     │  │  (NLog)       │  │
│  └─────────────────┘  └──────────────────┘  └───────────────┘  │
└───────────────┬─────────────────┬──────────────────┬───────────┘
                │                 │                  │
                ▼                 ▼                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                     驱动层 (Driver Layer)                        │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │  雷赛 LTDMC     │  │  驱动注册表      │  │  轴驱动抽象   │  │
│  │  BusAdapter     │  │  DriveRegistry   │  │  IAxisDrive   │  │
│  └─────────────────┘  └──────────────────┘  └───────────────┘  │
└───────────────┬─────────────────┬──────────────────┬───────────┘
                │                 │                  │
                ▼                 ▼                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                     硬件层 (Hardware Layer)                      │
├─────────────────────────────────────────────────────────────────┤
│         雷赛运动控制卡 (LTDMC Card) + 伺服电机系统              │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 项目结构

```
ZakYip.Singulation/
├── ZakYip.Singulation.Core/              # 核心领域层
│   ├── Abstractions/                     # 核心抽象接口
│   ├── Axis/                            # 轴控制器实现
│   ├── Configs/                         # 配置模型
│   ├── Contracts/                       # 契约和 DTO
│   └── Events/                          # 领域事件
├── ZakYip.Singulation.Drivers/           # 驱动层
│   ├── Abstractions/                    # 驱动抽象接口
│   ├── Leadshine/                       # 雷赛驱动实现
│   └── Registry/                        # 驱动注册表
├── ZakYip.Singulation.Infrastructure/    # 基础设施层
│   ├── Persistence/                     # LiteDB 持久化
│   ├── Safety/                          # 安全管理实现
│   └── Transport/                       # 传输管线
├── ZakYip.Singulation.Protocol/          # 协议解析层
│   ├── Abstractions/                    # 协议抽象
│   └── Vendors/                         # 厂商协议实现
├── ZakYip.Singulation.Transport/         # 传输层
│   ├── Tcp/                            # TCP 通信
│   └── Events/                         # 传输事件
├── ZakYip.Singulation.Host/              # 主机服务
│   ├── Controllers/                     # REST API 控制器
│   ├── SignalR/                        # SignalR Hub
│   ├── Services/                       # 后台服务
│   ├── Workers/                        # 后台工作器
│   └── Program.cs                      # 启动入口
├── ZakYip.Singulation.MauiApp/           # MAUI 客户端
│   ├── Services/                       # API 服务层
│   ├── ViewModels/                     # MVVM 视图模型
│   ├── Views/                          # XAML 视图
│   └── MauiProgram.cs                  # 应用入口
├── ZakYip.Singulation.Tests/             # 单元测试
└── ZakYip.Singulation.ConsoleDemo/       # 控制台演示
```

## 3. 核心组件设计

### 3.1 轴控制器 (AxisController)

**职责**：管理所有轴的状态、命令和事件

**关键接口**：
```csharp
public interface IAxisController
{
    Task<IAxisDrive?> GetAxisAsync(string axisId);
    Task<List<AxisResponseDto>> GetAllAxesAsync();
    Task EnableAxesAsync(IEnumerable<string> axisIds);
    Task DisableAxesAsync(IEnumerable<string> axisIds);
    Task SetAxesSpeedAsync(IEnumerable<string> axisIds, double speedMmps);
}
```

**设计特点**：
- 统一管理所有轴实例
- 异步操作，避免阻塞
- 事件聚合器发布状态变化
- 支持批量操作提升性能

### 3.2 安全管线 (SafetyPipeline)

**职责**：执行安全命令，管理安全状态机

**状态机**：
```
[Stopped] --Start--> [Starting] --Ready--> [Running]
                                              |
[Stopped] <--Stop-- [Stopping] <--Stop-------+
                        |
                     [Reset]
```

**关键组件**：
- **SafetyIsolator**：安全隔离器，硬件安全信号
- **FrameGuard**：帧防护器，防止误操作
- **CommissioningSequence**：调试序列，自动化启动流程

### 3.3 SignalR 实时通信

**EventsHub 接口**：
```csharp
public interface IEventsHub
{
    Task ReceiveMessage(string message);
    Task ReceiveEvent(string eventName, object data);
    Task AxisSpeedChanged(int axisId, double speed);
    Task SafetyEvent(string eventType, string message, DateTime timestamp);
}
```

**重连策略**：
- 指数退避：0s → 2s → 10s → 30s → 60s
- 自动重连，无需手动干预
- 连接状态事件通知客户端

### 3.4 UDP 服务发现

**工作原理**：
1. Host 每 3 秒通过 UDP 广播服务信息（端口 18888）
2. MAUI 客户端监听 UDP 广播
3. 解析服务信息（名称、版本、HTTP 端口、SignalR 路径）
4. 自动配置 API 地址

**广播数据格式**：
```json
{
  "serviceName": "Singulation Service",
  "version": "1.0.0",
  "httpPort": 5005,
  "httpsPort": 5006,
  "signalRPath": "/hubs/events"
}
```

### 3.5 MAUI 客户端架构

**分层设计**：
```
View Layer (XAML)
    ↓
ViewModel Layer (Prism MVVM)
    ↓
Service Layer (API Services)
    ↓
Network Layer (HTTP/UDP/SignalR)
```

**关键服务**：
- **ApiClient**：HTTP API 基础客户端
- **AxisApiService**：轴管理 API
- **ControllerApiService**：控制器管理 API
- **SafetyApiService**：安全命令 API
- **DecoderApiService**：解码器 API
- **UpstreamApiService**：上游通信 API
- **SystemApiService**：系统会话 API
- **UdpDiscoveryClient**：UDP 服务发现客户端
- **SignalRClientFactory**：SignalR 连接工厂

## 4. 数据流设计

### 4.1 轴速度控制流程

```
上游设备(PLC/传感器)
    ↓ [TCP]
UpstreamTcpServer (接收原始帧)
    ↓
IUpstreamCodec (解码协议帧)
    ↓
UpstreamFrameHub (分发速度命令)
    ↓
SpeedFrameWorker (批处理优化)
    ↓
IAxisController (执行轴速度设置)
    ↓
IAxisDrive (硬件驱动调用)
    ↓
雷赛运动控制卡 (LTDMC)
```

### 4.2 安全事件流程

```
安全传感器/按钮
    ↓
ISafetyIoModule (读取安全信号)
    ↓
SafetyPipeline (安全状态机)
    ↓
AxisController (批量停止轴)
    ↓
EventAggregator (发布事件)
    ↓
SignalR Hub (推送客户端)
    ↓
MAUI 客户端 (显示告警)
```

### 4.3 实时监控流程

```
雷赛运动控制卡 (轴状态)
    ↓
IAxisDrive (轮询状态)
    ↓
AxisController (状态变化检测)
    ↓
IAxisEventAggregator (事件聚合)
    ↓
SignalR Hub (实时推送)
    ↓
MAUI 客户端 (UI 更新)
```

## 5. 性能优化设计

### 5.1 事件聚合与批处理

**问题**：高频事件导致性能瓶颈

**解决方案**：
```csharp
// 事件聚合器，200ms 批量处理
public class AxisEventAggregator : IAxisEventAggregator
{
    private readonly Channel<AxisEvent> _eventChannel;
    private readonly IRealtimeNotifier _notifier;
    
    public async Task ProcessEventsAsync(CancellationToken ct)
    {
        var buffer = new List<AxisEvent>(100);
        
        while (await _eventChannel.Reader.WaitToReadAsync(ct))
        {
            // 收集 200ms 内的事件
            await Task.Delay(200, ct);
            
            while (_eventChannel.Reader.TryRead(out var evt))
            {
                buffer.Add(evt);
            }
            
            // 批量推送
            if (buffer.Count > 0)
            {
                await _notifier.NotifyBatchAsync(buffer);
                buffer.Clear();
            }
        }
    }
}
```

### 5.2 内存池与对象复用

**问题**：频繁分配小对象导致 GC 压力

**解决方案**：
```csharp
// 使用 ArrayPool 复用缓冲区
private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

public async Task ProcessDataAsync(Stream stream)
{
    var buffer = _bufferPool.Rent(4096);
    try
    {
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        // 处理数据...
    }
    finally
    {
        _bufferPool.Return(buffer);
    }
}

// 使用对象池复用 DTO
private readonly ObjectPool<AxisResponseDto> _dtoPool;

public AxisResponseDto CreateDto()
{
    var dto = _dtoPool.Get();
    // 重置 DTO 状态
    return dto;
}
```

### 5.3 异步 IO 优化

**关键技术**：
- `ConfigureAwait(false)`：避免上下文切换
- `ValueTask`：减少异步开销
- `Channel<T>`：高性能生产者-消费者队列
- `IAsyncEnumerable`：流式处理大数据

**示例**：
```csharp
public async ValueTask<int> GetAxisCountAsync()
{
    // ValueTask 避免分配 Task 对象
    return await CountAxesInternalAsync().ConfigureAwait(false);
}

public async IAsyncEnumerable<AxisEvent> StreamEventsAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // 流式返回，避免一次性加载所有数据
    await foreach (var evt in _eventSource.GetEventsAsync(ct))
    {
        yield return evt;
    }
}
```

### 5.4 并发控制优化

**技术选择**：
- `SemaphoreSlim`：轻量级异步锁
- `ConcurrentDictionary`：无锁并发字典
- `Interlocked`：原子操作

**示例**：
```csharp
private readonly SemaphoreSlim _lock = new(1, 1);

public async Task UpdateAxisAsync(string axisId, AxisDto dto)
{
    await _lock.WaitAsync();
    try
    {
        // 临界区代码
    }
    finally
    {
        _lock.Release();
    }
}
```

## 6. 安全性设计

### 6.1 安全管线机制

**三层防护**：
1. **硬件安全**：通过 ISafetyIoModule 读取硬件安全信号
2. **软件状态机**：SafetyPipeline 确保状态转换合法
3. **帧防护**：FrameGuard 防止误操作和高频切换

### 6.2 认证授权（待实现）

**推荐方案**：
- JWT Token 认证
- 基于角色的访问控制 (RBAC)
- API 密钥管理

### 6.3 审计日志

**记录内容**：
- 所有安全命令操作
- 轴使能/禁用操作
- 速度变更记录
- 错误和异常

**存储**：LiteDB + 定期归档

## 7. 可扩展性设计

### 7.1 多厂商驱动支持

**驱动注册表模式**：
```csharp
public interface IDriveRegistry
{
    void Register(string vendor, AxisDriveFactory factory);
    IAxisDrive Create(string vendor, string axisId, int port, AxisOptions opts);
}

// 注册新驱动
registry.Register("inovance", (axisId, port, opts) => 
    new InovanceAxisDrive(opts));
```

### 7.2 协议解码器扩展

**协议抽象**：
```csharp
public interface IUpstreamCodec
{
    bool TryDecode(ReadOnlySpan<byte> data, out SpeedFrame frame);
}

// 添加新协议
services.AddSingleton<IUpstreamCodec>(sp => 
    new CustomProtocolCodec());
```

### 7.3 插件化架构（未来）

**设计思路**：
- 基于 MEF 或自定义插件加载器
- 插件接口标准化
- 沙箱隔离和权限控制

## 8. 部署架构

### 8.1 单机部署

```
Windows 服务器
├── ZakYip.Singulation.Host.exe (Windows Service)
├── LiteDB 数据库文件 (data/)
├── 日志文件 (logs/)
└── 配置文件 (appsettings.json)
```

### 8.2 容器化部署（推荐）

```yaml
# docker-compose.yml
version: '3.8'
services:
  singulation-host:
    image: zakyip/singulation:latest
    ports:
      - "5005:5005"
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

### 8.3 高可用部署（未来）

```
负载均衡器 (Nginx/HAProxy)
    ↓
┌──────────┬──────────┬──────────┐
│ Host 1   │ Host 2   │ Host 3   │
└──────────┴──────────┴──────────┘
    ↓         ↓         ↓
┌──────────────────────────────────┐
│    Redis (分布式缓存/会话)       │
└──────────────────────────────────┘
```

## 9. 监控与运维

### 9.1 健康检查

**端点设计**：
```
GET /health
{
  "status": "Healthy",
  "components": {
    "axis_controller": "Healthy",
    "database": "Healthy",
    "safety_pipeline": "Healthy"
  }
}
```

### 9.2 关键指标

**建议监控指标**：
- CPU 使用率 < 60%
- 内存使用率 < 80%
- API 响应时间 < 500ms
- SignalR 连接数
- 轴错误率 < 1%

### 9.3 日志级别

**生产环境配置**：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "ZakYip.Singulation": "Information"
    }
  }
}
```

## 10. 技术债务与改进方向

### 10.1 当前限制

- ❌ 缺少用户认证授权
- ❌ 单机部署，无分布式支持
- ❌ 测试覆盖率较低 (约 40%)
- ❌ 缺少性能基准测试
- ❌ 日志未集中化处理

### 10.2 改进建议

**短期 (1-2 周)**：
- 实现 JWT 认证授权
- 添加健康检查端点
- 完善单元测试覆盖率

**中期 (1-2 月)**：
- Docker 容器化
- CI/CD 流水线
- 性能压测和优化
- 集中式日志 (ELK/Loki)

**长期 (3-6 月)**：
- 分布式架构演进
- 微服务拆分
- 服务网格 (Service Mesh)
- 多租户支持

## 11. 附录

### 11.1 术语表

| 术语 | 说明 |
|-----|------|
| 轴 (Axis) | 单个伺服电机控制单元 |
| LTDMC | 雷赛运动控制卡型号 |
| 安全管线 | 执行安全命令的状态机 |
| 帧防护 | 防止高频操作的保护机制 |
| 上游 | 上游 PLC/传感器设备 |
| 解码器 | 协议帧解析器 |

### 11.2 参考资料

- [ASP.NET Core 文档](https://docs.microsoft.com/aspnet/core)
- [SignalR 文档](https://docs.microsoft.com/aspnet/core/signalr)
- [.NET MAUI 文档](https://docs.microsoft.com/dotnet/maui)
- [雷赛 LTDMC SDK 文档](https://www.leadshine.com)

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 开发团队
