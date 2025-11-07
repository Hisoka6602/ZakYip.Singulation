using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ZakYip.Singulation.Infrastructure.Logging;

/// <summary>
/// 日志采样助手：对高频操作进行采样，避免日志泛滥
/// Log Sampling Helper: Sample high-frequency operations to prevent log flooding
/// </summary>
public sealed class LogSampler
{
    private readonly ConcurrentDictionary<string, SamplerState> _samplers = new();

    /// <summary>
    /// 判断是否应该记录日志（基于采样频率）
    /// Determine if a log should be recorded based on sampling frequency
    /// </summary>
    /// <param name="key">采样键，用于区分不同的采样器</param>
    /// <param name="samplingRate">采样率，例如100表示每100次记录一次</param>
    /// <returns>如果应该记录日志则返回true</returns>
    public bool ShouldLog(string key, int samplingRate = 100)
    {
        if (samplingRate <= 0)
            throw new ArgumentException("Sampling rate must be positive", nameof(samplingRate));

        if (samplingRate == 1)
            return true;

        var state = _samplers.GetOrAdd(key, _ => new SamplerState());

        var count = Interlocked.Increment(ref state.Counter);
        
        // 每达到采样率的倍数时记录一次
        if (count % samplingRate == 0)
        {
            state.LastLogTime = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判断是否应该记录日志（基于时间间隔）
    /// Determine if a log should be recorded based on time interval
    /// </summary>
    /// <param name="key">采样键，用于区分不同的采样器</param>
    /// <param name="minInterval">最小日志间隔</param>
    /// <returns>如果应该记录日志则返回true</returns>
    public bool ShouldLogByTime(string key, TimeSpan minInterval)
    {
        var state = _samplers.GetOrAdd(key, _ => new SamplerState());

        var now = DateTime.UtcNow;
        if (now - state.LastLogTime >= minInterval)
        {
            state.LastLogTime = now;
            Interlocked.Increment(ref state.Counter);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取采样计数器的当前值
    /// Get the current value of a sampling counter
    /// </summary>
    public long GetCount(string key)
    {
        return _samplers.TryGetValue(key, out var state) ? state.Counter : 0;
    }

    /// <summary>
    /// 重置采样器
    /// Reset a sampler
    /// </summary>
    public void Reset(string key)
    {
        if (_samplers.TryGetValue(key, out var state))
        {
            Interlocked.Exchange(ref state.Counter, 0);
            state.LastLogTime = DateTime.MinValue;
        }
    }

    private sealed class SamplerState
    {
        public long Counter;
        public DateTime LastLogTime = DateTime.MinValue;
    }
}
