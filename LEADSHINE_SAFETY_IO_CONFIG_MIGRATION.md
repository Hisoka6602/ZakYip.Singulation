# LeadshineSafetyIo 配置持久化与热更新功能说明

## 功能概述

本次更新将 LeadshineSafetyIo（雷赛安全 IO）的配置从 `appsettings.json` 迁移到 LiteDB 数据库持久化存储，并提供了 API 端点用于查询和更新配置，支持配置的热更新（无需重启服务）。

## 主要变更

### 1. 配置存储迁移

**之前**: 配置存储在 `appsettings.json` 文件中，需要手动编辑文件并重启服务才能生效。

**现在**: 配置存储在 LiteDB 数据库中（`singulation.db`），可以通过 API 动态更新，无需重启服务。

### 2. 新增 API 端点

#### GET /api/SafetyIo/configs
查询当前的安全 IO 配置。

**请求示例**:
```bash
GET http://localhost:5005/api/SafetyIo/configs
```

**响应示例**:
```json
{
  "success": true,
  "data": {
    "enabled": false,
    "emergencyStopBit": 0,
    "stopBit": 1,
    "startBit": 2,
    "resetBit": 3,
    "pollingIntervalMs": 50,
    "invertLogic": false,
    "invertEmergencyStopLogic": null,
    "invertStopLogic": null,
    "invertStartLogic": null,
    "invertResetLogic": null
  }
}
```

#### PUT /api/SafetyIo/configs
更新安全 IO 配置（支持热更新）。

**请求示例**:
```bash
PUT http://localhost:5005/api/SafetyIo/configs
Content-Type: application/json

{
  "enabled": true,
  "emergencyStopBit": 0,
  "stopBit": 1,
  "startBit": 2,
  "resetBit": 3,
  "pollingIntervalMs": 100,
  "invertLogic": false,
  "invertEmergencyStopLogic": true,
  "invertStopLogic": null,
  "invertStartLogic": false,
  "invertResetLogic": null
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "配置已保存并应用（热更新成功）",
  "data": "配置已保存并应用（热更新成功）"
}
```

### 3. 热更新功能

当通过 API 更新配置后，新的配置会立即应用到运行中的安全 IO 模块，无需重启服务。这包括：

- 按键端口配置（EmergencyStopBit, StopBit, StartBit, ResetBit）
- 轮询间隔（PollingIntervalMs）
- 逻辑反转设置（InvertLogic 及各按键独立的反转配置）

### 4. 配置项说明

所有配置项都已添加中文注释，详见 `appsettings.json` 或 API 文档。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| Enabled | bool | false | 是否启用物理按键（true=使用硬件按键, false=使用软件模拟） |
| EmergencyStopBit | int | -1 | 急停按键输入端口号，-1 表示禁用 |
| StopBit | int | -1 | 停止按键输入端口号，-1 表示禁用 |
| StartBit | int | -1 | 启动按键输入端口号，-1 表示禁用 |
| ResetBit | int | -1 | 复位按键输入端口号，-1 表示禁用 |
| PollingIntervalMs | int | 50 | 轮询间隔（毫秒），推荐 20-100 |
| InvertLogic | bool | false | 全局逻辑反转（false=常开按键，true=常闭按键） |
| InvertEmergencyStopLogic | bool? | null | 急停按键独立逻辑反转，null 时使用全局配置 |
| InvertStopLogic | bool? | null | 停止按键独立逻辑反转，null 时使用全局配置 |
| InvertStartLogic | bool? | null | 启动按键独立逻辑反转，null 时使用全局配置 |
| InvertResetLogic | bool? | null | 复位按键独立逻辑反转，null 时使用全局配置 |
| RedLightBit | int | -1 | 红灯输出端口号，-1 表示禁用 |
| YellowLightBit | int | -1 | 黄灯输出端口号，-1 表示禁用 |
| GreenLightBit | int | -1 | 绿灯输出端口号，-1 表示禁用 |
| StartButtonLightBit | int | -1 | 启动按钮灯输出端口号，-1 表示禁用 |
| StopButtonLightBit | int | -1 | 停止按钮灯输出端口号，-1 表示禁用 |
| InvertLightLogic | bool | false | 全局灯光逻辑反转（false=高电平亮灯，true=低电平亮灯） |
| InvertRedLightLogic | bool? | null | 红灯独立逻辑反转，null 时使用 InvertLightLogic |
| InvertYellowLightLogic | bool? | null | 黄灯独立逻辑反转，null 时使用 InvertLightLogic |
| InvertGreenLightLogic | bool? | null | 绿灯独立逻辑反转，null 时使用 InvertLightLogic |
| InvertStartButtonLightLogic | bool? | null | 启动按钮灯独立逻辑反转，null 时使用 InvertLightLogic |
| InvertStopButtonLightLogic | bool? | null | 停止按钮灯独立逻辑反转，null 时使用 InvertLightLogic |


## 使用指南

### 初次使用

1. 启动服务后，系统会自动从数据库读取配置（如果不存在则使用默认值）
2. 通过 GET `/api/SafetyIo/configs` 查询当前配置
3. 通过 PUT `/api/SafetyIo/configs` 更新配置

### 配置示例场景

#### 场景 1: 只使用急停按键
```json
{
  "enabled": true,
  "emergencyStopBit": 0,
  "stopBit": -1,
  "startBit": -1,
  "resetBit": -1,
  "pollingIntervalMs": 20
}
```

#### 场景 2: 混合按键类型（急停和启动为常闭，停止和复位为常开）
```json
{
  "enabled": true,
  "emergencyStopBit": 0,
  "stopBit": 1,
  "startBit": 2,
  "resetBit": 3,
  "pollingIntervalMs": 50,
  "invertLogic": false,
  "invertEmergencyStopLogic": true,
  "invertStopLogic": null,
  "invertStartLogic": true,
  "invertResetLogic": null
}
```

#### 场景 3: 配置三色灯和按钮灯（高电平亮灯）
```json
{
  "enabled": true,
  "redLightBit": 10,
  "yellowLightBit": 11,
  "greenLightBit": 12,
  "startButtonLightBit": 13,
  "stopButtonLightBit": 14,
  "invertLightLogic": false
}
```

#### 场景 4: 配置三色灯和按钮灯（低电平亮灯）
```json
{
  "enabled": true,
  "redLightBit": 10,
  "yellowLightBit": 11,
  "greenLightBit": 12,
  "startButtonLightBit": 13,
  "stopButtonLightBit": 14,
  "invertLightLogic": true
}
```

#### 场景 5: 混合配置（红灯低电平亮灯，其他高电平亮灯）
```json
{
  "enabled": true,
  "redLightBit": 10,
  "yellowLightBit": 11,
  "greenLightBit": 12,
  "startButtonLightBit": 13,
  "stopButtonLightBit": 14,
  "invertLightLogic": false,
  "invertRedLightLogic": true,
  "invertYellowLightLogic": null,
  "invertGreenLightLogic": null,
  "invertStartButtonLightLogic": null,
  "invertStopButtonLightLogic": null
}
```

## 技术实现

### 新增文件

1. **Core/Configs/LeadshineSafetyIoOptions.cs** - 配置选项类（从 Host 迁移到 Core）
2. **Core/Contracts/ILeadshineSafetyIoOptionsStore.cs** - 配置存储接口
3. **Infrastructure/Configs/Entities/LeadshineSafetyIoOptionsDoc.cs** - LiteDB 文档实体
4. **Infrastructure/Persistence/LiteDbLeadshineSafetyIoOptionsStore.cs** - LiteDB 存储实现
5. **Host/Controllers/SafetyIoController.cs** - API 控制器
6. **Tests/LeadshineSafetyIoStoreTests.cs** - 持久化存储测试

### 修改文件

1. **Infrastructure/Configs/Mappings/ConfigMappings.cs** - 添加配置映射方法
2. **Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs** - 添加服务注册
3. **Host/Safety/LeadshineSafetyIoModule.cs** - 添加热更新支持（UpdateOptions 方法）
4. **Host/Program.cs** - 更新服务注册，从数据库读取配置
5. **Host/appsettings.json** - 添加中文注释说明
6. **Tests/LeadshineSafetyIoOptionsTests.cs** - 更新测试引用

### 架构优势

1. **解耦**: 配置存储与应用逻辑分离
2. **灵活**: 支持运行时动态修改配置
3. **持久化**: 配置保存在数据库中，重启后自动恢复
4. **热更新**: 无需重启服务即可应用新配置
5. **可测试**: 使用内存数据库进行单元测试

## 注意事项

1. 配置更新会立即应用到运行中的安全 IO 模块
2. 如果当前使用的是 LoopbackSafetyIoModule（开发测试模式），配置仍会保存但需要重启后才能切换到硬件模式
3. appsettings.json 中的 LeadshineSafetyIo 配置现在仅作为参考，实际配置以数据库为准
4. 首次启动时，如果数据库中没有配置，会使用默认值（Enabled=false）

## 兼容性

- 向后兼容: 现有的测试用例无需修改（除了命名空间更新）
- 数据库: 使用现有的 LiteDB 数据库实例（singulation.db）
- API: 新增 API 端点，不影响现有功能
