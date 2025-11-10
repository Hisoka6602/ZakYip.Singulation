namespace ZakYip.Singulation.Infrastructure.Configuration;

/// <summary>
/// 基础设施层性能和容量常量
/// Infrastructure layer performance and capacity constants
/// </summary>
public static class InfrastructureConstants
{
    /// <summary>
    /// 轴事件通道默认容量
    /// Default capacity for axis event channel
    /// </summary>
    public const int AxisEventChannelCapacity = 512;

    /// <summary>
    /// 传输控制事件通道默认容量
    /// Default capacity for transport control event channel
    /// </summary>
    public const int TransportControlEventChannelCapacity = 1024;

    /// <summary>
    /// 事件泵处理循环空闲延迟（毫秒）
    /// Idle delay for event pump processing loop in milliseconds
    /// </summary>
    public const int EventPumpIdleDelayMs = 2;

    /// <summary>
    /// 缓存配置相关常量
    /// Cache configuration constants
    /// </summary>
    public static class Cache {
        /// <summary>
        /// 配置缓存的绝对过期时间（分钟）
        /// Absolute expiration time for configuration cache in minutes
        /// </summary>
        public const int ConfigAbsoluteExpirationMinutes = 5;

        /// <summary>
        /// 配置缓存的滑动过期时间（分钟）
        /// Sliding expiration time for configuration cache in minutes
        /// </summary>
        public const int ConfigSlidingExpirationMinutes = 2;

        /// <summary>
        /// 查询结果缓存的默认绝对过期时间（分钟）
        /// Default absolute expiration time for query result cache in minutes
        /// </summary>
        public const int QueryResultAbsoluteExpirationMinutes = 10;

        /// <summary>
        /// 查询结果缓存的默认滑动过期时间（分钟）
        /// Default sliding expiration time for query result cache in minutes
        /// </summary>
        public const int QueryResultSlidingExpirationMinutes = 3;
    }

    /// <summary>
    /// 数据库连接配置相关常量
    /// Database connection configuration constants
    /// </summary>
    public static class Database {
        /// <summary>
        /// LiteDB 连接模式（共享模式允许并发访问）
        /// LiteDB connection mode (Shared mode allows concurrent access)
        /// </summary>
        public const string ConnectionMode = "Shared";

        /// <summary>
        /// LiteDB 连接类型（Direct 模式提供更好的性能）
        /// LiteDB connection type (Direct mode provides better performance)
        /// </summary>
        public const string ConnectionType = "Direct";

        /// <summary>
        /// 是否自动升级数据库架构
        /// Whether to automatically upgrade database schema
        /// </summary>
        public const bool AutoUpgrade = true;

        /// <summary>
        /// 排序规则（None 表示使用二进制比较，性能最佳）
        /// Collation (None means binary comparison, best performance)
        /// </summary>
        public const string Collation = "en-US/None";
    }
}
