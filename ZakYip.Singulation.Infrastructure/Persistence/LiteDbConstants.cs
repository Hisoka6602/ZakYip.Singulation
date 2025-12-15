namespace ZakYip.Singulation.Infrastructure.Persistence;

/// <summary>
/// LiteDB 持久化存储常量定义
/// </summary>
/// <remarks>
/// 集中管理 LiteDB 相关的常量，避免在多个存储类中重复定义。
/// </remarks>
internal static class LiteDbConstants
{
    /// <summary>
    /// 单例配置的默认键名
    /// </summary>
    /// <remarks>
    /// 用于存储单例配置对象的默认主键值。
    /// 适用于只需要存储一条配置记录的场景（如系统配置、全局设置等）。
    /// </remarks>
    public const string DefaultKey = "default";
}
