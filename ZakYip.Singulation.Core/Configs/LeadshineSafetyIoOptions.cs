namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 雷赛安全 IO 模块配置选项。
    /// </summary>
    public sealed class LeadshineSafetyIoOptions {
        /// <summary>是否启用物理按键。</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>急停按键输入位编号，-1 表示禁用。</summary>
        public int EmergencyStopBit { get; set; } = -1;

        /// <summary>停止按键输入位编号，-1 表示禁用。</summary>
        public int StopBit { get; set; } = -1;

        /// <summary>启动按键输入位编号，-1 表示禁用。</summary>
        public int StartBit { get; set; } = -1;

        /// <summary>复位按键输入位编号，-1 表示禁用。</summary>
        public int ResetBit { get; set; } = -1;

        /// <summary>轮询间隔（毫秒），默认 50ms。</summary>
        public int PollingIntervalMs { get; set; } = 50;

        /// <summary>是否反转输入逻辑（用于常闭按键），默认 false。此属性作为默认值，可被各按键独立配置覆盖。</summary>
        public bool InvertLogic { get; set; } = false;

        /// <summary>急停按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertEmergencyStopLogic { get; set; } = null;

        /// <summary>停止按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertStopLogic { get; set; } = null;

        /// <summary>启动按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertStartLogic { get; set; } = null;

        /// <summary>复位按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertResetLogic { get; set; } = null;
    }
}
