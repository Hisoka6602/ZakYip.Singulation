# ZakYip.Singulation C# 编码规范

本文档定义了 ZakYip.Singulation 项目的 C# 编码规范和最佳实践。所有代码贡献者应遵循这些指南以确保代码质量、可维护性和一致性。

## 核心原则

本规范基于以下核心原则：

1. **零容忍影分身**：重复代码是最危险的技术债务，必须立即消除
2. **零容忍冗余代码**：未使用的代码是隐形负担，必须立即删除
3. **PR 完整性**：小型 PR 必须完整，大型 PR 必须登记技术债
4. **Id 类型统一**：所有内部 Id 使用 `long` 类型
5. **完整的 API 文档**：所有 API 端点必须有详细的 Swagger 注释
6. **使用现代 C# 特性**：record, readonly struct, file class, required, init
7. **启用可空引用类型**：明确表达可空性
8. **通过抽象接口访问系统资源**：时间、基础设施
9. **确保线程安全和并发正确性**
10. **保持方法短小精悍**：单一职责
11. **遵循清晰的命名约定**
12. **遵守分层架构原则**
13. **充分的测试覆盖**

**违规后果**: 任何违反本文档规则的修改，均视为**无效修改**，不得合并到主分支。

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

## 9. 影分身零容忍策略（Anti-Duplication）

### 核心原则

**影分身 = 重复代码 = 最危险的技术债务**

重复代码会导致：
- 修改时需要同步多处
- 容易遗漏某些副本
- 测试覆盖率下降
- 维护成本倍增

### 9.1 严禁的影分身模式

#### ❌ 禁止：转发 Facade/Adapter/Wrapper

```csharp
// 不要：纯粹的转发包装
public class UserServiceWrapper
{
    private readonly IUserService _inner;
    
    public Task<User> GetUserAsync(string id)
        => _inner.GetUserAsync(id);  // 纯转发，没有价值
}
```

#### ❌ 禁止：重复定义工具方法

```csharp
// 不要：在多个地方定义相同的工具方法
// 文件 A
public static class StringHelpers
{
    public static bool IsNullOrWhiteSpace(string value)
        => string.IsNullOrWhiteSpace(value);
}

// 文件 B
public static class TextUtils
{
    public static bool IsEmpty(string value)
        => string.IsNullOrWhiteSpace(value);  // 重复！
}
```

#### ❌ 禁止：重复定义 DTO/Model

```csharp
// 不要：定义结构相同但名称不同的 DTO
public record UserDto(string Name, string Email);
public record UserModel(string Name, string Email);  // 重复！
public record UserInfo(string Name, string Email);   // 重复！
```

#### ❌ 禁止：重复定义常量

```csharp
// 不要：在多个地方定义相同的常量
public class ConfigA
{
    public const int MaxRetries = 3;
}

public class ConfigB
{
    public const int MaxRetries = 3;  // 重复！
}
```

### 9.2 消除影分身的方法

✅ **提取共享工具类**
✅ **提取基类或接口**
✅ **使用组合替代继承**
✅ **定义统一的常量类**
✅ **使用依赖注入共享服务**

### 9.3 Code Review 必查项

- [ ] 是否有纯转发的 Facade/Adapter/Wrapper
- [ ] 是否有重复的工具方法
- [ ] 是否有结构相同的 DTO/Model
- [ ] 是否有重复定义的常量
- [ ] 历史影分身是否已清理（如果涉及相关模块）

## 10. 冗余代码零容忍策略

### 核心原则

**冗余代码 = 未使用的代码 = 隐形负担**

未使用的代码会导致：
- 增加代码阅读和理解难度
- 误导开发者
- 增加维护成本
- 降低代码质量

### 10.1 严禁的冗余模式

#### ❌ 禁止：定义从未注册的服务

```csharp
// 不要：定义服务但从未在 DI 中注册
public class UnusedService : IUnusedService
{
    // 从未在 Startup.cs 或 Program.cs 中注册
}
```

#### ❌ 禁止：注册从未使用的服务

```csharp
// 不要：注册服务但从未被注入
services.AddScoped<INeverUsedService, NeverUsedService>();
// 整个项目中没有任何地方注入 INeverUsedService
```

#### ❌ 禁止：注入从未调用的服务

```csharp
// 不要：注入服务但从未调用其方法
public class Controller
{
    private readonly IUnusedService _unused;  // 注入但从未使用
    
    public Controller(IUnusedService unused)
    {
        _unused = unused;
    }
}
```

#### ❌ 禁止：定义从未使用的方法和属性

```csharp
// 不要：定义方法但从未调用
public class Service
{
    public void UsedMethod() { }
    
    public void NeverCalledMethod() { }  // 从未被调用
}
```

### 10.2 消除冗余代码的方法

✅ **删除未注册的服务定义**
✅ **删除未使用的服务注册**
✅ **删除未调用的服务注入**
✅ **删除未使用的方法和属性**
✅ **使用代码分析工具检测死代码**

### 10.3 Code Review 必查项

- [ ] 是否有从未注册的服务
- [ ] 是否有注册但未使用的服务
- [ ] 是否有注入但未调用的服务
- [ ] 是否有从未使用的方法和属性
- [ ] 是否有从未使用的类型

## 11. Id 类型统一规范

### 核心原则

**所有内部 Id 统一使用 `long` 类型**

### 11.1 规则

- ✅ 所有实体 Id 使用 `long`
- ✅ 所有数据库主键使用 `long`
- ✅ 所有 API 的 Id 参数使用 `long`
- ❌ 禁止混用 `int` 和 `long` 作为同一语义的 Id

### 11.2 示例

```csharp
// ✅ 正确：统一使用 long
public class User
{
    public long Id { get; set; }
}

public class Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
}

// API Controller
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(long id)  // ✅ long
{
    // ...
}

// ❌ 错误：混用 int 和 long
public class User
{
    public int Id { get; set; }  // ❌ 应该用 long
}

public class Order
{
    public long Id { get; set; }
    public int UserId { get; set; }  // ❌ 不一致！
}
```

### 11.3 外部系统 Id

对于外部系统的 Id，如果其类型不是 `long`，应：
- 明确标注其来源和类型
- 在边界层进行类型转换
- 使用有意义的名称区分

```csharp
// ✅ 正确：明确标注外部 Id
public class IntegrationDto
{
    public long InternalOrderId { get; set; }  // 内部 Id，long
    public string ExternalOrderId { get; set; }  // 外部系统 Id，string
}
```

## 12. API 文档规范（Swagger 注释）

### 核心原则

**所有 API 端点必须有完整的 Swagger 注释**

### 12.1 Controller 注释

```csharp
/// <summary>
/// 用户管理 API
/// </summary>
/// <remarks>
/// 提供用户的增删改查功能，包括：
/// - 用户注册
/// - 用户信息查询
/// - 用户信息更新
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // ...
}
```

### 12.2 Action 注释

```csharp
/// <summary>
/// 创建新用户
/// </summary>
/// <remarks>
/// 创建一个新的用户账户。
/// 
/// 业务规则：
/// - 用户名必须唯一
/// - 邮箱必须唯一
/// - 密码必须满足复杂度要求
/// </remarks>
/// <param name="request">用户创建请求</param>
/// <returns>创建成功的用户信息</returns>
/// <response code="200">创建成功</response>
/// <response code="400">请求参数无效</response>
/// <response code="409">用户名或邮箱已存在</response>
/// <response code="500">服务器内部错误</response>
[HttpPost]
[ProducesResponseType(typeof(UserDto), 200)]
[ProducesResponseType(typeof(ProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[SwaggerOperation(
    Summary = "创建新用户",
    Description = "创建一个新的用户账户",
    OperationId = "CreateUser",
    Tags = new[] { "Users" }
)]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    // ...
}
```

### 12.3 DTO 注释

```csharp
/// <summary>
/// 用户创建请求
/// </summary>
public sealed record CreateUserRequest
{
    /// <summary>
    /// 用户名（必填）
    /// </summary>
    /// <example>john_doe</example>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在 3-50 个字符之间")]
    public required string UserName { get; init; }
    
    /// <summary>
    /// 电子邮箱（必填）
    /// </summary>
    /// <example>john@example.com</example>
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public required string Email { get; init; }
    
    /// <summary>
    /// 初始密码（必填）
    /// </summary>
    /// <remarks>
    /// 密码必须至少包含 8 个字符，包括大小写字母、数字和特殊字符
    /// </remarks>
    /// <example>P@ssw0rd123</example>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度必须在 8-100 个字符之间")]
    public required string Password { get; init; }
}
```

### 12.4 Code Review 检查点

- [ ] 所有 Controller 类有 `<summary>` 注释
- [ ] 所有 Action 方法有 `[SwaggerOperation]` 特性
- [ ] 所有 Action 方法标注了所有可能的响应码
- [ ] 所有 DTO 属性有 `<summary>` 注释
- [ ] 复杂字段有 `<remarks>` 详细说明
- [ ] 关键字段有 `<example>` 示例值

## 13. 通讯与重试原则

### 13.1 客户端连接失败应无限重试 + 指数退避

**原则**: 与外部系统的连接采用**无限重试**策略，使用指数退避算法。

**推荐退避策略**:
```
初始退避: 200ms
指数增长序列: 
  尝试1: 200ms
  尝试2: 400ms (200ms × 2)
  尝试3: 800ms (400ms × 2)
  尝试4: 1600ms (800ms × 2)
  尝试5: 2000ms (1600ms × 2 = 3200ms，但被限制为最大值 2000ms)
  尝试6+: 2000ms, 2000ms, 2000ms, ...（无限重试，持续使用最大值）

说明：采用指数退避算法，每次失败后延迟时间翻倍，但不超过最大退避时间（2000ms）
```

**示例**:

```csharp
// ✅ 正确：无限重试，指数退避
public class ExternalServiceClient
{
    private const int InitialBackoffMs = 200;
    private const int MaxBackoffMs = 2000;
    
    public async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        int backoffMs = InitialBackoffMs;
        int attemptCount = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                attemptCount++;
                _logger.LogInformation($"尝试连接外部服务（第 {attemptCount} 次）...");
                
                await ConnectAsync();
                
                _logger.LogInformation("成功连接到外部服务");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"连接失败，{backoffMs}ms 后重试: {ex.Message}");
                
                await Task.Delay(backoffMs, cancellationToken);
                
                // 指数退避，但不超过最大值
                backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
            }
        }
    }
}
```

### 13.2 请求失败应记录日志但不自动重试

**原则**: 单次请求失败**只记录日志**，不进行自动重试，由调用方决定如何处理。

**示例**:

```csharp
// ✅ 正确：请求失败只记录日志
public async Task<bool> SendRequestAsync(string requestId, object payload)
{
    try
    {
        await _httpClient.PostAsJsonAsync("/api/endpoint", payload);
        
        _logger.LogInformation($"请求已发送: {requestId}");
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"发送请求失败: {requestId}");
        return false;  // ✅ 不重试，由调用方处理
    }
}
```

## 14. 测试与质量保证

### 14.1 单元测试覆盖核心逻辑

**原则**: 核心业务逻辑必须有充分的单元测试覆盖。

**测试覆盖率目标**:
- 领域层：≥ 85%
- 应用层：≥ 80%
- 基础设施层：≥ 70%
- 表示层：≥ 60%

### 14.2 禁止删除或注释测试

**原则**: **严格禁止**注释或删除现有的测试用例来绕过规则检查。

**示例**:

```csharp
// ❌ 错误：注释测试
// [Fact]
// public async Task Should_Return_Error_When_Invalid_Input()
// {
//     // 这个测试失败了，先注释掉
// }

// ✅ 正确：修复代码或更新测试
[Fact]
public async Task Should_Return_Error_When_Invalid_Input()
{
    // 修复代码使测试通过
    // 或更新测试以反映新的业务规则
    var result = await _service.ProcessAsync("");
    Assert.False(result.IsSuccess);
    Assert.Contains("Invalid input", result.ErrorMessage);
}
```

### 14.3 所有测试失败必须在当前 PR 中修复

**规则**:

1. 任意测试失败，一旦在本 PR 的 CI 中出现，就视为本 PR 的工作内容，**必须在当前 PR 中修复**
2. **禁止**在 PR 描述中说明"这是已有问题 / 与本 PR 无关"
3. **禁止**把测试失败留给"后续 PR 处理"

## 15. 代码清理规范

### 15.1 过时/废弃/重复代码必须立即删除

**规则**:

1. 一旦新增实现已经覆盖旧实现，旧实现必须在**同一个 PR 中立即删除**
2. 相同语义的两套实现不允许并存
3. 不允许通过 `[Obsolete]`、`Legacy`、`Deprecated` 等方式长期保留废弃代码

**禁止行为**:
- ❌ 新实现已投入使用，却仍长时间保留旧实现
- ❌ 为了"兼容历史"同时维护两套等价实现
- ❌ 新增代码继续依赖已明确不推荐使用的旧实现

### 15.2 禁止使用 global using

**规则**:
1. 代码中禁止使用 `global using` 指令
2. 现有的 `global using` 应在后续重构中逐步移除

## 16. 分层架构原则（DDD 分层）

### 架构层次

```
┌─────────────────────────────────────┐
│        Presentation（表示层）         │
│  - API Controllers                   │
│  - 输入验证                          │
└─────────────────────────────────────┘
              ↓ 依赖
┌─────────────────────────────────────┐
│       Application（应用层）           │
│  - 业务编排                          │
│  - 用例实现                          │
└─────────────────────────────────────┘
              ↓ 依赖
┌─────────────────────────────────────┐
│         Domain（领域层）              │
│  - 领域模型                          │
│  - 业务规则                          │
└─────────────────────────────────────┘
              ↓ 依赖
┌─────────────────────────────────────┐
│    Infrastructure（基础设施层）       │
│  - 数据持久化                        │
│  - 外部系统通信                      │
└─────────────────────────────────────┘
```

### 层次依赖规则

- 上层可以依赖下层
- 下层**不得**依赖上层
- 同层之间通过接口通信
- 基础设施层实现领域层定义的接口（依赖反转）

### Presentation 层职责（严格限制）

**允许的职责**:
- ✅ API Controller 端点定义（仅调用应用层服务）
- ✅ 输入模型验证
- ✅ 响应格式化

**禁止的行为**:
- ❌ 直接包含业务逻辑
- ❌ 直接访问数据库或仓储
- ❌ 直接访问外部系统
- ❌ 复杂的数据处理和转换

## 17. 完整的代码审查清单

在提交代码前，请检查：

### PR 完整性
- [ ] PR 可独立编译、测试通过
- [ ] 未留下"TODO: 后续PR"标记
- [ ] 大型 PR 的未完成部分已登记技术债

### 影分身检查（最重要）
- [ ] 未创建纯转发 Facade/Adapter/Wrapper/Proxy
- [ ] 未重复定义相同的工具方法
- [ ] 未重复定义相同结构的 DTO/Model
- [ ] 未重复定义相同的 Options/Settings
- [ ] 未在多处定义相同的常量
- [ ] 已清理历史影分身（如果涉及相关模块）

### 冗余代码检查
- [ ] 未定义从未在 DI 中注册的服务
- [ ] 未注册从未被注入使用的服务
- [ ] 未注入从未调用的服务
- [ ] 未定义从未使用的方法和属性
- [ ] 未定义从未使用的类型
- [ ] 已清理发现的冗余代码

### 类型使用
- [ ] DTO 和只读数据使用 `record` / `record struct`
- [ ] 小型值类型使用 `readonly struct`
- [ ] 工具类使用 `file` 作用域类型
- [ ] 必填属性使用 `required + init`

### Id 类型规范
- [ ] 所有内部 Id 统一使用 `long` 类型
- [ ] 未混用 `int` 和 `long` 作为同一语义的 Id
- [ ] 外部系统 Id 类型有明确说明
- [ ] Id 类型转换在边界层进行

### 可空引用类型
- [ ] 启用可空引用类型（`Nullable=enable`）
- [ ] 未新增 `#nullable disable`
- [ ] 明确区分可空和不可空引用

### 时间处理
- [ ] 所有时间通过抽象接口（如 `ISystemClock`）获取
- [ ] 未直接使用 `DateTime.Now` / `DateTime.UtcNow`

### 并发安全
- [ ] 跨线程集合使用线程安全容器或锁
- [ ] 无数据竞争风险

### 异常处理
- [ ] 后台服务使用安全执行包装器
- [ ] 异常信息清晰具体

### API 设计
- [ ] 请求模型使用 `record + required + 验证`
- [ ] 响应使用统一的包装类型
- [ ] **所有 API 端点有完整的 Swagger 注释**
- [ ] Controller 类有 `<summary>` 注释
- [ ] Action 方法有 `[SwaggerOperation]` 特性
- [ ] Action 方法标注了所有可能的响应码（200、400、404、500等）
- [ ] DTO 属性有 `<summary>` 注释
- [ ] 复杂字段有 `<remarks>` 和 `<example>`

### 方法设计
- [ ] 方法短小（< 30 行）
- [ ] 单一职责
- [ ] 清晰的命名
- [ ] `async` 方法包含 `await` 操作符

### 命名约定
- [ ] 遵循 PascalCase / camelCase 约定
- [ ] 接口以 `I` 开头
- [ ] 异步方法以 `Async` 结尾
- [ ] 私有字段使用 `_camelCase` 前缀
- [ ] 命名空间与文件夹结构匹配

### 分层架构
- [ ] 遵循分层职责
- [ ] 表示层不包含业务逻辑
- [ ] 通过接口访问基础设施

### 测试
- [ ] 核心逻辑有单元测试覆盖
- [ ] 所有测试通过
- [ ] 未删除或注释测试

### 代码清理
- [ ] 未使用 `global using`
- [ ] 已删除过时/废弃/重复代码
- [ ] 未保留 `[Obsolete]` / `Legacy` / `Deprecated` 代码

### 通讯与重试
- [ ] 连接失败使用无限重试 + 指数退避
- [ ] 请求失败只记录日志，不自动重试

## 18. 参考资源

- [C# 编码约定（Microsoft 官方）](https://learn.microsoft.com/zh-cn/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Records（C# 参考）](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/builtin-types/record)
- [可空引用类型](https://learn.microsoft.com/zh-cn/dotnet/csharp/nullable-references)
- [readonly（C# 参考）](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/readonly)
- [required 修饰符](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/required)
- [文件作用域类型](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/file)
- [通用 Copilot 编码标准原文](https://github.com/Hisoka6602/ZakYip.WheelDiverterSorter/blob/master/docs/GENERAL_COPILOT_CODING_STANDARDS.md)（外部参考）

---

**版本**: 2.0  
**最后更新**: 2025-12-14  
**维护者**: ZakYip.Singulation 团队

**更新说明**: 
- 整合了通用 Copilot 编码标准
- 新增影分身零容忍策略
- 新增冗余代码零容忍策略
- 新增 Id 类型统一规范
- 新增完整的 API 文档规范（Swagger 注释）
- 新增通讯与重试原则
- 增强测试与质量保证要求
- 新增代码清理规范
- 新增分层架构原则
- 更新完整的代码审查清单
