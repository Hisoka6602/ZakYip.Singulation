using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs {
    /// <summary>
    /// 控制器模板（持久化用）：包含厂商标识与 DriverOptions 的可序列化镜像。
    /// </summary>
    public sealed record ControllerOptions {
        /// <summary>驱动厂商标识（例如 "leadshine"）。</summary>
        public required string Vendor { get; init; }

        /// <summary>可选：覆盖轴数量（不填则走总线发现）。</summary>
        public int? OverrideAxisCount { get; init; }

        /// <summary>
        /// 连接Ip
        /// </summary>
        public required string ControllerIp { get; init; } = "192.168.5.11";
        /// <summary>DriverOptions 的可序列化镜像。</summary>
        public DriverOptionsTemplateOptions Template { get; init; } = new();
    }
}