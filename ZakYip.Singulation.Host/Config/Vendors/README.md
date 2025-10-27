# 厂商配置目录说明

本目录用于存放不同厂商的专属配置文件，按照类别进行组织。

## 目录结构

```
Config/Vendors/
├── Axis/           # 轴控制厂商配置
│   ├── leadshine.json    # 雷赛运动控制器配置
│   └── [其他厂商].json
├── Protocol/       # 上游协议厂商配置
│   ├── guiwei.json       # 归位视觉协议配置
│   ├── huarary.json      # 华雷视觉协议配置
│   └── [其他厂商].json
└── Io/             # IO 厂商配置
    ├── leadshine.json    # 雷赛 IO 配置
    └── [其他厂商].json
```

## 配置文件规范

每个厂商配置文件应包含以下标准字段：

### 必需字段

- **Vendor**: 厂商名称（英文，首字母大写）
- **Category**: 配置类别（Axis/Protocol/Io）
- **Description**: 配置描述（中文）
- **Enabled**: 是否启用该厂商配置（可选，默认 true）

### 推荐字段

根据不同类别，包含相应的配置节点：

#### Axis（轴控制）配置示例：
```json
{
  "Vendor": "Leadshine",
  "Category": "Axis",
  "Description": "雷赛运动控制器配置",
  "BusConfig": { /* 总线配置 */ },
  "AxisConfig": { /* 轴配置 */ },
  "Communication": { /* 通信配置 */ },
  "Advanced": { /* 高级配置 */ }
}
```

#### Protocol（协议）配置示例：
```json
{
  "Vendor": "Huarary",
  "Category": "Protocol",
  "Description": "华雷视觉协议配置",
  "Enabled": true,
  "Transport": { /* 传输层配置 */ },
  "Protocol": { /* 协议配置 */ },
  "Features": { /* 功能特性 */ }
}
```

#### Io（输入输出）配置示例：
```json
{
  "Vendor": "Leadshine",
  "Category": "IO",
  "Description": "雷赛 IO 配置",
  "Enabled": true,
  "InputConfig": { /* 输入配置 */ },
  "OutputConfig": { /* 输出配置 */ },
  "Features": { /* 功能特性 */ },
  "Monitoring": { /* 监控配置 */ }
}
```

## 添加新厂商配置

1. 在对应类别目录下创建新的 JSON 文件
2. 文件名使用小写厂商名称，如 `siemens.json`
3. 按照上述规范填写配置字段
4. 在主配置文件 `appsettings.json` 中添加引用（如需要）
5. 更新相关文档

## 配置加载优先级

1. 专属厂商配置文件（此目录下的文件）
2. 主配置文件 `appsettings.json` 中的 `Vendors` 节点
3. 环境变量
4. 默认配置值

## 注意事项

- 配置文件应使用 UTF-8 编码
- 支持 JSON 注释（使用 `//` 或 `/* */`）
- 敏感信息（如密码）应使用环境变量或密钥管理工具
- 配置文件可包含特定于部署环境的覆盖配置

## 相关文档

- [VENDOR_STRUCTURE.md](../../../VENDOR_STRUCTURE.md) - 厂商代码结构文档
- [ARCHITECTURE.md](../../../docs/ARCHITECTURE.md) - 系统架构文档
