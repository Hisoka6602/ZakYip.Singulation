# 性能回归测试指南

本文档介绍 ZakYip.Singulation 项目的性能回归测试系统，包括如何使用、理解结果以及如何处理性能问题。

## 📋 目录

- [概述](#概述)
- [CI/CD 集成](#cicd-集成)
- [性能基准测试](#性能基准测试)
- [稳定性测试](#稳定性测试)
- [性能阈值](#性能阈值)
- [如何使用](#如何使用)
- [故障排除](#故障排除)
- [最佳实践](#最佳实践)

## 概述

性能回归测试系统旨在：

✅ **自动检测性能退化** - 在 PR 和主分支上自动运行  
✅ **建立性能基线** - 存储主分支的性能基准数据  
✅ **对比分析** - 将 PR 的性能与基线对比  
✅ **稳定性监控** - 检测内存泄漏、线程泄漏等问题  
✅ **夜间测试** - 定期运行扩展稳定性测试  

## CI/CD 集成

### 触发条件

性能测试工作流会在以下情况下自动触发：

1. **Pull Request** - 向 `master` 分支提交 PR 时
   - 运行完整性能基准测试
   - 运行 10 分钟快速稳定性测试
   - 与基线对比并在 PR 中评论结果

2. **Push to Master** - 推送到 `master` 分支时
   - 运行完整性能基准测试
   - 更新性能基线数据
   - 运行快速稳定性测试

3. **Schedule** - 每天凌晨 2 点 UTC（北京时间上午 10 点）
   - 运行完整性能基准测试
   - 运行 1 小时扩展稳定性测试
   - 如果失败，自动创建 Issue

4. **Manual Trigger** - 手动触发
   - 可以在 Actions 页面手动运行

### 工作流组成

性能测试工作流包含三个任务：

#### 1. `performance-benchmarks`

运行所有性能基准测试套件：

- **Protocol Benchmarks** - 协议编解码性能
- **LINQ vs Loop Benchmarks** - LINQ 与循环性能对比
- **Batch Operation Benchmarks** - 批量操作性能
- **Memory Allocation Benchmarks** - 内存分配和 GC 压力
- **IO Operation Benchmarks** - IO 操作性能
- **Concurrency Benchmarks** - 并发操作性能

输出：
- BenchmarkDotNet 详细报告（JSON/HTML/CSV）
- PR 评论包含性能对比摘要
- 保存 90 天的历史数据

#### 2. `stability-test`

运行 10 分钟快速稳定性测试：

监控指标：
- 内存增长率
- GC 频率（Gen0/1/2）
- 线程数变化
- 性能稳定性评分

失败阈值：
- 稳定性评分 < 50/100

#### 3. `stability-test-extended`

（仅在定时任务或手动触发时运行）

运行 1 小时扩展稳定性测试：

- 更长时间监控，发现潜在问题
- 更严格的阈值（60/100）
- 失败时自动创建 Issue

## 性能基准测试

### 测试套件详情

#### 1. 协议编解码测试 (ProtocolBenchmarks)

测试内容：
- 字节数组复制性能
- Span<byte> 切片性能
- 大端序整数解析性能

关键指标：
- Mean (平均时间)
- Allocated (内存分配)
- Ratio (相对基线)

#### 2. LINQ vs 循环对比 (LinqVsLoopBenchmarks)

测试内容：
- LINQ Where + Sum
- 传统 foreach 循环
- Span<T> 优化循环

用途：选择合适的数据处理方式

#### 3. 批量操作测试 (BatchOperationBenchmarks)

测试内容：
- 10/50/100 轴并行操作
- 顺序 vs 并行性能对比

关键指标：
- 并行加速比
- 内存消耗
- 线程使用情况

#### 4. 内存分配测试 (MemoryAllocationBenchmarks)

测试内容：
- 小对象分配（1000/10000 次）
- 数组分配（1KB/10KB）
- ArrayPool 使用性能

关键指标：
- Gen0/1/2 回收次数
- 分配内存大小

#### 5. IO 操作测试 (IoOperationBenchmarks)

测试内容：
- 顺序 IO 读写（100 端口）
- 并行 IO 读写（100 端口）

关键指标：
- 吞吐量
- 延迟

#### 6. 并发操作测试 (ConcurrencyBenchmarks)

测试内容：
- 10/50/100 并发任务

关键指标：
- 并发处理能力
- 资源竞争情况

### 查看测试结果

#### 在 PR 中查看

每个 PR 会收到自动评论，包含：

```markdown
## 🚀 Performance Benchmark Results

Comparing current results with baseline from master branch...

✅ Baseline found. Detailed comparison:

### Benchmark Result Files

**Current Results:**
- ProtocolBenchmarks-report.json
- BatchOperationBenchmarks-report.json
- ...

**Baseline Results:**
- (baseline files)

### Summary

- Benchmark artifacts are available for download
- Results stored for 90 days
- View detailed results in BenchmarkDotNet artifacts

---
📊 **Artifacts:** [Download benchmark results](...)
⏱️ **Run time:** 2025-11-12T10:00:00Z
🔗 **Commit:** abc123...
```

#### 下载详细报告

1. 进入 Actions 页面
2. 找到对应的工作流运行
3. 下载 `benchmark-results-{sha}` 工件
4. 解压查看详细的 HTML/JSON 报告

## 稳定性测试

### 快速稳定性测试（10 分钟）

**运行时机：** 每次 PR 和 Push

**监控内容：**
- 内存使用趋势（每 5 分钟采样）
- GC 回收统计
- 线程数变化
- 模拟工作负载执行

**评分标准：**
- 90-100: 优秀
- 70-89: 良好
- 50-69: 一般
- <50: 差（**测试失败**）

**评分计算：**
- 基础分：100
- 内存增长 >50%: -30 分
- 内存增长 >20%: -15 分
- Gen2 GC >100 次: -20 分
- 线程数增长 >50%: -25 分

### 扩展稳定性测试（1 小时）

**运行时机：** 每日定时任务或手动触发

**特点：**
- 更长的监控时间，发现缓慢的内存泄漏
- 更严格的评分阈值（60 分）
- 失败时自动创建 Issue 通知团队

**建议：**
- 重要发布前手动运行
- 关注长期趋势
- 定期审查夜间测试结果

## 性能阈值

### 基准测试阈值

当前系统记录性能数据但**不自动失败**构建。建议手动审查：

**需要关注的情况：**

1. **执行时间显著增加**
   - Mean 增长 > 20%
   - P95 增长 > 30%

2. **内存分配增加**
   - Allocated 增长 > 50%

3. **GC 压力增加**
   - Gen0 回收次数显著增加
   - Gen2 回收次数 > 0（对于短时测试）

### 稳定性测试阈值

**硬性失败条件：**

快速测试：
- 稳定性评分 < 50/100

扩展测试：
- 稳定性评分 < 60/100

**警告条件：**
- 内存增长 > 20%
- Gen2 GC 次数 > 100
- 线程数增长 > 50%

## 如何使用

### 在开发中使用

#### 1. 本地运行基准测试

在提交 PR 之前，建议本地运行基准测试：

```bash
# 运行所有基准测试
cd ZakYip.Singulation.Benchmarks
dotnet run -c Release -- all

# 运行特定测试
dotnet run -c Release -- batch
dotnet run -c Release -- memory
```

#### 2. 本地运行稳定性测试

快速验证（6 分钟）：
```bash
dotnet run -c Release -- stability 0.1
```

完整测试（1 小时）：
```bash
dotnet run -c Release -- stability 1
```

#### 3. 分析本地结果

查看生成的报告：
- `BenchmarkDotNet.Artifacts/results/*.html` - 详细 HTML 报告
- `BenchmarkDotNet.Artifacts/results/*.csv` - CSV 数据
- 控制台输出 - 稳定性测试结果

### 在 PR 中使用

#### 1. 提交 PR

性能测试自动运行，无需手动操作。

#### 2. 查看结果

- 等待 Actions 完成（约 30-45 分钟）
- 查看 PR 中的自动评论
- 下载详细报告查看具体数据

#### 3. 处理性能问题

如果发现性能退化：

1. **下载对比报告**
   - 下载当前和基线的结果
   - 使用 BenchmarkDotNet 的对比工具

2. **定位问题**
   - 确定哪个测试退化
   - 查看代码变更
   - 使用性能分析工具（dotnet-trace, PerfView）

3. **优化代码**
   - 针对性优化
   - 重新运行本地测试
   - 推送更新

4. **重新测试**
   - PR 更新后自动重新运行
   - 验证优化效果

### 手动触发测试

在 GitHub Actions 页面：

1. 进入 "Actions" 标签
2. 选择 "Performance Regression Testing"
3. 点击 "Run workflow"
4. 选择分支
5. 点击 "Run workflow" 按钮

## 故障排除

### 常见问题

#### 1. 测试超时

**症状：** 工作流运行超过 60 分钟被取消

**原因：**
- 机器资源不足
- 测试配置错误
- BenchmarkDotNet 陷入死循环

**解决方案：**
- 检查是否有无限循环
- 减少迭代次数
- 联系维护者调整超时设置

#### 2. 基线缺失

**症状：** PR 评论显示 "No baseline results found"

**原因：**
- 首次运行
- 基线数据过期（90 天）
- 主分支未成功运行

**解决方案：**
- 等待主分支成功运行一次
- 手动触发主分支的测试
- 临时不对比基线，仅查看绝对值

#### 3. 稳定性测试失败

**症状：** 评分 < 50（快速）或 < 60（扩展）

**原因：**
- 内存泄漏
- 线程泄漏
- GC 压力过大

**解决方案：**
1. 查看详细报告
2. 定位问题代码
3. 使用内存分析工具
4. 修复并重新测试

#### 4. 结果不稳定

**症状：** 同一代码多次运行结果差异大

**原因：**
- GitHub Actions 机器负载不同
- 随机因素（时间、GC）
- 测试设计问题

**解决方案：**
- 多次运行取平均
- 关注趋势而非绝对值
- 增加预热和迭代次数
- 使用更稳定的测试方法

## 最佳实践

### 编写性能测试

1. **使用 [Benchmark] 属性**
   ```csharp
   [Benchmark(Baseline = true)]
   public void MyBaselineTest() { }
   
   [Benchmark]
   public void MyOptimizedTest() { }
   ```

2. **添加诊断器**
   ```csharp
   [MemoryDiagnoser]
   [ThreadingDiagnoser]
   public class MyBenchmarks { }
   ```

3. **使用 GlobalSetup**
   ```csharp
   [GlobalSetup]
   public void Setup() {
       // 初始化测试数据
   }
   ```

4. **避免测试中分配**
   - 在 Setup 中准备数据
   - 重用对象
   - 避免装箱

### 性能优化流程

1. **建立基线** - 在优化前运行测试
2. **做出改变** - 实施优化
3. **测量影响** - 对比前后结果
4. **验证正确性** - 确保功能未破坏
5. **提交改进** - 附带性能数据

### 定期审查

建议定期审查性能数据：

- **每周** - 查看主分支的性能趋势
- **每月** - 审查扩展稳定性测试结果
- **发布前** - 运行完整的性能测试套件
- **重要变更后** - 验证性能未退化

### 性能文化

培养团队性能意识：

- ✅ 在代码审查中讨论性能影响
- ✅ 对性能敏感的代码添加基准测试
- ✅ 设定性能目标（延迟、吞吐量）
- ✅ 分享性能优化经验
- ⚠️ 避免过早优化
- ⚠️ 平衡可读性和性能

## 相关资源

- [BenchmarkDotNet 文档](https://benchmarkdotnet.org/)
- [.NET 性能最佳实践](https://learn.microsoft.com/zh-cn/dotnet/core/performance/)
- [性能分析工具](https://learn.microsoft.com/zh-cn/dotnet/core/diagnostics/performance-profiling)
- [项目性能基准测试 README](../ZakYip.Singulation.Benchmarks/README.md)

## 贡献

如需改进性能测试系统：

1. 添加新的基准测试套件
2. 优化测试准确性
3. 改进报告格式
4. 调整阈值设置

欢迎提交 Issue 和 PR！

---

**维护者：** ZakYip.Singulation 团队  
**最后更新：** 2025-11-12  
**版本：** 1.0
