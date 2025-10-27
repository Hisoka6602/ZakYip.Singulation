using System;

namespace ZakYip.Singulation.Core.Contracts.Events.Safety {

    /// <summary>
    /// 远程/本地模式变更事件参数。
    /// </summary>
    public sealed class RemoteLocalModeChangedEventArgs : EventArgs {
        /// <summary>
        /// 初始化 <see cref="RemoteLocalModeChangedEventArgs"/> 类的新实例。
        /// </summary>
        /// <param name="isRemoteMode">是否为远程模式。true=远程模式，false=本地模式。</param>
        /// <param name="description">描述信息。</param>
        public RemoteLocalModeChangedEventArgs(bool isRemoteMode, string? description) {
            IsRemoteMode = isRemoteMode;
            Description = description;
            TimestampUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// 获取是否为远程模式。true=远程模式，false=本地模式。
        /// </summary>
        public bool IsRemoteMode { get; }

        /// <summary>
        /// 获取描述信息。
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// 获取事件时间戳（UTC）。
        /// </summary>
        public DateTime TimestampUtc { get; }
    }
}
