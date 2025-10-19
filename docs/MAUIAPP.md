# ZakYip.Singulation MAUI App 文档

## 概述

ZakYip.Singulation.MauiApp 是一个跨平台移动应用，用于远程控制和监控 ZakYip.Singulation 系统。该应用采用 MVVM (Model-View-ViewModel) 架构模式，使用 .NET MAUI 8.0 框架开发。

## 功能特性

### 1. UDP 服务自动发现（新增）
- **自动发现服务**: 通过 UDP 广播自动发现同一网络中的 Singulation 服务
- **无需手动配置**: 连接到同一 WiFi/网络后即可自动发现服务
- **实时服务列表**: 显示所有发现的服务及其状态
- **一键连接**: 点击发现的服务即可自动配置并连接
- **服务超时检测**: 自动检测和移除失联的服务（10 秒超时）

### 2. MVVM 架构
- **ViewModels**: 使用 Prism.Mvvm 实现响应式数据绑定
  - `MainViewModel`: 主控制台页面
  - `SettingsViewModel`: 设置页面（支持 UDP 服务发现）
- **Views**: XAML 声明式 UI，采用扁平化设计
  - `MainPage`: 主控制台界面（图标+文字设计）
  - `SettingsPage`: 应用设置界面（集成服务发现）
- **Services**: 业务逻辑服务（清晰分层）
  - `ApiClient`: REST API 客户端（保留兼容性）
  - `AxisApiService`: 轴管理 API 服务
  - `ControllerApiService`: 控制器管理 API 服务
  - `DecoderApiService`: 解码器 API 服务
  - `UpstreamApiService`: 上游通信 API 服务
  - `SystemApiService`: 系统会话 API 服务
  - `SafetyApiService`: 安全管线 API 服务
  - `UdpDiscoveryClient`: UDP 服务发现客户端
  - `SignalRClientFactory`: SignalR 实时连接工厂

### 3. 完整的 API 集成（已扩展）
应用通过 HTTP REST API 与 ZakYip.Singulation.Host 服务通信，支持以下功能：

#### 控制器管理
- **获取轴列表**: `GET /api/axes/axes`
- **获取指定轴信息**: `GET /api/axes/axes/{id}`
- **获取控制器状态**: `GET /api/axes/controller`
- **获取控制器选项**: `GET /api/axes/controller/options`
- **更新控制器选项**: `PUT /api/axes/controller/options`
- **复位控制器**: `POST /api/axes/controller/reset` (支持 soft/hard)
- **获取控制器错误**: `GET /api/axes/controller/errors`
- **清除控制器错误**: `DELETE /api/axes/controller/errors`

#### 轴拓扑管理
- **获取轴拓扑布局**: `GET /api/axes/topology`
- **更新轴拓扑布局**: `PUT /api/axes/topology`
- **删除轴拓扑布局**: `DELETE /api/axes/topology`

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

#### 解码器服务
- **解码器健康检查**: `GET /api/decoder/health`
- **获取解码器配置**: `GET /api/decoder/options`
- **更新解码器配置**: `PUT /api/decoder/options`
- **提交帧数据解码**: `POST /api/decoder/frames`

#### 上游通信
- **获取上游配置**: `GET /api/upstream/configuration`
- **更新上游配置**: `PUT /api/upstream/configuration`
- **获取上游连接状态**: `GET /api/upstream/status`

#### 系统会话
- **删除运行会话**: `DELETE /api/system/session` (触发宿主退出)

#### 实时通信
- **SignalR 连接**: `/hubs/events`
- **实时事件推送**: 接收系统状态变化通知
- **自动重连**: 断线后自动尝试重连

### 4. 扁平化 UI 设计（全新）

#### 主控制台页面 (MainPage)
- **标题**: 🎯 Singulation 控制台
- **状态信息栏**: 浅蓝色背景，显示当前操作状态和消息
- **加载指示器**: 异步操作时显示加载动画
- **控制器管理区域** (灰色背景):
  - 🔄 刷新控制器按钮（绿色）
  - 轴列表卡片显示：
    - 🔧 轴名称
    - 🆔 轴 ID
    - 📈 状态（中文化）
    - ⚡ 使能状态
  - CollectionView 支持空状态提示
- **安全命令区域** (橙黄色背景):
  - 🛡️ 安全命令标题
  - 命令类型选择器
  - 原因输入框
  - 📤 发送按钮（橙色）
- **轴控制区域** (绿色背景):
  - ⚙️ 轴控制标题
  - ✅ 全部使能按钮（绿色）
  - ❌ 全部禁用按钮（红色）
  - 🚀 目标速度输入
  - ⚡ 设置速度按钮（蓝色）
- **SignalR 连接区域** (紫色背景):
  - 📡 实时连接标题
  - 🔗 连接按钮（紫色）

#### 设置页面 (SettingsPage)
- **UDP 服务发现区域** (灰色背景):
  - 🔍 自动发现服务标题
  - 开始/停止发现按钮（动态颜色）
  - 发现的服务列表（卡片式）
  - 点击服务自动连接
  - 空状态提示和使用说明
- **API 配置区域** (灰色背景):
  - 🔗 API 配置标题
  - API 基础地址输入
  - 连接超时设置
  - 💾 保存设置按钮（绿色）
  - 状态反馈
- **应用信息区域** (灰色背景):
  - ℹ️ 应用信息标题
  - 版本号
  - 平台信息
  - 项目名称

### 5. 数据持久化
- 使用 `Preferences` API 保存用户设置
- 支持的设置项:
  - `ApiBaseUrl`: API 基础地址
  - `TimeoutSeconds`: 连接超时时间

## 技术栈

- **.NET MAUI 8.0**: 跨平台应用框架
- **Prism.Maui 9.0.537**: MVVM 框架和导航
- **Prism.DryIoc.Maui 9.0.537**: 依赖注入容器
- **Microsoft.AspNetCore.SignalR.Client 8.0.11**: SignalR 客户端
- **Microsoft.Extensions.Http 8.0.1**: HTTP 客户端扩展
- **Newtonsoft.Json 13.0.3**: JSON 序列化/反序列化

## 架构设计

### 分层架构

```
┌─────────────────────────────────────┐
│         Views (XAML)                │  UI 层
│  MainPage, SettingsPage             │
└─────────────┬───────────────────────┘
              │ Data Binding
┌─────────────▼───────────────────────┐
│       ViewModels                    │  表示层
│  MainViewModel, SettingsViewModel   │
└─────────────┬───────────────────────┘
              │ Service Calls
┌─────────────▼───────────────────────┐
│         Services                    │  服务层
│  AxisApiService                     │
│  ControllerApiService               │
│  DecoderApiService                  │
│  UpstreamApiService                 │
│  SystemApiService                   │
│  SafetyApiService                   │
│  UdpDiscoveryClient                 │
│  SignalRClientFactory               │
└─────────────┬───────────────────────┘
              │ HTTP/UDP/SignalR
┌─────────────▼───────────────────────┐
│      Network Layer                  │  网络层
│  HttpClient, UdpClient,             │
│  HubConnection                      │
└─────────────────────────────────────┘
```

### 设计原则

1. **单一职责原则** (SRP)
   - 每个服务类只负责特定的 API 组
   - ViewModel 只负责 UI 逻辑和状态管理
   - View 只负责 UI 呈现

2. **依赖注入** (DI)
   - 所有服务通过 Prism 容器注册
   - ViewModel 通过构造函数注入服务
   - 便于测试和替换实现

3. **开放封闭原则** (OCP)
   - 易于扩展新的 API 服务
   - 无需修改现有代码

4. **低耦合高内聚**
   - 服务之间相互独立
   - 通过接口通信
   - 易于维护和测试

### 数据流

```
用户操作 → View → ViewModel → Service → HTTP API → Host
                    ↓
                Observable
                Collection
                    ↓
                  View
                  更新
```

### 关键组件

#### 1. ViewModels
- 继承 `BindableBase` (Prism)
- 使用 `DelegateCommand` 绑定命令
- 使用 `ObservableCollection` 实现数据自动更新
- 负责 UI 状态管理和业务逻辑调用

#### 2. Services
- 独立的服务类，每个类负责特定功能域
- 统一使用 `ApiResponse<T>` 响应包装
- 异常处理和错误反馈
- HttpClient 通过依赖注入获取

#### 3. DTOs
- 数据传输对象，与 Host API 保持一致
- 包含便捷显示属性（如 StatusText）
- 支持中文化显示

#### 4. Converters
- `BoolToTextConverter`: 布尔值转文本
- `BoolToColorConverter`: 布尔值转颜色
- `InvertedBoolConverter`: 布尔值取反

## UDP 服务发现详解

### 工作原理

1. **Host 端广播**:
   - Host 服务每 3 秒通过 UDP 端口 18888 广播服务信息
   - 广播内容包括：服务名、版本、HTTP 端口、HTTPS 端口、SignalR 路径
   - 使用 JSON 格式序列化数据

2. **MauiApp 端监听**:
   - 监听 UDP 端口 18888
   - 接收并解析广播消息
   - 维护发现的服务列表
   - 自动检测服务超时（10 秒无响应则移除）

3. **服务连接**:
   - 用户在设置页面启动服务发现
   - 显示所有发现的服务
   - 点击服务自动填充 API 地址
   - 保存设置并重启应用生效

### 配置说明

#### Host 端配置 (appsettings.json)
```json
{
  "UdpDiscovery": {
    "Enabled": true,
    "BroadcastPort": 18888,
    "BroadcastIntervalSeconds": 3,
    "ServiceName": "Singulation Service",
    "HttpPort": 5005,
    "HttpsPort": 5006
  }
}
```

#### MauiApp 使用流程
1. 打开应用，进入设置页面
2. 点击"开始发现"按钮
3. 等待服务列表填充（通常 3 秒内）
4. 点击发现的服务
5. 点击"保存设置"
6. 重启应用使用新配置

### 网络要求
- 设备需连接到同一网络/WiFi
- 网络需支持 UDP 广播
- 防火墙需允许 UDP 端口 18888

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

#### 方式一：UDP 自动发现（推荐）
1. **确保网络连接**: 设备和 Host 服务连接到同一网络
2. **打开应用**: 启动 ZakYip.Singulation.MauiApp
3. **进入设置页面**: 点击底部 "设置" 选项卡
4. **启动服务发现**:
   - 点击 "🔍 开始发现" 按钮
   - 等待 3-5 秒，服务列表将自动填充
5. **选择服务**: 点击发现的服务卡片
6. **保存配置**: 
   - 检查 API 地址已自动填充
   - 点击 "💾 保存设置"
7. **重启应用**: 使配置生效

#### 方式二：手动配置
1. **打开应用**: 启动 ZakYip.Singulation.MauiApp
2. **进入设置页面**: 点击底部 "设置" 选项卡
3. **配置 API 地址**:
   - 输入服务器地址，例如: `http://192.168.1.100:5005`
   - 设置连接超时 (默认 30 秒)
   - 点击 "💾 保存设置"
4. **重启应用**: 使配置生效

### 主要操作流程

#### 1. 查看轴状态
- 在主控制台页面点击 "🔄 刷新控制器"
- 查看轴列表中每个轴的状态信息（中文化显示）
- 支持滚动查看所有轴

#### 2. 控制轴运行
1. **使能轴**: 点击 "✅ 全部使能" 按钮（绿色）
2. **设置速度**: 
   - 在 "🚀 目标速度" 输入框输入速度值 (mm/s)
   - 点击 "⚡ 设置速度" 按钮（蓝色）
3. **禁用轴**: 点击 "❌ 全部禁用" 按钮（红色）

#### 3. 发送安全命令
1. 选择命令类型 (Start/Stop/Reset)
2. 输入执行原因（可选）
3. 点击 "📤 发送安全命令"（橙色）
4. 查看状态栏反馈

#### 4. 连接实时通知
- 点击 "🔗 连接 SignalR" 建立实时连接（紫色）
- 连接成功后可接收系统事件通知
- 状态栏显示连接状态

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

### AxisInfo (轴信息)
```csharp
public class AxisInfo
{
    public string AxisId { get; set; }        // 轴 ID
    public int Status { get; set; }           // 状态枚举 (0-4)
    public double? TargetLinearMmps { get; set; }    // 目标速度
    public double? FeedbackLinearMmps { get; set; }  // 反馈速度
    public bool? Enabled { get; set; }        // 是否使能
    public int? LastErrorCode { get; set; }   // 错误码
    public string? LastErrorMessage { get; set; }    // 错误信息
    
    // 状态文本显示
    // 0=离线, 1=初始化中, 2=就绪, 3=运行中, 4=故障
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

#### 1. UDP 服务发现找不到服务
**症状**: 点击"开始发现"后服务列表为空

**解决方案**:
- 确认设备和 Host 服务在同一网络/WiFi
- 检查 Host 服务的 UDP 发现是否已启用（appsettings.json）
- 验证防火墙是否允许 UDP 端口 18888
- 检查网络是否支持 UDP 广播（某些企业网络可能禁用）
- 尝试手动配置 API 地址

#### 2. 无法连接到服务器
**症状**: 刷新控制器时显示连接错误

**解决方案**:
- 检查 API 地址是否正确（应为 http://ip:5005）
- 确认服务器正在运行
- 检查网络连接
- 验证防火墙设置
- 尝试使用浏览器访问 API 地址测试连接

#### 3. SignalR 连接失败
**症状**: 点击 "🔗 连接 SignalR" 后显示连接失败

**解决方案**:
- 确认服务器 SignalR Hub 已启用
- 检查 `/hubs/events` 端点是否可访问
- 查看服务器日志
- 验证 API 地址配置正确

#### 4. 命令执行失败
**症状**: 发送命令后返回错误

**解决方案**:
- 检查控制器是否已初始化
- 确认轴状态正常
- 查看错误消息获取详细信息
- 检查 Host 服务日志

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

### 已完成功能 ✅
- [x] UDP 服务自动发现
- [x] 完整 API 集成（所有控制器 API）
- [x] 扁平化 UI 设计（图标+文字）
- [x] 服务分层架构（清晰的职责划分）
- [x] MVVM 模式实现
- [x] 中文化状态显示
- [x] 值转换器支持
- [x] CollectionView 空状态处理

### 待实现功能

#### 1. 增强功能
- [ ] 添加轴状态实时图表（速度、位置曲线）
- [ ] 实现单个轴的独立控制页面
- [ ] 添加命令历史记录和审计日志
- [ ] 实现用户认证和授权（JWT）
- [ ] 支持多语言切换 (中文/英文)
- [ ] 添加深色主题支持
- [ ] 记住最近连接的服务
- [ ] 添加服务收藏功能

#### 2. UI 改进
- [ ] 自定义应用图标和启动画面（替换默认图标）
- [ ] 添加页面转换动画效果
- [ ] 优化平板电脑布局（横屏支持）
- [ ] 添加手势操作（滑动刷新、长按菜单）
- [ ] 实现拖拽排序功能
- [ ] 添加进度条和骨架屏

#### 3. 功能增强
- [ ] 离线模式支持（缓存数据）
- [ ] 数据缓存机制（减少网络请求）
- [ ] 批量操作确认对话框
- [ ] 导出日志和数据功能
- [ ] 二维码扫描配置（快速配置 API 地址）
- [ ] 推送通知支持（告警提醒）
- [ ] Widget 桌面小部件（快速查看状态）

#### 4. 高级功能
- [ ] 轴编程模式（预设速度曲线）
- [ ] 故障诊断助手
- [ ] 性能监控仪表板
- [ ] 数据可视化图表（Chart.js 集成）
- [ ] 远程固件更新
- [ ] 配置备份和恢复

## 许可证

与主项目保持一致

## 贡献

欢迎提交问题和拉取请求！

## 联系方式

- 项目仓库: https://github.com/Hisoka6602/ZakYip.Singulation
- 问题反馈: 在 GitHub 上创建 Issue
