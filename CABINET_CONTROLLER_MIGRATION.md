# Cabinet Controller Migration Guide

## 概述 (Overview)

本指南说明如何从旧的 `SafetyController` 迁移到新的 `CabinetController`，以及配置结构的变更。

This guide explains how to migrate from the old `SafetyController` to the new `CabinetController` and the configuration structure changes.

## API 端点变更 (API Endpoint Changes)

### 命令接口 (Command Interface)

**旧端点 (Old):**
```
POST /api/Safety/commands
```

**新端点 (New):**
```
POST /api/Cabinet/commands
```

**请求体保持不变 (Request body unchanged):**
```json
{
  "Command": 1,  // 1=Start, 2=Stop, 3=Reset, 4=EmergencyStop
  "Reason": "用户触发"
}
```

### 配置接口 (Configuration Interface)

**旧端点 (Old):**
```
GET /api/Safety/io-configs
PUT /api/Safety/io-configs
```

**新端点 (New):**
```
GET /api/Cabinet/io-configs
PUT /api/Cabinet/io-configs
```

## 配置结构变更 (Configuration Structure Changes)

### 旧配置结构 (Old Configuration - Flat)

```json
{
  "Enabled": true,
  "EmergencyStopBit": 4,
  "StopBit": 2,
  "StartBit": 1,
  "ResetBit": 3,
  "RemoteLocalModeBit": 5,
  "PollingIntervalMs": 50,
  "InvertLogic": false,
  "InvertEmergencyStopLogic": null,
  "RedLightBit": 10,
  "YellowLightBit": 11,
  "GreenLightBit": 12,
  "StartButtonLightBit": 13,
  "StopButtonLightBit": 14,
  "InvertLightLogic": true
}
```

### 新配置结构 (New Configuration - Nested)

```json
{
  "Enabled": true,
  "PollingIntervalMs": 50,
  "CabinetInputPoint": {
    "EmergencyStop": 4,
    "Stop": 2,
    "Start": 1,
    "Reset": 3,
    "RemoteLocalMode": 5,
    "InvertLogic": false,
    "InvertEmergencyStopLogic": null,
    "InvertStopLogic": null,
    "InvertStartLogic": null,
    "InvertResetLogic": null,
    "InvertRemoteLocalLogic": null,
    "RemoteLocalActiveHigh": true
  },
  "CabinetIndicatorPoint": {
    "RedLight": 10,
    "YellowLight": 11,
    "GreenLight": 12,
    "StartButtonLight": 13,
    "StopButtonLight": 14,
    "RemoteConnectionLight": 15,
    "InvertLightLogic": true,
    "InvertRedLightLogic": null,
    "InvertYellowLightLogic": null,
    "InvertGreenLightLogic": null,
    "InvertStartButtonLightLogic": null,
    "InvertStopButtonLightLogic": null,
    "InvertRemoteConnectionLightLogic": null
  }
}
```

## 迁移步骤 (Migration Steps)

### 1. 更新客户端代码 (Update Client Code)

更新所有调用 Safety API 的代码，将端点从 `/api/Safety` 改为 `/api/Cabinet`。

Update all code calling the Safety API, changing endpoints from `/api/Safety` to `/api/Cabinet`.

**示例 (Example):**

```javascript
// 旧代码 (Old)
fetch('/api/Safety/commands', {
  method: 'POST',
  body: JSON.stringify({ Command: 1, Reason: 'Start' })
});

// 新代码 (New)
fetch('/api/Cabinet/commands', {
  method: 'POST',
  body: JSON.stringify({ Command: 1, Reason: 'Start' })
});
```

### 2. 迁移配置数据 (Migrate Configuration Data)

如果您有现有的配置数据，需要将其转换为新的嵌套结构。

If you have existing configuration data, you need to convert it to the new nested structure.

**转换脚本示例 (Conversion Script Example):**

```javascript
function migrateConfig(oldConfig) {
  return {
    Enabled: oldConfig.Enabled,
    PollingIntervalMs: oldConfig.PollingIntervalMs,
    CabinetInputPoint: {
      EmergencyStop: oldConfig.EmergencyStopBit,
      Stop: oldConfig.StopBit,
      Start: oldConfig.StartBit,
      Reset: oldConfig.ResetBit,
      RemoteLocalMode: oldConfig.RemoteLocalModeBit,
      InvertLogic: oldConfig.InvertLogic,
      InvertEmergencyStopLogic: oldConfig.InvertEmergencyStopLogic,
      InvertStopLogic: oldConfig.InvertStopLogic,
      InvertStartLogic: oldConfig.InvertStartLogic,
      InvertResetLogic: oldConfig.InvertResetLogic,
      InvertRemoteLocalLogic: oldConfig.InvertRemoteLocalLogic,
      RemoteLocalActiveHigh: oldConfig.RemoteLocalActiveHigh
    },
    CabinetIndicatorPoint: {
      RedLight: oldConfig.RedLightBit,
      YellowLight: oldConfig.YellowLightBit,
      GreenLight: oldConfig.GreenLightBit,
      StartButtonLight: oldConfig.StartButtonLightBit,
      StopButtonLight: oldConfig.StopButtonLightBit,
      RemoteConnectionLight: oldConfig.RemoteConnectionLightBit,
      InvertLightLogic: oldConfig.InvertLightLogic,
      InvertRedLightLogic: oldConfig.InvertRedLightLogic,
      InvertYellowLightLogic: oldConfig.InvertYellowLightLogic,
      InvertGreenLightLogic: oldConfig.InvertGreenLightLogic,
      InvertStartButtonLightLogic: oldConfig.InvertStartButtonLightLogic,
      InvertStopButtonLightLogic: oldConfig.InvertStopButtonLightLogic,
      InvertRemoteConnectionLightLogic: oldConfig.InvertRemoteConnectionLightLogic
    }
  };
}
```

### 3. 向后兼容 (Backward Compatibility)

在迁移期间，旧的 `/api/Safety` 端点仍然可用。系统会优先使用新的 Cabinet 配置，如果未启用则回退到旧的 Safety 配置。

During migration, the old `/api/Safety` endpoint is still available. The system will prioritize the new Cabinet configuration and fall back to the old Safety configuration if not enabled.

## 配置优势 (Configuration Advantages)

### 旧结构问题 (Old Structure Issues)
- 所有配置项都在同一级别，难以区分输入和输出
- 属性名称带有 "Bit" 后缀，不够直观
- 缺乏组织结构

### 新结构优势 (New Structure Benefits)
- ✅ 清晰的二级分类：输入点位和指示灯点位
- ✅ 更直观的属性命名（去掉 "Bit" 后缀）
- ✅ 更好的可维护性和可读性
- ✅ 便于扩展新的点位类型

## 常见问题 (FAQ)

### Q: 旧的 Safety API 什么时候会被移除？
**A:** 旧的 API 将在 v2.0.0 版本中被移除。建议尽快迁移到新的 Cabinet API。

### Q: 数据库中的旧配置会自动迁移吗？
**A:** 不会。您需要手动读取旧配置，转换为新格式，然后保存到新的 Cabinet 配置中。

### Q: 可以同时使用旧的和新的配置吗？
**A:** 系统会优先使用新的 Cabinet 配置。如果新配置未启用，则会尝试使用旧配置。

### Q: 配置的功能有变化吗？
**A:** 没有。功能完全相同，只是结构更清晰了。

## 技术支持 (Technical Support)

如有问题，请联系开发团队或提交 Issue。

For questions, please contact the development team or submit an Issue.
