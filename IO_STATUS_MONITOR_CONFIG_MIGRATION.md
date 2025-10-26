# IoStatusMonitor 配置持久化与热更新功能说明

## 功能概述

本次更新将 IoStatusMonitor（IO 状态监控）的配置从 `appsettings.json` 迁移到 LiteDB 数据库持久化存储，并提供了 API 端点用于查询和更新配置，支持配置的热更新（无需重启服务）。

## 主要变更

### 1. 配置存储迁移

**之前**: 配置存储在 `appsettings.json` 文件中，需要手动编辑文件并重启服务才能生效。

**现在**: 配置存储在 LiteDB 数据库中（`singulation.db`），可以通过 API 动态更新，无需重启服务。

### 2. 新增 API 端点

#### GET /api/IoMonitor/configs
查询当前的 IO 状态监控配置。

**请求示例**:
```bash
GET http://localhost:5005/api/IoMonitor/configs
```

**响应示例**:
```json
{
  "success": true,
  "data": {
    "enabled": true,
    "inputStart": 0,
    "inputCount": 32,
    "outputStart": 0,
    "outputCount": 32,
    "pollingIntervalMs": 500,
    "signalRChannel": "/io/status"
  }
}
```

#### PUT /api/IoMonitor/configs
更新 IO 状态监控配置（支持热更新）。

**请求示例**:
```bash
PUT http://localhost:5005/api/IoMonitor/configs
Content-Type: application/json

{
  "enabled": true,
  "inputStart": 0,
  "inputCount": 64,
  "outputStart": 0,
  "outputCount": 64,
  "pollingIntervalMs": 1000,
  "signalRChannel": "/io/status"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "配置已保存并将在下次轮询时生效（热更新）",
  "data": "配置已保存并将在下次轮询时生效（热更新）"
}
```

#### DELETE /api/IoMonitor/configs
删除当前配置，恢复到默认配置状态。

**请求示例**:
```bash
DELETE http://localhost:5005/api/IoMonitor/configs
```

**响应示例**:
```json
{
  "success": true,
  "message": "配置已删除，将使用默认配置",
  "data": "配置已删除，将使用默认配置"
}
```

### 3. 热更新功能

当通过 API 更新配置后，新的配置会在下次轮询周期时自动应用到运行中的监控服务，无需重启服务。这包括：

- 启用/禁用状态（Enabled）
- IO 范围配置（InputStart, InputCount, OutputStart, OutputCount）
- 轮询间隔（PollingIntervalMs）
- SignalR 广播频道（SignalRChannel）

### 4. 配置项说明

所有配置项都已添加中文注释，详见 API 文档。

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| Enabled | bool | true | 是否启用 IO 状态实时监控和广播 |
| InputStart | int | 0 | 输入 IO 起始位编号（范围：0-1000） |
| InputCount | int | 32 | 输入 IO 数量（范围：1-1024） |
| OutputStart | int | 0 | 输出 IO 起始位编号（范围：0-1000） |
| OutputCount | int | 32 | 输出 IO 数量（范围：1-1024） |
| PollingIntervalMs | int | 500 | 轮询间隔（毫秒，范围：100-10000） |
| SignalRChannel | string | "/io/status" | SignalR 广播频道名称 |

## 使用指南

### 初次使用

1. 启动服务后，系统会自动从数据库读取配置（如果不存在则使用默认值）
2. 通过 GET `/api/IoMonitor/configs` 查询当前配置
3. 通过 PUT `/api/IoMonitor/configs` 更新配置

### 配置示例场景

#### 场景 1: 监控少量关键 IO（快速响应）
```json
{
  "enabled": true,
  "inputStart": 0,
  "inputCount": 8,
  "outputStart": 0,
  "outputCount": 8,
  "pollingIntervalMs": 100,
  "signalRChannel": "/io/status"
}
```

#### 场景 2: 监控大量 IO（节能模式）
```json
{
  "enabled": true,
  "inputStart": 0,
  "inputCount": 512,
  "outputStart": 0,
  "outputCount": 512,
  "pollingIntervalMs": 1000,
  "signalRChannel": "/io/high-volume"
}
```

#### 场景 3: 禁用监控（开发测试）
```json
{
  "enabled": false,
  "inputStart": 0,
  "inputCount": 32,
  "outputStart": 0,
  "outputCount": 32,
  "pollingIntervalMs": 500,
  "signalRChannel": "/io/status"
}
```

#### 场景 4: 自定义 IO 范围
```json
{
  "enabled": true,
  "inputStart": 10,
  "inputCount": 16,
  "outputStart": 20,
  "outputCount": 8,
  "pollingIntervalMs": 500,
  "signalRChannel": "/io/custom"
}
```

## 技术实现

### 新增文件

1. **Core/Contracts/IIoStatusMonitorOptionsStore.cs** - 配置存储接口
2. **Infrastructure/Configs/Entities/IoStatusMonitorOptionsDoc.cs** - LiteDB 文档实体
3. **Infrastructure/Persistence/LiteDbIoStatusMonitorOptionsStore.cs** - LiteDB 存储实现
4. **Host/Controllers/IoMonitorController.cs** - API 控制器
5. **Tests/IoStatusMonitorStoreTests.cs** - 持久化存储测试

### 修改文件

1. **Infrastructure/Configs/Mappings/ConfigMappings.cs** - 添加配置映射方法
2. **Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs** - 添加服务注册
3. **Host/Workers/IoStatusWorker.cs** - 从 IOptionsMonitor 切换到直接读取数据库
4. **Host/Program.cs** - 更新服务注册，从数据库读取配置
5. **Host/appsettings.json** - 移除 IoStatusMonitor 配置节（现在使用数据库）

### 架构优势

1. **解耦**: 配置存储与应用逻辑分离
2. **灵活**: 支持运行时动态修改配置
3. **持久化**: 配置保存在数据库中，重启后自动恢复
4. **热更新**: 无需重启服务即可应用新配置
5. **可测试**: 使用内存数据库进行单元测试

## 注意事项

1. 配置更新会在下次轮询周期时自动应用（通常在 100-10000 毫秒内）
2. 如果禁用监控（Enabled = false），监控服务会在当前轮询完成后停止
3. appsettings.json 中不再包含 IoStatusMonitor 配置，实际配置以数据库为准
4. 首次启动时，如果数据库中没有配置，会使用默认值（Enabled=true, 32 inputs, 32 outputs, 500ms 轮询）
5. 配置验证使用 DataAnnotations，确保所有值在合理范围内

## 兼容性

- 向后兼容: 现有的测试用例已更新
- 数据库: 使用现有的 LiteDB 数据库实例（singulation.db）
- API: 新增 API 端点，不影响现有功能
- 热更新: 配置更改在下次轮询周期自动生效，无需重启

## 与 LeadshineSafetyIo 配置迁移的对比

| 特性 | LeadshineSafetyIo | IoStatusMonitor |
|------|------------------|-----------------|
| 配置存储 | LiteDB | LiteDB |
| 热更新支持 | 是（需要 LeadshineSafetyIoModule 实例） | 是（自动在下次轮询生效） |
| API 端点 | /api/Safety/io-configs | /api/IoMonitor/configs |
| 默认启用 | 否（Enabled=false） | 是（Enabled=true） |
| 配置复杂度 | 较高（多个按键、逻辑反转） | 较低（IO 范围、轮询间隔） |

## 迁移步骤（从 appsettings.json）

如果您之前在 `appsettings.json` 中配置了 IoStatusMonitor，可以按以下步骤迁移：

1. **记录现有配置**: 在更新前，记录 `appsettings.json` 中的 IoStatusMonitor 配置
2. **更新代码**: 拉取最新代码并重新编译
3. **启动服务**: 服务会使用默认配置启动
4. **通过 API 更新**: 使用 PUT `/api/IoMonitor/configs` 设置您之前的配置
5. **验证**: 通过 GET `/api/IoMonitor/configs` 验证配置已正确保存

## 故障排查

### 配置未生效
- 检查配置是否已成功保存到数据库（通过 GET API）
- 查看日志确认 IoStatusWorker 是否正在运行
- 确认 Enabled 字段为 true

### 监控服务未启动
- 检查日志中是否有 "IO 状态监控已禁用" 消息
- 确认数据库配置中 Enabled 为 true
- 检查是否有异常导致服务停止

### API 调用失败
- 确认服务已启动并监听正确端口
- 检查请求体 JSON 格式是否正确
- 查看日志中的错误信息
