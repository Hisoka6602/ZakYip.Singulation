# ZakYip.Singulation 更新历史

本文件记录项目的历史更新。最近3次更新请查看 [README.md](README.md)。

---

## 🎯 历史更新（2025-11-02）

### ✅ 2025-11-02 重要Bug修复和功能改进

本次更新修复了多个关键问题，提升了系统的可靠性和准确性：

#### 1. **速度联动使用目标速度（仅远程模式）** ⚡
- **说明**：速度联动服务使用目标速度(`TargetSpeedsMmps`)进行判断
- **原因**：需要基于上游下发的目标速度来触发IO联动，而不是实际反馈速度
- **影响**：当上游将目标速度设置为0时，IO联动立即触发，无需等待轴实际停止
- **模式限制**：**速度联动仅在远程模式下生效，本地模式下不触发**
- **相关文件**：`SpeedLinkageService.cs`

#### 2. **实时速度反馈API** 📊
- **验证**：确认`LastFeedbackMmps`在`PingAsync`方法中正确更新
- **影响**：`GET /api/Axes/axes` API能够正确返回轴的实时反馈速度
- **相关字段**：`FeedbackLinearMmps` (mm/s)

#### 3. **轴使能/失能状态验证** 🔍
- **改进**：在使能/失能轴后，读取ControlWord验证状态转换是否成功
- **目的**：确保状态机转换成功完成，如果验证失败则通过Polly重试
- **实现**：
  - `EnableAsync`：写入EnableOperation后验证bit0-3是否都为1
  - `DisableAsync`：写入Shutdown后验证bit3 (EnableOperation)是否为0
  - 验证失败时抛出异常，触发Polly重试机制（最多3次）
- **相关文件**：`LeadshineLtdmcAxisDrive.cs`

#### 4. **LeadshineProtocolMap注释完善** 📝
- **改进**：为所有协议映射字段添加详细的XML文档注释
- **覆盖范围**：
  - `BitLen` 类：所有位宽常量
  - `ControlWord` 类：所有控制字命令及其位定义
  - `Mode` 类：所有操作模式值
  - `DelayMs` 类：所有延时参数及其用途
- **影响**：提高代码可读性和可维护性
- **相关文件**：`LeadshineProtocolMap.cs`

#### 5. **运行预警时间持久化修复** 🐛
- **问题**：`RunningWarningSeconds`（运行预警秒数）设置后无法保存到数据库
- **原因**：数据库文档实体和映射方法中缺少该字段
- **修复**：
  - 在`CabinetIndicatorPointDoc`中添加`RunningWarningSeconds`属性
  - 更新`ToOptions`和`ToDoc`映射方法包含该字段
- **影响**：运行预警时间现在能够正确保存和读取
- **相关文件**：
  - `LeadshineCabinetIoOptionsDoc.cs`
  - `ConfigMappings.cs`

---

### ✅ 速度联动配置功能

**核心功能**：新增基于轴速度变化自动控制IO端口的速度联动配置功能

#### 1. 功能特性 ✅
- **多组联动**：支持配置多个独立的速度联动组
- **灵活配置**：每组可包含多个轴ID和多个IO端口
- **自动触发**：
  - 当组内所有轴速度从非0降到0时，自动将指定IO设置为配置的电平
  - 当组内所有轴速度从0提升到非0时，自动将指定IO设置为相反电平
- **实时监控**：后台服务每100ms检查一次轴速度状态
- **持久化存储**：配置保存在LiteDB数据库中
- **REST API**：提供完整的查询、更新、删除配置接口

#### 2. 使用示例

**配置示例**：
```json
PUT /api/io-linkage/speed/configs
{
  "enabled": true,
  "linkageGroups": [
    {
      "axisIds": [1001, 1002],
      "ioPoints": [
        { "bitNumber": 3, "levelWhenStopped": 0 },
        { "bitNumber": 4, "levelWhenStopped": 0 }
      ]
    },
    {
      "axisIds": [1003, 1004],
      "ioPoints": [
        { "bitNumber": 5, "levelWhenStopped": 0 },
        { "bitNumber": 6, "levelWhenStopped": 0 }
      ]
    }
  ]
}
```

**工作原理**：
- 第一组：当轴1001和1002都停止时，IO 3和4设为高电平；当任一轴运动时，IO 3和4设为低电平
- 第二组：当轴1003和1004都停止时，IO 5和6设为高电平；当任一轴运动时，IO 5和6设为低电平

#### 3. API 端点
- `GET /api/io-linkage/speed/configs` - 获取速度联动配置
- `PUT /api/io-linkage/speed/configs` - 更新速度联动配置
- `DELETE /api/io-linkage/speed/configs` - 删除速度联动配置

#### 4. 技术实现
- **配置模型**：`SpeedLinkageOptions`, `SpeedLinkageGroup`, `SpeedLinkageIoPoint`
- **存储层**：`LiteDbSpeedLinkageOptionsStore` - 基于LiteDB的持久化存储
- **服务层**：`SpeedLinkageService` - 后台服务监控轴速度变化
- **控制器**：`SpeedLinkageController` - REST API控制器
- **单元测试**：完整的配置和存储测试覆盖

---

*更多历史更新请查看 Git 提交历史*
