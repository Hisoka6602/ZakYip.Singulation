# MauiApp 图标字体使用指南

## 概述

ZakYip.Singulation.MauiApp 实现了基于 Font Awesome 的统一图标字体管理系统，替代了原有的 Emoji 表情，提供了更专业、更一致的视觉体验。

## 目录结构

```
ZakYip.Singulation.MauiApp/
├── Icons/
│   ├── AppIcon.cs              # 图标枚举定义
│   ├── AppIconExtensions.cs    # 扩展方法（带缓存）
│   └── IconFont.cs             # 字体别名常量
├── Resources/
│   ├── Fonts/
│   │   └── FontAwesome6FreeSolid.otf  # Font Awesome 字体文件
│   └── Styles/
│       └── Icons.xaml          # 图标资源字典
└── App.xaml                    # 合并 Icons.xaml
```

## 1. 图标枚举（AppIcon.cs）

所有图标通过 `AppIcon` 枚举集中管理，每个枚举值使用 `Description` 特性存储 Unicode 码位。

```csharp
public enum AppIcon
{
    /// <summary>添加</summary>
    [Description("\uf067")] Add,

    /// <summary>首页</summary>
    [Description("\uf015")] Home,

    /// <summary>设置</summary>
    [Description("\uf013")] Settings,
    
    // ... 更多图标
}
```

### 当前可用图标（25个）

| 图标名称 | Unicode | 说明 |
|---------|---------|------|
| Add | \uf067 | 添加 |
| Home | \uf015 | 首页 |
| Settings | \uf013 | 设置 |
| Refresh | \uf021 | 刷新 |
| Play | \uf04b | 播放/开始 |
| Stop | \uf04d | 停止 |
| Pause | \uf04c | 暂停 |
| Info | \uf05a | 信息 |
| Warning | \uf071 | 警告 |
| Error | \uf057 | 错误 |
| Success | \uf058 | 成功/勾选 |
| Link | \uf0c1 | 连接 |
| Unlink | \uf127 | 断开连接 |
| Speed | \uf0e7 | 速度 |
| Controller | \uf2db | 控制器 |
| Axis | \uf1de | 轴 |
| Grid | \uf00a | 网格/模块 |
| List | \uf03a | 列表 |
| Send | \uf1d8 | 发送 |
| Safety | \uf132 | 安全 |
| ArrowUp | \uf062 | 向上箭头 |
| ArrowDown | \uf063 | 向下箭头 |
| Circle | \uf111 | 圆圈 |
| Dot | \uf192 | 圆点 |

## 2. 扩展方法（AppIconExtensions.cs）

提供 `ToGlyph()` 扩展方法，将枚举值转换为 Glyph 字符串，结果会被缓存以提升性能。

```csharp
public static class AppIconExtensions
{
    public static string ToGlyph(this AppIcon icon)
    {
        // 从缓存获取或生成 Glyph
        // ...
    }
}
```

## 3. 字体别名（IconFont.cs）

定义字体别名常量，确保与 MauiProgram.cs 中注册的别名一致。

```csharp
public static class IconFont
{
    public const string FASolid = "FontAwesomeSolid";
}
```

## 4. 资源字典（Icons.xaml）

定义可复用的图标资源和样式，包括：

### 预定义的 FontImageSource 资源

```xml
<!-- 通用操作图标 (24px) -->
<FontImageSource x:Key="Icon.Add.24" FontFamily="FontAwesomeSolid" Glyph="&#xf067;" Size="24" />
<FontImageSource x:Key="Icon.Refresh.24" FontFamily="FontAwesomeSolid" Glyph="&#xf021;" Size="24" />

<!-- 大图标 (32px) -->
<FontImageSource x:Key="Icon.Home.32" FontFamily="FontAwesomeSolid" Glyph="&#xf015;" Size="32" />
```

### 预定义的图标样式

```xml
<!-- 图标按钮样式 -->
<Style x:Key="IconButton" TargetType="Button">
    <Setter Property="ContentLayout" Value="Left,8" />
    <Setter Property="Padding" Value="12,8" />
</Style>

<!-- 图标标签样式 -->
<Style x:Key="IconLabel" TargetType="Label">
    <Setter Property="FontFamily" Value="FontAwesomeSolid" />
    <Setter Property="FontSize" Value="24" />
</Style>
```

## 5. 使用方式

### 方式 A：直接使用资源（推荐用于静态图标）

适用于固定的、不需要动态变化的图标。

**在 XAML 中使用：**

```xml
<!-- 使用 FontImageSource 作为 Button 的 ImageSource -->
<Button Text="刷新"
        Style="{StaticResource IconButton}"
        ImageSource="{StaticResource Icon.Refresh.24}" />

<!-- 使用 Image 控件 -->
<Image Source="{StaticResource Icon.Speed.24}" 
       WidthRequest="24" 
       HeightRequest="24" />
```

### 方式 B：通过 ViewModel 绑定（推荐用于 MVVM）

适用于需要动态变化或统一管理的图标。

**在 ViewModel 中暴露 Glyph 属性：**

```csharp
using ZakYip.Singulation.MauiApp.Icons;

public class MainViewModel : BindableBase
{
    // 图标 Glyphs（用于绑定）
    public string HomeGlyph => AppIcon.Home.ToGlyph();
    public string RefreshGlyph => AppIcon.Refresh.ToGlyph();
    public string SpeedGlyph => AppIcon.Speed.ToGlyph();
}
```

**在 XAML 中绑定：**

```xml
<ContentPage x:DataType="viewmodels:MainViewModel">
    <!-- 绑定 Glyph 到 Label -->
    <Label Text="{Binding HomeGlyph}"
           FontFamily="FontAwesomeSolid"
           FontSize="32"
           TextColor="#2196F3" />

    <!-- 或使用预定义样式 -->
    <Label Text="{Binding SpeedGlyph}"
           Style="{StaticResource IconLabel}" />
</ContentPage>
```

### 方式 C：直接在代码中使用

```csharp
using ZakYip.Singulation.MauiApp.Icons;

// 获取图标 Glyph
string homeGlyph = AppIcon.Home.ToGlyph();

// 创建 Label
var iconLabel = new Label
{
    Text = homeGlyph,
    FontFamily = "FontAwesomeSolid",
    FontSize = 24
};
```

## 6. 添加新图标

### 步骤 1：在 iconfont.cn 查找图标

1. 访问 https://www.iconfont.cn/ 或 Font Awesome 官网
2. 搜索所需图标
3. 获取 Unicode 码位（例如：`\uf015`）

### 步骤 2：添加到 AppIcon 枚举

```csharp
public enum AppIcon
{
    // ... 现有图标

    /// <summary>新图标</summary>
    [Description("\uf123")] NewIcon,
}
```

### 步骤 3：（可选）在 Icons.xaml 添加资源

```xml
<FontImageSource x:Key="Icon.NewIcon.24" 
                 FontFamily="FontAwesomeSolid" 
                 Glyph="&#xf123;" 
                 Size="24" />
```

### 步骤 4：在 ViewModel 暴露 Glyph（如需要）

```csharp
public string NewIconGlyph => AppIcon.NewIcon.ToGlyph();
```

## 7. 最佳实践

### ✅ 推荐做法

1. **统一管理**：所有图标通过 `AppIcon` 枚举管理，避免魔法值
2. **使用缓存**：使用 `ToGlyph()` 扩展方法，自动缓存提升性能
3. **MVVM 模式**：在 ViewModel 暴露 Glyph 属性，便于测试和维护
4. **资源复用**：对于常用图标，定义在 Icons.xaml 中统一复用
5. **中文注释**：为每个图标添加中文注释，提升可读性

### ❌ 避免做法

1. ❌ 直接使用 Unicode 字符串：`Text="&#xf015;"`
2. ❌ 在多处重复定义相同图标
3. ❌ 混用 Emoji 和图标字体
4. ❌ 硬编码 FontFamily 名称，应使用 `IconFont` 常量

## 8. 实际案例

### 案例 1：主页面轴卡片（MainPage.xaml）

显示轴信息，右侧突出显示实时速度。

```xml
<Frame Margin="0,5" Padding="15" BorderColor="#2196F3">
    <Grid RowDefinitions="Auto,Auto,Auto,Auto,Auto" 
          ColumnDefinitions="Auto,*,Auto" 
          RowSpacing="5" ColumnSpacing="10">
        
        <!-- 轴名称 -->
        <Image Grid.Row="0" Grid.Column="0" 
               Source="{StaticResource Icon.Axis.24}" 
               WidthRequest="16" HeightRequest="16" />
        <Label Grid.Row="0" Grid.Column="1" 
               Text="{Binding Name}" 
               FontAttributes="Bold" FontSize="16" />
        
        <!-- 实时速度显示（突出显示） -->
        <Frame Grid.Row="0" Grid.Column="2" Grid.RowSpan="2" 
               Padding="10,5" BackgroundColor="#E3F2FD" 
               BorderColor="#2196F3" CornerRadius="5">
            <VerticalStackLayout Spacing="2">
                <HorizontalStackLayout Spacing="5" HorizontalOptions="Center">
                    <Image Source="{StaticResource Icon.Speed.24}" 
                           WidthRequest="14" HeightRequest="14" />
                    <Label Text="速度" FontSize="10" TextColor="#666" />
                </HorizontalStackLayout>
                <Label Text="{Binding CurrentSpeed, StringFormat='{0:F1} mm/s'}" 
                       FontSize="16" FontAttributes="Bold" 
                       TextColor="#2196F3" HorizontalOptions="Center" />
            </VerticalStackLayout>
        </Frame>
    </Grid>
</Frame>
```

### 案例 2：带图标的按钮（MainViewModel + MainPage.xaml）

**ViewModel：**
```csharp
public class MainViewModel : BindableBase
{
    public string RefreshGlyph => AppIcon.Refresh.ToGlyph();
}
```

**XAML：**
```xml
<Button Command="{Binding RefreshControllersCommand}"
        Style="{StaticResource IconButton}"
        ImageSource="{StaticResource Icon.Refresh.24}"
        Text="刷新控制器"
        BackgroundColor="#4CAF50"
        TextColor="White" />
```

### 案例 3：区域标题（ControllerDetailsPage.xaml）

```xml
<HorizontalStackLayout Spacing="8">
    <Label Text="{Binding SpeedGlyph}"
           FontFamily="FontAwesomeSolid"
           FontSize="18"
           TextColor="#4CAF50"
           VerticalOptions="Center" />
    <Label Text="速度信息"
           FontSize="18"
           FontAttributes="Bold"
           VerticalOptions="Center" />
</HorizontalStackLayout>
```

## 9. 性能优化

### 缓存机制

`AppIconExtensions.ToGlyph()` 使用 `ConcurrentDictionary` 缓存转换结果，避免重复反射：

```csharp
private static readonly ConcurrentDictionary<AppIcon, string> Cache = new();

public static string ToGlyph(this AppIcon icon)
    => Cache.GetOrAdd(icon, k => /* 反射获取 Description */ );
```

### 资源预加载

`Icons.xaml` 中定义的 `FontImageSource` 资源在应用启动时加载，后续使用无需重新创建。

## 10. 故障排查

### 问题：图标不显示

**可能原因 1：字体未注册**

检查 `MauiProgram.cs` 是否注册了字体：

```csharp
builder.ConfigureFonts(fonts => {
    fonts.AddFont("FontAwesome6FreeSolid.otf", "FontAwesomeSolid");
});
```

**可能原因 2：FontFamily 名称不匹配**

确保 XAML 中使用的 `FontFamily` 与注册的别名一致：

```xml
<!-- ✅ 正确 -->
<Label Text="{Binding HomeGlyph}" FontFamily="FontAwesomeSolid" />

<!-- ❌ 错误（别名不匹配） -->
<Label Text="{Binding HomeGlyph}" FontFamily="FontAwesome" />
```

**可能原因 3：Unicode 码位错误**

确认 `AppIcon` 枚举中的 Unicode 码位与字体文件匹配。

### 问题：ViewModel 中图标 Glyph 为空

检查是否正确导入命名空间：

```csharp
using ZakYip.Singulation.MauiApp.Icons;  // ✅ 必须导入
```

### 问题：编译错误 "StaticResource not found"

确保 `Icons.xaml` 已在 `App.xaml` 中合并：

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Styles/Icons.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## 11. 更换字体

如需更换为其他图标字体（如 Material Symbols）：

### 步骤 1：添加字体文件

将新字体文件（如 `MaterialSymbolsRounded.ttf`）放入 `Resources/Fonts/` 目录。

### 步骤 2：注册字体

在 `MauiProgram.cs` 中注册：

```csharp
fonts.AddFont("MaterialSymbolsRounded.ttf", "Material");
```

### 步骤 3：更新 IconFont 常量

```csharp
public static class IconFont
{
    public const string Material = "Material";
    public const string FASolid = "FontAwesomeSolid";
}
```

### 步骤 4：更新 AppIcon 枚举

根据新字体更新 Unicode 码位。

### 步骤 5：更新 Icons.xaml 资源

```xml
<FontImageSource x:Key="Icon.Add.24" 
                 FontFamily="Material" 
                 Glyph="&#xE145;" 
                 Size="24" />
```

## 12. 总结

本图标字体系统提供了：

✅ **统一管理**：所有图标通过枚举集中管理  
✅ **类型安全**：IntelliSense 支持，避免拼写错误  
✅ **高性能**：缓存机制，减少反射开销  
✅ **MVVM 友好**：支持数据绑定，便于测试  
✅ **易于维护**：添加/修改图标只需更新枚举  
✅ **专业外观**：矢量图标，任意缩放不失真  

建议在开发新功能时优先使用本图标系统，确保 UI 一致性和专业性。
