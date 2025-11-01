using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 控制面板指示灯点位配置。
    /// </summary>
    public sealed record class CabinetIndicatorPoint {
        /// <summary>红灯输出位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输出位编号必须在 -1 到 1000 之间")]
        public int RedLight { get; init; } = -1;

        /// <summary>黄灯输出位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输出位编号必须在 -1 到 1000 之间")]
        public int YellowLight { get; init; } = -1;

        /// <summary>绿灯输出位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输出位编号必须在 -1 到 1000 之间")]
        public int GreenLight { get; init; } = -1;

        /// <summary>启动按钮灯输出位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输出位编号必须在 -1 到 1000 之间")]
        public int StartButtonLight { get; init; } = -1;

        /// <summary>停止按钮灯输出位编号，-1 表示禁用。</summary>
        [Range(-1, 1000, ErrorMessage = "输出位编号必须在 -1 到 1000 之间")]
        public int StopButtonLight { get; init; } = -1;

        /// <summary>远程连接指示灯输出位编号，-1 表示禁用。当远程 TCP 连接成功时亮灯，断开时灭灯。</summary>
        [Range(-1, 1000, ErrorMessage = "输出位编号必须在 -1 到 1000 之间")]
        public int RemoteConnectionLight { get; init; } = -1;

        /// <summary>灯光有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。此属性作为默认值，可被各灯独立配置覆盖。默认为 true（低电平亮灯）。</summary>
        public bool LightActiveLow { get; init; } = true;

        /// <summary>红灯有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。null 时使用 LightActiveLow 的值。</summary>
        public bool? RedLightActiveLow { get; init; } = null;

        /// <summary>黄灯有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。null 时使用 LightActiveLow 的值。</summary>
        public bool? YellowLightActiveLow { get; init; } = null;

        /// <summary>绿灯有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。null 时使用 LightActiveLow 的值。</summary>
        public bool? GreenLightActiveLow { get; init; } = null;

        /// <summary>启动按钮灯有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。null 时使用 LightActiveLow 的值。</summary>
        public bool? StartButtonLightActiveLow { get; init; } = null;

        /// <summary>停止按钮灯有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。null 时使用 LightActiveLow 的值。</summary>
        public bool? StopButtonLightActiveLow { get; init; } = null;

        /// <summary>远程连接指示灯有效电平配置（低电平有效）：false=高电平亮灯，true=低电平亮灯。null 时使用 LightActiveLow 的值。</summary>
        public bool? RemoteConnectionLightActiveLow { get; init; } = null;
    }
}
