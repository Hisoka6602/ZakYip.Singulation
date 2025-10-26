using System;

namespace ZakYip.Singulation.Core.Contracts.Events.Safety {

    /// <summary>
    /// 远程/本地模式变更事件参数。
    /// </summary>
    public sealed class RemoteLocalModeChangedEventArgs : EventArgs {
        public RemoteLocalModeChangedEventArgs(bool isRemoteMode, string? description) {
            IsRemoteMode = isRemoteMode;
            Description = description;
            TimestampUtc = DateTime.UtcNow;
        }

        /// <summary>是否为远程模式。true=远程模式，false=本地模式。</summary>
        public bool IsRemoteMode { get; }

        /// <summary>描述信息。</summary>
        public string? Description { get; }

        /// <summary>事件时间戳（UTC）。</summary>
        public DateTime TimestampUtc { get; }
    }
}
