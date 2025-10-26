using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    /// <summary>
    /// 控制器模板的 LiteDB 持久化模型（Doc）。
    /// 仅供 Infrastructure 持久化使用，外部仍然使用 Core 的 DTO/Options。
    /// </summary>
    public sealed class ControllerOptionsDoc {

        /// <summary>单文档固定主键。</summary>
        [BsonId] public string Id { get; set; } = "default";

        /// <summary>驱动厂商标识（如 "leadshine"）。</summary>
        public string Vendor { get; set; } = "leadshine";

        /// <summary>可选：覆盖轴数量（不填则走总线发现）。</summary>
        public int? OverrideAxisCount { get; set; }

        /// <summary>控制器 IP（例如 192.168.5.11）。</summary>
        public string ControllerIp { get; set; } = "192.168.5.11";

        /// <summary>本地模式固定速度（mm/s），默认 100.0。</summary>
        [Range(0.0, 10000.0, ErrorMessage = "本地固定速度必须在 0.0 到 10000.0 mm/s 之间")]
        public decimal LocalFixedSpeedMmps { get; set; } = 100.0m;

        /// <summary>DriverOptions 的可序列化镜像（去掉 Card/Port/NodeId/IsReverse）。</summary>
        public DriverOptionsTemplateDoc Template { get; set; } = new();
    }
}