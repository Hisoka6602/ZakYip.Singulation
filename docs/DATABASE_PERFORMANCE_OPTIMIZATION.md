# LiteDB 数据库性能优化文档 / LiteDB Database Performance Optimization

## 概述 / Overview

本文档描述了对 ZakYip.Singulation 项目中 LiteDB 数据库访问层的性能优化措施。

This document describes the performance optimizations applied to the LiteDB database access layer in the ZakYip.Singulation project.

## 优化内容 / Optimizations

### 1. 内存缓存策略 / Memory Caching Strategy

**目的 / Purpose**: 减少数据库访问频率，提升配置读取性能。
Reduce database access frequency and improve configuration read performance.

**实现 / Implementation**:
- 所有配置存储类（8个）都集成了 `IMemoryCache`
- All configuration store classes (8 in total) are integrated with `IMemoryCache`

**缓存配置 / Cache Configuration**:
```csharp
// 绝对过期时间：5分钟 / Absolute expiration: 5 minutes
AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)

// 滑动过期时间：2分钟 / Sliding expiration: 2 minutes  
SlidingExpiration = TimeSpan.FromMinutes(2)
```

**缓存失效策略 / Cache Invalidation Strategy**:
- 更新操作后立即清除缓存 / Cache cleared immediately after update operations
- 删除操作后立即清除缓存 / Cache cleared immediately after delete operations
- 确保数据一致性 / Ensures data consistency

**涉及的存储类 / Affected Store Classes**:
1. `LiteDbControllerOptionsStore`
2. `LiteDbAxisLayoutStore`
3. `LiteDbSpeedLinkageOptionsStore`
4. `LiteDbIoLinkageOptionsStore`
5. `LiteDbIoStatusMonitorOptionsStore`
6. `LiteDbLeadshineCabinetIoOptionsStore`
7. `LiteDbUpstreamOptionsStore`
8. `LiteDbUpstreamCodecOptionsStore`

### 2. 数据库连接优化 / Database Connection Optimization

**优化的连接字符串 / Optimized Connection String**:
```csharp
$"Filename={resolvedPath};Mode=Shared;Connection=Direct;Upgrade=true;Collation=en-US/None"
```

**参数说明 / Parameter Explanation**:

| 参数 / Parameter | 值 / Value | 说明 / Description |
|-----------------|-----------|-------------------|
| Mode | Shared | 允许多进程/线程并发访问 / Allows concurrent access from multiple processes/threads |
| Connection | Direct | 直接连接模式，提供更好的性能 / Direct connection mode for better performance |
| Upgrade | true | 自动升级数据库架构 / Automatically upgrade database schema |
| Collation | en-US/None | 使用二进制比较，性能最佳 / Binary comparison, best performance |

### 3. 索引优化 / Index Optimization

**已有索引 / Existing Indexes**:
- 所有集合的 `Id` 字段都有唯一索引 / All collections have unique index on `Id` field
- 确保主键查询的最佳性能 / Ensures optimal performance for primary key lookups

**新增索引 / New Indexes**:
```csharp
// SpeedLinkageOptionsDoc - 为 Enabled 字段创建索引
// SpeedLinkageOptionsDoc - Index created on Enabled field
_col.EnsureIndex(x => x.Enabled);
```

**索引策略 / Indexing Strategy**:
- 对频繁查询的字段创建索引 / Create indexes on frequently queried fields
- 平衡查询性能和写入性能 / Balance query performance and write performance
- 避免过度索引 / Avoid over-indexing

### 4. 并发访问策略 / Concurrent Access Strategy

**读操作 / Read Operations**:
- 使用共享模式（Mode=Shared）允许多个读取者 / Shared mode allows multiple readers
- 通过缓存减少数据库竞争 / Cache reduces database contention
- 无需锁定 / No locking required

**写操作 / Write Operations**:
```csharp
private readonly object _gate = new();

public Task SaveAsync(Options options, CancellationToken ct = default) {
    lock (_gate) {
        // 写入操作使用锁保护
        // Write operations protected by lock
        _col.Upsert(doc);
        _cache.Remove(CacheKey);
    }
    return Task.CompletedTask;
}
```

- 使用对象锁（lock gate）保护写入操作 / Object lock protects write operations
- 防止并发写入冲突 / Prevents concurrent write conflicts
- 写入后立即失效缓存 / Cache invalidated immediately after write

## 性能改进 / Performance Improvements

### 预期效果 / Expected Benefits

1. **查询性能 / Query Performance**:
   - 首次查询：从数据库读取 / First query: Read from database
   - 后续查询（5分钟内）：从内存缓存读取 / Subsequent queries (within 5 min): Read from cache
   - 预计提升：10-100倍（取决于数据复杂度）/ Expected improvement: 10-100x (depends on data complexity)

2. **并发性能 / Concurrent Performance**:
   - 支持多线程并发读取 / Supports multi-threaded concurrent reads
   - 写入操作串行化，避免数据竞态 / Write operations serialized to avoid race conditions
   - 共享连接模式减少连接开销 / Shared connection mode reduces connection overhead

3. **数据库负载 / Database Load**:
   - 减少约80-90%的数据库查询 / Reduces database queries by ~80-90%
   - 降低磁盘I/O操作 / Reduces disk I/O operations
   - 提高系统整体响应速度 / Improves overall system responsiveness

## 配置常量 / Configuration Constants

所有缓存和数据库相关的配置常量定义在 `InfrastructureConstants` 类中：
All cache and database related configuration constants are defined in the `InfrastructureConstants` class:

```csharp
public static class InfrastructureConstants {
    public static class Cache {
        // 配置缓存过期时间 / Configuration cache expiration
        public const int ConfigAbsoluteExpirationMinutes = 5;
        public const int ConfigSlidingExpirationMinutes = 2;
        
        // 查询结果缓存过期时间 / Query result cache expiration
        public const int QueryResultAbsoluteExpirationMinutes = 10;
        public const int QueryResultSlidingExpirationMinutes = 3;
    }

    public static class Database {
        // 数据库连接配置 / Database connection configuration
        public const string ConnectionMode = "Shared";
        public const string ConnectionType = "Direct";
        public const bool AutoUpgrade = true;
        public const string Collation = "en-US/None";
    }
}
```

## 测试验证 / Test Validation

### 单元测试 / Unit Tests

新增了 `CacheBehaviorTests` 测试类，包含以下测试用例：
Added `CacheBehaviorTests` test class with the following test cases:

1. `ControllerOptionsStore_UsesCacheOnSecondRead` - 验证缓存读取 / Validates cache reads
2. `ControllerOptionsStore_InvalidatesCacheOnUpdate` - 验证缓存失效 / Validates cache invalidation
3. `AxisLayoutStore_UsesCacheOnSecondRead` - 验证轴布局缓存 / Validates axis layout cache
4. `IoStatusMonitorStore_InvalidatesCacheOnDelete` - 验证删除后缓存失效 / Validates cache invalidation on delete
5. `LeadshineCabinetIoStore_UsesCacheCorrectly` - 验证厂商特定配置缓存 / Validates vendor-specific config cache
6. `UpstreamOptionsStore_CacheInvalidatesOnUpdate` - 验证上游配置缓存失效 / Validates upstream config cache invalidation
7. `SpeedLinkageStore_CacheWorksWithComplexObjects` - 验证复杂对象缓存 / Validates complex object caching
8. `IoLinkageStore_ConcurrentAccessDoesNotCorruptCache` - 验证并发访问 / Validates concurrent access

### 运行测试 / Run Tests

```bash
# 运行所有缓存行为测试 / Run all cache behavior tests
dotnet test --filter "FullyQualifiedName~CacheBehavior"

# 运行所有存储层测试 / Run all storage layer tests
dotnet test --filter "FullyQualifiedName~Store"
```

## 最佳实践 / Best Practices

### 使用建议 / Usage Recommendations

1. **读多写少场景 / Read-Heavy Scenarios**:
   - 缓存策略最适合配置类数据 / Cache strategy is ideal for configuration data
   - 减少不必要的数据库往返 / Reduces unnecessary database round-trips

2. **实时性要求 / Real-Time Requirements**:
   - 如需实时数据，考虑缩短缓存过期时间 / For real-time data, consider shorter cache expiration
   - 或在更新时主动通知相关组件 / Or actively notify related components on updates

3. **内存使用 / Memory Usage**:
   - 配置数据通常较小，内存占用可控 / Configuration data is typically small, memory usage is manageable
   - 如有大量数据，考虑使用分布式缓存 / For large datasets, consider distributed caching

### 注意事项 / Considerations

1. **缓存一致性 / Cache Consistency**:
   - 所有更新必须通过存储类进行 / All updates must go through store classes
   - 直接修改数据库会导致缓存不一致 / Direct database modifications cause cache inconsistency

2. **分布式环境 / Distributed Environments**:
   - 当前实现使用本地内存缓存 / Current implementation uses local memory cache
   - 多实例部署时考虑使用 Redis 等分布式缓存 / Consider Redis for multi-instance deployments

3. **监控和调试 / Monitoring and Debugging**:
   - 建议添加缓存命中率监控 / Recommended to add cache hit rate monitoring
   - 日志记录缓存失效事件 / Log cache invalidation events

## 版本历史 / Version History

- **2025-11-09**: 初始版本 - 完成所有存储类的缓存优化 / Initial version - Completed cache optimization for all store classes
- 优化了8个LiteDB存储类 / Optimized 8 LiteDB store classes
- 添加了连接字符串优化 / Added connection string optimization
- 增加了索引和并发访问策略 / Added indexing and concurrent access strategy
- 新增了全面的单元测试 / Added comprehensive unit tests

## 参考资料 / References

- [LiteDB Documentation](https://www.litedb.org/)
- [Memory Caching in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [Cache-Aside Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cache-aside)
