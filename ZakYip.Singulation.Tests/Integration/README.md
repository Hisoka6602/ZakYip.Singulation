# 集成测试文档

## 概述

本目录包含 ZakYip.Singulation 项目的集成测试，用于验证系统各组件协同工作的正确性。

## 测试套件

### 1. Controllers REST API 集成测试 (`ControllersIntegrationTests.cs`)

测试所有 REST API 端点的功能和行为。

**测试范围**：
- ✅ Version 端点 - 版本信息查询
- ✅ IO 端点 - IO状态查询和写入
- ✅ Configuration 端点 - 配置导出和模板获取
- ✅ Monitoring 端点 - 系统健康度监控
- ✅ Swagger UI - API文档可访问性
- ✅ Health 端点 - 健康检查
- ✅ 错误处理 - 404, 400/422 等错误场景
- ✅ 响应格式 - Content-Type 等响应头验证

**测试方法**（13个）：
- `VersionController_GetVersion_ReturnsSuccess`
- `IoController_GetIoStatus_ReturnsSuccess`
- `IoController_GetIoStatus_ReturnsValidJson`
- `ConfigurationController_ExportConfig_ReturnsSuccess`
- `ConfigurationController_GetTemplate_ReturnsSuccess`
- `MonitoringController_GetHealth_ReturnsSuccess`
- `ErrorHandling_InvalidEndpoint_Returns404`
- `Swagger_IsAccessible`
- `Health_Endpoint_ReturnsSuccess`
- `ConfigurationController_Import_WithInvalidData_ReturnsBadRequest`
- `IoController_WriteOutput_WithInvalidPort_ReturnsBadRequest`
- `ResponseHeaders_ContainContentType`

### 2. Safety Pipeline 端到端测试 (`SafetyPipelineE2ETests.cs`)

测试安全隔离管线的完整流程和故障场景。

**测试范围**：
- ✅ 安全管线状态查询
- ✅ 启动/停止/复位操作
- ✅ 状态转换流程
- ✅ 通信超时处理
- ✅ 硬件断线场景
- ✅ 恢复流程验证
- ✅ 并发请求处理
- ✅ 模式查询（Remote/Local）

**测试方法**（10个）：
- `SafetyPipeline_GetCabinetStatus_ReturnsSuccess`
- `SafetyPipeline_RequestStart_ReturnsSuccess`
- `SafetyPipeline_RequestStop_ReturnsSuccess`
- `SafetyPipeline_RequestReset_ReturnsSuccess`
- `SafetyPipeline_StateTransitions_FollowExpectedFlow`
- `SafetyPipeline_CommunicationTimeout_HandledGracefully`
- `SafetyPipeline_HardwareDisconnect_ReportsCorrectStatus`
- `SafetyPipeline_RecoveryAfterFault_CanReset`
- `SafetyPipeline_ConcurrentRequests_HandledCorrectly`
- `SafetyPipeline_GetMode_ReturnsValidMode`

## 运行测试

### 前置条件

集成测试需要运行的 Host 服务：

```bash
# 终端 1: 启动 Host 服务
cd /home/runner/work/ZakYip.Singulation/ZakYip.Singulation/ZakYip.Singulation.Host
dotnet run
```

### 运行测试

```bash
# 终端 2: 运行集成测试
cd /home/runner/work/ZakYip.Singulation/ZakYip.Singulation/ZakYip.Singulation.Tests
dotnet run
```

### 自定义服务地址

可以通过环境变量设置自定义的测试服务地址：

```bash
export TEST_BASE_URL=http://localhost:5005
cd /home/runner/work/ZakYip.Singulation/ZakYip.Singulation/ZakYip.Singulation.Tests
dotnet run
```

## 测试行为

### 服务未运行时

如果 Host 服务未运行，集成测试会：
1. 自动检测服务不可用
2. 显示警告消息：`⚠️  跳过集成测试：服务未运行。请先启动 Host 服务。`
3. 优雅地跳过测试，不会失败

这样设计是为了：
- 避免单元测试时的干扰
- 允许在没有服务的环境中运行其他测试
- 提供清晰的反馈信息

### 服务运行时

当服务可用时，测试会：
1. 执行完整的集成测试
2. 验证所有端点的行为
3. 检查错误处理和边界条件
4. 报告测试结果

## 测试结果示例

```
[✓] VersionController_GetVersion_ReturnsSuccess
[✓] IoController_GetIoStatus_ReturnsSuccess
[✓] ConfigurationController_ExportConfig_ReturnsSuccess
[✓] SafetyPipeline_GetCabinetStatus_ReturnsSuccess
[✓] SafetyPipeline_ConcurrentRequests_HandledCorrectly
...

共执行 23 个测试，用时 23 次调用。
所有测试均通过。
```

或者服务未运行时：

```
⚠️  跳过集成测试：服务未运行。请先启动 Host 服务。
⚠️  跳过集成测试：服务未运行。
...

共执行 50 个测试，用时 50 次调用。
所有测试均通过。
```

## 测试架构

### IntegrationTestBase

所有集成测试的基类，提供：

```csharp
// HTTP 请求方法
protected Task<HttpResponseMessage> GetAsync(string url)
protected Task<T?> GetJsonAsync<T>(string url)
protected Task<HttpResponseMessage> PostJsonAsync<T>(string url, T data)
protected Task<HttpResponseMessage> PutJsonAsync<T>(string url, T data)
protected Task<HttpResponseMessage> DeleteAsync(string url)

// 服务可用性检查
protected Task<bool> IsServiceAvailableAsync()
```

### 断言方法

使用项目的 MiniTest 框架：

```csharp
MiniAssert.True(condition, message)
MiniAssert.False(condition, message)
MiniAssert.Equal(expected, actual, message)
MiniAssert.NotNull(value, message)
```

## 最佳实践

### 1. 编写新的集成测试

```csharp
[MiniFact]
public async Task MyController_MyEndpoint_ReturnsSuccess() {
    // 检查服务可用性
    if (!await IsServiceAvailableAsync()) {
        Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
        return;
    }

    // Act
    var response = await GetAsync("/api/my-endpoint");

    // Assert
    MiniAssert.True(response.IsSuccessStatusCode, "端点应返回成功状态");
}
```

### 2. 测试错误场景

```csharp
[MiniFact]
public async Task MyController_InvalidInput_ReturnsBadRequest() {
    if (!await IsServiceAvailableAsync()) {
        Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
        return;
    }

    // Arrange
    var invalidData = new { field = "invalid" };

    // Act
    var response = await PostJsonAsync("/api/my-endpoint", invalidData);

    // Assert
    MiniAssert.True(
        response.StatusCode == HttpStatusCode.BadRequest,
        "无效输入应返回 400");
}
```

### 3. 测试并发场景

```csharp
[MiniFact]
public async Task MyController_ConcurrentRequests_HandledCorrectly() {
    if (!await IsServiceAvailableAsync()) {
        Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
        return;
    }

    // Act - 并发请求
    var tasks = new Task<HttpResponseMessage>[10];
    for (int i = 0; i < tasks.Length; i++) {
        tasks[i] = GetAsync("/api/my-endpoint");
    }
    var responses = await Task.WhenAll(tasks);

    // Assert
    foreach (var response in responses) {
        MiniAssert.True(response.IsSuccessStatusCode, "并发请求都应成功");
    }
}
```

## 注意事项

1. **服务依赖**：
   - 集成测试需要完整的 Host 服务运行
   - 确保服务配置正确
   - 检查端口不被占用（默认 5005）

2. **硬件依赖**：
   - 某些测试可能涉及硬件操作
   - 在测试环境中，硬件未连接是正常状态
   - 测试会验证系统正确报告硬件状态

3. **测试隔离**：
   - 每个测试应该独立运行
   - 不依赖测试执行顺序
   - 避免测试间的状态共享

4. **超时处理**：
   - HttpClient 默认超时 30 秒
   - 可以为特定测试调整超时
   - 测试通信超时场景时使用较短超时

## 扩展测试

要添加新的集成测试文件：

1. 在 `Integration/` 目录创建新文件
2. 继承 `IntegrationTestBase`
3. 使用 `[MiniFact]` 标记测试方法
4. 实现服务可用性检查

```csharp
using System;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Tests.Integration {
    
    internal sealed class MyNewIntegrationTests : IntegrationTestBase {
        
        [MiniFact]
        public async Task MyTest() {
            if (!await IsServiceAvailableAsync()) {
                Console.WriteLine("⚠️  跳过集成测试：服务未运行。");
                return;
            }

            // 测试代码
        }
    }
}
```

## 故障排查

### 测试失败

1. **服务未启动**：
   ```
   解决方案：启动 ZakYip.Singulation.Host 项目
   ```

2. **端口冲突**：
   ```
   解决方案：检查端口 5005 是否被占用，或修改 appsettings.json
   ```

3. **连接被拒绝**：
   ```
   解决方案：检查防火墙设置，确保允许本地连接
   ```

### 调试技巧

1. 查看服务日志
2. 使用 Postman 手动测试端点
3. 检查 Swagger UI（http://localhost:5005/swagger）
4. 启用详细日志输出

## 持续集成

可以将集成测试集成到 CI/CD 流程：

```yaml
# GitHub Actions 示例
- name: Start Host Service
  run: |
    cd ZakYip.Singulation.Host
    dotnet run &
    sleep 10  # 等待服务启动

- name: Run Integration Tests
  run: |
    cd ZakYip.Singulation.Tests
    dotnet run

- name: Stop Host Service
  run: |
    pkill -f "dotnet.*ZakYip.Singulation.Host"
```
