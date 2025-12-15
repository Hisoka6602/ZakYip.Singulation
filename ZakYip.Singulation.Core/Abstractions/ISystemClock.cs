namespace ZakYip.Singulation.Core.Abstractions;

/// <summary>
/// 系统时钟抽象接口，用于获取当前时间。
/// </summary>
/// <remarks>
/// 使用此接口替代直接使用 DateTime.Now / DateTime.UtcNow，以提高代码的可测试性。
/// 在测试场景中，可以注入模拟的时钟实现来控制时间。
/// </remarks>
public interface ISystemClock
{
    /// <summary>
    /// 获取当前 UTC 时间
    /// </summary>
    /// <returns>当前 UTC 时间</returns>
    DateTime UtcNow { get; }

    /// <summary>
    /// 获取当前本地时间
    /// </summary>
    /// <returns>当前本地时间</returns>
    DateTime Now { get; }
}
