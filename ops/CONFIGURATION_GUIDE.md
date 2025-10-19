# ZakYip.Singulation 配置指南

## 文档概述

本配置指南详细说明 ZakYip.Singulation 系统的所有配置参数、调优建议和最佳实践。

**文档版本**：1.0  
**最后更新**：2025-10-19  
**适用版本**：ZakYip.Singulation v1.0.0+

## 目录

1. [配置文件结构](#1-配置文件结构)
2. [核心配置详解](#2-核心配置详解)
3. [性能调优](#3-性能调优)
4. [生产环境配置](#4-生产环境配置)
5. [开发测试配置](#5-开发测试配置)
6. [常见配置场景](#6-常见配置场景)

---

## 1. 配置文件结构

### 1.1 配置文件层次

ZakYip.Singulation 使用 ASP.NET Core 配置系统，支持多层配置：

```
appsettings.json                    # 基础配置（所有环境共用）
├── appsettings.Development.json    # 开发环境配置
├── appsettings.Production.json     # 生产环境配置
└── 环境变量                         # 最高优先级，覆盖文件配置
```

**优先级顺序**（从高到低）：
1. 环境变量
2. appsettings.{Environment}.json
3. appsettings.json
4. 默认值

### 1.2 完整配置文件模板

**appsettings.json**：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "ZakYip.Singulation": "Information",
      "ZakYip.Singulation.Drivers": "Debug",
      "ZakYip.Singulation.Transport": "Debug"
    }
  },
  "AllowedHosts": "*",
  "KestrelUrl": "http://0.0.0.0:5005",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5005"
      }
    },
    "Limits": {
      "MaxRequestBodySize": 31457280,
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "KeepAliveTimeout": "00:02:00",
      "RequestHeadersTimeout": "00:00:30"
    }
  },
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Service",
    "Version": "1.0.0",
    "HttpPort": 5005,
    "HttpsPort": 5006,
    "SignalRPath": "/hubs/events",
    "Metadata": {
      "Location": "工厂车间 A",
      "Description": "主控制系统"
    }
  },
  "FrameGuard": {
    "MinIntervalMs": 500,
    "DebounceMs": 200,
    "MaxRetries": 3
  },
  "SignalR": {
    "KeepAliveInterval": "00:00:15",
    "HandshakeTimeout": "00:00:15",
    "ClientTimeoutInterval": "00:00:30",
    "MaximumReceiveMessageSize": 32768,
    "StreamBufferCapacity": 10
  },
  "EventPump": {
    "BatchSize": 50,
    "BatchDelayMs": 100,
    "ChannelCapacity": 1000
  },
  "AxisController": {
    "HeartbeatIntervalMs": 1000,
    "CommandTimeoutMs": 5000,
    "ReconnectDelayMs": 5000
  }
}
```

---

## 2. 核心配置详解

### 2.1 日志配置 (Logging)

#### 2.1.1 日志级别

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "ZakYip.Singulation": "Information"
    }
  }
}
```

**日志级别说明**：

| 级别 | 数值 | 用途 | 适用场景 |
|------|------|------|----------|
| Trace | 0 | 最详细的调试信息 | 开发调试 |
| Debug | 1 | 调试信息 | 开发和测试 |
| Information | 2 | 常规信息 | 生产环境默认 |
| Warning | 3 | 警告信息 | 生产环境 |
| Error | 4 | 错误信息 | 所有环境 |
| Critical | 5 | 严重错误 | 所有环境 |
| None | 6 | 禁用日志 | 不推荐 |

**推荐配置**：

```json
// 开发环境
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "ZakYip.Singulation": "Debug"
    }
  }
}

// 生产环境
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "ZakYip.Singulation": "Information",
      "ZakYip.Singulation.Drivers": "Warning"
    }
  }
}

// 故障排查
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ZakYip.Singulation": "Trace"
    }
  }
}
```

### 2.2 Kestrel Web 服务器配置

#### 2.2.1 基础配置

```json
{
  "KestrelUrl": "http://0.0.0.0:5005"
}
```

**参数说明**：
- `0.0.0.0` - 监听所有网络接口
- `localhost` 或 `127.0.0.1` - 仅监听本地回环
- `*` - 等同于 `0.0.0.0`

#### 2.2.2 高级配置

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5005"
      },
      "Https": {
        "Url": "https://0.0.0.0:5006",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "YourPassword"
        }
      }
    },
    "Limits": {
      "MaxRequestBodySize": 31457280,
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "KeepAliveTimeout": "00:02:00",
      "RequestHeadersTimeout": "00:00:30",
      "MaxRequestHeaderCount": 100,
      "MaxRequestHeadersTotalSize": 32768,
      "MaxRequestLineSize": 8192,
      "MaxResponseBufferSize": 65536
    }
  }
}
```

**参数说明**：

| 参数 | 默认值 | 说明 | 调优建议 |
|------|--------|------|----------|
| MaxRequestBodySize | 30MB | 请求体最大大小 | API 调用通常无需修改 |
| MaxConcurrentConnections | 无限制 | 最大并发连接数 | 根据服务器资源设置（建议 100-500） |
| KeepAliveTimeout | 2分钟 | Keep-Alive 超时 | SignalR 需要较长时间 |
| RequestHeadersTimeout | 30秒 | 请求头超时 | 正常网络环境无需修改 |

#### 2.2.3 HTTPS 配置

**生成自签名证书**（测试用）：

```powershell
# Windows
dotnet dev-certs https -ep certificate.pfx -p YourPassword
dotnet dev-certs https --trust

# Linux
dotnet dev-certs https -ep certificate.pfx -p YourPassword --format Pfx
```

**使用 Let's Encrypt 证书**（生产环境推荐）：

```bash
# 安装 Certbot
sudo apt install certbot

# 申请证书
sudo certbot certonly --standalone -d your-domain.com

# 转换为 PFX 格式
openssl pkcs12 -export \
  -out certificate.pfx \
  -inkey /etc/letsencrypt/live/your-domain.com/privkey.pem \
  -in /etc/letsencrypt/live/your-domain.com/cert.pem \
  -certfile /etc/letsencrypt/live/your-domain.com/chain.pem
```

### 2.3 UDP 服务发现配置

#### 2.3.1 基础配置

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

**参数说明**：

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| Enabled | bool | 是否启用 UDP 发现 | true |
| BroadcastPort | int | UDP 广播端口 | 18888 |
| BroadcastIntervalSeconds | int | 广播间隔（秒） | 3 |
| ServiceName | string | 服务名称 | Singulation Service |
| Version | string | 服务版本 | 1.0.0 |
| HttpPort | int | HTTP API 端口 | 5005 |
| HttpsPort | int | HTTPS API 端口 | 5006 |
| SignalRPath | string | SignalR Hub 路径 | /hubs/events |

#### 2.3.2 元数据配置

```json
{
  "UdpDiscovery": {
    "Metadata": {
      "Location": "工厂车间 A",
      "Description": "主控制系统",
      "Tags": ["production", "primary"],
      "Capacity": 8,
      "Manufacturer": "Leadshine"
    }
  }
}
```

**用途**：客户端可以根据元数据筛选和识别服务。

#### 2.3.3 网络环境调优

**高延迟网络**：
```json
{
  "UdpDiscovery": {
    "BroadcastIntervalSeconds": 5,
    "SocketTimeout": 5000
  }
}
```

**多服务环境**（避免广播冲突）：
```json
{
  "UdpDiscovery": {
    "BroadcastPort": 18889,
    "ServiceName": "Singulation Service - Backup"
  }
}
```

### 2.4 安全管线配置 (FrameGuard)

```json
{
  "FrameGuard": {
    "MinIntervalMs": 500,
    "DebounceMs": 200,
    "MaxRetries": 3
  }
}
```

**参数说明**：

| 参数 | 类型 | 说明 | 调优建议 |
|------|------|------|----------|
| MinIntervalMs | int | 两次命令最小间隔（毫秒） | 防抖动，降低可加快响应 |
| DebounceMs | int | 命令去抖时间（毫秒） | 过滤重复命令 |
| MaxRetries | int | 命令失败最大重试次数 | 提高可靠性，但增加延迟 |

**场景配置**：

```json
// 高速响应场景（降低延迟）
{
  "FrameGuard": {
    "MinIntervalMs": 200,
    "DebounceMs": 100,
    "MaxRetries": 2
  }
}

// 稳定性优先场景（提高可靠性）
{
  "FrameGuard": {
    "MinIntervalMs": 1000,
    "DebounceMs": 500,
    "MaxRetries": 5
  }
}
```

### 2.5 SignalR 配置

```json
{
  "SignalR": {
    "KeepAliveInterval": "00:00:15",
    "HandshakeTimeout": "00:00:15",
    "ClientTimeoutInterval": "00:00:30",
    "MaximumReceiveMessageSize": 32768,
    "StreamBufferCapacity": 10
  }
}
```

**参数说明**：

| 参数 | 说明 | 默认值 | 调优建议 |
|------|------|--------|----------|
| KeepAliveInterval | 保活间隔 | 15秒 | 网络不稳定时缩短 |
| HandshakeTimeout | 握手超时 | 15秒 | 正常网络无需修改 |
| ClientTimeoutInterval | 客户端超时 | 30秒 | 应为 KeepAliveInterval 的 2 倍 |
| MaximumReceiveMessageSize | 最大消息大小 | 32KB | 大消息时增加 |
| StreamBufferCapacity | 流缓冲容量 | 10 | 高并发时增加 |

**高并发配置**：
```json
{
  "SignalR": {
    "KeepAliveInterval": "00:00:10",
    "ClientTimeoutInterval": "00:00:20",
    "MaximumReceiveMessageSize": 65536,
    "StreamBufferCapacity": 50
  }
}
```

### 2.6 事件泵配置 (EventPump)

```json
{
  "EventPump": {
    "BatchSize": 50,
    "BatchDelayMs": 100,
    "ChannelCapacity": 1000
  }
}
```

**参数说明**：

| 参数 | 说明 | 影响 | 调优建议 |
|------|------|------|----------|
| BatchSize | 批处理大小 | 单次处理的事件数量 | 增大可提高吞吐量，但增加延迟 |
| BatchDelayMs | 批处理延迟（毫秒） | 事件聚合等待时间 | 减少可降低延迟，但增加处理次数 |
| ChannelCapacity | 通道容量 | 事件队列最大长度 | 根据事件频率调整 |

**场景配置**：

```json
// 低延迟场景（实时性优先）
{
  "EventPump": {
    "BatchSize": 20,
    "BatchDelayMs": 50,
    "ChannelCapacity": 500
  }
}

// 高吞吐场景（批处理优先）
{
  "EventPump": {
    "BatchSize": 100,
    "BatchDelayMs": 200,
    "ChannelCapacity": 2000
  }
}
```

### 2.7 轴控制器配置 (AxisController)

```json
{
  "AxisController": {
    "HeartbeatIntervalMs": 1000,
    "CommandTimeoutMs": 5000,
    "ReconnectDelayMs": 5000
  }
}
```

**参数说明**：

| 参数 | 说明 | 调优建议 |
|------|------|----------|
| HeartbeatIntervalMs | 心跳间隔 | 网络稳定时可增大，减少开销 |
| CommandTimeoutMs | 命令超时 | 复杂命令或慢速网络时增加 |
| ReconnectDelayMs | 重连延迟 | 频繁断连时适当增加 |

---

## 3. 性能调优

### 3.1 硬件资源优化

#### 3.1.1 CPU 优化

**多核 CPU 利用**：

```json
{
  "ThreadPool": {
    "MinWorkerThreads": 50,
    "MinCompletionPortThreads": 50
  }
}
```

**GC 服务器模式**（csproj 配置）：

```xml
<PropertyGroup>
  <ServerGarbageCollection>true</ServerGarbageCollection>
  <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
</PropertyGroup>
```

#### 3.1.2 内存优化

**对象池配置**：

```csharp
// 在 Program.cs 中配置
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});
```

**GC 设置**（环境变量）：

```bash
# 降低 GC 延迟
export DOTNET_GCHeapCount=4
export DOTNET_GCLatencyLevel=1

# 或在 runtimeconfig.json 中配置
{
  "runtimeOptions": {
    "configProperties": {
      "System.GC.HeapCount": 4,
      "System.GC.Server": true
    }
  }
}
```

### 3.2 网络性能优化

#### 3.2.1 Kestrel 优化

```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 500,
      "MaxConcurrentUpgradedConnections": 500,
      "Http2": {
        "MaxStreamsPerConnection": 100,
        "InitialConnectionWindowSize": 131072,
        "InitialStreamWindowSize": 98304
      }
    }
  }
}
```

#### 3.2.2 响应压缩

```json
{
  "ResponseCompression": {
    "EnableForHttps": true,
    "Providers": ["Brotli", "Gzip"],
    "MimeTypes": [
      "text/plain",
      "text/json",
      "application/json",
      "text/html"
    ]
  }
}
```

### 3.3 数据库性能优化

#### 3.3.1 LiteDB 优化

```csharp
// 连接字符串配置
var connectionString = new ConnectionString
{
    Filename = "data/singulation.db",
    Connection = ConnectionType.Shared,  // 共享连接
    InitialSize = 10 * 1024 * 1024,      // 初始大小 10MB
    CacheSize = 5000,                     // 缓存页数
    ReadOnly = false,
    Upgrade = true
};
```

**建议**：
- 定期执行 `db.Rebuild()` 压缩数据库
- 使用索引提高查询性能
- 大批量操作使用事务

### 3.4 SignalR 性能优化

#### 3.4.1 Scale-Out 配置（多实例）

```json
{
  "SignalR": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "ChannelPrefix": "zakyip"
    }
  }
}
```

#### 3.4.2 消息压缩

```csharp
// 在 Program.cs 中配置
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;  // 生产环境禁用
    options.MaximumParallelInvocationsPerClient = 2;
    options.StreamBufferCapacity = 10;
})
.AddMessagePackProtocol();  // 使用 MessagePack 替代 JSON
```

### 3.5 日志性能优化

#### 3.5.1 异步日志

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "File",
              "Args": {
                "path": "logs/app-.log",
                "rollingInterval": "Day",
                "buffered": true,
                "flushToDiskInterval": "00:00:05"
              }
            }
          ]
        }
      }
    ]
  }
}
```

#### 3.5.2 结构化日志

```csharp
// 使用结构化日志，减少字符串拼接
_logger.LogInformation("Axis {AxisId} speed changed to {Speed} mm/s", 
    axisId, speed);
```

---

## 4. 生产环境配置

### 4.1 appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "ZakYip.Singulation": "Information",
      "ZakYip.Singulation.Drivers": "Warning"
    }
  },
  "KestrelUrl": "http://0.0.0.0:5005",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5005"
      },
      "Https": {
        "Url": "https://0.0.0.0:5006",
        "Certificate": {
          "Path": "/app/certs/certificate.pfx",
          "Password": "${CERT_PASSWORD}"
        }
      }
    },
    "Limits": {
      "MaxRequestBodySize": 31457280,
      "MaxConcurrentConnections": 500,
      "KeepAliveTimeout": "00:02:00"
    }
  },
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Production",
    "Metadata": {
      "Environment": "Production",
      "Location": "主生产线"
    }
  },
  "FrameGuard": {
    "MinIntervalMs": 500,
    "DebounceMs": 200,
    "MaxRetries": 3
  }
}
```

### 4.2 环境变量配置

**Docker Compose**：

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - KestrelUrl=http://0.0.0.0:5005
  - CERT_PASSWORD=${CERT_PASSWORD}
  - UdpDiscovery__Enabled=true
  - UdpDiscovery__BroadcastPort=18888
  - Logging__LogLevel__Default=Information
```

**Windows 服务**：

```powershell
# 设置环境变量
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("KestrelUrl", "http://0.0.0.0:5005", "Machine")
```

### 4.3 安全配置

#### 4.3.1 敏感信息保护

```bash
# 使用 User Secrets（开发环境）
dotnet user-secrets init
dotnet user-secrets set "Certificate:Password" "YourPassword"

# 使用环境变量（生产环境）
export CERT_PASSWORD="YourPassword"
```

#### 4.3.2 CORS 配置

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.example.com",
      "https://admin.example.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": true
  }
}
```

---

## 5. 开发测试配置

### 5.1 appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ZakYip.Singulation": "Debug",
      "ZakYip.Singulation.Drivers": "Trace",
      "ZakYip.Singulation.Transport": "Trace"
    }
  },
  "KestrelUrl": "http://localhost:5005",
  "UdpDiscovery": {
    "Enabled": true,
    "ServiceName": "Singulation Dev",
    "Metadata": {
      "Environment": "Development"
    }
  },
  "DetailedErrors": true,
  "Swagger": {
    "Enabled": true
  }
}
```

### 5.2 本地开发配置

```json
{
  "ConnectionStrings": {
    "Default": "Filename=data/singulation-dev.db;Connection=shared"
  },
  "AxisController": {
    "MockMode": true,
    "SimulatedDelay": 100
  }
}
```

---

## 6. 常见配置场景

### 6.1 高可用配置

**多实例部署 + Redis**：

```json
{
  "SignalR": {
    "Redis": {
      "ConnectionString": "redis-cluster:6379",
      "ChannelPrefix": "zakyip-ha"
    }
  },
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000
    }
  }
}
```

### 6.2 离线部署配置

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

### 6.3 调试配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace"
    }
  },
  "DetailedErrors": true,
  "DeveloperExceptionPage": true,
  "FrameGuard": {
    "MinIntervalMs": 0,
    "DebounceMs": 0
  }
}
```

---

## 附录

### A. 配置验证工具

```bash
# 验证 JSON 格式
python -m json.tool appsettings.json

# 或使用 jq
jq . appsettings.json
```

### B. 配置迁移

从旧版本迁移配置时，注意以下变更：

| 旧配置 | 新配置 | 说明 |
|--------|--------|------|
| `HttpUrl` | `KestrelUrl` | 重命名 |
| `Discovery.Port` | `UdpDiscovery.BroadcastPort` | 结构调整 |

### C. 故障排查

**配置不生效**：
1. 检查环境变量 `ASPNETCORE_ENVIRONMENT`
2. 验证配置文件格式（JSON 语法）
3. 检查配置层次和优先级
4. 查看启动日志确认加载的配置文件

**性能不佳**：
1. 启用详细日志，分析瓶颈
2. 使用性能分析工具（dotnet-trace）
3. 逐步调整参数，测试效果

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**维护者**：ZakYip.Singulation 运维团队
