using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs.Defaults {

    public static class ConfigDefaults {

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

        public static UpstreamOptions Upstream() => new();          // Host=127.0.0.1, Speed/Position/Heartbeat=5001/2/3, Role=Client, ValidateCrc=true

        public static UpstreamCodecOptions Codec() => new();   // MainCount=28, EjectCount=3（record 的默认）

        public static AxisGridLayoutOptions AxisGrid() => new() {
            // 用 0/0 表示“未配置”，避免凭空造现场布局
            Rows = 0,
            Cols = 0
        };
    }
}