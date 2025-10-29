using System;

namespace ZakYip.Singulation.Core.Contracts.Events.Safety {

    /// <summary>
    /// 远程/本地模式变更事件参数。
    /// </summary>
    public sealed record class RemoteLocalModeChangedEventArgs {
        /// <summary>
        /// 获取是否为远程模式。true=远程模式，false=本地模式。
        /// </summary>
        public required bool IsRemoteMode { get; init; }

        /// <summary>
        /// 获取描述信息。
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// 获取事件时间戳（UTC）。
        /// </summary>
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }
}
