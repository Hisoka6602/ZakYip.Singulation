using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Abstractions.Realtime;

namespace ZakYip.Singulation.Tests.TestHelpers;

/// <summary>
/// 假的实时通知器实现，用于测试
/// </summary>
public class FakeRealtimeNotifier : IRealtimeNotifier
{
    /// <summary>
    /// 发布的消息数量
    /// </summary>
    public int PublishCount { get; private set; }

    /// <summary>
    /// 最后一次发布的通道
    /// </summary>
    public string? LastChannel { get; private set; }

    /// <summary>
    /// 最后一次发布的负载
    /// </summary>
    public object? LastPayload { get; private set; }

    public ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default)
    {
        PublishCount++;
        LastChannel = channel;
        LastPayload = payload;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 重置统计信息
    /// </summary>
    public void Reset()
    {
        PublishCount = 0;
        LastChannel = null;
        LastPayload = null;
    }
}
