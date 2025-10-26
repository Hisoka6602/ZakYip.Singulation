# Singulation REST API 指南

本文档覆盖默认部署下的主机接口：所有路径均以 `http://localhost:5005/` 为基准（开发环境）。实际生产环境请替换为部署时的域名或 IP。

## 目录

1. [统一返回约定](#统一返回约定)
2. [认证与授权](#认证与授权)
3. [轴管理 API](#轴管理-api)
4. [控制器管理 API](#控制器管理-api)
5. [安全命令 API](#安全命令-api)
6. [解码器 API](#解码器-api)
7. [上游通信 API](#上游通信-api)
8. [IO 状态查询 API](#io-状态查询-api)
9. [系统管理 API](#系统管理-api)
10. [SignalR 实时通信](#signalr-实时通信)
11. [错误码参考](#错误码参考)
12. [客户端示例代码](#客户端示例代码)

## 统一返回约定

所有接口均返回 `ApiResponse<T>` 包装体：

```json
{
  "result": true,
  "msg": "操作成功",
  "data": {}
}
```

**字段说明**：
- `result`：布尔值，指示请求是否成功
- `msg`：人类可读的提示信息
- `data`：实际业务数据，可为空

**HTTP 状态码**：
- `200 OK`：请求成功
- `400 Bad Request`：请求参数错误
- `404 Not Found`：资源不存在
- `500 Internal Server Error`：服务器内部错误

## 认证与授权

> ⚠️ **注意**：当前版本未实现认证授权，所有接口均可直接访问。生产环境强烈建议添加 JWT Token 认证。

**未来实现**（规划中）：
```http
# 登录获取 Token
POST /api/auth/login
{
  "username": "admin",
  "password": "password"
}

# 使用 Token 访问接口
GET /api/axes/axes
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 控制器（Controller）资源

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/axes/controller` | 查询总线控制器的当前状态（轴数量、错误码、初始化状态）。 |
| `POST` | `/api/axes/controller/reset` | 创建一次控制器复位请求，体内提供 `type` (`hard`/`soft`)。 |
| `GET` | `/api/axes/controller/errors` | 获取当前错误码。 |
| `DELETE` | `/api/axes/controller/errors` | 清除错误状态。 |
| `GET` | `/api/axes/controller/options` | 拉取控制器驱动模板。 |
| `PUT` | `/api/axes/controller/options` | 覆盖控制器驱动模板。 |

### 示例：获取控制器状态

```http
GET https://localhost:5001/api/axes/controller
Accept: application/json
```

成功响应：

```json
{
  "result": true,
  "msg": "获取控制器状态成功",
  "data": {
    "axisCount": 4,
    "errorCode": 0,
    "initialized": true
  }
}
```

## 轴拓扑资源

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/axes/topology` | 读取当前的轴网格布局定义。 |
| `PUT` | `/api/axes/topology` | 覆盖布局，体内需包含行列与布局。 |
| `DELETE` | `/api/axes/topology` | 删除现有布局，恢复为空。 |

## 轴管理 API

### 获取所有轴状态

获取系统中所有轴的当前状态快照。

**请求**：
```http
GET /api/axes/axes HTTP/1.1
Host: localhost:5005
Accept: application/json
```

**响应**：
```json
{
  "result": true,
  "msg": "Success",
  "data": [
    {
      "axisId": "axis1",
      "status": 2,
      "targetLinearMmps": 100.0,
      "feedbackLinearMmps": 99.8,
      "enabled": true,
      "lastErrorCode": 0,
      "lastErrorMessage": null
    },
    {
      "axisId": "axis2",
      "status": 0,
      "targetLinearMmps": null,
      "feedbackLinearMmps": null,
      "enabled": false,
      "lastErrorCode": 123,
      "lastErrorMessage": "Servo alarm"
    }
  ]
}
```

**状态码说明**：
- `0` - 离线 (Offline)
- `1` - 初始化中 (Initializing)
- `2` - 就绪 (Ready)
- `3` - 运行中 (Running)
- `4` - 故障 (Fault)

### 获取单个轴状态

**请求**：
```http
GET /api/axes/axes/axis1 HTTP/1.1
Host: localhost:5005
```

**响应**：
```json
{
  "result": true,
  "msg": "Success",
  "data": {
    "axisId": "axis1",
    "status": 2,
    "targetLinearMmps": 100.0,
    "feedbackLinearMmps": 99.8,
    "enabled": true,
    "lastErrorCode": 0,
    "lastErrorMessage": null
  }
}
```

### 使能轴

批量使能指定的轴。

**请求**：
```http
POST /api/axes/axes/enable HTTP/1.1
Host: localhost:5005
Content-Type: application/json

{
  "axisIds": ["axis1", "axis2", "axis3"]
}
```

**响应**：
```json
{
  "result": true,
  "msg": "Axes enabled successfully"
}
```

### 禁用轴

批量禁用指定的轴。

**请求**：
```http
POST /api/axes/axes/disable HTTP/1.1
Host: localhost:5005
Content-Type: application/json

{
  "axisIds": ["axis1", "axis2"]
}
```

**响应**：
```json
{
  "result": true,
  "msg": "Axes disabled successfully"
}
```

### 设置轴速度

批量设置轴的目标速度。

**请求**：
```http
POST /api/axes/axes/speed HTTP/1.1
Host: localhost:5005
Content-Type: application/json

{
  "axisIds": ["axis1", "axis2"],
  "speedMmps": 150.0
}
```

**参数说明**：
- `axisIds`：轴 ID 列表
- `speedMmps`：目标速度，单位 mm/s，范围 0-2000

**响应**：
```json
{
  "result": true,
  "msg": "Speed set successfully"
}
```

## 解码服务

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/decoder/health` | 解码服务健康检查。 |
| `GET` | `/api/decoder/options` | 获取持久化的解码配置。 |
| `PUT` | `/api/decoder/options` | 覆盖解码配置。 |
| `POST` | `/api/decoder/frames` | 提交一帧数据进行解码，支持 raw/hex/base64。 |

`POST /api/decoder/frames` 请求示例：

```http
POST https://localhost:5001/api/decoder/frames
Content-Type: application/json

{
  "hex": "AA 55 01 02"
}
```

## 安全管线命令

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `POST` | `/api/safety/commands` | 向安全管线提交一条命令，体内包含 `command` (`Start`/`Stop`/`Reset`) 与可选 `reason`。 |

示例：

```http
POST https://localhost:5001/api/safety/commands
Content-Type: application/json

{
  "command": "Stop",
  "reason": "手动停止"
}
```

## 运行会话管理

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `DELETE` | `/api/system/session` | 删除当前运行会话，通知宿主进程优雅退出（外部部署器负责拉起）。 |

## Upstream 解码配置与状态

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/upstream/configuration` | 读取上游 TCP 配置。 |
| `PUT` | `/api/upstream/configuration` | 更新上游 TCP 配置。 |
| `GET` | `/api/upstream/status` | 查看当前上游连接状态。 |

> ⚠️ Upstream 配置接口仍保持与 LiteDB 同步，需要先通过 `PUT` 写入后再调用 `POST /api/system/session` 触发宿主重载。

## IO 状态查询 API

IO 状态查询接口提供读取雷赛控制器所有输入和输出 IO 端口当前状态的功能。

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/io/status` | 查询所有 IO 的当前状态（支持自定义查询范围）。 |

### 查询参数

| 参数 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `inputStart` | int | 0 | 输入 IO 起始位号 |
| `inputCount` | int | 32 | 输入 IO 数量（1-1024） |
| `outputStart` | int | 0 | 输出 IO 起始位号 |
| `outputCount` | int | 32 | 输出 IO 数量（1-1024） |

### 示例：查询所有 IO 状态（默认范围）

```http
GET http://localhost:5005/api/io/status
Accept: application/json
```

成功响应：

```json
{
  "result": true,
  "msg": "查询 IO 状态成功",
  "data": {
    "inputIos": [
      {
        "bitNumber": 0,
        "type": 0,
        "state": 1,
        "isValid": true,
        "errorMessage": null
      },
      {
        "bitNumber": 1,
        "type": 0,
        "state": 0,
        "isValid": true,
        "errorMessage": null
      }
    ],
    "outputIos": [
      {
        "bitNumber": 0,
        "type": 1,
        "state": 0,
        "isValid": true,
        "errorMessage": null
      }
    ],
    "totalCount": 64,
    "validCount": 64,
    "errorCount": 0
  }
}
```

### 示例：查询自定义范围

```http
GET http://localhost:5005/api/io/status?inputStart=0&inputCount=16&outputStart=0&outputCount=16
Accept: application/json
```

### 字段说明

**IoStatusDto**：

- `bitNumber`：IO 端口编号
- `type`：IO 类型（0=输入，1=输出）
- `state`：IO 状态（0=低电平，1=高电平）
- `isValid`：读取是否成功
- `errorMessage`：错误信息（如果读取失败）

**IoStatusResponseDto**：

- `inputIos`：输入 IO 状态列表
- `outputIos`：输出 IO 状态列表
- `totalCount`：总 IO 数量
- `validCount`：成功读取的 IO 数量
- `errorCount`：读取失败的 IO 数量

### 注意事项

- IO 端口编号从 0 开始
- 如果硬件不支持某些端口，对应的 IO 状态会标记为无效（`isValid=false`）
- 建议根据实际硬件配置调整查询范围，避免查询不存在的端口
- 读取单个 IO 失败不会中断整个查询，会继续读取其他 IO

## SignalR 实时通信

SignalR Hub 提供实时双向通信，客户端可订阅以下事件：

### Hub 地址

```
ws://localhost:5005/hubs/events
```

### 服务端推送事件

#### 1. AxisSpeedChanged - 轴速度变化

当轴速度发生变化时触发。

**事件名称**：`AxisSpeedChanged`

**参数**：
```typescript
(axisId: number, speed: number) => void
```

**示例**：
```javascript
connection.on("AxisSpeedChanged", (axisId, speed) => {
  console.log(`Axis ${axisId} speed changed to ${speed} mm/s`);
});
```

#### 2. SafetyEvent - 安全事件

当发生安全相关事件时触发（启动、停止、故障等）。

**事件名称**：`SafetyEvent`

**参数**：
```typescript
(eventType: string, message: string, timestamp: string) => void
```

**示例**：
```javascript
connection.on("SafetyEvent", (eventType, message, timestamp) => {
  console.log(`[${timestamp}] ${eventType}: ${message}`);
});
```

#### 3. ReceiveMessage - 通用消息

通用的文本消息推送。

**事件名称**：`ReceiveMessage`

**参数**：
```typescript
(message: string) => void
```

#### 4. ReceiveEvent - 通用事件

通用的事件推送，包含事件名和数据。

**事件名称**：`ReceiveEvent`

**参数**：
```typescript
(eventName: string, data: any) => void
```

### 连接管理

**建立连接**：
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5005/hubs/events")
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .build();

await connection.start();
console.log("SignalR Connected");
```

**断开连接**：
```javascript
await connection.stop();
```

**监听连接状态**：
```javascript
connection.onreconnecting((error) => {
  console.log("Reconnecting...", error);
});

connection.onreconnected((connectionId) => {
  console.log("Reconnected", connectionId);
});

connection.onclose((error) => {
  console.log("Connection closed", error);
});
```

## 错误码参考

### HTTP 错误码

| 状态码 | 说明 | 处理建议 |
|--------|------|----------|
| 400 | 请求参数错误 | 检查请求体格式和参数值 |
| 404 | 资源不存在 | 检查 URL 路径和资源 ID |
| 500 | 服务器内部错误 | 查看服务器日志，联系技术支持 |
| 503 | 服务不可用 | 检查服务状态，稍后重试 |

### 业务错误码

| 错误码 | 说明 | 解决方案 |
|--------|------|----------|
| 1001 | 轴未找到 | 检查轴 ID 是否正确 |
| 1002 | 轴状态异常 | 复位控制器后重试 |
| 1003 | 速度超出范围 | 速度值应在 0-2000 mm/s |
| 2001 | 控制器未初始化 | 等待初始化完成 |
| 2002 | 控制器通信失败 | 检查网络连接和控制器电源 |
| 3001 | 安全管线未就绪 | 发送 Start 命令启动 |
| 3002 | 安全状态不允许操作 | 检查安全管线状态 |

## 客户端示例代码

### C# / .NET

#### 使用 HttpClient

```csharp
using System.Net.Http;
using System.Net.Http.Json;

public class SingulationApiClient
{
    private readonly HttpClient _httpClient;
    
    public SingulationApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }
    
    // 获取所有轴
    public async Task<List<AxisInfo>> GetAllAxesAsync()
    {
        var response = await _httpClient.GetAsync("/api/axes/axes");
        response.EnsureSuccessStatusCode();
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<AxisInfo>>>();
        return apiResponse?.Data ?? new List<AxisInfo>();
    }
    
    // 使能轴
    public async Task EnableAxesAsync(params string[] axisIds)
    {
        var request = new { axisIds };
        var response = await _httpClient.PostAsJsonAsync("/api/axes/axes/enable", request);
        response.EnsureSuccessStatusCode();
    }
    
    // 设置速度
    public async Task SetSpeedAsync(double speed, params string[] axisIds)
    {
        var request = new { axisIds, speedMmps = speed };
        var response = await _httpClient.PostAsJsonAsync("/api/axes/axes/speed", request);
        response.EnsureSuccessStatusCode();
    }
    
    // 发送安全命令
    public async Task SendSafetyCommandAsync(string command, string reason)
    {
        var request = new { command, reason };
        var response = await _httpClient.PostAsJsonAsync("/api/safety/commands", request);
        response.EnsureSuccessStatusCode();
    }
}

// 使用示例
var client = new SingulationApiClient("http://192.168.1.100:5005");

// 获取轴状态
var axes = await client.GetAllAxesAsync();
foreach (var axis in axes)
{
    Console.WriteLine($"{axis.AxisId}: {axis.Status}");
}

// 使能轴并设置速度
await client.EnableAxesAsync("axis1", "axis2");
await client.SetSpeedAsync(100.0, "axis1", "axis2");

// 发送安全命令
await client.SendSafetyCommandAsync("Start", "Manual start");
```

#### 使用 SignalR

```csharp
using Microsoft.AspNetCore.SignalR.Client;

public class SingulationSignalRClient
{
    private HubConnection _connection;
    
    public event EventHandler<(int axisId, double speed)>? SpeedChanged;
    public event EventHandler<(string type, string message)>? SafetyEvent;
    
    public async Task ConnectAsync(string baseUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/events")
            .WithAutomaticReconnect(new[] { 
                TimeSpan.Zero, 
                TimeSpan.FromSeconds(2), 
                TimeSpan.FromSeconds(10) 
            })
            .Build();
        
        // 订阅事件
        _connection.On<int, double>("AxisSpeedChanged", (axisId, speed) => 
        {
            SpeedChanged?.Invoke(this, (axisId, speed));
        });
        
        _connection.On<string, string, DateTime>("SafetyEvent", (type, message, timestamp) => 
        {
            SafetyEvent?.Invoke(this, (type, message));
        });
        
        await _connection.StartAsync();
    }
    
    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}

// 使用示例
var signalR = new SingulationSignalRClient();
signalR.SpeedChanged += (sender, data) => 
{
    Console.WriteLine($"Axis {data.axisId} speed: {data.speed} mm/s");
};

await signalR.ConnectAsync("http://192.168.1.100:5005");
```

### JavaScript / TypeScript

#### 使用 Fetch API

```javascript
class SingulationApiClient {
  constructor(baseUrl) {
    this.baseUrl = baseUrl;
  }
  
  async getAllAxes() {
    const response = await fetch(`${this.baseUrl}/api/axes/axes`);
    const data = await response.json();
    return data.data;
  }
  
  async enableAxes(...axisIds) {
    const response = await fetch(`${this.baseUrl}/api/axes/axes/enable`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ axisIds })
    });
    return response.json();
  }
  
  async setSpeed(speed, ...axisIds) {
    const response = await fetch(`${this.baseUrl}/api/axes/axes/speed`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ axisIds, speedMmps: speed })
    });
    return response.json();
  }
  
  async sendSafetyCommand(command, reason) {
    const response = await fetch(`${this.baseUrl}/api/safety/commands`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ command, reason })
    });
    return response.json();
  }
}

// 使用示例
const client = new SingulationApiClient('http://192.168.1.100:5005');

// 获取轴状态
const axes = await client.getAllAxes();
axes.forEach(axis => {
  console.log(`${axis.axisId}: ${axis.status}`);
});

// 使能轴并设置速度
await client.enableAxes('axis1', 'axis2');
await client.setSpeed(100.0, 'axis1', 'axis2');
```

#### 使用 SignalR (需要 @microsoft/signalr)

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://192.168.1.100:5005/hubs/events')
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .configureLogging(signalR.LogLevel.Information)
  .build();

// 订阅事件
connection.on('AxisSpeedChanged', (axisId, speed) => {
  console.log(`Axis ${axisId} speed: ${speed} mm/s`);
});

connection.on('SafetyEvent', (eventType, message, timestamp) => {
  console.log(`[${timestamp}] ${eventType}: ${message}`);
});

// 连接
await connection.start();
console.log('SignalR Connected');
```

### Python

```python
import requests
import json

class SingulationApiClient:
    def __init__(self, base_url):
        self.base_url = base_url
        self.session = requests.Session()
    
    def get_all_axes(self):
        response = self.session.get(f"{self.base_url}/api/axes/axes")
        response.raise_for_status()
        return response.json()['data']
    
    def enable_axes(self, *axis_ids):
        response = self.session.post(
            f"{self.base_url}/api/axes/axes/enable",
            json={'axisIds': list(axis_ids)}
        )
        response.raise_for_status()
        return response.json()
    
    def set_speed(self, speed, *axis_ids):
        response = self.session.post(
            f"{self.base_url}/api/axes/axes/speed",
            json={'axisIds': list(axis_ids), 'speedMmps': speed}
        )
        response.raise_for_status()
        return response.json()

# 使用示例
client = SingulationApiClient('http://192.168.1.100:5005')

# 获取轴状态
axes = client.get_all_axes()
for axis in axes:
    print(f"{axis['axisId']}: {axis['status']}")

# 使能轴并设置速度
client.enable_axes('axis1', 'axis2')
client.set_speed(100.0, 'axis1', 'axis2')
```

## Swagger/OpenAPI

应用默认启用 Swagger 交互式文档：

- **文档地址**：http://localhost:5005/swagger
- **OpenAPI 定义**：http://localhost:5005/swagger/v1/swagger.json

Swagger UI 提供：
- 完整的 API 接口列表
- 请求/响应示例
- 在线测试功能
- 数据模型定义

## 调试建议

### 1. 使用 Postman

导入 OpenAPI 定义到 Postman：
```
File -> Import -> Link
输入：http://localhost:5005/swagger/v1/swagger.json
```

### 2. 使用 curl

```bash
# 获取所有轴
curl http://localhost:5005/api/axes/axes

# 使能轴
curl -X POST http://localhost:5005/api/axes/axes/enable \
  -H "Content-Type: application/json" \
  -d '{"axisIds": ["axis1", "axis2"]}'

# 设置速度
curl -X POST http://localhost:5005/api/axes/axes/speed \
  -H "Content-Type: application/json" \
  -d '{"axisIds": ["axis1"], "speedMmps": 100.0}'
```

### 3. 日志调试

启用详细日志：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ZakYip.Singulation": "Trace"
    }
  }
}
```

### 4. 网络抓包

使用 Fiddler 或 Wireshark 抓取 HTTP 请求：
- 查看完整请求/响应
- 分析性能瓶颈
- 排查网络问题

## 注意事项

1. **并发控制**：同时操作同一轴可能导致竞态条件，建议使用队列或锁机制
2. **超时设置**：长时间操作建议设置合理的超时时间（建议 30-60 秒）
3. **错误重试**：网络抖动时建议实现指数退避重试策略
4. **连接池**：使用 HttpClient 单例，避免创建大量连接
5. **资源释放**：及时释放 SignalR 连接和 HttpClient 资源

---

**文档版本**：1.0  
**最后更新**：2025-10-19  
**API 版本**：v1  
**维护者**：ZakYip.Singulation 开发团队
