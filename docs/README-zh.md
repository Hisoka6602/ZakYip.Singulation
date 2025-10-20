# 🏠 工业单件分离主页 - 实现指南

## 📋 概述

本项目实现了一个完整的工业单件分离系统移动端主页，采用 iOS 风格的浅色主题和卡片化设计。这是一个基于 .NET MAUI 的跨平台移动应用页面。

## ✨ 主要特性

### 设计特性
- 🎨 **iOS 风格设计**: 遵循 iOS 设计规范的浅色主题
- 🃏 **卡片化布局**: 所有组件采用圆角卡片设计
- 🌈 **精确配色**: 严格遵循工业配色规范
- 📱 **移动优化**: 针对手机竖屏模式优化
- 💫 **流畅交互**: 所有操作提供即时反馈

### 功能特性
- 🔄 **控制器管理**: 刷新和管理控制器
- 🛡️ **安全指令**: 启动、停止、重置三态控制
- ⚡ **批量操作**: 一键启用/禁用所有电机
- 📊 **速度设置**: 批量设置电机转速
- 🎯 **电机监控**: 实时监控 20 个电机轴状态
- 🔀 **模式切换**: 自动/手动分离模式
- 📦 **批次管理**: 显示和管理当前批次

## 📁 文件结构

```
ZakYip.Singulation.MauiApp/
├── Views/
│   ├── SingulationHomePage.xaml           # UI 定义 (308 行)
│   └── SingulationHomePage.xaml.cs        # Code-behind (10 行)
├── ViewModels/
│   └── SingulationHomeViewModel.cs        # 视图模型 (279 行)
├── AppShell.xaml                          # 应用导航 (已修改)
└── MauiProgram.cs                         # DI 注册 (已修改)

docs/
├── SingulationHomePage.md                 # 功能文档
├── SingulationHomePage-UI-Spec.md         # UI 规范
├── Implementation-Verification.md         # 实现验证
├── Component-Structure.md                 # 组件结构
├── SUMMARY.md                             # 实现总结
└── README-zh.md                           # 本文档
```

## 🎨 设计规范

### 颜色系统
| 用途 | 颜色代码 | 应用场景 |
|------|---------|---------|
| 主色 | `#3B82F6` | 主要按钮、选中状态 |
| 危险色 | `#EF4444` | 异常状态、警告 |
| 成功色 | `#10B981` | 成功状态、启用操作 |
| 禁用灰 | `#94A3B8` | 禁用状态 |
| 背景色 | `#F6F7FB` | 页面背景 |
| 文字主色 | `#0F172A` | 主要文字 |
| 文字次色 | `#64748B` | 次要文字 |

### 尺寸规范
- **圆角**: 20-24dp
- **按钮高度**: 44-60dp
- **间距**: 8-16dp
- **阴影**: 柔和低高度阴影
- **字体大小**: 12-32pt

## 🏗️ 架构设计

### MVVM 模式
```
View (XAML)
    ↕ Data Binding
ViewModel (C#)
    ↕ Business Logic
Model (Data)
```

### 依赖注入
使用 Prism.Maui 的 DI 容器：
- `IContainerRegistry.RegisterForNavigation<TView, TViewModel>()`
- 自动解析构造函数依赖

### 命令模式
所有用户交互通过 `DelegateCommand` 实现：
- 解耦 UI 和业务逻辑
- 支持 `CanExecute` 状态管理
- 异步操作支持

## 🚀 快速开始

### 前置要求
- .NET 8.0 SDK
- .NET MAUI 工作负载
- Visual Studio 2022 或 VS Code
- Android SDK 或 Xcode (iOS)

### 安装步骤

1. **克隆仓库**
```bash
git clone https://github.com/Hisoka6602/ZakYip.Singulation.git
cd ZakYip.Singulation
```

2. **安装 MAUI 工作负载**
```bash
# Android
dotnet workload install maui-android

# iOS (需要 macOS)
dotnet workload install maui-ios

# 全部平台
dotnet workload install maui
```

3. **还原依赖**
```bash
dotnet restore
```

4. **构建项目**
```bash
dotnet build ZakYip.Singulation.MauiApp/ZakYip.Singulation.MauiApp.csproj
```

5. **运行应用**
```bash
# Android
dotnet build -f net8.0-android -t:Run

# iOS
dotnet build -f net8.0-ios -t:Run
```

## 📱 页面布局

```
┌─────────────────────────────────┐
│ 🏠 分件助手        🔍  ⚙️      │ 标题栏
├─────────────────────────────────┤
│ [刷新] [安全] [使能] [禁用]     │ 工具栏 1
│ [轴速度设置]                    │ 工具栏 2
├─────────────────────────────────┤
│ 批次：DJ61957AAK00025           │ 批次信息
├─────────────────────────────────┤
│ [自动分离] | [手动分离]         │ 模式切换
├─────────────────────────────────┤
│ ┌─────┐ ┌─────┐ ┌─────┐         │
│ │ M01 │ │ M02 │ │ M03 │         │
│ │1000 │ │2000 │ │2000 │         │ 电机网格
│ │r/min│ │r/min│ │r/min│         │
│ └─────┘ └─────┘ └─────┘         │
│        ... (共 20 个)            │
├─────────────────────────────────┤
│          [分离]                 │ 主操作
└─────────────────────────────────┘
```

## 💡 功能说明

### 1. 标题栏
- **标题**: 显示 "分件助手"
- **搜索按钮**: 搜索功能（待实现）
- **设置按钮**: 跳转到设置页面

### 2. 工具栏
#### 第一行
- **刷新控制器**: 刷新控制器数据
- **安全指令**: 弹出菜单选择启动/停止/重置
- **全部使能**: 启用所有电机轴
- **全部禁用**: 禁用所有电机轴

#### 第二行
- **轴速度设置**: 批量设置所有轴的转速

### 3. 批次信息
显示当前操作的批次号

### 4. 模式切换
在自动分离和手动分离模式之间切换

### 5. 电机网格
- **布局**: 3 列网格
- **数量**: 20 个电机 (M01-M20)
- **显示**: 电机ID、转速值、单位
- **交互**: 点击选中电机

### 6. 主操作按钮
执行分离操作

## 🎯 电机状态

| 状态 | 背景色 | 文字色 | 边框 | 说明 |
|------|--------|--------|------|------|
| 正常 | 白色 | 深色 | 无 | 默认状态 |
| 选中 | 蓝色 | 白色 | 无 | 用户点击选中 |
| 异常 | 红色 | 白色 | 无 | 电机故障 |
| 禁用 | 透明 | 灰色 | 灰色 | 电机禁用 |

## 🔄 交互流程

### 安全指令流程
```
点击"安全指令" → 显示菜单 → 选择选项 → 显示确认
```

### 速度设置流程
```
点击"轴速度设置" → 输入转速 → 验证数值 → 更新所有电机
```

### 电机选择流程
```
点击电机卡片 → 取消其他选择 → 选中当前电机 → 背景变蓝
```

### 分离操作流程
```
点击"分离"按钮 → 显示确认对话框 → 确认 → 执行分离
```

## 🔧 自定义和扩展

### 修改颜色主题
编辑 `SingulationHomePage.xaml` 中的资源定义：
```xml
<Color x:Key="PrimaryColor">#3B82F6</Color>
```

### 修改电机数量
编辑 `SingulationHomeViewModel.cs` 中的初始化方法：
```csharp
for (int i = 1; i <= 20; i++) // 修改这里的数量
```

### 添加新命令
1. 在 ViewModel 中声明命令：
```csharp
public DelegateCommand MyCommand { get; }
```

2. 在构造函数中初始化：
```csharp
MyCommand = new DelegateCommand(OnMyCommand);
```

3. 实现命令方法：
```csharp
private void OnMyCommand()
{
    // 你的逻辑
}
```

4. 在 XAML 中绑定：
```xml
<Button Command="{Binding MyCommand}" Text="My Button"/>
```

## 📊 性能优化

1. **CollectionView 虚拟化**: 只渲染可见的电机卡片
2. **Observable Collections**: 最小化 UI 刷新
3. **异步操作**: 使用 async/await 防止 UI 阻塞
4. **数据绑定**: 减少手动 UI 更新

## 🧪 测试建议

### 功能测试
- [ ] 测试所有按钮点击
- [ ] 验证电机选择行为
- [ ] 测试模式切换
- [ ] 验证对话框显示
- [ ] 测试启用/禁用功能

### UI 测试
- [ ] 验证颜色准确性
- [ ] 检查圆角和阴影
- [ ] 测试滚动行为
- [ ] 验证安全区域适配

### 性能测试
- [ ] 监控内存使用
- [ ] 测试滚动流畅度
- [ ] 验证启动时间

## 🐛 已知问题

暂无已知问题

## 📝 待办事项

- [ ] 连接后端 API
- [ ] 实现实时数据更新
- [ ] 添加电机详情页
- [ ] 支持批次历史
- [ ] 添加数据图表
- [ ] 实现离线模式

## 📚 相关文档

- [功能文档](./SingulationHomePage.md)
- [UI 规范](./SingulationHomePage-UI-Spec.md)
- [实现验证](./Implementation-Verification.md)
- [组件结构](./Component-Structure.md)
- [实现总结](./SUMMARY.md)

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

请参考项目根目录的 LICENSE 文件

## 👥 联系方式

- 项目维护者: Hisoka6602
- GitHub: https://github.com/Hisoka6602/ZakYip.Singulation

---

**最后更新**: 2025-10-20
**版本**: 1.0.0
**状态**: ✅ 实现完成
