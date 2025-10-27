using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    /// <summary>
    /// 雷赛安全 IO 配置的 LiteDB 文档实体。
    /// </summary>
    public sealed class LeadshineSafetyIoOptionsDoc {
        /// <summary>文档 ID（单例模式，固定为 "default"）。</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>是否启用物理按键。</summary>
        public bool Enabled { get; set; }

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

        /// <summary>是否反转输入逻辑（用于常闭按键），默认 false。</summary>
        public bool InvertLogic { get; set; } = false;

        /// <summary>急停按键是否反转输入逻辑。</summary>
        public bool? InvertEmergencyStopLogic { get; set; } = null;

        /// <summary>停止按键是否反转输入逻辑。</summary>
        public bool? InvertStopLogic { get; set; } = null;

        /// <summary>启动按键是否反转输入逻辑。</summary>
        public bool? InvertStartLogic { get; set; } = null;

        /// <summary>复位按键是否反转输入逻辑。</summary>
        public bool? InvertResetLogic { get; set; } = null;

        /// <summary>远程/本地模式切换输入位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输入位编号必须在 -1 到 1000 之间")]
        public int RemoteLocalModeBit { get; set; } = -1;

        /// <summary>远程/本地模式是否反转输入逻辑。</summary>
        public bool? InvertRemoteLocalLogic { get; set; } = null;

        /// <summary>远程/本地模式高电平对应的模式：true=高电平为远程模式，false=高电平为本地模式。</summary>
        public bool RemoteLocalActiveHigh { get; set; } = true;

        /// <summary>红灯输出位编号，-1 表示禁用。</summary>
        public int RedLightBit { get; set; } = -1;

        /// <summary>黄灯输出位编号，-1 表示禁用。</summary>
        public int YellowLightBit { get; set; } = -1;

        /// <summary>绿灯输出位编号，-1 表示禁用。</summary>
        public int GreenLightBit { get; set; } = -1;

        /// <summary>启动按钮灯输出位编号，-1 表示禁用。</summary>
        public int StartButtonLightBit { get; set; } = -1;

        /// <summary>停止按钮灯输出位编号，-1 表示禁用。</summary>
        public int StopButtonLightBit { get; set; } = -1;
    }
}
