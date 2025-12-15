using System;
using ZakYip.Singulation.Core.Abstractions;

namespace ZakYip.Singulation.Tests.TestHelpers;

/// <summary>
/// Fake system clock for testing purposes.
/// Allows tests to control time without depending on the actual system clock.
/// </summary>
internal sealed class FakeSystemClock : ISystemClock
{
    private DateTime _utcNow;
    private DateTime _now;

    /// <summary>
    /// Creates a FakeSystemClock with the current time.
    /// </summary>
    public FakeSystemClock()
    {
        _utcNow = DateTime.UtcNow;
        _now = DateTime.Now;
    }

    /// <summary>
    /// Creates a FakeSystemClock with a specific UTC time.
    /// </summary>
    public FakeSystemClock(DateTime utcNow)
    {
        _utcNow = utcNow;
        _now = utcNow.ToLocalTime();
    }

    /// <inheritdoc/>
    public DateTime UtcNow => _utcNow;

    /// <inheritdoc/>
    public DateTime Now => _now;

    /// <summary>
    /// Sets the current UTC time for testing.
    /// </summary>
    public void SetUtcNow(DateTime utcNow)
    {
        _utcNow = utcNow;
        _now = utcNow.ToLocalTime();
    }

    /// <summary>
    /// Advances the clock by the specified duration.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
        _now = _now.Add(duration);
    }
}
