using ZakYip.Singulation.Tests.TestHelpers;
using System;
using System.Threading.Tasks;
using ZakYip.Singulation.Infrastructure.Logging;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// 日志采样器测试
/// </summary>
public class LogSamplerTests
{
    [MiniFact]
    public void ShouldLog_WithSamplingRate1_AlwaysReturnsTrue()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            MiniAssert.True(sampler.ShouldLog("test", 1), $"Iteration {i} should return true");
        }
    }

    [MiniFact]
    public void ShouldLog_WithSamplingRate100_ReturnsEvery100th()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());
        int trueCount = 0;

        // Act
        for (int i = 0; i < 1000; i++)
        {
            if (sampler.ShouldLog("test", 100))
            {
                trueCount++;
            }
        }

        // Assert - should return true 10 times (at 100, 200, 300, ..., 1000)
        MiniAssert.Equal(10, trueCount, "Should log exactly 10 times");
    }

    [MiniFact]
    public void ShouldLog_WithDifferentKeys_MaintainsSeparateCounters()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());

        // Act
        var result1 = sampler.ShouldLog("key1", 2);
        var result2 = sampler.ShouldLog("key2", 2);

        // Assert - both should return false on first call (counter = 1)
        MiniAssert.False(result1, "First key should not log on first call");
        MiniAssert.False(result2, "Second key should not log on first call");
        
        // Second calls should return true (counter = 2)
        MiniAssert.True(sampler.ShouldLog("key1", 2), "First key should log on second call");
        MiniAssert.True(sampler.ShouldLog("key2", 2), "Second key should log on second call");
    }

    [MiniFact]
    public void ShouldLogByTime_WithMinInterval_RespectsTimeConstraint()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());
        var interval = TimeSpan.FromMilliseconds(100);

        // Act - first call should return true
        var result1 = sampler.ShouldLogByTime("test", interval);
        MiniAssert.True(result1, "First call should return true");

        // Immediate second call should return false
        var result2 = sampler.ShouldLogByTime("test", interval);
        MiniAssert.False(result2, "Immediate second call should return false");
    }

    [MiniFact]
    public async Task ShouldLogByTime_AfterInterval_ReturnsTrue()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());
        var interval = TimeSpan.FromMilliseconds(50);

        // Act
        var result1 = sampler.ShouldLogByTime("test", interval);
        MiniAssert.True(result1, "First call should return true");

        // Wait for interval to pass
        await Task.Delay(60);

        var result2 = sampler.ShouldLogByTime("test", interval);

        // Assert
        MiniAssert.True(result2, "Call after interval should return true");
    }

    [MiniFact]
    public void GetCount_ReturnsAccurateCount()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());

        // Act
        for (int i = 0; i < 5; i++)
        {
            sampler.ShouldLog("test", 100);
        }

        // Assert
        MiniAssert.Equal(5L, sampler.GetCount("test"), "Count should be 5");
    }

    [MiniFact]
    public void GetCount_ForNonExistentKey_ReturnsZero()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());

        // Act
        var count = sampler.GetCount("nonexistent");

        // Assert
        MiniAssert.Equal(0L, count, "Non-existent key should return 0");
    }

    [MiniFact]
    public void Reset_ClearsCounterAndTime()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());
        sampler.ShouldLog("test", 100);
        sampler.ShouldLog("test", 100);

        // Act
        sampler.Reset("test");

        // Assert
        MiniAssert.Equal(0L, sampler.GetCount("test"), "Count should be 0 after reset");
        
        // First call after reset should not log (counter = 1)
        MiniAssert.False(sampler.ShouldLog("test", 2), "Should not log on first call after reset");
    }

    [MiniFact]
    public void ShouldLog_WithNegativeSamplingRate_ThrowsException()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());

        // Act & Assert
        MiniAssert.Throws<ArgumentException>(() => sampler.ShouldLog("test", -1), 
            "Should throw ArgumentException for negative sampling rate");
    }

    [MiniFact]
    public void ShouldLog_WithZeroSamplingRate_ThrowsException()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());

        // Act & Assert
        MiniAssert.Throws<ArgumentException>(() => sampler.ShouldLog("test", 0),
            "Should throw ArgumentException for zero sampling rate");
    }

    [MiniFact]
    public void ShouldLog_ConcurrentAccess_MaintainsAccuracy()
    {
        // Arrange
        var sampler = new LogSampler(new FakeSystemClock());
        var tasks = new Task[10];
        
        // Act - simulate concurrent access
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    sampler.ShouldLog("concurrent", 100);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - total count should be 1000 (10 tasks * 100 calls)
        MiniAssert.Equal(1000L, sampler.GetCount("concurrent"), "Count should be 1000 after concurrent access");
    }
}
