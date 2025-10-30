# 类型选择指南 (Type Selection Guide)

本文档提供了在 ZakYip.Singulation 项目中选择合适类型（class vs record vs struct）的指导原则。

## 目录
- [概述](#概述)
- [类型对比](#类型对比)
- [选择决策树](#选择决策树)
- [最佳实践](#最佳实践)
- [实际案例](#实际案例)

## 概述

C# 提供了多种类型系统来满足不同的需求：
- **class**: 引用类型，适用于可变的、具有身份的对象
- **record class**: 引用类型，适用于不可变的数据载体
- **struct**: 值类型，适用于小型、简单的值对象
- **readonly struct**: 值类型，适用于不可变的小型值对象
- **record struct**: 值类型，适用于不可变的数据载体（值语义）

## 类型对比

| 特性 | class | record class | struct | readonly struct | record struct |
|------|-------|--------------|--------|-----------------|---------------|
| 类型 | 引用 | 引用 | 值 | 值 | 值 |
| 默认可变性 | 可变 | 不可变（建议） | 可变 | 不可变 | 不可变 |
| 相等性 | 引用相等 | 值相等 | 值相等 | 值相等 | 值相等 |
| 继承 | 支持 | 支持 | 不支持 | 不支持 | 不支持 |
| 堆栈分配 | 否（堆） | 否（堆） | 是（栈） | 是（栈） | 是（栈） |
| 解构支持 | 手动实现 | 自动 | 手动实现 | 手动实现 | 自动 |
| with 表达式 | 否 | 是 | 否 | 否 | 是 |
| 适用场景 | 复杂对象 | DTO/事件 | 小型值对象 | 小型不可变值 | 小型数据载体 |

## 选择决策树

### 1. 这是一个数据传输对象（DTO）或请求/响应对象吗？

**是** → 使用 `sealed record class`

**优点:**
- 值相等语义（基于内容而非引用）
- 不可变性（通过 `init` 属性）
- 简洁的语法（位置参数、解构）
- 自动生成 `ToString()`、`GetHashCode()`、`Equals()`

**示例:**
```csharp
public sealed record class SetSpeedRequestDto {
    [Range(-100000, 100000)]
    public double LinearMmps { get; init; }
}

public sealed record class ApiResponse<T> {
    public bool Result { get; set; }
    public string Msg { get; set; } = string.Empty;
    public T? Data { get; set; }
}
```

### 2. 这是一个事件参数类吗？

**是** → 使用 `sealed record class`

**原因:**
- 事件参数应该是不可变的
- 值相等语义便于测试和比较
- 简化事件数据的创建和传递

**示例:**
```csharp
public sealed record class AxisErrorEventArgs {
    public required AxisId Axis { get; init; }
    public required Exception Exception { get; init; }
}

public sealed record class TransportErrorEventArgs {
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
    public bool IsTransient { get; init; } = true;
}
```

### 3. 这是一个小型值对象（≤16字节）吗？

**是** → 使用 `readonly struct` 或 `readonly record struct`

**优点:**
- 栈分配，减少 GC 压力
- 零拷贝语义（按值传递）
- 不可变性保证

**选择标准:**
- 大小 ≤ 16 字节（推荐）
- 逻辑上表示值而非实体
- 不需要多态

**示例:**
```csharp
// readonly struct - 简单值对象
public readonly record struct AxisId {
    public int Value { get; init; }
    public AxisId(int value) => Value = value;
    public override string ToString() => Value.ToString();
}

// readonly struct - 带计算属性
public readonly struct PprRatio {
    public int Numerator { get; init; }
    public int Denominator { get; init; }
    public double Value { get; init; }
    public bool IsExact => Denominator != 0 && (Numerator % Denominator) == 0;
    
    public PprRatio(int numerator, int denominator) {
        Numerator = numerator;
        Denominator = denominator;
        Value = denominator != 0 ? (double)numerator / denominator : numerator;
    }
}

// readonly record struct - 复合值对象
public readonly record struct KinematicParams {
    public double LinearVelocity { get; init; }
    public double AngularVelocity { get; init; }
    public double Acceleration { get; init; }
    public TimeSpan Duration { get; init; }
}
```

### 4. 这是一个领域实体或聚合根吗？

**是** → 使用 `class`

**原因:**
- 需要身份标识
- 可能需要可变状态
- 可能需要继承
- 生命周期管理

**示例:**
```csharp
public class AxisController : IDisposable {
    private readonly IAxisEventAggregator _eventAggregator;
    private AxisState _currentState;
    
    public AxisId Id { get; }
    public AxisState State => _currentState;
    
    public void UpdateState(AxisState newState) {
        _currentState = newState;
        // 触发事件等
    }
}
```

### 5. 这是一个配置对象吗？

**看情况:**

**运行时不可变** → 使用 `sealed record class`
```csharp
public sealed record class DatabaseOptions {
    public required string ConnectionString { get; init; }
    public int MaxRetries { get; init; } = 3;
}
```

**运行时可变** → 使用 `class`
```csharp
public class RuntimeSettings {
    public int MaxConcurrency { get; set; }
    public TimeSpan Timeout { get; set; }
}
```

## 最佳实践

### 1. 优先使用不可变类型

不可变类型更安全、更容易推理、支持并发。

```csharp
// ✅ 推荐
public sealed record class UserDto {
    public required string Name { get; init; }
    public required string Email { get; init; }
}

// ❌ 避免（除非确实需要可变性）
public class UserDto {
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 2. 使用 `sealed` 修饰符

对于 record class 和普通 class，如果不打算继承，使用 `sealed`。

```csharp
// ✅ 推荐
public sealed record class DecodeRequest { ... }

// ⚠️ 仅在需要继承时省略 sealed
public record class BaseRequest { ... }
public sealed record class DecodeRequest : BaseRequest { ... }
```

### 3. 使用 `required` 关键字

对于必需的属性，使用 `required` 关键字确保初始化。

```csharp
public sealed record class AxisCommandIssuedEventArgs {
    public required AxisId Axis { get; init; }
    public required string Invocation { get; init; }
    public required int Result { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Note { get; init; }  // 可选属性不需要 required
}
```

### 4. Struct 大小限制

- struct 应该 ≤ 16 字节
- 超过此大小考虑使用 class 或 record class
- 包含大型字段（如数组）使用 class

```csharp
// ✅ 适合 struct（8 字节）
public readonly record struct Point2D {
    public double X { get; init; }
    public double Y { get; init; }
}

// ❌ 不适合 struct（太大）
public struct LargeData {
    public double[] Values { get; init; }  // 引用，但 struct 本身不适合
    public string Description { get; init; }
}

// ✅ 改用 record class
public sealed record class LargeData {
    public required double[] Values { get; init; }
    public required string Description { get; init; }
}
```

### 5. 默认值处理

```csharp
// ✅ 为可选字段提供合理的默认值
public sealed record class TransportErrorEventArgs {
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
    public bool IsTransient { get; init; } = true;  // 默认值
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;  // 默认值
}
```

## 实际案例

### 案例 1: DTO - 使用 record class

```csharp
// API 请求 DTO
public sealed record class SetSpeedRequestDto {
    [Range(-100000, 100000, ErrorMessage = "线速度必须在 -100000 到 100000 之间")]
    public double LinearMmps { get; init; }
}

// API 响应 DTO
public sealed record class AxisCommandResultDto {
    [Required(ErrorMessage = "轴标识符不能为空")]
    public string AxisId { get; set; } = default!;
    public bool Accepted { get; set; }
    public string? LastError { get; set; }
}
```

### 案例 2: 事件参数 - 使用 record class

```csharp
// 旧方式（继承 EventArgs）
public sealed class AxisErrorEventArgs : EventArgs {
    public AxisErrorEventArgs(AxisId axis, Exception ex) {
        Axis = axis;
        Exception = ex;
    }
    public AxisId Axis { get; }
    public Exception Exception { get; }
}

// 新方式（record class）
public sealed record class AxisErrorEventArgs {
    public required AxisId Axis { get; init; }
    public required Exception Exception { get; init; }
}

// 使用方式更简洁
_eventAggregator.PublishError(new AxisErrorEventArgs {
    Axis = axisId,
    Exception = ex
});
```

### 案例 3: 值对象 - 使用 readonly struct

```csharp
// 轴标识（4 字节）
public readonly record struct AxisId {
    public int Value { get; init; }
    public AxisId(int value) => Value = value;
    public override string ToString() => Value.ToString();
}

// PPR 比率（16 字节：4 + 4 + 8）
public readonly struct PprRatio {
    public int Numerator { get; init; }
    public int Denominator { get; init; }
    public double Value { get; init; }
    public bool IsExact => Denominator != 0 && (Numerator % Denominator) == 0;
    
    public PprRatio(int numerator, int denominator) {
        Numerator = numerator;
        Denominator = denominator;
        Value = denominator != 0 ? (double)numerator / denominator : numerator;
    }
}
```

### 案例 4: 服务类 - 使用 class

```csharp
public class AxisController : IDisposable {
    private readonly ILogger<AxisController> _logger;
    private readonly IAxisEventAggregator _eventAggregator;
    private volatile bool _disposed;

    public AxisController(
        ILogger<AxisController> logger,
        IAxisEventAggregator eventAggregator) {
        _logger = logger;
        _eventAggregator = eventAggregator;
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        // 清理资源
    }
}
```

## 性能考量

### Record Class vs Class

- **内存**: 相同（都是引用类型）
- **性能**: record class 的相等比较可能稍慢（值相等 vs 引用相等）
- **建议**: 性能差异通常可忽略，优先考虑代码清晰度

### Struct vs Class

| 场景 | struct | class |
|------|--------|-------|
| 大小 ≤ 16 字节 | ✅ 更好 | ❌ 不必要的堆分配 |
| 大小 > 16 字节 | ❌ 复制开销大 | ✅ 更好 |
| 作为数组元素 | ✅ 连续内存布局 | ❌ 指针数组 |
| 作为字段 | ✅ 内联存储 | ❌ 额外解引用 |
| 装箱频繁 | ❌ 性能开销 | ✅ 已在堆上 |

## 迁移指南

### 从 class 到 record class

```csharp
// 前
public class AxisErrorEventArgs : EventArgs {
    public AxisErrorEventArgs(AxisId axis, Exception ex) {
        Axis = axis;
        Exception = ex;
    }
    public AxisId Axis { get; }
    public Exception Exception { get; }
}

// 后
public sealed record class AxisErrorEventArgs {
    public required AxisId Axis { get; init; }
    public required Exception Exception { get; init; }
}
```

### 从 struct 到 readonly struct

```csharp
// 前
public struct PprRatio {
    public int Numerator;
    public int Denominator;
    public double Value;
}

// 后
public readonly struct PprRatio {
    public int Numerator { get; init; }
    public int Denominator { get; init; }
    public double Value { get; init; }
    
    public PprRatio(int numerator, int denominator) {
        Numerator = numerator;
        Denominator = denominator;
        Value = denominator != 0 ? (double)numerator / denominator : numerator;
    }
}
```

## 总结

| 用途 | 推荐类型 | 示例 |
|------|----------|------|
| DTO/请求/响应 | `sealed record class` | SetSpeedRequestDto, ApiResponse |
| 事件参数 | `sealed record class` | AxisErrorEventArgs, TransportErrorEventArgs |
| 小型值对象 | `readonly struct` | AxisId, PprRatio |
| 领域实体 | `class` | AxisController, ServiceManager |
| 配置对象（不可变） | `sealed record class` | DatabaseOptions |
| 配置对象（可变） | `class` | RuntimeSettings |

遵循这些指南可以提高代码的一致性、可维护性和性能。
