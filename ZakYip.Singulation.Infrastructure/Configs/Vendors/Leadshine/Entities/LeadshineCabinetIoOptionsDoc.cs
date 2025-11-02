namespace ZakYip.Singulation.Infrastructure.Configs.Vendors.Leadshine.Entities {

    /// <summary>
    /// 雷赛控制面板 IO 配置的 LiteDB 文档实体。
    /// </summary>
    public sealed class LeadshineCabinetIoOptionsDoc {
        /// <summary>文档 ID（单例模式，固定为 "default"）。</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>是否启用物理按键。</summary>
        public bool Enabled { get; set; }

        /// <summary>轮询间隔（毫秒），默认 50ms。</summary>
        public int PollingIntervalMs { get; set; } = 50;

        /// <summary>控制面板输入点位配置（控制按钮）。</summary>
        public CabinetInputPointDoc CabinetInputPoint { get; set; } = new();

        /// <summary>控制面板指示灯点位配置。</summary>
        public CabinetIndicatorPointDoc CabinetIndicatorPoint { get; set; } = new();
    }

    /// <summary>
    /// 控制面板输入点位配置文档。
    /// </summary>
    public sealed class CabinetInputPointDoc {
        /// <summary>急停按键输入位编号，-1 表示禁用。</summary>
        public int EmergencyStop { get; set; } = -1;

        /// <summary>停止按键输入位编号，-1 表示禁用。</summary>
        public int Stop { get; set; } = -1;

        /// <summary>启动按键输入位编号，-1 表示禁用。</summary>
        public int Start { get; set; } = -1;

        /// <summary>复位按键输入位编号，-1 表示禁用。</summary>
        public int Reset { get; set; } = -1;

        /// <summary>远程/本地模式切换输入位编号，-1 表示禁用。</summary>
        public int RemoteLocalMode { get; set; } = -1;

        /// <summary>急停按键触发电平配置。</summary>
        public int EmergencyStopTriggerLevel { get; set; } = 0;

        /// <summary>停止按键触发电平配置。</summary>
        public int StopTriggerLevel { get; set; } = 0;

        /// <summary>启动按键触发电平配置。</summary>
        public int StartTriggerLevel { get; set; } = 0;

        /// <summary>复位按键触发电平配置。</summary>
        public int ResetTriggerLevel { get; set; } = 0;

        /// <summary>远程/本地模式触发电平配置。</summary>
        public int RemoteLocalTriggerLevel { get; set; } = 0;

        /// <summary>远程/本地模式高电平对应的模式：true=高电平为远程模式，false=高电平为本地模式。</summary>
        public bool RemoteLocalActiveHigh { get; set; } = true;
    }

    /// <summary>
    /// 控制面板指示灯点位配置文档。
    /// </summary>
    public sealed class CabinetIndicatorPointDoc {
        /// <summary>红灯输出位编号，-1 表示禁用。</summary>
        public int RedLight { get; set; } = -1;

        /// <summary>黄灯输出位编号，-1 表示禁用。</summary>
        public int YellowLight { get; set; } = -1;

        /// <summary>绿灯输出位编号，-1 表示禁用。</summary>
        public int GreenLight { get; set; } = -1;

        /// <summary>启动按钮灯输出位编号，-1 表示禁用。</summary>
        public int StartButtonLight { get; set; } = -1;

        /// <summary>停止按钮灯输出位编号，-1 表示禁用。</summary>
        public int StopButtonLight { get; set; } = -1;

        /// <summary>远程连接指示灯输出位编号，-1 表示禁用。</summary>
        public int RemoteConnectionLight { get; set; } = -1;

        /// <summary>红灯有效电平配置。</summary>
        public int RedLightTriggerLevel { get; set; } = 1;

        /// <summary>黄灯有效电平配置。</summary>
        public int YellowLightTriggerLevel { get; set; } = 1;

        /// <summary>绿灯有效电平配置。</summary>
        public int GreenLightTriggerLevel { get; set; } = 1;

        /// <summary>启动按钮灯有效电平配置。</summary>
        public int StartButtonLightTriggerLevel { get; set; } = 1;

        /// <summary>停止按钮灯有效电平配置。</summary>
        public int StopButtonLightTriggerLevel { get; set; } = 1;

        /// <summary>远程连接指示灯有效电平配置。</summary>
        public int RemoteConnectionLightTriggerLevel { get; set; } = 1;

        /// <summary>运行预警秒数：用于在本地模式下按下启动按钮时三色灯亮红灯的持续秒数，默认0秒。</summary>
        public int RunningWarningSeconds { get; set; } = 0;
    }
}
