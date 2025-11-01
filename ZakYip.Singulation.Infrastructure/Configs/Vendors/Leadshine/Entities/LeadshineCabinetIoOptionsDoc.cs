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

        /// <summary>输入触发电平配置（低电平有效），默认 false。</summary>
        public bool ActiveLow { get; set; } = false;

        /// <summary>急停按键触发电平配置（低电平有效）。</summary>
        public bool? EmergencyStopActiveLow { get; set; } = null;

        /// <summary>停止按键触发电平配置（低电平有效）。</summary>
        public bool? StopActiveLow { get; set; } = null;

        /// <summary>启动按键触发电平配置（低电平有效）。</summary>
        public bool? StartActiveLow { get; set; } = null;

        /// <summary>复位按键触发电平配置（低电平有效）。</summary>
        public bool? ResetActiveLow { get; set; } = null;

        /// <summary>远程/本地模式触发电平配置（低电平有效）。</summary>
        public bool? RemoteLocalActiveLow { get; set; } = null;

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

        /// <summary>灯光有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。默认为 true（低电平亮灯）。</summary>
        public bool LightActiveLow { get; set; } = true;

        /// <summary>红灯有效电平配置（低电平有效）。</summary>
        public bool? RedLightActiveLow { get; set; } = null;

        /// <summary>黄灯有效电平配置（低电平有效）。</summary>
        public bool? YellowLightActiveLow { get; set; } = null;

        /// <summary>绿灯有效电平配置（低电平有效）。</summary>
        public bool? GreenLightActiveLow { get; set; } = null;

        /// <summary>启动按钮灯有效电平配置（低电平有效）。</summary>
        public bool? StartButtonLightActiveLow { get; set; } = null;

        /// <summary>停止按钮灯有效电平配置（低电平有效）。</summary>
        public bool? StopButtonLightActiveLow { get; set; } = null;

        /// <summary>远程连接指示灯有效电平配置（低电平有效）。</summary>
        public bool? RemoteConnectionLightActiveLow { get; set; } = null;
    }
}
