# ZakYip.Singulation C# 编码规范

本文档定义了 ZakYip.Singulation 项目的 C# 编码规范和最佳实践。所有代码贡献者应遵循这些指南以确保代码质量、可维护性和一致性。

## 1. 使用 required + init 实现更安全的对象创建

确保某些属性在对象创建时必须被设置，通过避免部分初始化的对象来减少错误。

### 规则
- 对于**必须**在创建时提供的属性，使用 `required` 修饰符
- 使用 `init` 访问器使属性在初始化后不可变
- 为非必需属性提供合理的默认值

### ✅ 推荐做法

```csharp
// 配置类：必需属性使用 required，可选属性提供默认值
public sealed record class TcpServerOptions
{
    /// <summary>监听地址；默认 0.0.0.0。</summary>
    public required IPAddress Address { get; init; } = IPAddress.Any;
    
    public int Port { get; init; } = 5000;
    
    public int MaxActiveConnections { get; init; } = 100;
}

// 驱动配置：关键参数必须显式设置
public record DriverOptions
{
    public required int Card { get; init; }
    public required ushort Port { get; init; }
    public required ushort NodeId { get; init; }
    public required decimal GearRatio { get; init; } = 1m;
    
    // 可选参数提供默认值
    public decimal MaxRpm { get; init; } = 1813m;
    public TimeSpan MinWriteInterval { get; init; } = TimeSpan.FromMilliseconds(5);
}
```

### ❌ 避免做法

```csharp
// 不要：允许创建部分初始化的对象
public class DriverOptions
{
    public int Card { get; set; }  // 可能未初始化
    public ushort Port { get; set; }  // 可能未初始化
}

// 不要：使用可变属性
public class TcpServerOptions
{
    public IPAddress Address { get; set; } = IPAddress.Any;  // 可以在初始化后被修改
}
```

### 优势
- ✅ 编译时验证必需属性已设置
- ✅ 防止运行时 NullReferenceException
- ✅ 提高代码可读性和意图表达
- ✅ 减少防御性编程代码

## 2. 启用可空引用类型

让编译器对可能的空引用问题发出警告，在运行前发现问题。

### 规则
- **所有项目**必须在 `.csproj` 文件中启用 `<Nullable>enable</Nullable>`
- 明确标注可为空的引用类型使用 `?`
- 对于不可为空的引用类型，确保始终赋值

### ✅ 推荐做法

```csharp
// .csproj 文件
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
</PropertyGroup>

// 代码中明确标注可空性
public class UpstreamController
{
    // 明确不可为空
    private readonly IUpstreamCodec _codec;
    private readonly ILogger<UpstreamController> _logger;
    
    // 明确可为空
    private string? _lastError;
    private DateTime? _lastConnectionTime;
    
    public UpstreamController(IUpstreamCodec codec, ILogger<UpstreamController> logger)
    {
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void SetError(string? error)
    {
        _lastError = error;
    }
}
```

### ❌ 避免做法

```csharp
// 不要：禁用可空引用类型
#nullable disable

// 不要：不明确标注可空性
public class Controller
{
    private string _lastError;  // 不清楚是否可为空
}
```

### 优势
- ✅ 编译时发现潜在的空引用问题
- ✅ 减少运行时 NullReferenceException
- ✅ 提高代码的自文档性
- ✅ 与现代 C# 生态系统保持一致

## 3. 使用文件作用域类型实现真正封装

保持工具类在文件内私有，避免污染全局命名空间，帮助强制执行边界。

### 规则
- 对于**仅在单个文件内使用**的辅助类、结构体或枚举，使用 `file` 修饰符
- 将实现细节隐藏在文件作用域内
- 减少不必要的 `internal` 或 `public` 可见性

### ✅ 推荐做法

```csharp
namespace ZakYip.Singulation.Drivers.Common;

// 公共 API
public class AxisController
{
    private readonly AxisHelper _helper = new();
    
    public void ProcessAxis(int axisId, double speed)
    {
        var normalized = _helper.NormalizeSpeed(speed);
        // ... 处理逻辑
    }
}

// 文件作用域辅助类：只在此文件中使用
file sealed class AxisHelper
{
    public double NormalizeSpeed(double speed)
        => Math.Clamp(speed, 0, 3000);
}

// 文件作用域结构体：只在此文件中使用
file readonly record struct AxisState(int Id, double Speed, bool IsActive);
```

### ❌ 避免做法

```csharp
// 不要：将仅用于一个文件的辅助类暴露为 internal
namespace ZakYip.Singulation.Drivers.Common;

public class AxisController
{
    private readonly AxisHelper _helper = new();
}

// 污染命名空间，可能被其他地方误用
internal sealed class AxisHelper  // 应该使用 file
{
    public double NormalizeSpeed(double speed)
        => Math.Clamp(speed, 0, 3000);
}
```

### 优势
- ✅ 防止实现细节泄露
- ✅ 减少命名空间污染
- ✅ 提高代码组织性
- ✅ 避免意外依赖
- ✅ 更好的模块化设计

## 4. 使用 record 处理不可变数据

Record 是 DTO 和只读数据的理想选择。

### 规则
- 对于**数据传输对象（DTO）**，使用 `record class`
- 对于**轻量级值类型数据**，使用 `readonly record struct`
- 使用 `sealed` 防止不必要的继承
- 优先使用主构造函数（Primary Constructor）简化语法

### ✅ 推荐做法

```csharp
// DTO：使用 sealed record class
public sealed record class StatusSnapshot(
    bool Running,
    byte CameraFps,
    IReadOnlyList<(string Sn, byte State)> Cameras,
    VisionAlarm AlarmFlags
);

// 轻量级值类型：使用 readonly record struct
public readonly record struct ParcelPose(
    float CenterXmm,
    float CenterYmm,
    float LengthMm,
    float WidthMm,
    float AngleDeg
);

// 带验证的 record
public sealed record class SpeedSet
{
    public required DateTime TimestampUtc { get; init; }
    public required int Sequence { get; init; }
    public required IReadOnlyList<int> MainMmps { get; init; }
    public required IReadOnlyList<int> EjectMmps { get; init; }
    
    // 可以添加验证逻辑
    public SpeedSet
    {
        if (MainMmps.Count == 0)
            throw new ArgumentException("MainMmps cannot be empty", nameof(MainMmps));
    }
}
```

### ❌ 避免做法

```csharp
// 不要：对 DTO 使用传统 class
public class StatusSnapshot
{
    public bool Running { get; set; }
    public byte CameraFps { get; set; }
    // ... 冗长且可变
}

// 不要：对值类型使用 class
public class ParcelPose  // 应该使用 readonly record struct
{
    public float CenterXmm { get; set; }
    // ... 分配在堆上，性能较差
}

// 不要：允许继承 record
public record class StatusSnapshot { }  // 缺少 sealed
```

### 优势
- ✅ 值相等性语义（Value Equality）
- ✅ 简洁的语法
- ✅ 不可变性（Immutability）
- ✅ 内置解构支持
- ✅ `with` 表达式支持
- ✅ `readonly record struct` 避免堆分配

## 5. 保持方法专注且小巧

一个方法 = 一个职责。较小的方法更易于阅读、测试和重用。

### 规则
- 每个方法应该**只做一件事**
- 方法长度尽量控制在 **20-30 行以内**
- 如果方法超过一屏，考虑拆分
- 使用有意义的方法名，清晰表达意图
- 复杂逻辑拆分为多个小方法

### ✅ 推荐做法

```csharp
// 好：每个方法职责单一
public class AxisController
{
    public async Task<bool> StartAxisAsync(int axisId)
    {
        if (!ValidateAxisId(axisId))
            return false;
            
        PrepareAxis(axisId);
        await EnableAxisAsync(axisId);
        await WaitForAxisReadyAsync(axisId);
        
        return true;
    }
    
    private bool ValidateAxisId(int axisId)
        => axisId >= 0 && axisId < MaxAxisCount;
    
    private void PrepareAxis(int axisId)
    {
        ClearAxisErrors(axisId);
        ResetAxisPosition(axisId);
    }
    
    private async Task EnableAxisAsync(int axisId)
    {
        await _driver.SetAxisStateAsync(axisId, AxisState.Enabled);
    }
    
    private async Task WaitForAxisReadyAsync(int axisId)
    {
        var timeout = TimeSpan.FromSeconds(5);
        await WaitUntilAsync(() => IsAxisReady(axisId), timeout);
    }
}
```

### ❌ 避免做法

```csharp
// 不好：方法过长，职责不清
public async Task<bool> StartAxisAsync(int axisId)
{
    // 验证
    if (axisId < 0 || axisId >= MaxAxisCount)
        return false;
    
    // 准备
    _errors[axisId].Clear();
    _positions[axisId] = 0;
    
    // 启用
    await _driver.SendCommand(axisId, 0x6040, 0x06);
    await Task.Delay(10);
    await _driver.SendCommand(axisId, 0x6040, 0x07);
    await Task.Delay(10);
    await _driver.SendCommand(axisId, 0x6040, 0x0F);
    
    // 等待就绪
    var startTime = DateTime.UtcNow;
    while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(5))
    {
        var state = await _driver.ReadRegister(axisId, 0x6041);
        if ((state & 0x37) == 0x37)
            break;
        await Task.Delay(50);
    }
    
    // ... 更多代码
    return true;
}
```

### 优势
- ✅ 更易于理解和维护
- ✅ 更容易编写单元测试
- ✅ 更好的代码重用
- ✅ 更容易定位和修复 bug
- ✅ 提高团队协作效率

## 6. 不需要可变性时优先使用 readonly struct

防止意外更改并提高性能。

### 规则
- 对于**值类型数据**，优先使用 `readonly struct`
- 结合 `record struct` 获得更好的语法
- 对于性能关键的代码路径，使用 `readonly struct` 避免防御性拷贝
- 确保所有字段都是 `readonly`

### ✅ 推荐做法

```csharp
// 轻量级不可变值类型：使用 readonly record struct
public readonly record struct ParcelPose(
    float CenterXmm,
    float CenterYmm,
    float LengthMm,
    float WidthMm,
    float AngleDeg
);

// 性能关键的值类型
public readonly struct Point2D
{
    public readonly double X;
    public readonly double Y;
    
    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public double DistanceTo(Point2D other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

// 事件状态封装（文件作用域 + readonly）
file readonly record struct EvState<T>(
    object Sender,
    EventHandler<T> Handler,
    T Args
);
```

### ❌ 避免做法

```csharp
// 不要：可变 struct（难以推理，容易出错）
public struct ParcelPose
{
    public float CenterXmm { get; set; }  // 可变！
    public float CenterYmm { get; set; }  // 可变！
}

// 不要：对值类型数据使用 class
public class Point2D  // 应该使用 readonly struct
{
    public double X { get; }
    public double Y { get; }
    // 不必要的堆分配
}

// 不要：混用 readonly 和非 readonly 字段
public struct MixedStruct
{
    public readonly int X;
    public int Y;  // 非 readonly，破坏了不可变性
}
```

### 优势
- ✅ 避免防御性拷贝，提高性能
- ✅ 值语义更清晰
- ✅ 线程安全（不可变）
- ✅ 避免意外修改
- ✅ 更好的编译器优化

## 7. 其他最佳实践

### 7.1 命名约定

```csharp
// Pascal Case for public members
public class AxisController { }
public void ProcessAxis() { }
public int MaxSpeed { get; }

// Camel Case with _ prefix for private fields
private readonly ILogger _logger;
private int _axisCount;

// Camel Case for parameters and local variables
public void SetSpeed(int axisId, double targetSpeed)
{
    var normalizedSpeed = NormalizeSpeed(targetSpeed);
}
```

### 7.2 异步编程

```csharp
// ✅ 推荐：Async 后缀 + Task 返回类型
public async Task<bool> StartAxisAsync(int axisId)
{
    await Task.Delay(100);
    return true;
}

// ❌ 避免：async void（除了事件处理程序）
public async void StartAxis()  // 不好！
{
    await Task.Delay(100);
}
```

### 7.3 使用表达式体

```csharp
// ✅ 简单方法使用表达式体
public bool IsValid(int value) => value >= 0 && value < 100;
public double Square(double x) => x * x;

// ✅ 只读属性使用表达式体
public double Speed => _rpm * _gearRatio;
public bool IsRunning => _state == State.Running;
```

### 7.4 现代 C# 特性

```csharp
// ✅ 使用 using 声明（而非 using 语句块）
public void ProcessFile(string path)
{
    using var stream = File.OpenRead(path);
    // ... stream 在方法结束时自动释放
}

// ✅ 使用模式匹配
public string GetStatus(object obj) => obj switch
{
    null => "Null",
    int i when i > 0 => "Positive",
    int i when i < 0 => "Negative",
    string s => $"String: {s}",
    _ => "Unknown"
};

// ✅ 使用目标类型 new
Dictionary<string, int> map = new();
List<string> list = new();
```

## 8. 代码审查检查清单

在提交代码前，请确认：

- [ ] 所有必需属性都使用了 `required` + `init`
- [ ] 项目已启用 `<Nullable>enable</Nullable>`
- [ ] 可空引用类型正确标注（使用 `?`）
- [ ] 文件内部的辅助类使用了 `file` 修饰符
- [ ] DTO 和只读数据使用了 `record`
- [ ] 轻量级值类型使用了 `readonly record struct`
- [ ] 方法保持简短（< 30 行），职责单一
- [ ] 值类型数据使用了 `readonly struct`
- [ ] 代码通过编译，无警告
- [ ] 遵循项目命名约定

## 9. 参考资源

- [C# 编码约定（Microsoft 官方）](https://learn.microsoft.com/zh-cn/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Records（C# 参考）](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/builtin-types/record)
- [可空引用类型](https://learn.microsoft.com/zh-cn/dotnet/csharp/nullable-references)
- [readonly（C# 参考）](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/readonly)
- [required 修饰符](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/required)
- [文件作用域类型](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/file)

---

**版本**: 1.0  
**最后更新**: 2024-11-14  
**维护者**: ZakYip.Singulation 团队
