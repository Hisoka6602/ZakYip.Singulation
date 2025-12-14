using ZakYip.Singulation.Core.Abstractions;

namespace ZakYip.Singulation.Infrastructure.Runtime;

/// <summary>
/// 系统时钟的默认实现，使用系统时间。
/// </summary>
public sealed class SystemClock : ISystemClock
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTime Now => DateTime.Now;
}
