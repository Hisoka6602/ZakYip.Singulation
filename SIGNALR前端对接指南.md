# SignalR 前端对接指南

## 概述

本文档提供完整的 SignalR 前端对接指南，包括所有可用的端点、频道、事件和示例代码。

**版本**: 1.0  
**生成日期**: 2025-10-30  
**适用系统**: ZakYip.Singulation

---

## 目录

1. [快速开始](#快速开始)
2. [SignalR Hub 端点](#signalr-hub-端点)
3. [频道说明](#频道说明)
4. [服务端到客户端事件](#服务端到客户端事件)
5. [客户端到服务端方法](#客户端到服务端方法)
6. [消息格式](#消息格式)
7. [连接示例](#连接示例)
   - [JavaScript/TypeScript](#javascripttypescript-示例)
   - [C# 客户端](#c-客户端示例)
   - [React 示例](#react-示例)
   - [Vue 示例](#vue-示例)
8. [高级主题](#高级主题)
9. [常见问题](#常见问题)

---

## 快速开始

### 1. 安装依赖

**npm/yarn:**
```bash
npm install @microsoft/signalr
# 或
yarn add @microsoft/signalr
```

**CDN:**
```html
<script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
```

### 2. 基础连接示例

```javascript
import * as signalR from '@microsoft/signalr';

// 创建连接
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5005/hubs/events")
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

// 订阅事件
connection.on("event", (envelope) => {
    console.log("收到事件:", envelope);
    console.log("频道:", envelope.channel);
    console.log("数据:", envelope.data);
});

// 启动连接
await connection.start();
console.log("SignalR 连接成功");

// 加入频道
await connection.invoke("Join", "/io/status");
```

---

## SignalR Hub 端点

### EventsHub

**端点地址**: `/hubs/events`  
**完整URL**: `http://your-server:5005/hubs/events`

这是主要的事件中心，用于实时推送各种系统事件到客户端。

**支持的传输协议**:
- WebSockets (推荐)
- Server-Sent Events
- Long Polling

---

## 频道说明

频道（Channel）用于分组消息，客户端需要先加入频道才能接收该频道的消息。

### 系统预定义频道

| 频道名称 | 说明 | 消息类型 | 推送频率 |
|---------|------|---------|---------|
| `/sys` | 系统级事件 | 系统状态、配置变更等 | 不定期 |
| `/device` | 设备相关事件 | 设备状态、连接状态 | 不定期 |
| `/vision` | 视觉相关事件 | 视觉检测结果 | 不定期 |
| `/errors` | 异常相关事件 | 错误信息、警告 | 不定期 |
| `/io/status` | IO 状态监控 | 输入输出状态 | 可配置 (默认500ms) |

### 频道使用示例

```javascript
// 加入单个频道
await connection.invoke("Join", "/io/status");

// 加入多个频道
await connection.invoke("Join", "/sys");
await connection.invoke("Join", "/device");
await connection.invoke("Join", "/errors");

// 离开频道
await connection.invoke("Leave", "/io/status");
```

---

## 服务端到客户端事件

### 1. event (通用事件)

**说明**: 这是主要的事件推送方法，所有频道的消息都通过此事件发送。

**消息格式**:
```typescript
interface MessageEnvelope {
    version: number;        // 消息格式版本，当前为 1
    type: string;           // 数据类型名称
    timestamp: string;      // ISO 8601 格式的时间戳
    channel: string;        // 频道名称
    data: any;              // 实际数据负载
    traceId?: string;       // 跟踪ID (可选)
    sequence: number;       // 消息序列号
}
```

**订阅示例**:
```javascript
connection.on("event", (envelope) => {
    console.log(`频道: ${envelope.channel}`);
    console.log(`类型: ${envelope.type}`);
    console.log(`序列号: ${envelope.sequence}`);
    console.log(`时间: ${envelope.timestamp}`);
    console.log(`数据:`, envelope.data);
    
    // 根据频道处理不同的消息
    switch(envelope.channel) {
        case "/io/status":
            handleIoStatus(envelope.data);
            break;
        case "/device":
            handleDeviceEvent(envelope.data);
            break;
        case "/errors":
            handleError(envelope.data);
            break;
        default:
            console.log("未知频道:", envelope.channel);
    }
});
```

### 2. ReceiveMessage (遗留方法)

**说明**: 简单的消息接收方法，主要用于测试。

**参数**: `string message`

**订阅示例**:
```javascript
connection.on("ReceiveMessage", (message) => {
    console.log("收到消息:", message);
});
```

### 3. ReceiveEvent (遗留方法)

**说明**: 旧版事件接收方法，新应用建议使用 `event`。

**参数**: 
- `string eventName`: 事件名称
- `object data`: 事件数据

**订阅示例**:
```javascript
connection.on("ReceiveEvent", (eventName, data) => {
    console.log(`事件: ${eventName}`, data);
});
```

### 4. AxisSpeedChanged (特定事件)

**说明**: 轴速度变化事件 (如果系统支持)。

**参数**:
- `int axisId`: 轴ID
- `double speed`: 速度值 (mm/s)

**订阅示例**:
```javascript
connection.on("AxisSpeedChanged", (axisId, speed) => {
    console.log(`轴 ${axisId} 速度变更为 ${speed} mm/s`);
});
```

### 5. SafetyEvent (特定事件)

**说明**: 安全事件通知 (如果系统支持)。

**参数**:
- `string eventType`: 事件类型
- `string message`: 消息内容
- `DateTime timestamp`: 时间戳

**订阅示例**:
```javascript
connection.on("SafetyEvent", (eventType, message, timestamp) => {
    console.log(`安全事件: ${eventType}`);
    console.log(`消息: ${message}`);
    console.log(`时间: ${timestamp}`);
});
```

---

## 客户端到服务端方法

### 1. Join(channel)

**说明**: 加入指定的频道组，开始接收该频道的消息。

**参数**:
- `channel` (string): 频道名称

**返回**: `Promise<void>`

**示例**:
```javascript
await connection.invoke("Join", "/io/status");
```

### 2. Leave(channel)

**说明**: 离开指定的频道组，停止接收该频道的消息。

**参数**:
- `channel` (string): 频道名称

**返回**: `Promise<void>`

**示例**:
```javascript
await connection.invoke("Leave", "/io/status");
```

### 3. Ping()

**说明**: 心跳检测方法，用于测量客户端到服务器的延迟。

**参数**: 无

**返回**: `Promise<void>`

**示例**:
```javascript
const startTime = Date.now();
await connection.invoke("Ping");
const latency = Date.now() - startTime;
console.log(`延迟: ${latency}ms`);
```

---

## 消息格式

### IO 状态消息 (IoStatus)

**频道**: `/io/status`

**数据格式**:
```typescript
interface IoStatus {
    deviceId: string;           // 设备ID
    timestamp: string;          // 时间戳
    inputs: boolean[];          // 输入状态数组
    outputs: boolean[];         // 输出状态数组
    inputStart?: number;        // 输入起始地址
    outputStart?: number;       // 输出起始地址
}
```

**示例**:
```json
{
    "version": 1,
    "type": "IoStatus",
    "timestamp": "2025-10-30T12:00:00.000Z",
    "channel": "/io/status",
    "sequence": 123,
    "data": {
        "deviceId": "plc-001",
        "timestamp": "2025-10-30T12:00:00.000Z",
        "inputs": [true, false, true, false],
        "outputs": [false, true, false, true],
        "inputStart": 0,
        "outputStart": 0
    }
}
```

### 设备事件消息

**频道**: `/device`

**数据格式**: 根据具体事件类型而定

**示例**:
```json
{
    "version": 1,
    "type": "DeviceConnected",
    "timestamp": "2025-10-30T12:00:00.000Z",
    "channel": "/device",
    "sequence": 45,
    "data": {
        "deviceId": "device-001",
        "status": "connected",
        "ipAddress": "192.168.1.100"
    }
}
```

### 错误事件消息

**频道**: `/errors`

**数据格式**:
```typescript
interface ErrorEvent {
    errorCode?: string;
    message: string;
    severity: "warning" | "error" | "critical";
    timestamp: string;
    source?: string;
}
```

---

## 连接示例

### JavaScript/TypeScript 示例

#### 完整的连接管理类

```typescript
import * as signalR from '@microsoft/signalr';

export class SignalRClient {
    private connection: signalR.HubConnection;
    private reconnectAttempts = 0;
    private maxReconnectAttempts = 10;
    
    constructor(baseUrl: string) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/events`)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (retryContext) => {
                    // 指数退避策略: 0s, 2s, 10s, 30s, 60s
                    const delays = [0, 2000, 10000, 30000, 60000];
                    const index = Math.min(retryContext.previousRetryCount, delays.length - 1);
                    return delays[index];
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();
        
        this.setupEventHandlers();
        this.setupConnectionHandlers();
    }
    
    private setupEventHandlers(): void {
        // 通用事件处理
        this.connection.on("event", (envelope) => {
            this.handleEvent(envelope);
        });
    }
    
    private setupConnectionHandlers(): void {
        this.connection.onreconnecting((error) => {
            console.warn('SignalR 重连中...', error);
            this.reconnectAttempts++;
        });
        
        this.connection.onreconnected((connectionId) => {
            console.log('SignalR 重连成功:', connectionId);
            this.reconnectAttempts = 0;
            // 重新加入频道
            this.rejoinChannels();
        });
        
        this.connection.onclose((error) => {
            console.error('SignalR 连接关闭:', error);
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                this.reconnect();
            }
        });
    }
    
    private handleEvent(envelope: any): void {
        console.log(`[${envelope.channel}] ${envelope.type}`, envelope.data);
        
        // 触发自定义事件
        const event = new CustomEvent('signalr-event', {
            detail: envelope
        });
        window.dispatchEvent(event);
    }
    
    async connect(): Promise<void> {
        try {
            await this.connection.start();
            console.log('SignalR 连接成功');
        } catch (error) {
            console.error('SignalR 连接失败:', error);
            throw error;
        }
    }
    
    async disconnect(): Promise<void> {
        await this.connection.stop();
    }
    
    async join(channel: string): Promise<void> {
        await this.connection.invoke("Join", channel);
        console.log(`已加入频道: ${channel}`);
    }
    
    async leave(channel: string): Promise<void> {
        await this.connection.invoke("Leave", channel);
        console.log(`已离开频道: ${channel}`);
    }
    
    async ping(): Promise<number> {
        const startTime = Date.now();
        await this.connection.invoke("Ping");
        return Date.now() - startTime;
    }
    
    private async rejoinChannels(): Promise<void> {
        // 在这里重新加入之前订阅的频道
        // 根据你的应用需求实现
    }
    
    private async reconnect(): Promise<void> {
        setTimeout(async () => {
            try {
                await this.connect();
            } catch (error) {
                console.error('重连失败:', error);
            }
        }, 5000);
    }
    
    get state(): signalR.HubConnectionState {
        return this.connection.state;
    }
    
    get isConnected(): boolean {
        return this.connection.state === signalR.HubConnectionState.Connected;
    }
}
```

#### 使用示例

```typescript
// 初始化客户端
const client = new SignalRClient('http://localhost:5005');

// 连接
await client.connect();

// 加入频道
await client.join('/io/status');
await client.join('/device');
await client.join('/errors');

// 监听事件
window.addEventListener('signalr-event', (e: any) => {
    const envelope = e.detail;
    
    switch(envelope.channel) {
        case '/io/status':
            console.log('IO状态更新:', envelope.data);
            break;
        case '/device':
            console.log('设备事件:', envelope.data);
            break;
        case '/errors':
            console.error('错误事件:', envelope.data);
            break;
    }
});

// 测量延迟
setInterval(async () => {
    if (client.isConnected) {
        const latency = await client.ping();
        console.log(`延迟: ${latency}ms`);
    }
}, 5000);
```

### C# 客户端示例

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

public class SignalRClient
{
    private HubConnection _connection;
    
    public event EventHandler<MessageEnvelope> EventReceived;
    
    public SignalRClient(string baseUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/events")
            .WithAutomaticReconnect(new[] { 
                TimeSpan.Zero, 
                TimeSpan.FromSeconds(2), 
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30) 
            })
            .Build();
        
        SetupEventHandlers();
    }
    
    private void SetupEventHandlers()
    {
        // 订阅通用事件
        _connection.On<MessageEnvelope>("event", (envelope) =>
        {
            Console.WriteLine($"[{envelope.Channel}] {envelope.Type}");
            EventReceived?.Invoke(this, envelope);
        });
        
        // 连接状态处理
        _connection.Reconnecting += (error) =>
        {
            Console.WriteLine("重连中...");
            return Task.CompletedTask;
        };
        
        _connection.Reconnected += (connectionId) =>
        {
            Console.WriteLine($"重连成功: {connectionId}");
            return Task.CompletedTask;
        };
        
        _connection.Closed += (error) =>
        {
            Console.WriteLine($"连接关闭: {error?.Message}");
            return Task.CompletedTask;
        };
    }
    
    public async Task ConnectAsync()
    {
        await _connection.StartAsync();
        Console.WriteLine("SignalR 连接成功");
    }
    
    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
    }
    
    public async Task JoinAsync(string channel)
    {
        await _connection.InvokeAsync("Join", channel);
        Console.WriteLine($"已加入频道: {channel}");
    }
    
    public async Task LeaveAsync(string channel)
    {
        await _connection.InvokeAsync("Leave", channel);
        Console.WriteLine($"已离开频道: {channel}");
    }
    
    public async Task<long> PingAsync()
    {
        var startTime = DateTime.UtcNow;
        await _connection.InvokeAsync("Ping");
        return (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
    }
    
    public bool IsConnected => _connection.State == HubConnectionState.Connected;
}

// 消息封装类
public class MessageEnvelope
{
    public int Version { get; set; }
    public string Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Channel { get; set; }
    public object Data { get; set; }
    public string TraceId { get; set; }
    public long Sequence { get; set; }
}

// 使用示例
var client = new SignalRClient("http://localhost:5005");
await client.ConnectAsync();

// 订阅事件
client.EventReceived += (sender, envelope) =>
{
    switch(envelope.Channel)
    {
        case "/io/status":
            Console.WriteLine($"IO状态: {envelope.Data}");
            break;
        case "/device":
            Console.WriteLine($"设备事件: {envelope.Data}");
            break;
        case "/errors":
            Console.WriteLine($"错误: {envelope.Data}");
            break;
    }
};

// 加入频道
await client.JoinAsync("/io/status");
await client.JoinAsync("/device");
```

### React 示例

```typescript
import React, { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

interface MessageEnvelope {
    version: number;
    type: string;
    timestamp: string;
    channel: string;
    data: any;
    traceId?: string;
    sequence: number;
}

export const useSignalR = (baseUrl: string) => {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);
    const [latency, setLatency] = useState<number>(0);
    const [events, setEvents] = useState<MessageEnvelope[]>([]);
    
    useEffect(() => {
        // 创建连接
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/events`)
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();
        
        // 设置事件处理
        newConnection.on("event", (envelope: MessageEnvelope) => {
            console.log('收到事件:', envelope);
            setEvents(prev => [...prev.slice(-99), envelope]); // 保留最近100条
        });
        
        // 连接状态处理
        newConnection.onreconnecting(() => {
            console.log('重连中...');
            setIsConnected(false);
        });
        
        newConnection.onreconnected(() => {
            console.log('重连成功');
            setIsConnected(true);
        });
        
        newConnection.onclose(() => {
            console.log('连接关闭');
            setIsConnected(false);
        });
        
        setConnection(newConnection);
        
        // 启动连接
        newConnection.start()
            .then(() => {
                console.log('SignalR 连接成功');
                setIsConnected(true);
            })
            .catch(err => console.error('连接失败:', err));
        
        // 清理
        return () => {
            newConnection.stop();
        };
    }, [baseUrl]);
    
    const join = useCallback(async (channel: string) => {
        if (connection) {
            await connection.invoke("Join", channel);
            console.log(`已加入频道: ${channel}`);
        }
    }, [connection]);
    
    const leave = useCallback(async (channel: string) => {
        if (connection) {
            await connection.invoke("Leave", channel);
            console.log(`已离开频道: ${channel}`);
        }
    }, [connection]);
    
    const ping = useCallback(async () => {
        if (connection) {
            const startTime = Date.now();
            await connection.invoke("Ping");
            const delay = Date.now() - startTime;
            setLatency(delay);
            return delay;
        }
        return 0;
    }, [connection]);
    
    return {
        connection,
        isConnected,
        latency,
        events,
        join,
        leave,
        ping
    };
};

// 使用 Hook 的组件示例
export const SignalRDashboard: React.FC = () => {
    const { isConnected, latency, events, join, leave, ping } = useSignalR('http://localhost:5005');
    const [ioStatus, setIoStatus] = useState<any>(null);
    
    useEffect(() => {
        if (isConnected) {
            // 加入频道
            join('/io/status');
            join('/device');
            join('/errors');
            
            // 定期ping
            const interval = setInterval(() => ping(), 5000);
            return () => clearInterval(interval);
        }
    }, [isConnected, join, ping]);
    
    // 处理特定频道的事件
    useEffect(() => {
        const ioEvents = events.filter(e => e.channel === '/io/status');
        if (ioEvents.length > 0) {
            setIoStatus(ioEvents[ioEvents.length - 1].data);
        }
    }, [events]);
    
    return (
        <div>
            <h1>SignalR Dashboard</h1>
            <div>
                <p>连接状态: {isConnected ? '已连接' : '未连接'}</p>
                <p>延迟: {latency}ms</p>
            </div>
            
            <div>
                <h2>IO 状态</h2>
                {ioStatus && (
                    <pre>{JSON.stringify(ioStatus, null, 2)}</pre>
                )}
            </div>
            
            <div>
                <h2>最近事件</h2>
                <ul>
                    {events.slice(-10).reverse().map((event, index) => (
                        <li key={index}>
                            [{event.channel}] {event.type} - {event.timestamp}
                        </li>
                    ))}
                </ul>
            </div>
        </div>
    );
};
```

### Vue 示例

```typescript
// composables/useSignalR.ts
import { ref, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';

interface MessageEnvelope {
    version: number;
    type: string;
    timestamp: string;
    channel: string;
    data: any;
    traceId?: string;
    sequence: number;
}

export function useSignalR(baseUrl: string) {
    const connection = ref<signalR.HubConnection | null>(null);
    const isConnected = ref(false);
    const latency = ref(0);
    const events = ref<MessageEnvelope[]>([]);
    
    const connect = async () => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/events`)
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();
        
        // 设置事件处理
        newConnection.on("event", (envelope: MessageEnvelope) => {
            console.log('收到事件:', envelope);
            events.value = [...events.value.slice(-99), envelope];
        });
        
        // 连接状态处理
        newConnection.onreconnecting(() => {
            console.log('重连中...');
            isConnected.value = false;
        });
        
        newConnection.onreconnected(() => {
            console.log('重连成功');
            isConnected.value = true;
        });
        
        newConnection.onclose(() => {
            console.log('连接关闭');
            isConnected.value = false;
        });
        
        connection.value = newConnection;
        
        try {
            await newConnection.start();
            console.log('SignalR 连接成功');
            isConnected.value = true;
        } catch (err) {
            console.error('连接失败:', err);
        }
    };
    
    const disconnect = async () => {
        if (connection.value) {
            await connection.value.stop();
        }
    };
    
    const join = async (channel: string) => {
        if (connection.value) {
            await connection.value.invoke("Join", channel);
            console.log(`已加入频道: ${channel}`);
        }
    };
    
    const leave = async (channel: string) => {
        if (connection.value) {
            await connection.value.invoke("Leave", channel);
            console.log(`已离开频道: ${channel}`);
        }
    };
    
    const ping = async () => {
        if (connection.value) {
            const startTime = Date.now();
            await connection.value.invoke("Ping");
            const delay = Date.now() - startTime;
            latency.value = delay;
            return delay;
        }
        return 0;
    };
    
    onMounted(() => {
        connect();
    });
    
    onUnmounted(() => {
        disconnect();
    });
    
    return {
        connection,
        isConnected,
        latency,
        events,
        join,
        leave,
        ping
    };
}
```

```vue
<!-- components/SignalRDashboard.vue -->
<template>
  <div class="signalr-dashboard">
    <h1>SignalR Dashboard</h1>
    
    <div class="status">
      <p>连接状态: <span :class="{ connected: isConnected }">{{ isConnected ? '已连接' : '未连接' }}</span></p>
      <p>延迟: {{ latency }}ms</p>
    </div>
    
    <div class="io-status" v-if="ioStatus">
      <h2>IO 状态</h2>
      <pre>{{ JSON.stringify(ioStatus, null, 2) }}</pre>
    </div>
    
    <div class="events">
      <h2>最近事件</h2>
      <ul>
        <li v-for="(event, index) in recentEvents" :key="index">
          [{{ event.channel }}] {{ event.type }} - {{ event.timestamp }}
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, watch, onMounted } from 'vue';
import { useSignalR } from '../composables/useSignalR';

const { isConnected, latency, events, join, ping } = useSignalR('http://localhost:5005');

const ioStatus = computed(() => {
  const ioEvents = events.value.filter(e => e.channel === '/io/status');
  return ioEvents.length > 0 ? ioEvents[ioEvents.length - 1].data : null;
});

const recentEvents = computed(() => {
  return events.value.slice(-10).reverse();
});

watch(isConnected, (connected) => {
  if (connected) {
    // 加入频道
    join('/io/status');
    join('/device');
    join('/errors');
    
    // 定期ping
    setInterval(() => ping(), 5000);
  }
});
</script>

<style scoped>
.signalr-dashboard {
  padding: 20px;
}

.status .connected {
  color: green;
  font-weight: bold;
}

pre {
  background: #f5f5f5;
  padding: 10px;
  border-radius: 5px;
}
</style>
```

---

## 高级主题

### 1. 消息序列号检测丢失

客户端可以通过检查序列号来检测是否有消息丢失：

```typescript
class SequenceTracker {
    private sequences = new Map<string, number>();
    
    checkSequence(envelope: MessageEnvelope): boolean {
        const lastSeq = this.sequences.get(envelope.channel) || 0;
        const currentSeq = envelope.sequence;
        
        if (lastSeq > 0 && currentSeq !== lastSeq + 1) {
            const missed = currentSeq - lastSeq - 1;
            console.warn(`频道 ${envelope.channel} 丢失了 ${missed} 条消息`);
            return false;
        }
        
        this.sequences.set(envelope.channel, currentSeq);
        return true;
    }
}

// 使用
const tracker = new SequenceTracker();
connection.on("event", (envelope) => {
    if (!tracker.checkSequence(envelope)) {
        // 处理消息丢失情况
    }
});
```

### 2. 自定义重连策略

```typescript
class CustomRetryPolicy implements signalR.IRetryPolicy {
    private attempts = 0;
    
    nextRetryDelayInMilliseconds(retryContext: signalR.RetryContext): number | null {
        this.attempts = retryContext.previousRetryCount;
        
        // 前5次使用指数退避
        if (this.attempts < 5) {
            return Math.pow(2, this.attempts) * 1000;
        }
        
        // 之后每60秒重试一次，最多尝试30次
        if (this.attempts < 30) {
            return 60000;
        }
        
        // 超过30次后停止重连
        return null;
    }
}

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5005/hubs/events")
    .withAutomaticReconnect(new CustomRetryPolicy())
    .build();
```

### 3. 消息过滤和处理

```typescript
class MessageFilter {
    private handlers = new Map<string, ((data: any) => void)[]>();
    
    register(channel: string, handler: (data: any) => void): void {
        if (!this.handlers.has(channel)) {
            this.handlers.set(channel, []);
        }
        this.handlers.get(channel)!.push(handler);
    }
    
    handle(envelope: MessageEnvelope): void {
        const handlers = this.handlers.get(envelope.channel);
        if (handlers) {
            handlers.forEach(handler => {
                try {
                    handler(envelope.data);
                } catch (error) {
                    console.error(`处理 ${envelope.channel} 消息时出错:`, error);
                }
            });
        }
    }
}

// 使用
const filter = new MessageFilter();

// 注册处理器
filter.register('/io/status', (data) => {
    console.log('IO状态更新:', data);
});

filter.register('/device', (data) => {
    console.log('设备事件:', data);
});

// 在事件处理中使用
connection.on("event", (envelope) => {
    filter.handle(envelope);
});
```

### 4. 性能监控

```typescript
class PerformanceMonitor {
    private messageCount = 0;
    private startTime = Date.now();
    private latencies: number[] = [];
    
    recordMessage(envelope: MessageEnvelope): void {
        this.messageCount++;
        
        // 计算延迟 (从服务端时间戳到现在)
        const serverTime = new Date(envelope.timestamp).getTime();
        const latency = Date.now() - serverTime;
        this.latencies.push(latency);
        
        // 只保留最近100条
        if (this.latencies.length > 100) {
            this.latencies.shift();
        }
    }
    
    getStats() {
        const runtime = (Date.now() - this.startTime) / 1000;
        const avgLatency = this.latencies.reduce((a, b) => a + b, 0) / this.latencies.length;
        const maxLatency = Math.max(...this.latencies);
        const minLatency = Math.min(...this.latencies);
        
        return {
            messagesPerSecond: this.messageCount / runtime,
            totalMessages: this.messageCount,
            averageLatency: avgLatency,
            maxLatency: maxLatency,
            minLatency: minLatency
        };
    }
}

// 使用
const monitor = new PerformanceMonitor();
connection.on("event", (envelope) => {
    monitor.recordMessage(envelope);
});

// 定期输出统计
setInterval(() => {
    console.log('性能统计:', monitor.getStats());
}, 10000);
```

---

## 常见问题

### Q1: 如何处理连接失败？

**A**: 使用 `withAutomaticReconnect` 配置自动重连策略，并监听连接状态事件：

```typescript
connection.onclose(async (error) => {
    console.error('连接关闭:', error);
    // 可以在这里实现自定义重连逻辑
    await tryReconnect();
});
```

### Q2: 如何确保消息不丢失？

**A**: 
1. 监控消息序列号，检测丢失的消息
2. 使用 WebSocket 传输协议（最可靠）
3. 实现客户端消息队列，缓存未处理的消息
4. 在重连后请求服务器重发丢失的消息（需要服务器支持）

### Q3: 如何优化性能？

**A**:
1. 只订阅需要的频道
2. 实现消息节流/防抖
3. 使用 MessagePack 协议代替 JSON（更高效）
4. 避免在消息处理器中执行耗时操作
5. 使用 Web Workers 处理大量消息

### Q4: 如何调试 SignalR 连接？

**A**:
```typescript
// 启用详细日志
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5005/hubs/events")
    .configureLogging(signalR.LogLevel.Debug) // 或 Trace
    .build();

// 浏览器开发者工具 -> Network -> WS 标签页查看 WebSocket 流量
```

### Q5: 如何处理认证？

**A**: 如果服务器需要认证，在连接时传递 access token：

```typescript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5005/hubs/events", {
        accessTokenFactory: () => {
            // 返回你的 JWT token
            return localStorage.getItem('access_token') || '';
        }
    })
    .build();
```

### Q6: 消息队列满了怎么办？

**A**: 服务器使用 DropOldest 策略，队列满时会丢弃旧消息。客户端应该：
1. 通过序列号检测消息丢失
2. 优化消息处理速度
3. 考虑降低订阅的频道数量

### Q7: 如何测试 SignalR 连接？

**A**: 
1. 使用浏览器控制台直接测试
2. 使用 Postman 或类似工具（支持 WebSocket）
3. 编写自动化测试：

```typescript
// Jest 测试示例
describe('SignalR Connection', () => {
    let connection: signalR.HubConnection;
    
    beforeEach(async () => {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5005/hubs/events")
            .build();
        await connection.start();
    });
    
    afterEach(async () => {
        await connection.stop();
    });
    
    test('should connect successfully', () => {
        expect(connection.state).toBe(signalR.HubConnectionState.Connected);
    });
    
    test('should receive events after joining channel', (done) => {
        connection.on("event", (envelope) => {
            expect(envelope.channel).toBe('/test');
            done();
        });
        
        connection.invoke("Join", "/test");
    });
});
```

---

## 版本历史

- **v1.0** (2025-10-30): 初始版本，包含基础连接示例和频道说明

---

## 联系支持

如有问题或建议，请联系开发团队或在项目 GitHub 仓库提交 Issue。
