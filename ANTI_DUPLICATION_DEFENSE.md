# 影分身防线 (Anti-Duplication Defense)

本文档建立了一套全面的代码重复防御系统，从工具、流程到最佳实践，帮助团队避免产生"影分身"代码。

## 🛡️ 防线概述

### 三层防御体系

```
┌─────────────────────────────────────────────────────┐
│  第一层：预防（Prevention）                          │
│  - 编码规范和最佳实践                               │
│  - 代码模板和脚手架                                 │
│  - 设计模式和架构指导                               │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  第二层：检测（Detection）                          │
│  - 自动化代码分析工具                               │
│  - IDE实时提示                                      │
│  - CI/CD集成检查                                    │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  第三层：修复（Remediation）                        │
│  - 重构工具和指南                                   │
│  - 代码审查流程                                     │
│  - 持续改进机制                                     │
└─────────────────────────────────────────────────────┘
```

---

## 🎯 第一层：预防（Prevention）

### 1.1 强制性编码规范

#### 规则1: DRY原则 (Don't Repeat Yourself)
**规定**: 相同或相似的代码块不能出现超过2次。

**实施**:
```csharp
// ❌ 违规：重复的验证逻辑
public void ProcessAxis1(int axisId) {
    if (axisId < 0 || axisId > 100) throw new ArgumentException();
    // ...
}

public void ProcessAxis2(int axisId) {
    if (axisId < 0 || axisId > 100) throw new ArgumentException();
    // ...
}

// ✅ 正确：提取公共方法
private void ValidateAxisId(int axisId) {
    if (axisId < 0 || axisId > 100) 
        throw new ArgumentException($"Invalid axis ID: {axisId}");
}

public void ProcessAxis1(int axisId) {
    ValidateAxisId(axisId);
    // ...
}

public void ProcessAxis2(int axisId) {
    ValidateAxisId(axisId);
    // ...
}
```

#### 规则2: 优先使用继承或组合
**规定**: 发现相似类时，必须评估是否可以使用继承或组合。

**决策树**:
```
发现相似代码
    ↓
是否共享接口/行为？
    ├─ 是 → 提取基类或接口
    └─ 否 → 是否共享数据/状态？
            ├─ 是 → 使用组合
            └─ 否 → 提取静态工具方法
```

#### 规则3: 强制使用项目工具类
**规定**: 禁止在业务代码中直接实现已有工具方法的功能。

**项目工具类清单**:
```csharp
// 已提供的工具类 - 必须使用
- LeadshineHelpers - 雷赛通用辅助
  └─ FireEachNonBlocking() - 事件触发
  └─ ToStopwatchTicks() - 时间转换

- LeadshineConversions - 单位换算
  └─ MmpsToLoadPps() - 速度转换
  └─ Mmps2ToLoadPps2() - 加速度转换

- LeadshinePdoHelpers - PDO操作
  └─ WriteRxPdoWithPool() - PDO写入
  └─ ReadTxPdoWithPool() - PDO读取

- ICabinetIsolator - 安全执行
  └─ SafeExecute() - 安全执行操作
  └─ SafeExecuteAsync() - 异步安全执行
```

### 1.2 代码模板和脚手架

#### 模板1: 新建Service类
```csharp
// 使用此模板创建新的Service类
namespace ZakYip.Singulation.Infrastructure.Services;

public class MyNewService : IHostedService
{
    private readonly ILogger<MyNewService> _logger;
    private readonly ICabinetIsolator _isolator;
    
    public MyNewService(
        ILogger<MyNewService> logger,
        ICabinetIsolator isolator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isolator = isolator ?? throw new ArgumentNullException(nameof(isolator));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {ServiceName}", nameof(MyNewService));
        // 使用 _isolator.SafeExecuteAsync 执行操作
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {ServiceName}", nameof(MyNewService));
        return Task.CompletedTask;
    }
}
```

#### 模板2: 新建Helper类
```csharp
// 文件作用域工具类模板
namespace ZakYip.Singulation.Infrastructure.MyArea;

public class MyFeature
{
    private readonly MyHelper _helper = new();
    
    public void DoSomething()
    {
        _helper.HelperMethod();
    }
}

// 使用 file 修饰符限制作用域
file sealed class MyHelper
{
    public void HelperMethod()
    {
        // 工具方法实现
    }
}
```

### 1.3 架构设计指导

#### 指导原则1: 单一职责原则 (SRP)
每个类只负责一件事，避免"上帝类"。

**检查清单**:
- [ ] 类名能否清晰表达单一职责？
- [ ] 类的方法是否都服务于这一职责？
- [ ] 修改此类是否只有一个理由？

#### 指导原则2: 接口隔离原则 (ISP)
不要强迫客户端依赖它们不使用的接口。

**实践**:
```csharp
// ❌ 违规：臃肿的接口
public interface IAxisOperations
{
    void Start();
    void Stop();
    void Reset();
    void Configure();
    void Monitor();
    void Diagnose();
}

// ✅ 正确：分离的接口
public interface IAxisControl
{
    void Start();
    void Stop();
    void Reset();
}

public interface IAxisConfiguration
{
    void Configure();
}

public interface IAxisDiagnostics
{
    void Monitor();
    void Diagnose();
}
```

---

## 🔍 第二层：检测（Detection）

### 2.1 自动化代码分析工具

#### 工具1: .editorconfig 规则强化

已启用的防重复规则：
```ini
# CA1502: 避免过度复杂
dotnet_diagnostic.CA1502.severity = warning
dotnet_code_quality.CA1502.cyclomatic_complexity = 25

# CA1505: 避免不可维护的代码
dotnet_diagnostic.CA1505.severity = warning
dotnet_code_quality.CA1505.maintainability_index = 20

# CA1506: 避免过度类耦合
dotnet_diagnostic.CA1506.severity = warning
dotnet_code_quality.CA1506.class_coupling_threshold = 50
```

#### 工具2: 自定义代码分析脚本

在项目根目录创建 `tools/check-duplication.sh`:
```bash
#!/bin/bash
# 代码重复检测脚本

echo "🔍 检查代码重复..."

# 1. 检查相同的方法实现
echo "1. 检查重复方法实现..."
find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | while read file; do
    # 提取方法签名和内容的hash
    grep -A 20 "^\s*public\|^\s*private\|^\s*protected" "$file" | \
    md5sum | sort | uniq -d
done

# 2. 检查SafeExecute模式重复
echo "2. 检查SafeExecute模式重复..."
SAFE_EXEC_COUNT=$(grep -r "public.*SafeExecute" --include="*.cs" | grep -v "obj/" | wc -l)
if [ "$SAFE_EXEC_COUNT" -gt 2 ]; then
    echo "⚠️  发现 $SAFE_EXEC_COUNT 处SafeExecute实现（预期≤2）"
    grep -rn "public.*SafeExecute" --include="*.cs" | grep -v "obj/"
fi

# 3. 检查事件触发模式重复
echo "3. 检查事件触发模式重复..."
FIRE_EVENT_COUNT=$(grep -r "Task\.Run.*Invoke" --include="*.cs" | grep -v "obj/" | wc -l)
if [ "$FIRE_EVENT_COUNT" -gt 10 ]; then
    echo "⚠️  发现 $FIRE_EVENT_COUNT 处手动事件触发（建议使用FireEachNonBlocking）"
fi

# 4. 检查相似的类名
echo "4. 检查相似的类名（可能表示重复）..."
find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" -exec basename {} \; | \
    sed 's/\.cs$//' | sort | uniq -d

echo "✅ 检查完成"
```

#### 工具3: Git Pre-commit Hook

创建 `.git/hooks/pre-commit`:
```bash
#!/bin/bash
# Pre-commit hook: 检测代码重复

echo "🛡️ 运行影分身防线检查..."

# 获取即将提交的C#文件
CHANGED_CS_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$')

if [ -z "$CHANGED_CS_FILES" ]; then
    echo "✅ 没有C#文件变更"
    exit 0
fi

# 检查1: 是否使用了项目工具类
echo "检查是否正确使用项目工具类..."
for file in $CHANGED_CS_FILES; do
    # 检查是否有自己实现的SafeExecute
    if grep -q "try.*{.*action().*}.*catch.*Exception" "$file" 2>/dev/null; then
        if ! grep -q "using.*ICabinetIsolator\|: ICabinetIsolator" "$file" 2>/dev/null; then
            echo "⚠️  $file 可能重复实现了SafeExecute模式"
            echo "   建议使用 ICabinetIsolator.SafeExecute()"
        fi
    fi
    
    # 检查是否有自己实现的事件触发
    if grep -q "Task\.Run.*=>.*Invoke" "$file" 2>/dev/null; then
        if ! grep -q "LeadshineHelpers\.FireEachNonBlocking" "$file" 2>/dev/null; then
            echo "⚠️  $file 可能重复实现了事件触发模式"
            echo "   建议使用 LeadshineHelpers.FireEachNonBlocking()"
        fi
    fi
done

echo "✅ 预提交检查完成"
exit 0
```

### 2.2 IDE实时提示

#### Visual Studio 配置
在 `.editorconfig` 中已配置的实时提示：
- IDE0001: 简化名称
- IDE0002: 简化成员访问
- IDE0004: 删除不必要的强制转换
- IDE0005: 删除不必要的using指令

#### Rider / VS Code 配置
推荐安装的扩展：
- SonarLint - 实时代码质量检查
- CodeMaid - 代码清理和重构
- ReSharper (Rider内置) - 代码分析和重构

### 2.3 CI/CD 集成检查

#### GitHub Actions 工作流

创建 `.github/workflows/anti-duplication.yml`:
```yaml
name: Anti-Duplication Check

on:
  pull_request:
    branches: [ main, develop ]
  push:
    branches: [ main, develop ]

jobs:
  check-duplication:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
      with:
        fetch-depth: 0
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Run duplication check
      run: |
        chmod +x tools/check-duplication.sh
        ./tools/check-duplication.sh
    
    - name: Check code metrics
      run: |
        dotnet tool install --global dotnet-counters
        # 检查代码度量
        find . -name "*.csproj" -not -path "*/obj/*" -exec \
          dotnet build {} -p:RunAnalyzers=true -p:TreatWarningsAsErrors=false \;
    
    - name: Fail if duplication found
      if: failure()
      run: |
        echo "❌ 发现代码重复问题，请修复后再提交"
        exit 1
```

---

## 🔧 第三层：修复（Remediation）

### 3.1 重构工具和指南

#### 重构模式1: 提取方法 (Extract Method)

**识别信号**:
- 方法超过30行
- 代码块有明确的注释说明其用途
- 相同的代码片段出现多次

**重构步骤**:
1. 选择要提取的代码块
2. 识别输入参数和返回值
3. 创建新方法并移动代码
4. 使用有意义的方法名
5. 替换所有重复出现的位置

#### 重构模式2: 提取类 (Extract Class)

**识别信号**:
- 类超过500行
- 一组方法总是一起使用
- 类有多个职责

**重构步骤**:
```csharp
// Before: 臃肿的类
public class AxisController
{
    // 轴控制
    public void Start() { }
    public void Stop() { }
    
    // 数据验证
    public bool ValidatePosition() { }
    public bool ValidateSpeed() { }
    
    // 数据转换
    public double ConvertMmpsToRpm() { }
    public double ConvertRpmToMmps() { }
}

// After: 职责分离
public class AxisController
{
    private readonly AxisValidator _validator;
    private readonly AxisConverter _converter;
    
    public void Start() { }
    public void Stop() { }
}

file sealed class AxisValidator
{
    public bool ValidatePosition() { }
    public bool ValidateSpeed() { }
}

file sealed class AxisConverter
{
    public double ConvertMmpsToRpm() { }
    public double ConvertRpmToMmps() { }
}
```

#### 重构模式3: 使用策略模式替代重复逻辑

**场景**: 多个类有相似但不完全相同的算法。

```csharp
// Before: 重复的逻辑
public class VendorADriver
{
    public void ProcessData()
    {
        // 数据预处理
        // 调用VendorA的API
        // 数据后处理
    }
}

public class VendorBDriver
{
    public void ProcessData()
    {
        // 数据预处理（相同）
        // 调用VendorB的API（不同）
        // 数据后处理（相同）
    }
}

// After: 策略模式
public interface IVendorStrategy
{
    void CallVendorApi();
}

public abstract class BaseDriver
{
    protected readonly IVendorStrategy _strategy;
    
    protected BaseDriver(IVendorStrategy strategy)
    {
        _strategy = strategy;
    }
    
    public void ProcessData()
    {
        PreProcess();
        _strategy.CallVendorApi();
        PostProcess();
    }
    
    private void PreProcess() { /* 通用预处理 */ }
    private void PostProcess() { /* 通用后处理 */ }
}

public class VendorADriver : BaseDriver
{
    public VendorADriver() : base(new VendorAStrategy()) { }
}

file sealed class VendorAStrategy : IVendorStrategy
{
    public void CallVendorApi() { /* VendorA特定实现 */ }
}
```

### 3.2 代码审查流程

#### Pull Request 检查清单

每个PR必须通过以下检查：

**重复代码检查**:
- [ ] 是否有3行以上的重复代码？
- [ ] 是否有相似的类或方法名？
- [ ] 是否可以使用现有的工具类或辅助方法？
- [ ] 是否违反了DRY原则？

**设计模式检查**:
- [ ] 是否正确使用了继承和组合？
- [ ] 是否遵循了SOLID原则？
- [ ] 是否使用了适当的设计模式？

**代码质量检查**:
- [ ] 圈复杂度是否≤25？
- [ ] 类耦合度是否≤50？
- [ ] 方法长度是否≤30行？

#### 审查者指南

**发现重复代码时**:
1. 标记所有重复的位置
2. 建议重构方案（提取方法/类、使用继承等）
3. 指出可以使用的现有工具类
4. 要求修改后再次审查

**审查模板**:
```markdown
## 代码重复问题

**位置**: 
- FileA.cs:123-145
- FileB.cs:234-256

**重复内容**: 
SafeExecute模式的实现

**建议**: 
请使用 `ICabinetIsolator.SafeExecute()` 替代自己的实现。

**相关文档**: 
参见 ANTI_DUPLICATION_DEFENSE.md 第1.3节
```

### 3.3 持续改进机制

#### 每月代码健康报告

自动生成报告，跟踪以下指标：

**指标1: 代码重复率**
```
目标: <3%
当前: 2.1%
趋势: ↓ (上月2.5%)
```

**指标2: 方法复杂度**
```
目标: 平均≤10
当前: 8.3
趋势: → (上月8.4)
```

**指标3: 类耦合度**
```
目标: 平均≤30
当前: 28.5
趋势: ↓ (上月31.2)
```

#### 技术债务管理

**流程**:
1. **识别**: 通过自动化工具和代码审查识别技术债务
2. **评估**: 评估影响范围和修复成本
3. **优先级**: 根据影响和成本确定优先级
4. **计划**: 每个Sprint分配20%时间处理技术债务
5. **跟踪**: 在Backlog中跟踪技术债务项

**技术债务卡片模板**:
```markdown
## [技术债务] SafeExecute重复实现

**位置**: CabinetIsolator.cs, SafeOperationHelper.cs
**影响**: 中
**修复成本**: 2小时
**优先级**: P2
**计划Sprint**: Sprint 12

**修复方案**:
1. 统一到ICabinetIsolator
2. 更新所有调用点
3. 移除重复实现
```

---

## 📚 附录

### A. 常见重复模式识别

#### 模式1: 参数验证重复
```csharp
// 重复模式
if (value < min || value > max) throw new ArgumentException();

// 解决方案：创建验证类
file static class Validators
{
    public static void ValidateRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, 
                $"Value {value} is out of range [{min}, {max}]");
    }
}
```

#### 模式2: 日志记录重复
```csharp
// 重复模式
try {
    DoSomething();
} catch (Exception ex) {
    _logger.LogError(ex, "Operation failed");
    throw;
}

// 解决方案：使用ICabinetIsolator
_isolator.SafeExecute(
    () => DoSomething(),
    "DoSomething",
    ex => _logger.LogError(ex, "Operation failed")
);
```

#### 模式3: 异步事件触发重复
```csharp
// 重复模式
_ = Task.Run(() => {
    try {
        OnEvent?.Invoke(this, args);
    } catch (Exception ex) {
        _logger.LogError(ex, "Event handler failed");
    }
});

// 解决方案：使用LeadshineHelpers
LeadshineHelpers.FireEachNonBlocking(
    sender: this,
    handler: OnEvent,
    args: args
);
```

### B. 工具类索引

| 工具类 | 用途 | 位置 |
|--------|------|------|
| ICabinetIsolator | 安全执行操作 | Infrastructure/Cabinet/ |
| LeadshineHelpers | 通用辅助方法 | Drivers/Leadshine/ |
| LeadshineConversions | 单位换算 | Drivers/Leadshine/ |
| LeadshinePdoHelpers | PDO操作 | Drivers/Leadshine/ |

### C. 参考资源

**内部文档**:
- `copilot-instructions.md` - 编码规范
- `ISSUE_DETECTION_REPORT.md` - 问题检测报告
- `QUICK_FIX_GUIDE.md` - 快速修复指南

**外部资源**:
- [Refactoring Guru - 设计模式](https://refactoring.guru/design-patterns)
- [Clean Code - Robert C. Martin](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882)
- [Code Complete - Steve McConnell](https://www.amazon.com/Code-Complete-Practical-Handbook-Construction/dp/0735619670)

---

## 🎯 实施计划

### 阶段1: 立即执行（本周）
- [x] 创建本防线文档
- [ ] 配置 .editorconfig 规则
- [ ] 创建检测脚本
- [ ] 设置 Git pre-commit hook

### 阶段2: 短期（2周内）
- [ ] 实施 GitHub Actions 工作流
- [ ] 更新代码审查检查清单
- [ ] 培训团队成员
- [ ] 修复现有的SafeExecute重复

### 阶段3: 中期（1个月内）
- [ ] 建立每月代码健康报告
- [ ] 实施技术债务管理流程
- [ ] 创建代码模板库
- [ ] 完善工具类文档

### 阶段4: 持续改进
- [ ] 定期审查和更新防线
- [ ] 收集团队反馈
- [ ] 优化检测工具
- [ ] 扩展最佳实践库

---

**版本**: 1.0  
**创建日期**: 2025-12-06  
**维护者**: ZakYip.Singulation 团队  
**最后更新**: 2025-12-06
