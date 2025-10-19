# ZakYip.Singulation MAUI App 文档

## 概述

ZakYip.Singulation.MauiApp 是一个跨平台移动应用，用于远程控制和监控 ZakYip.Singulation 系统。该应用采用 MVVM (Model-View-ViewModel) 架构模式，使用 .NET MAUI 8.0 框架开发。

## 功能特性

### 1. MVVM 架构
- **ViewModels**: 使用 CommunityToolkit.Mvvm 实现响应式数据绑定
  - `MainViewModel`: 主控制台页面
  - `SettingsViewModel`: 设置页面
- **Views**: XAML 声明式 UI
  - `MainPage`: 主控制台界面
  - `SettingsPage`: 应用设置界面
- **Services**: 业务逻辑服务
  - `ApiClient`: REST API 客户端
  - `SignalRClientFactory`: SignalR 实时连接工厂

### 2. API 集成
应用通过 HTTP REST API 与 ZakYip.Singulation.Host 服务通信，支持以下功能：

#### 控制器管理
- **获取轴列表**: `GET /api/axes/axes`
- **获取控制器状态**: `GET /api/axes/controller`
- **刷新控制器列表**: 显示所有轴的状态、速度和错误信息

#### 安全命令
- **发送安全命令**: `POST /api/safety/commands`
  - Start (启动): Command=1
  - Stop (停止): Command=2
  - Reset (复位): Command=3
- **命令原因记录**: 每个命令可附带原因说明

#### 轴控制
- **使能所有轴**: `POST /api/axes/axes/enable`
- **禁用所有轴**: `POST /api/axes/axes/disable`
- **设置轴速度**: `POST /api/axes/axes/speed`
  - 支持设置线速度 (mm/s)
  - 可批量控制所有轴或指定轴

#### 实时通信
- **SignalR 连接**: `/hubs/events`
- **实时事件推送**: 接收系统状态变化通知
- **自动重连**: 断线后自动尝试重连

### 3. UI 界面设计

#### 主控制台页面 (MainPage)
- **状态信息栏**: 显示当前操作状态和消息
- **加载指示器**: 异步操作时显示加载动画
- **控制器列表**: 
  - 以卡片形式展示每个轴的信息
  - 显示轴 ID、状态、使能状态
  - 实时更新数据
- **安全命令区域**:
  - 命令类型选择器 (Start/Stop/Reset)
  - 原因输入框
  - 发送按钮
- **轴控制区域**:
  - 使能/禁用所有轴按钮
  - 目标速度输入
  - 设置速度按钮
- **SignalR 连接按钮**: 建立实时通信连接

#### 设置页面 (SettingsPage)
- **API 配置**:
  - API 基础地址设置
  - 连接超时设置
  - 保存按钮
  - 状态反馈
- **应用信息**:
  - 版本号
  - 平台信息
  - 开发团队

### 4. 数据持久化
- 使用 `Preferences` API 保存用户设置
- 支持的设置项:
  - `ApiBaseUrl`: API 基础地址
  - `TimeoutSeconds`: 连接超时时间

## 技术栈

- **.NET MAUI 8.0**: 跨平台应用框架
- **CommunityToolkit.Mvvm 8.3.2**: MVVM 工具包
- **Microsoft.AspNetCore.SignalR.Client 8.0.11**: SignalR 客户端
- **Microsoft.Extensions.Http 8.0.1**: HTTP 客户端扩展
- **System.Text.Json**: JSON 序列化/反序列化

## 构建和部署

### 前置要求
- .NET 8.0 SDK
- MAUI 工作负载: `dotnet workload install maui-android`
- (可选) Visual Studio 2022 或 VS Code

### 构建 Debug 版本

```bash
cd ZakYip.Singulation.MauiApp

# Android
dotnet build -f net8.0-android

# Windows (需在 Windows 上)
dotnet build -f net8.0-windows10.0.19041.0
```

### 构建 Release 版本并打包

#### Android APK
```bash
# 发布并生成 APK (推荐)
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk

# 输出文件位置
# bin/Release/net8.0-android/publish/*.apk
```

#### Android AAB (Google Play)
```bash
# 发布并生成 AAB
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=aab

# 输出文件位置
# bin/Release/net8.0-android/publish/*.aab
```

### APK 签名说明

默认情况下，Debug 和 Release 构建都会生成以下文件：
- `*.apk`: 未签名的 APK
- `*-Signed.apk`: 使用调试证书签名的 APK

**生产环境部署建议**:
1. 使用正式的密钥库签名
2. 在 `csproj` 中配置签名属性:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidKeyStore>true</AndroidKeyStore>
    <AndroidSigningKeyStore>your-keystore.keystore</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>your-key-alias</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass>your-key-password</AndroidSigningKeyPass>
    <AndroidSigningStorePass>your-store-password</AndroidSigningStorePass>
</PropertyGroup>
```

### 安装和运行

#### Android 设备
1. 启用开发者选项和 USB 调试
2. 连接设备到电脑
3. 安装 APK:
```bash
adb install bin/Release/net8.0-android/publish/com.companyname.zakyip.singulation.mauiapp-Signed.apk
```

#### Android 模拟器
```bash
# 启动模拟器
emulator -avd <avd_name>

# 安装 APK
adb install bin/Release/net8.0-android/publish/com.companyname.zakyip.singulation.mauiapp-Signed.apk
```

## 配置和使用

### 初次使用配置

1. **打开应用**: 启动 ZakYip.Singulation.MauiApp
2. **进入设置页面**: 点击底部 "设置" 选项卡
3. **配置 API 地址**:
   - 输入服务器地址，例如: `http://192.168.1.100:5000`
   - 设置连接超时 (默认 30 秒)
   - 点击 "保存设置"
4. **重启应用**: 使配置生效

### 主要操作流程

#### 1. 查看轴状态
- 在主控制台页面点击 "Refresh Controllers"
- 查看轴列表中每个轴的状态信息

#### 2. 控制轴运行
1. **使能轴**: 点击 "Enable All" 按钮
2. **设置速度**: 
   - 在 "Target Speed" 输入框输入速度值 (mm/s)
   - 点击 "Set Speed" 按钮
3. **禁用轴**: 点击 "Disable All" 按钮

#### 3. 发送安全命令
1. 选择命令类型 (Start/Stop/Reset)
2. 输入执行原因
3. 点击 "Send Safety Command"

#### 4. 连接实时通知
- 点击 "Connect to SignalR" 建立实时连接
- 连接成功后可接收系统事件通知

## API 响应格式

所有 API 响应使用统一格式:
```json
{
  "Result": true,
  "Msg": "操作成功",
  "Data": { /* 响应数据 */ }
}
```

### 字段说明
- `Result` (bool): 操作是否成功
- `Msg` (string): 响应消息
- `Data` (object): 响应数据，类型根据 API 而定

## 数据模型

### ControllerInfo (轴信息)
```csharp
public class ControllerInfo
{
    public string AxisId { get; set; }        // 轴 ID
    public int Status { get; set; }           // 状态枚举
    public double? TargetLinearMmps { get; set; }    // 目标速度
    public double? FeedbackLinearMmps { get; set; }  // 反馈速度
    public bool? Enabled { get; set; }        // 是否使能
    public int? LastErrorCode { get; set; }   // 错误码
    public string? LastErrorMessage { get; set; }    // 错误信息
}
```

### ControllerStatus (控制器状态)
```csharp
public class ControllerStatus
{
    public int AxisCount { get; set; }        // 轴数量
    public int ErrorCode { get; set; }        // 错误码
    public bool Initialized { get; set; }     // 是否初始化
}
```

### SafetyCommandRequest (安全命令请求)
```csharp
public class SafetyCommandRequest
{
    public int Command { get; set; }          // 1=Start, 2=Stop, 3=Reset
    public string? Reason { get; set; }       // 命令原因
}
```

## 故障排查

### 常见问题

#### 1. 无法连接到服务器
**症状**: 刷新控制器时显示连接错误

**解决方案**:
- 检查 API 地址是否正确
- 确认服务器正在运行
- 检查网络连接
- 验证防火墙设置

#### 2. SignalR 连接失败
**症状**: 点击 "Connect to SignalR" 后显示连接失败

**解决方案**:
- 确认服务器 SignalR Hub 已启用
- 检查 `/hubs/events` 端点是否可访问
- 查看服务器日志

#### 3. 命令执行失败
**症状**: 发送命令后返回错误

**解决方案**:
- 检查控制器是否已初始化
- 确认轴状态正常
- 查看错误消息获取详细信息

## 开发和调试

### 调试模式运行
```bash
# Android
dotnet build -f net8.0-android
dotnet run -f net8.0-android

# Windows
dotnet build -f net8.0-windows10.0.19041.0
dotnet run -f net8.0-windows10.0.19041.0
```

### 查看应用日志
```bash
# Android 实时日志
adb logcat | grep -i "ZakYip"
```

### 热重载
在 Visual Studio 或 VS Code 中使用 XAML 热重载功能，可实时预览 UI 更改。

## 扩展功能建议

### 1. 增强功能
- [ ] 添加轴状态实时图表
- [ ] 实现单个轴的独立控制
- [ ] 添加命令历史记录
- [ ] 实现用户认证和授权
- [ ] 支持多语言 (中文/英文)
- [ ] 添加深色主题

### 2. UI 改进
- [ ] 自定义应用图标和启动画面
- [ ] 添加动画效果
- [ ] 优化平板电脑布局
- [ ] 添加手势操作

### 3. 功能增强
- [ ] 离线模式支持
- [ ] 数据缓存机制
- [ ] 批量操作确认对话框
- [ ] 导出日志功能

## 许可证

与主项目保持一致

## 贡献

欢迎提交问题和拉取请求！

## 联系方式

- 项目仓库: https://github.com/Hisoka6602/ZakYip.Singulation
- 问题反馈: 在 GitHub 上创建 Issue
