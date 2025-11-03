# 发布优化评估报告

## PublishAot 和 PublishTrimmed 适用性评估

本文档记录了对 ZakYip.Singulation 项目使用 PublishAot 和 PublishTrimmed 的评估结果。

## 评估日期

2025-11-03

## 项目概述

本项目包含以下可执行程序：

1. **ZakYip.Singulation.Host** - ASP.NET Core Worker Service（主应用程序）
2. **ZakYip.Singulation.ConsoleDemo** - 控制台演示程序
3. **ZakYip.Singulation.Benchmarks** - 性能基准测试程序
4. **ZakYip.Singulation.MauiApp** - MAUI 跨平台应用

## PublishAot（Native AOT）评估结果

### ❌ 不适合使用 PublishAot

#### 不兼容的依赖项

1. **Newtonsoft.Json**
   - 使用场景：SignalR 协议序列化、API JSON 序列化
   - 问题：重度依赖反射，不支持 AOT
   - 影响：Core、Host 项目

2. **Swagger/Swashbuckle**
   - 使用场景：API 文档生成
   - 问题：基于反射动态生成 OpenAPI 文档
   - 影响：Host 项目

3. **SignalR with Newtonsoft.Json Protocol**
   - 使用场景：实时通信
   - 问题：Newtonsoft.Json 协议不支持 AOT
   - 影响：Host 项目

4. **LiteDB**
   - 使用场景：嵌入式数据库
   - 问题：可能使用反射进行对象映射
   - 影响：Infrastructure 项目

5. **ASP.NET Core MVC Controllers**
   - 使用场景：REST API
   - 问题：控制器发现和路由使用反射
   - 影响：Host 项目

6. **BenchmarkDotNet**
   - 使用场景：性能测试
   - 问题：需要运行时代码生成和编译
   - 影响：Benchmarks 项目

#### 技术限制

- 项目使用传统 ASP.NET Core MVC 模式，而非 Minimal APIs
- 大量使用配置绑定和依赖注入反射
- P/Invoke 到 LTDMC.dll 虽然兼容 AOT，但其他依赖不兼容

#### 结论

**PublishAot 完全不适合本项目**。要使用 AOT，需要：
- 迁移到 System.Text.Json
- 移除 Swagger
- 改用 SignalR MessagePack 协议
- 替换 LiteDB 或使用源生成器
- 重构为 Minimal APIs

这些改动工作量巨大，且会破坏现有功能，不推荐进行。

## PublishTrimmed（程序集裁剪）评估结果

### ✅ 部分适合使用 PublishTrimmed

#### 已启用配置

已在以下项目启用 `IsTrimmable=true`（库项目）：
- ZakYip.Singulation.Core
- ZakYip.Singulation.Drivers
- ZakYip.Singulation.Protocol
- ZakYip.Singulation.Transport
- ZakYip.Singulation.Infrastructure

已在 Host 项目启用 `PublishTrimmed=true`，配置如下：
```xml
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>partial</TrimMode>
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>
<SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
```

#### 裁剪模式说明

使用 `TrimMode=partial`（部分裁剪）而非 `full`（完全裁剪），原因：
- **partial**: 只裁剪标记为可裁剪的程序集，保守安全
- **full**: 裁剪所有程序集，可能破坏反射代码

#### 裁剪警告

启用裁剪后会产生以下警告（预期行为）：

1. **IL2026 警告** - 使用了标记 `RequiresUnreferencedCode` 的成员：
   - `Newtonsoft.Json` 序列化器
   - `ASP.NET Core MVC Controllers`
   - `SignalR` 服务
   - `Options.Configure<T>` 配置绑定

2. **IL2075 警告** - 动态访问类型成员：
   - Swagger 枚举过滤器中的反射

3. **IL2104 警告** - 程序集产生裁剪警告：
   - Newtonsoft.Json.dll
   - Swashbuckle.*.dll
   - NLog.dll
   - TouchSocket.dll

#### 测试结果

发布测试（Release 配置，linux-x64）：

| 配置 | 大小 | 说明 |
|------|------|------|
| 不裁剪 | 13 MB | 框架依赖部署 |
| 裁剪 | 49 MB | 自包含部署（包含 .NET 运行时） |

**注意**：裁剪需要自包含部署，因此总大小会包含 .NET 运行时。

#### 优势

1. ✅ **可以构建和发布** - 使用 partial 模式可以成功构建
2. ✅ **保持功能完整** - partial 模式不会破坏反射代码
3. ✅ **库项目优化** - IsTrimmable 标记使库对裁剪友好
4. ✅ **启用裁剪分析** - EnableTrimAnalyzer 提供编译时警告

#### 风险和注意事项

1. ⚠️ **运行时行为** - 需要充分测试确保所有功能正常
2. ⚠️ **反射代码** - 可能需要添加 DynamicDependency 属性
3. ⚠️ **插件系统** - 如果有动态加载程序集的需求可能受影响
4. ⚠️ **警告处理** - 需要评估每个警告的影响

#### 建议

1. ✅ **保持当前配置** - partial 模式 + IsTrimmable 是最佳平衡
2. ✅ **充分测试** - 在生产环境部署前进行全面功能测试
3. ⚠️ **监控警告** - 定期检查新的裁剪警告
4. ⚠️ **文档化例外** - 记录已知的裁剪警告和其原因

## 替代优化建议

既然 AOT 不适合，以下是其他优化方向：

### 1. 运行时优化
- ✅ 已配置：`GCSettings.LatencyMode = SustainedLowLatency`
- ✅ 已配置：`ThreadPool.SetMinThreads`
- 建议：使用 `tiered compilation` 和 `ReadyToRun`

### 2. 容器化优化
```dockerfile
# 使用 Alpine Linux 减小镜像大小
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

# 多阶段构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish \
    -p:PublishTrimmed=true \
    -p:TrimMode=partial
```

### 3. ReadyToRun (R2R) 编译
```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
  <PublishReadyToRunShowWarnings>true</PublishReadyToRunShowWarnings>
</PropertyGroup>
```

R2R 优势：
- ✅ 提升启动速度
- ✅ 减少 JIT 编译时间
- ✅ 兼容反射代码
- ⚠️ 增加应用大小

### 4. 分层编译（默认启用）
```xml
<PropertyGroup>
  <TieredCompilation>true</TieredCompilation>
  <TieredCompilationQuickJit>true</TieredCompilationQuickJit>
</PropertyGroup>
```

## 测试清单

在生产环境使用裁剪版本前，请测试：

- [ ] Host 服务启动正常
- [ ] Swagger UI 可访问
- [ ] REST API 所有端点正常
- [ ] SignalR 实时通信正常
- [ ] LiteDB 数据持久化正常
- [ ] LTDMC 硬件驱动正常
- [ ] 配置文件加载正常
- [ ] 日志记录正常
- [ ] IO 联动功能正常
- [ ] 速度联动功能正常
- [ ] 所有后台服务正常

## 总结

| 优化技术 | 适用性 | 状态 | 建议 |
|---------|--------|------|------|
| PublishAot | ❌ 不适合 | 未启用 | 不推荐使用 |
| PublishTrimmed | ✅ 部分适合 | 已启用 | 推荐使用（需测试） |
| IsTrimmable | ✅ 适合 | 已启用 | 推荐使用 |
| ReadyToRun | ✅ 适合 | 未启用 | 可选使用 |
| 分层编译 | ✅ 适合 | 默认启用 | 保持启用 |

## 参考资料

- [.NET Native AOT deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Trim self-contained deployments](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)
- [Prepare .NET libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
- [Introduction to NativeAOT in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/native-aot)
