# 迁移总结 - Prism MVVM 与 Newtonsoft.Json

## 概述
本文档总结了将 ZakYip.Singulation MAUI 应用迁移到使用 Prism for .NET MAUI 作为 MVVM 框架以及 Newtonsoft.Json 作为 JSON 序列化库的变更，同时包含性能增强。

## 已实施的变更

### 1. MVVM 框架：CommunityToolkit.Mvvm → Prism for .NET MAUI

#### NuGet 包变更
- **移除**：`CommunityToolkit.Mvvm` (8.3.2)
- **添加**：
  - `Prism.Maui` (9.0.537)
  - `Prism.DryIoc.Maui` (9.0.537)

#### 代码变更

**MauiProgram.cs**
- 使用 `.UsePrism()` 扩展方法添加 Prism 集成
- 配置 DryIoc 容器用于依赖注入
- 使用 `IContainerRegistry` 注册服务、页面和 ViewModels
- 使用 `CreateWindow` 模式设置初始导航

**ViewModels**
- 将基类从 `ObservableObject` 迁移到 `BindableBase`
- 将 `[ObservableProperty]` 特性替换为使用 `SetProperty()` 的显式属性实现
- 将 `[RelayCommand]` 转换为手动初始化的 `DelegateCommand`
- 实现 `ObservesProperty()` 以自动更新命令的 CanExecute

**MainViewModel.cs**
- 6 个命令转换为 `DelegateCommand`
- 7 个可观察属性转换为 `BindableBase` 模式
- 为所有命令方法添加触觉反馈

**SettingsViewModel.cs**
- 1 个命令转换为 `DelegateCommand`
- 3 个可观察属性转换为 `BindableBase` 模式
- 为 SaveSettings 命令添加触觉反馈

### 2. JSON 库：System.Text.Json → Newtonsoft.Json

#### NuGet 包变更
- **添加**：`Newtonsoft.Json` (13.0.3)

#### 代码变更

**ApiClient.cs**
- 将 `System.Text.Json.JsonSerializer` 替换为 `Newtonsoft.Json.JsonConvert`
- 从 `JsonSerializerOptions` 改为 `JsonSerializerSettings`
- 将所有 `GetFromJsonAsync` 调用转换为使用 `ReadAsStringAsync()` + `JsonConvert.DeserializeObject()` 的手动反序列化
- 将所有 `PostAsJsonAsync` 调用转换为使用 `JsonConvert.SerializeObject()` + `StringContent` 的手动序列化
- 配置 `NullValueHandling.Ignore` 以优化 JSON 输出

### 3. 性能增强

#### 页面过渡动画
**AppShell.xaml.cs**
- 添加 `Navigating` 事件处理程序，带淡出动画（100ms，CubicOut 缓动）
- 添加 `Navigated` 事件处理程序，带淡入动画（150ms，CubicIn 缓动）
- 在异步动画之前实现空安全的页面引用捕获
- 提供流畅、专业的页面过渡

#### 触觉反馈
为所有命令方法添加 `HapticFeedback.Default.Perform(HapticFeedbackType.Click)`：
- `RefreshControllersAsync()` - MainViewModel
- `SendSafetyCommandAsync()` - MainViewModel
- `ConnectSignalRAsync()` - MainViewModel
- `EnableAllAxesAsync()` - MainViewModel
- `DisableAllAxesAsync()` - MainViewModel
- `SetAllAxesSpeedAsync()` - MainViewModel
- `SaveSettingsAsync()` - SettingsViewModel

## 优势

### Prism for .NET MAUI
- **行业标准**：Prism 是一个成熟的 MVVM 框架，拥有丰富的文档
- **强大的导航**：高级导航服务，支持参数传递和生命周期事件
- **灵活的依赖注入**：DryIoc 容器提供卓越的性能和功能
- **可测试性**：通过依赖注入更好地支持单元测试
- **模块化**：支持模块化应用架构

### Newtonsoft.Json
- **成熟稳定**：.NET 生态系统中使用最广泛的 JSON 库
- **功能丰富**：广泛的自定义选项和高级功能
- **性能**：针对大型负载高度优化
- **兼容性**：与传统系统和第三方 API 更好的兼容性
- **精细控制**：对序列化行为的详细控制

### 性能增强
- **流畅动画**：页面间的专业淡入淡出过渡增强用户体验
- **触觉反馈**：触觉反馈为所有操作提供即时的用户确认
- **优化的用户体验**：视觉和触觉反馈的结合创造了响应迅速的现代应用体验

## 构建验证
- ✅ Android (net8.0-android)：构建成功
- ⏳ Windows (net8.0-windows)：需要 Windows 操作系统进行测试
- ⏳ iOS/MacCatalyst：需要 macOS 进行构建

## 测试建议
1. 测试所有 ViewModels 以确保属性绑定正常工作
2. 验证所有命令按预期执行
3. 测试页面导航和过渡
4. 在物理设备上验证触觉反馈是否工作
5. 测试 API 调用以确保 JSON 序列化/反序列化正常工作
6. 验证依赖注入是否正确解析所有服务

## 迁移说明
- 公共 API 或数据模型没有破坏性变更
- 保留所有现有功能
- 与现有的后端 REST API 兼容
- 无需更改数据库架构
