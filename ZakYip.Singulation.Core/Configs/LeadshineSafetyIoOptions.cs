using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 雷赛安全 IO 模块配置选项。
    /// </summary>
    public sealed record class LeadshineSafetyIoOptions {
        /// <summary>是否启用物理按键。</summary>
        public bool Enabled { get; init; } = false;

        /// <summary>急停按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int EmergencyStopBit { get; init; } = -1;

        /// <summary>停止按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int StopBit { get; init; } = -1;

        /// <summary>启动按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int StartBit { get; init; } = -1;

        /// <summary>复位按键输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int ResetBit { get; init; } = -1;

        /// <summary>轮询间隔（毫秒），默认 50ms。</summary>
        [Range(10, 10000, ErrorMessage = "轮询间隔必须在 10 到 10000 毫秒之间")]
        public int PollingIntervalMs { get; init; } = 50;

        /// <summary>是否反转输入逻辑（用于常闭按键），默认 false。此属性作为默认值，可被各按键独立配置覆盖。</summary>
        public bool InvertLogic { get; init; } = false;

        /// <summary>急停按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertEmergencyStopLogic { get; init; } = null;

        /// <summary>停止按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertStopLogic { get; init; } = null;

        /// <summary>启动按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertStartLogic { get; init; } = null;

        /// <summary>复位按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertResetLogic { get; init; } = null;
    }
}
