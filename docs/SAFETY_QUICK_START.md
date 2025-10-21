# 安全按键系统快速入门

## 5 分钟配置指南

### 步骤 1：硬件接线

将物理按键连接到雷赛控制器数字输入端口：

```
急停按键 ──→ IN0 (输入端口 0)
停止按键 ──→ IN1 (输入端口 1)
启动按键 ──→ IN2 (输入端口 2)
复位按键 ──→ IN3 (输入端口 3)
```

**接线标准**：
- 使用屏蔽线，避免干扰
- 急停按键推荐使用带机械锁定的常闭触点
- 其他按键可使用常开触点

### 步骤 2：配置文件

编辑 `appsettings.json`：

```json
{
  "LeadshineSafetyIo": {
    "Enabled": true,              // 设为 true 启用物理按键
    "EmergencyStopBit": 0,        // 急停按键端口号
    "StopBit": 1,                 // 停止按键端口号
    "StartBit": 2,                // 启动按键端口号
    "ResetBit": 3,                // 复位按键端口号
    "PollingIntervalMs": 50,      // 50ms 轮询间隔（推荐）
    "InvertLogic": false          // 常开按键设为 false，常闭设为 true
  }
}
```

**禁用物理按键**（开发测试模式）：
```json
{
  "LeadshineSafetyIo": {
    "Enabled": false              // 使用软件模拟模式
  }
}
```

### 步骤 3：启动服务

```bash
cd ZakYip.Singulation.Host
dotnet run --configuration Release
```

### 步骤 4：测试验证

#### 测试急停按键
1. 按下急停按键
2. 观察日志：`grep "急停" logs/app-*.log`
3. 验证：所有轴停止运动，系统进入降级模式

#### 测试其他按键
1. 启动按键 → 触发启动流程
2. 停止按键 → 进入降级模式
3. 复位按键 → 从降级恢复到正常

#### 使用远程 API 测试
```bash
# 测试急停 API
curl -X POST http://localhost:5005/api/safety/commands \
  -H "Content-Type: application/json" \
  -d '{"command": 4, "reason": "测试急停"}'
```

## 常见配置场景

### 场景 1：只使用急停按键
```json
{
  "LeadshineSafetyIo": {
    "Enabled": true,
    "EmergencyStopBit": 0,
    "StopBit": -1,        // -1 表示禁用
    "StartBit": -1,
    "ResetBit": -1,
    "PollingIntervalMs": 20   // 更快的响应
  }
}
```

### 场景 2：常闭急停按键 + 常开其他按键
```json
{
  "LeadshineSafetyIo": {
    "Enabled": true,
    "EmergencyStopBit": 0,
    "StopBit": 1,
    "StartBit": 2,
    "ResetBit": 3,
    "PollingIntervalMs": 50,
    "InvertLogic": false      // 注意：仅影响所有按键
  }
}
```

如需单独配置急停为常闭，其他为常开，需要在代码中分别处理，或使用外部继电器转换。

### 场景 3：高速响应（高性能场景）
```json
{
  "LeadshineSafetyIo": {
    "Enabled": true,
    "EmergencyStopBit": 0,
    "StopBit": 1,
    "StartBit": 2,
    "ResetBit": 3,
    "PollingIntervalMs": 20,  // 20ms = 50Hz 轮询
    "InvertLogic": false
  }
}
```

### 场景 4：降低 CPU 占用（低速设备）
```json
{
  "LeadshineSafetyIo": {
    "Enabled": true,
    "EmergencyStopBit": 0,
    "StopBit": 1,
    "StartBit": 2,
    "ResetBit": 3,
    "PollingIntervalMs": 100, // 100ms = 10Hz 轮询
    "InvertLogic": false
  }
}
```

## 快速故障排查

### 问题：按键无响应

**排查步骤**：
1. 检查配置：`Enabled = true`
2. 检查端口号是否正确
3. 检查日志：`tail -f logs/app-*.log | grep "安全 IO"`
4. 测试硬件：万用表测试按键通断

### 问题：误触发/重复触发

**解决方案**：
1. 检查 `InvertLogic` 是否与按键类型匹配
2. 增加轮询间隔：`PollingIntervalMs = 100`
3. 检查按键防抖电路

### 问题：响应延迟过大

**解决方案**：
1. 降低轮询间隔：`PollingIntervalMs = 20`
2. 检查 CPU 负载：`top` 或 `htop`
3. 检查控制器通信延迟

## API 快速参考

### 安全命令 API

**端点**：`POST /api/safety/commands`

**命令值**：
- `1` - Start（启动）
- `2` - Stop（停止）
- `3` - Reset（复位）
- `4` - EmergencyStop（急停）

**示例**：
```bash
# 急停
curl -X POST http://localhost:5005/api/safety/commands \
  -H "Content-Type: application/json" \
  -d '{"command": 4, "reason": "紧急停机"}'

# 复位
curl -X POST http://localhost:5005/api/safety/commands \
  -H "Content-Type: application/json" \
  -d '{"command": 3, "reason": "恢复运行"}'
```

## 日志查看

```bash
# 查看所有安全日志
grep -E "安全|急停|Safety" logs/app-*.log

# 实时监控安全事件
tail -f logs/app-*.log | grep --color=auto "安全\|急停"

# 查看最近的急停记录
grep "急停" logs/app-*.log | tail -20
```

## 监控指标

系统运行后可监控：
- 安全事件触发次数
- 急停响应时间（< 100ms）
- 按键轮询正常率（> 99.9%）
- 系统状态转换日志

## 生产环境检查清单

部署到生产环境前，确认：

- [ ] 物理按键接线正确且牢固
- [ ] 配置文件中 `Enabled = true`
- [ ] 所有按键端口号配置正确
- [ ] 测试急停功能正常（系统立即停机）
- [ ] 测试复位功能正常（可恢复运行）
- [ ] 日志记录完整
- [ ] 制定应急预案

## 下一步

详细文档请参考：
- [安全按键系统完整指南](SAFETY_BUTTONS.md)
- [运维手册](../ops/OPERATIONS_MANUAL.md)
- [应急响应预案](../ops/EMERGENCY_RESPONSE.md)

如有问题，请查看 [故障排查手册](TROUBLESHOOTING.md) 或提交 GitHub Issue。
