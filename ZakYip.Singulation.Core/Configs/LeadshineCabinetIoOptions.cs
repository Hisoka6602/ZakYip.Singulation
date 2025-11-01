using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 雷赛控制面板 IO 模块配置选项。
    /// </summary>
    public sealed record class LeadshineCabinetIoOptions {
        /// <summary>是否启用物理按键。</summary>
        public bool Enabled { get; init; } = false;

        /// <summary>轮询间隔（毫秒），默认 50ms。</summary>
        [Range(10, 10000, ErrorMessage = "轮询间隔必须在 10 到 10000 毫秒之间")]
        public int PollingIntervalMs { get; init; } = 50;

        /// <summary>控制面板输入点位配置（控制按钮）。</summary>
        public CabinetInputPoint CabinetInputPoint { get; init; } = new();

        /// <summary>控制面板指示灯点位配置。</summary>
        public CabinetIndicatorPoint CabinetIndicatorPoint { get; init; } = new();
    }
}
