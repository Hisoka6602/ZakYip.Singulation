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
    }
}
