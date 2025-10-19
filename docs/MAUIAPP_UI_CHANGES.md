# MauiApp UI 变更说明

## 概述
本次更新实现了问题陈述中的所有要求，主要包括：
1. 分离图标和文字，使用单色扁平化图标
2. UDP 配置已在 appsettings.json 中完成
3. 自动启动服务发现功能
4. 仅发现服务，不发现其他客户端

## UI 变更详情

### 1. 图标和文字分离

#### 之前（使用 Emoji）
```xml
<Label Text="🎯 Singulation 控制台" />
<Label Text="📊 控制器管理" />
<Label Text="🔄 刷新控制器" />
```

#### 之后（使用单色扁平图标）
```xml
<HorizontalStackLayout Spacing="10">
    <Label Text="●" FontSize="32" TextColor="#2196F3" />
    <Label Text="Singulation 控制台" />
</HorizontalStackLayout>
```

### 2. 使用的扁平图标符号

| 符号 | 用途 | 颜色 |
|-----|------|------|
| ● | 主标题，点符号 | #2196F3 (蓝色) |
| ▣ | 控制器管理 | #666 (灰色) |
| ⚙ | 轴控制、设置 | #4CAF50 (绿色) / #666 (灰色) |
| ◆ | 状态、API配置 | #666 (灰色) / #4CAF50 (绿色) |
| ◈ | 安全命令、使能状态 | #F57C00 (橙色) / #666 (灰色) |
| ◉ | 实时连接、服务发现 | #9C27B0 (紫色) / #2196F3 (蓝色) |
| ▪ | 列表项 | #666 (灰色) |
| # | 轴 ID | #666 (灰色) |
| ↻ | 刷新 | 白色（按钮内） |
| ✓ | 确认、使能 | 白色（按钮内） |
| ✕ | 取消、禁用 | 白色（按钮内） |
| ► | 发送、执行 | 白色（按钮内） |
| ▲ | 速度 | #666 (灰色) |
| ○ | 空状态 | LightGray (浅灰色) |
| □ | 空状态 | LightGray (浅灰色) |
| i | 信息 | #666 (灰色) |

### 3. 主要页面变更

#### MainPage (主控制台)

**标题栏**
- 蓝色圆点 (●) + "Singulation 控制台"
- 图标和文字分离，横向排列

**控制器管理区域**
- 灰色方块符号 (▣) + "控制器管理"
- 刷新按钮：圆形箭头 (↻) + "刷新控制器"
- 轴信息卡片使用几何符号：⚙ (轴名), # (ID), ◆ (状态), ◈ (使能)
- 空状态：大号方框 (□) + 说明文字

**安全命令区域**
- 橙色菱形 (◈) + "安全命令"
- 发送按钮：右箭头 (►) + "发送安全命令"

**轴控制区域**
- 绿色齿轮 (⚙) + "轴控制"
- 使能按钮：勾号 (✓) + "全部使能"
- 禁用按钮：叉号 (✕) + "全部禁用"
- 速度标签：三角形 (▲) + "目标速度 (mm/s)"
- 设置按钮：右箭头 (►) + "设置速度"

**实时连接区域**
- 紫色圆圈 (◉) + "实时连接"
- 连接按钮：圆圈 (◉) + "连接 SignalR"

#### SettingsPage (设置)

**标题栏**
- 灰色齿轮 (⚙) + "应用设置"

**服务发现区域**
- 蓝色圆圈 (◉) + "自动发现服务"
- 空状态：大号空心圆 (○) + 多行说明文字

**API 配置区域**
- 绿色菱形 (◆) + "API 配置"
- 保存按钮：勾号 (✓) + "保存设置"

**应用信息区域**
- 灰色 i 符号 + "应用信息"
- 列表项使用小方块 (▪) 作为项目符号

#### AppShell (导航栏)

**标签页**
- 控制台：`● 控制台`
- 设置：`⚙ 设置`

### 4. 自动启动服务发现

在 `SettingsViewModel.cs` 中添加了 `AutoStartDiscoveryAsync()` 方法：
- 应用启动后延迟 500ms 自动开始 UDP 服务发现
- 自动设置 `IsDiscovering = true`
- 显示状态消息："自动搜索服务中..."

```csharp
private async Task AutoStartDiscoveryAsync()
{
    try
    {
        await Task.Delay(500); // 延迟启动，确保UI已加载
        await _discoveryClient.StartListeningAsync();
        IsDiscovering = true;
        StatusMessage = "自动搜索服务中...";
    }
    catch (Exception ex)
    {
        StatusMessage = $"❌ 自动启动发现失败: {ex.Message}";
        IsDiscovering = false;
    }
}
```

## UI 设计原则

1. **图标与文字分离**：所有图标都在独立的 Label 中，与文字 Label 分开
2. **单色扁平化**：使用 Unicode 几何符号，避免彩色 Emoji
3. **语义化颜色**：
   - 蓝色 (#2196F3)：主要功能、信息
   - 绿色 (#4CAF50)：成功、确认、启用
   - 橙色 (#F57C00, #FF9800)：警告、安全
   - 紫色 (#9C27B0)：连接、通信
   - 灰色 (#666)：中性、通用图标
   - 红色 (#F44336)：危险、禁用
4. **一致性**：相同功能使用相同图标
5. **可访问性**：图标和文字分别设置，支持辅助技术

## 技术实现

### XAML 结构模式

```xml
<!-- 标题模式 -->
<HorizontalStackLayout Spacing="8">
    <Label Text="[图标]" FontSize="24" TextColor="[颜色]" VerticalOptions="Center" />
    <Label Text="[文字]" FontAttributes="Bold" FontSize="20" VerticalOptions="Center" />
</HorizontalStackLayout>

<!-- 按钮模式 -->
<Button Command="{Binding Command}">
    <Button.Content>
        <HorizontalStackLayout Spacing="8" HorizontalOptions="Center">
            <Label Text="[图标]" FontSize="20" TextColor="White" VerticalOptions="Center" />
            <Label Text="[文字]" FontSize="16" TextColor="White" VerticalOptions="Center" />
        </HorizontalStackLayout>
    </Button.Content>
</Button>
```

## 预期效果

### 主页面
- 清晰的功能分区，每个区域有明确的图标标识
- 按钮带有图标和文字，提高可识别性
- 统一的设计风格，专业的外观

### 设置页面
- 自动启动服务发现，无需手动点击
- 清晰的状态反馈
- 简洁的信息展示

## 后续优化建议

1. **响应式设计**：根据屏幕尺寸调整图标大小和间距
2. **深色主题**：添加深色主题支持，调整图标颜色
3. **动画效果**：为图标添加微妙的动画（如旋转、脉冲）
4. **自定义字体**：使用 Icon Font（如 Material Icons）替代 Unicode 符号
5. **辅助功能**：为图标添加 SemanticProperties 描述
6. **国际化**：支持多语言界面

## 测试清单

- [ ] 主页面标题正确显示（图标+文字）
- [ ] 所有按钮图标和文字分离显示
- [ ] 控制器列表项图标显示正确
- [ ] 设置页面自动启动服务发现
- [ ] 导航栏标签显示图标和文字
- [ ] 空状态图标显示正确
- [ ] 所有颜色符合设计规范
- [ ] 布局在不同屏幕尺寸下正常
