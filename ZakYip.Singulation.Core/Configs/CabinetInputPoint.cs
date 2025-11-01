using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 控制面板输入点位配置（控制按钮）。
    /// </summary>
    public sealed record class CabinetInputPoint {
        /// <summary>急停按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int EmergencyStop { get; init; } = -1;

        /// <summary>停止按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int Stop { get; init; } = -1;

        /// <summary>启动按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int Start { get; init; } = -1;

        /// <summary>复位按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int Reset { get; init; } = -1;

        /// <summary>远程/本地模式切换输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int RemoteLocalMode { get; init; } = -1;

        /// <summary>输入触发电平配置（低电平有效）：false=高电平触发（常开按键），true=低电平触发（常闭按键）。此属性作为默认值，可被各按键独立配置覆盖。</summary>
        public bool ActiveLow { get; init; } = false;

        /// <summary>急停按键触发电平配置（低电平有效）：false=高电平触发，true=低电平触发。null 时使用 ActiveLow 的值。</summary>
        public bool? EmergencyStopActiveLow { get; init; } = null;

        /// <summary>停止按键触发电平配置（低电平有效）：false=高电平触发，true=低电平触发。null 时使用 ActiveLow 的值。</summary>
        public bool? StopActiveLow { get; init; } = null;

        /// <summary>启动按键触发电平配置（低电平有效）：false=高电平触发，true=低电平触发。null 时使用 ActiveLow 的值。</summary>
        public bool? StartActiveLow { get; init; } = null;

        /// <summary>复位按键触发电平配置（低电平有效）：false=高电平触发，true=低电平触发。null 时使用 ActiveLow 的值。</summary>
        public bool? ResetActiveLow { get; init; } = null;

        /// <summary>远程/本地模式触发电平配置（低电平有效），null 时使用 ActiveLow 的值。</summary>
        public bool? RemoteLocalActiveLow { get; init; } = null;

        /// <summary>远程/本地模式高电平对应的模式：true=高电平为远程模式，false=高电平为本地模式。</summary>
        public bool RemoteLocalActiveHigh { get; init; } = true;
    }
}
