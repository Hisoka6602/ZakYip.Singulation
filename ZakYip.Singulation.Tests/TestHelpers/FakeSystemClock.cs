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
    /// Creates a FakeSystemClock with a specific UTC time.
    /// </summary>
    /// <param name="utcNow">The initial UTC time for the fake clock.</param>
    public FakeSystemClock(DateTime utcNow)
    {
        _utcNow = utcNow;
        _now = utcNow.ToLocalTime();
    }

    /// <summary>
    /// Creates a FakeSystemClock with a fixed, deterministic time (2025-01-01 00:00:00 UTC).
    /// Use this factory method in tests to avoid dependency on the actual system clock.
    /// </summary>
    /// <returns>A FakeSystemClock instance with a fixed time.</returns>
    public static FakeSystemClock CreateDefault()
    {
        return new FakeSystemClock(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
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
