# 灯光逻辑反转配置功能

## 问题描述

原始问题：目前所有的灯的配置都反了，需要能配置是高或低电平等于亮灯。

## 解决方案

新增了灯光输出逻辑反转配置功能，允许用户配置高电平或低电平对应亮灯状态。

### 配置项

#### 全局配置
- `InvertLightLogic` (bool, 默认: false)
  - `false` = 高电平亮灯（默认）
  - `true` = 低电平亮灯

#### 独立配置（可覆盖全局配置）
- `InvertRedLightLogic` (bool?, 默认: null)
- `InvertYellowLightLogic` (bool?, 默认: null)
- `InvertGreenLightLogic` (bool?, 默认: null)
- `InvertStartButtonLightLogic` (bool?, 默认: null)
- `InvertStopButtonLightLogic` (bool?, 默认: null)

当独立配置为 `null` 时，使用 `InvertLightLogic` 的全局配置。

## 使用示例

### 示例 1: 所有灯使用高电平亮灯（默认行为）

```json
{
  "invertLightLogic": false
}
```

或者不配置（默认就是 false）

### 示例 2: 所有灯使用低电平亮灯

```json
{
  "invertLightLogic": true
}
```

### 示例 3: 红灯使用低电平亮灯，其他灯使用高电平亮灯

```json
{
  "invertLightLogic": false,
  "invertRedLightLogic": true,
  "invertYellowLightLogic": null,
  "invertGreenLightLogic": null,
  "invertStartButtonLightLogic": null,
  "invertStopButtonLightLogic": null
}
```

### 示例 4: 混合配置

```json
{
  "invertLightLogic": false,
  "invertRedLightLogic": true,
  "invertYellowLightLogic": false,
  "invertGreenLightLogic": true
}
```

在这个例子中：
- 红灯：低电平亮灯
- 黄灯：高电平亮灯（显式指定）
- 绿灯：低电平亮灯
- 启动按钮灯：高电平亮灯（使用全局默认）
- 停止按钮灯：高电平亮灯（使用全局默认）

## 实现逻辑

在 `IndicatorLightService.SetLightAsync()` 方法中：

```csharp
// invertLogic = false: on=true → state=1 (高电平亮灯)
// invertLogic = true:  on=true → state=0 (低电平亮灯)
ushort state = (on != invertLogic) ? (ushort)1 : (ushort)0;
```

### 真值表

| on (亮灯) | invertLogic | state (输出电平) |
|----------|-------------|-----------------|
| true     | false       | 1 (高电平)      |
| true     | true        | 0 (低电平)      |
| false    | false       | 0 (低电平)      |
| false    | true        | 1 (高电平)      |

## API 端点

### 查询配置

```bash
GET /api/SafetyIo/configs
```

响应示例：
```json
{
  "success": true,
  "data": {
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
}
```

### 更新配置

```bash
PUT /api/SafetyIo/configs
Content-Type: application/json

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

## 测试

新增了以下测试用例：

1. `DefaultValuesAreCorrect` - 验证默认值正确
2. `LightInvertLogicConfiguration` - 验证灯光反转逻辑配置
3. `AllLightsInvertedConfiguration` - 验证所有灯使用低电平亮灯配置

## 兼容性

- **向后兼容**: 默认 `InvertLightLogic = false` 保持现有行为（高电平亮灯）
- **热更新**: 支持通过 API 动态更新配置，无需重启服务
- **持久化**: 配置保存在 LiteDB 数据库中

## 修改文件列表

1. `ZakYip.Singulation.Core/Configs/LeadshineSafetyIoOptions.cs` - 新增配置属性
2. `ZakYip.Singulation.Host/Services/IndicatorLightService.cs` - 实现反转逻辑
3. `ZakYip.Singulation.Infrastructure/Configs/Entities/LeadshineSafetyIoOptionsDoc.cs` - 持久化实体
4. `ZakYip.Singulation.Infrastructure/Configs/Mappings/ConfigMappings.cs` - 映射配置
5. `ZakYip.Singulation.Tests/LeadshineSafetyIoOptionsTests.cs` - 新增测试
6. `README.md` - 更新文档
7. `LEADSHINE_SAFETY_IO_CONFIG_MIGRATION.md` - 更新迁移指南
