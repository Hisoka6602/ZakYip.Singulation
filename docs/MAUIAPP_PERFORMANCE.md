# MauiApp 性能优化和新功能说明

## 新增功能

### 1. 单件分离模块网格页面

新增了一个专门的模块网格页面，用于显示单件分离系统的实时状态。

#### 功能特性：
- **4x6 默认布局**：默认显示 4 行 6 列共 24 个模块
- **动态网格**：可根据实际服务器配置动态调整行列数
- **实时速度显示**：每个模块上实时显示当前速度（mm/s）
- **状态可视化**：
  - 空闲（灰色）
  - 运行中（绿色）
  - 错误（红色）
  - 离线（深灰色）
- **统计信息**：顶部显示总数、运行中、空闲、错误模块数量
- **自动刷新**：连接服务器后自动加载和更新数据
- **SignalR 实时更新**：通过 SignalR 实时接收速度变化事件

#### 使用方式：
1. 打开应用后点击底部导航栏的"📦 模块网格"标签
2. 点击"🔄 刷新"按钮加载最新数据
3. 点击任意模块可查看详细信息（触觉反馈）

### 2. 安全执行器（SafeExecutor）

为所有可能抛出异常的异步操作添加了安全执行包装器。

#### 功能特性：
- **异常隔离**：所有异常被捕获并安全处理，不会导致应用崩溃
- **超时保护**：为每个操作设置超时时间（默认 30 秒），防止长时间等待
- **断路器模式**：连续失败 3 次后自动开启断路器，60 秒后自动重试
- **友好错误提示**：技术错误信息自动转换为用户友好的提示
- **操作日志**：所有异常都记录到调试日志，便于故障排查

#### 应用范围：
- MainViewModel 的所有异步方法
- ControllerDetailsViewModel 的所有异步方法
- ModuleGridViewModel 的所有异步方法
- API 调用和 SignalR 连接操作

### 3. 性能优化

#### 3.1 虚拟化渲染
- 使用 `CollectionView` 替代传统列表控件
- 仅渲染可见区域的模块，大幅减少内存占用
- 支持数千个模块的流畅滚动

#### 3.2 数据缓存
- **内存缓存**：API 响应数据缓存 2-5 分钟
- **智能刷新**：首先使用缓存数据立即更新 UI，然后在后台获取最新数据
- **自动清理**：过期缓存每分钟自动清理一次

#### 3.3 绑定优化
- 使用计算属性减少数据转换
- 避免不必要的属性通知
- 最小化 UI 线程切换

## 技术细节

### SafeExecutor 使用示例

```csharp
// 异步操作包装
await SafeExecutor.ExecuteAsync(
    async () =>
    {
        // 您的异步代码
        var data = await _apiClient.GetDataAsync();
        ProcessData(data);
    },
    onError: ex =>
    {
        // 错误处理
        _notificationService.ShowError($"操作失败: {ex.Message}");
    },
    operationName: "LoadData",
    timeout: 15000  // 15秒超时
);

// 带返回值的异步操作
var result = await SafeExecutor.ExecuteAsync(
    async () => await _apiClient.GetResultAsync(),
    defaultValue: new Result(),
    onError: ex => Console.WriteLine(ex.Message),
    operationName: "GetResult",
    timeout: 10000
);

// 断路器模式
var circuitBreaker = new SafeExecutor.CircuitBreaker(
    failureThreshold: 3,
    resetTimeoutSeconds: 60
);

await circuitBreaker.ExecuteAsync(
    async () => await _apiClient.CallUnstableServiceAsync(),
    onError: ex => _logger.LogError(ex, "Service call failed"),
    operationName: "UnstableService"
);
```

### 模块缓存管理器使用示例

```csharp
var cacheManager = ModuleCacheManager.Instance;

// 设置缓存（默认 5 分钟过期）
cacheManager.Set("my_key", myData);

// 设置缓存（自定义过期时间）
cacheManager.Set("my_key", myData, TimeSpan.FromMinutes(2));

// 获取缓存
var cachedData = cacheManager.Get<MyDataType>("my_key");
if (cachedData != null)
{
    // 使用缓存数据
}

// 移除缓存
cacheManager.Remove("my_key");

// 清空所有缓存
cacheManager.Clear();
```

## 性能提升效果

理论预期性能提升（实际效果取决于设备配置、网络环境和数据量）：

1. **启动速度**：首次显示模块网格大幅缩短（使用缓存机制）
2. **内存占用**：大量模块场景下内存占用显著降低（虚拟化渲染）
3. **响应速度**：API 调用响应速度明显提升（本地缓存机制）
4. **稳定性**：应用稳定性显著增强（SafeExecutor 异常保护）
5. **滚动流畅度**：网格滚动更加流畅（CollectionView 虚拟化）

> 注：建议在实际生产环境中进行性能测试和监控，以获得准确的性能数据。

## 注意事项

1. **缓存过期时间**：默认缓存 2-5 分钟，可根据实际需求调整
2. **超时设置**：根据网络环境调整超时时间，默认 30 秒
3. **断路器配置**：连续失败 3 次后开启，60 秒后自动重试
4. **内存管理**：缓存会自动清理，但在低内存设备上可手动调用 `Clear()`

## 后续优化计划

1. 添加模块详情页面
2. 实现模块分组功能
3. 添加速度曲线图表
4. 实现模块搜索和筛选
5. 添加离线数据缓存
6. 优化 SignalR 重连策略
