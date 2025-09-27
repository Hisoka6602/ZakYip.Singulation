using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Infrastructure.Configs.Entities;

namespace ZakYip.Singulation.Infrastructure.Configs.Mappings {

    public static class ConfigMappings {

        // ====== 已有：UpstreamOptions ======
        public static UpstreamOptions ToDto(this UpstreamOptionsDoc d) => new() {
            Host = d.Host,
            SpeedPort = d.SpeedPort,
            PositionPort = d.PositionPort,
            HeartbeatPort = d.HeartbeatPort,
            ValidateCrc = d.ValidateCrc,
            Role = d.Role
        };

        public static UpstreamOptionsDoc ToDoc(this UpstreamOptions dto) => new() {
            Host = dto.Host,
            SpeedPort = dto.SpeedPort,
            PositionPort = dto.PositionPort,
            HeartbeatPort = dto.HeartbeatPort,
            ValidateCrc = dto.ValidateCrc,
            Role = dto.Role
        };

        // ====== 已有：UpstreamCodecOptions ======
        public static UpstreamCodecOptions ToOptions(this UpstreamCodecOptionsDoc d) => new() {
            MainCount = d.MainCount,
            EjectCount = d.EjectCount
        };

        public static UpstreamCodecOptionsDoc ToDoc(this UpstreamCodecOptions o) => new() {
            MainCount = o.MainCount,
            EjectCount = o.EjectCount
        };

        // ====== 补全：ControllerOptions（含 DriverOptionsTemplate 全字段） ======
        public static ControllerOptions ToDto(this ControllerOptionsDoc d) => new() {
            Vendor = d.Vendor,
            OverrideAxisCount = d.OverrideAxisCount,
            ControllerIp = d.ControllerIp,
            Template = new DriverOptionsTemplateOptions {
                // 机构/传动
                GearRatio = d.Template.GearRatio,
                ScrewPitchMm = d.Template.ScrewPitchMm,
                PulleyDiameterMm = d.Template.PulleyDiameterMm,
                PulleyPitchDiameterMm = d.Template.PulleyPitchDiameterMm,

                // 速度/加减速
                MaxRpm = d.Template.MaxRpm,
                MaxAccelRpmPerSec = d.Template.MaxAccelRpmPerSec,
                MaxDecelRpmPerSec = d.Template.MaxDecelRpmPerSec,

                // 行为/健康
                MinWriteInterval = d.Template.MinWriteInterval,
                ConsecutiveFailThreshold = d.Template.ConsecutiveFailThreshold,
                EnableHealthMonitor = d.Template.EnableHealthMonitor,
                HealthPingInterval = d.Template.HealthPingInterval,

                // 若 DriverOptionsTemplateDto 中保留了 Card/Port 字段（你当前包里是有的），一并映射：
                Card = d.Template.Card,
                Port = d.Template.Port,
            }
        };

        public static ControllerOptionsDoc ToDoc(this ControllerOptions dto, string id = "default") => new() {
            Id = id,
            Vendor = dto.Vendor,
            OverrideAxisCount = dto.OverrideAxisCount,
            ControllerIp = dto.ControllerIp,
            Template = new DriverOptionsTemplateDoc {
                // 机构/传动
                GearRatio = dto.Template.GearRatio,
                ScrewPitchMm = dto.Template.ScrewPitchMm,
                PulleyDiameterMm = dto.Template.PulleyDiameterMm,
                PulleyPitchDiameterMm = dto.Template.PulleyPitchDiameterMm,

                // 速度/加减速
                MaxRpm = dto.Template.MaxRpm,
                MaxAccelRpmPerSec = dto.Template.MaxAccelRpmPerSec,
                MaxDecelRpmPerSec = dto.Template.MaxDecelRpmPerSec,

                // 行为/健康
                MinWriteInterval = dto.Template.MinWriteInterval,
                ConsecutiveFailThreshold = dto.Template.ConsecutiveFailThreshold,
                EnableHealthMonitor = dto.Template.EnableHealthMonitor,
                HealthPingInterval = dto.Template.HealthPingInterval,

                // 同步硬件定位字段（若你决定在 DTO 中保留）
                Card = dto.Template.Card,
                Port = dto.Template.Port,
            }
        };

        public static AxisGridLayoutOptions ToDto(this AxisGridLayoutDoc d) => new() { Rows = d.Rows, Cols = d.Cols };

        public static AxisGridLayoutDoc ToDoc(this AxisGridLayoutOptions dto) => new() { Rows = dto.Rows, Cols = dto.Cols };
    }
}