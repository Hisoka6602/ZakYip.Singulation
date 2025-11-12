using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs.Defaults {

    /// <summary>
    /// 提供系统各组件的默认配置选项。
    /// </summary>
    /// <remarks>
    /// 此类包含工厂方法，用于创建控制器、上游通信、编解码器和轴网格布局的默认配置实例。
    /// 这些默认值通常在系统首次启动或配置缺失时使用。
    /// </remarks>
    public static class ConfigDefaults {

        /// <summary>
        /// 创建控制器的默认配置选项。
        /// </summary>
        /// <returns>包含默认供应商、IP 地址和驱动模板的控制器选项。</returns>
        /// <remarks>
        /// 默认配置：
        /// - 供应商: leadshine
        /// - 控制器IP: 192.168.5.11
        /// - 卡号: 8, 端口: 2
        /// - 齿轮比: 0.4, 滑轮节径: 79mm
        /// </remarks>
        public static ControllerOptions Controller()
            => new() {
                Vendor = "leadshine",
                ControllerIp = "192.168.5.11",
                Template = new DriverOptionsTemplateOptions {
                    Card = 8,
                    Port = 2,
                    GearRatio = 0.4m,
                    PulleyPitchDiameterMm = 79,
                }
            };

        /// <summary>
        /// 创建上游通信的默认配置选项。
        /// </summary>
        /// <returns>包含默认主机地址和端口的上游选项。</returns>
        /// <remarks>
        /// 默认配置：
        /// - Host: 127.0.0.1
        /// - Speed/Position/Heartbeat 端口: 5001/2/3
        /// - Role: Client
        /// - ValidateCrc: true
        /// </remarks>
        public static UpstreamOptions Upstream() => new();          // Host=127.0.0.1, Speed/Position/Heartbeat=5001/2/3, Role=Client, ValidateCrc=true

        /// <summary>
        /// 创建编解码器的默认配置选项。
        /// </summary>
        /// <returns>包含默认主轴和出料轴数量的编解码器选项。</returns>
        /// <remarks>
        /// 默认配置：
        /// - MainCount: 28（主轴数量）
        /// - EjectCount: 3（出料轴数量）
        /// </remarks>
        public static UpstreamCodecOptions Codec() => new();   // MainCount=28, EjectCount=3（record 的默认）

        /// <summary>
        /// 创建轴网格布局的默认配置选项。
        /// </summary>
        /// <returns>包含未配置状态的轴网格布局选项。</returns>
        /// <remarks>
        /// 默认配置使用 0/0 表示"未配置"状态，避免预设现场布局。
        /// 实际使用时需要根据现场情况配置 Rows 和 Cols。
        /// </remarks>
        public static AxisGridLayoutOptions AxisGrid() => new() {
            // 用 0/0 表示“未配置”，避免凭空造现场布局
            Rows = 0,
            Cols = 0
        };
    }
}