using System;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// EMC 复位类型枚举。
    /// </summary>
    public enum EmcResetType
    {
        /// <summary>
        /// 冷复位（Cold Reset）- 完全断电重启，耗时约 15 秒。
        /// </summary>
        Cold,

        /// <summary>
        /// 热复位（Warm Reset）- 软复位，不断电，耗时约 1-2 秒。
        /// </summary>
        Warm
    }

    /// <summary>
    /// EMC 复位通知消息。
    /// <para>
    /// 用于在进程间传递复位事件通知。
    /// </para>
    /// </summary>
    public sealed class EmcResetNotification
    {
        /// <summary>
        /// 初始化一个新的复位通知。
        /// </summary>
        /// <param name="cardNo">控制器卡号。</param>
        /// <param name="resetType">复位类型。</param>
        /// <param name="processId">发起复位的进程 ID。</param>
        /// <param name="processName">发起复位的进程名称。</param>
        /// <param name="timestamp">通知时间戳（UTC），由调用方提供。</param>
        public EmcResetNotification(ushort cardNo, EmcResetType resetType, int processId, string processName, DateTime timestamp)
        {
            CardNo = cardNo;
            ResetType = resetType;
            ProcessId = processId;
            ProcessName = processName ?? "Unknown";
            Timestamp = timestamp;
        }

        /// <summary>
        /// 控制器卡号。
        /// </summary>
        public ushort CardNo { get; }

        /// <summary>
        /// 复位类型。
        /// </summary>
        public EmcResetType ResetType { get; }

        /// <summary>
        /// 发起复位的进程 ID。
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// 发起复位的进程名称。
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// 通知时间戳（UTC）。
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 预计恢复时间（秒）。
        /// </summary>
        public int EstimatedRecoverySeconds => ResetType switch
        {
            EmcResetType.Cold => 15,
            EmcResetType.Warm => 2,
            _ => 5
        };

        /// <summary>
        /// 序列化为字符串（用于 IPC 传输）。
        /// </summary>
        public string Serialize()
        {
            return $"{CardNo}|{ResetType}|{ProcessId}|{ProcessName}|{Timestamp:O}";
        }

        /// <summary>
        /// 从字符串反序列化。
        /// </summary>
        public static EmcResetNotification? Deserialize(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
                return null;

            try
            {
                var parts = serialized.Split('|');
                if (parts.Length < 5)
                    return null;

                var cardNo = ushort.Parse(parts[0]);
                var resetType = Enum.Parse<EmcResetType>(parts[1]);
                var processId = int.Parse(parts[2]);
                var processName = parts[3];
                var timestamp = DateTime.Parse(parts[4]);

                return new EmcResetNotification(cardNo, resetType, processId, processName, timestamp);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// EMC 复位事件参数。
    /// </summary>
    public sealed class EmcResetEventArgs : EventArgs
    {
        /// <summary>
        /// 初始化一个新的复位事件参数。
        /// </summary>
        /// <param name="notification">复位通知。</param>
        public EmcResetEventArgs(EmcResetNotification notification)
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
        }

        /// <summary>
        /// 复位通知详情。
        /// </summary>
        public EmcResetNotification Notification { get; }
    }
}
