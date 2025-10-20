# 单件分离主页 (Singulation Home Page)

## 概述 (Overview)

单件分离主页是一个针对工业单件分离系统设计的移动端主页，采用 iOS 风格的浅色主题和卡片化设计。

## 设计特性 (Design Features)

### 主题颜色 (Theme Colors)
- **主色 (Primary)**: #3B82F6 - 蓝色，用于主要按钮和强调元素
- **危险色 (Danger)**: #EF4444 - 红色，用于异常状态和警告
- **成功色 (Success)**: #10B981 - 绿色，用于成功状态和启用操作
- **禁用灰 (Disabled)**: #94A3B8 - 灰色，用于禁用状态
- **背景色 (Background)**: #F6F7FB - 浅灰色背景
- **文字主色 (Text)**: #0F172A - 深色主要文字
- **次级文字 (Text Secondary)**: #64748B - 灰色次级文字

### 设计元素
- **圆角**: 20-24dp，提供柔和的视觉体验
- **阴影**: 细腻柔和的低高度阴影
- **字体**: 几何无衬线字体

## 页面布局 (Page Layout)

### 1. 标题栏 (Header)
- 显示 "分件助手" 标题
- 右上角包含搜索和设置图标按钮

### 2. 功能区 (Toolbar)

#### 第一行（4个按钮）
- **刷新控制器**: 次要样式，用于刷新控制器数据
- **安全指令**: 主样式，展开为三态弹出菜单（启动/停止/重置）
- **全部使能**: 成功绿色，启用所有电机轴
- **全部禁用**: 禁用灰色，禁用所有电机轴

#### 第二行（1个按钮）
- **轴速度设置**: 主样式，打开速度设置对话框

### 3. 批次信息 (Batch Info)
显示当前批次号：DJ61957AAK00025

### 4. 模式切换 (Mode Switcher)
胶囊分段控件，用于切换：
- **自动分离** (Auto) - 默认激活
- **手动分离** (Manual)

### 5. 电机轴分布栅格 (Motor Axis Grid)
- **布局**: 3列网格，显示 M01-M20 共20个电机轴
- **卡片内容**:
  - 顶部: 电机ID（如 M01）
  - 中间: 大号转速数值
  - 底部: 单位 "r/min"

#### 电机状态
- **正常**: 浅灰卡片背景，深色文字
- **高负载/选中**: 主色底 (#3B82F6) + 白色文字
- **异常**: 红色底 (#EF4444) + 白色文字
- **禁用**: 透明背景 + 灰色边框和文字

### 6. 主操作按钮 (Main Action)
大号胶囊按钮 "分离"，主色填充，带阴影效果

## 功能实现 (Features)

### 命令 (Commands)
1. **SearchCommand**: 搜索功能
2. **SettingsCommand**: 导航到设置页面
3. **RefreshControllerCommand**: 刷新控制器数据
4. **SafetyCommandCommand**: 显示安全指令菜单（启动/停止/重置）
5. **EnableAllCommand**: 启用所有电机轴
6. **DisableAllCommand**: 禁用所有电机轴
7. **AxisSpeedSettingCommand**: 设置所有轴的速度
8. **SelectModeCommand**: 切换自动/手动分离模式
9. **SeparateCommand**: 执行分离操作
10. **SelectMotorCommand**: 选择电机轴

### 数据模型 (Data Models)

#### SingulationHomeViewModel
- `BatchNumber`: 批次号
- `SelectedMode`: 当前选择的模式（Auto/Manual）
- `MotorAxes`: 电机轴集合（M01-M20）
- `SelectedMotor`: 当前选中的电机轴

#### MotorAxisInfo
- `MotorId`: 电机ID
- `Rpm`: 转速（r/min）
- `IsSelected`: 是否被选中
- `IsAbnormal`: 是否异常
- `IsDisabled`: 是否禁用
- `IsNormal`: 是否正常（计算属性）

## 使用说明 (Usage)

1. **查看电机状态**: 在电机轴网格中查看所有20个电机的实时转速
2. **选择电机**: 点击任意电机卡片进行选择（高亮显示）
3. **启用/禁用电机**: 使用"全部使能"或"全部禁用"按钮控制所有电机
4. **设置速度**: 点击"轴速度设置"按钮输入新的转速值
5. **执行安全指令**: 点击"安全指令"按钮，选择启动、停止或重置
6. **选择模式**: 在"自动分离"和"手动分离"之间切换
7. **执行分离**: 点击底部"分离"按钮执行分离操作

## 技术栈 (Tech Stack)

- **.NET MAUI**: 跨平台移动应用框架
- **Prism.Maui**: MVVM 框架
- **C#**: 编程语言
- **XAML**: UI 标记语言

## 文件结构 (File Structure)

```
Views/
  └── SingulationHomePage.xaml        # XAML UI 定义
  └── SingulationHomePage.xaml.cs     # Code-behind

ViewModels/
  └── SingulationHomeViewModel.cs     # ViewModel 逻辑和数据绑定
```

## 配置 (Configuration)

页面已在以下位置注册：
- `MauiProgram.cs`: 依赖注入容器注册
- `AppShell.xaml`: 作为第一个标签页添加到应用导航

## 待完善功能 (Future Enhancements)

- [ ] 与后端 API 集成获取实时数据
- [ ] 添加电机轴详情页面
- [ ] 实现实时速度更新（通过 SignalR）
- [ ] 添加批次历史记录
- [ ] 支持批量电机选择操作
- [ ] 添加图表展示性能数据
- [ ] 支持自定义主题配色
