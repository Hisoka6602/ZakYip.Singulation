# 灯光电平配置功能

## 问题描述

原始问题：目前所有的灯的配置都反了，需要能配置是高或低电平等于亮灯。

## 解决方案

新增了灯光输出电平配置功能，允许用户配置高电平或低电平对应亮灯状态。

### 配置项

#### 全局配置
- `LightActiveLow` (bool, 默认: true)
  - `false` = 高电平亮灯
  - `true` = 低电平亮灯（默认）

#### 独立配置（可覆盖全局配置）
- `RedLightActiveLow` (bool?, 默认: null)
- `YellowLightActiveLow` (bool?, 默认: null)
- `GreenLightActiveLow` (bool?, 默认: null)
- `StartButtonLightActiveLow` (bool?, 默认: null)
- `StopButtonLightActiveLow` (bool?, 默认: null)
- `RemoteConnectionLightActiveLow` (bool?, 默认: null)

当独立配置为 `null` 时，使用 `LightActiveLow` 的全局配置。

## 使用示例

### 示例 1: 所有灯使用低电平亮灯（默认行为）

```json
{
  "lightActiveLow": true
}
```

或者不配置（默认就是 true）

### 示例 2: 所有灯使用高电平亮灯

```json
{
  "lightActiveLow": false
}
```

### 示例 3: 红灯使用高电平亮灯，其他灯使用低电平亮灯

```json
{
  "lightActiveLow": true,
  "redLightActiveLow": false,
  "yellowLightActiveLow": null,
  "greenLightActiveLow": null,
  "startButtonLightActiveLow": null,
  "stopButtonLightActiveLow": null
}
```

### 示例 4: 混合配置

```json
{
  "lightActiveLow": false,
  "redLightActiveLow": true,
  "yellowLightActiveLow": false,
  "greenLightActiveLow": true
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
// activeLow = false: on=true → state=1 (高电平亮灯)
// activeLow = true:  on=true → state=0 (低电平亮灯)
ushort state = (on ^ activeLow) ? (ushort)1 : (ushort)0;
```

### 真值表

| on (亮灯) | activeLow | state (输出电平) |
|----------|-----------|-----------------|
| true     | false     | 1 (高电平)      |
| true     | true      | 0 (低电平)      |
| false    | false     | 0 (低电平)      |
| false    | true      | 1 (高电平)      |

## API 端点

### 查询配置

```bash
GET /api/Cabinet/io-configs
```

响应示例：
```json
{
  "success": true,
  "data": {
    "enabled": true,
    "cabinetIndicatorPoint": {
      "redLight": 10,
      "yellowLight": 11,
      "greenLight": 12,
      "startButtonLight": 13,
      "stopButtonLight": 14,
      "remoteConnectionLight": 15,
      "lightActiveLow": true,
      "redLightActiveLow": false,
      "yellowLightActiveLow": null,
      "greenLightActiveLow": null,
      "startButtonLightActiveLow": null,
      "stopButtonLightActiveLow": null,
      "remoteConnectionLightActiveLow": null
    }
  }
}
```

### 更新配置

```bash
PUT /api/Cabinet/io-configs
Content-Type: application/json

{
  "enabled": true,
  "cabinetIndicatorPoint": {
    "redLight": 10,
    "yellowLight": 11,
    "greenLight": 12,
    "startButtonLight": 13,
    "stopButtonLight": 14,
    "remoteConnectionLight": 15,
    "lightActiveLow": true
  }
}
```

## 测试

新增了以下测试用例：

1. `DefaultValuesAreCorrect` - 验证默认值正确
2. `IndividualInvertLogicOverridesGlobal_IndicatorPoint` - 验证独立配置覆盖全局配置

## 兼容性

- **重大变更**: 从 `InvertLightLogic` 重命名为 `LightActiveLow`，更清晰地表达配置含义
- **默认值变更**: 默认 `LightActiveLow = true` （低电平亮灯）
- **热更新**: 支持通过 API 动态更新配置，无需重启服务
- **持久化**: 配置保存在 LiteDB 数据库中

## 修改文件列表

1. `ZakYip.Singulation.Core/Configs/CabinetIndicatorPoint.cs` - 更新配置属性名称
2. `ZakYip.Singulation.Infrastructure/Services/IndicatorLightService.cs` - 更新实现逻辑
3. `ZakYip.Singulation.Infrastructure/Configs/Entities/LeadshineCabinetIoOptionsDoc.cs` - 更新持久化实体
4. `ZakYip.Singulation.Infrastructure/Configs/Mappings/ConfigMappings.cs` - 更新映射配置
5. `ZakYip.Singulation.Tests/LeadshineCabinetIoOptionsTests.cs` - 更新测试
6. `ZakYip.Singulation.Tests/LeadshineCabinetIoStoreTests.cs` - 更新测试
7. `CABINET_CONTROLLER_MIGRATION.md` - 更新迁移指南
