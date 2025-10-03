using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Drivers.Common;
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

                // 若 DriverOptionsTemplateDto 中保留了 Card/Port 字段（当前包里是有的），一并映射：
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

                // 同步硬件定位字段（若决定在 DTO 中保留）
                Card = dto.Template.Card,
                Port = dto.Template.Port,
            }
        };

        public static AxisGridLayoutOptions ToDto(this AxisGridLayoutDoc d) => new() { Rows = d.Rows, Cols = d.Cols };

        public static AxisGridLayoutDoc ToDoc(this AxisGridLayoutOptions dto) => new() { Rows = dto.Rows, Cols = dto.Cols };

        /// <summary>
        /// 生成“模板级别”的 DriverOptions：已带 Card/Port，NodeId 留 0（让控制器在按轴创建时再映射）。
        /// </summary>
        public static DriverOptions ToDriverOptionsTemplate(this DriverOptionsTemplateOptions tpl) {
            if (tpl is null) throw new ArgumentNullException(nameof(tpl));

            // 1) 创建目标，复制同名同型字段（排除硬件字段）
            var o = new DriverOptions {
                Card = tpl.Card,
                Port = (ushort)tpl.Port,
                NodeId = 0,
                GearRatio = tpl.GearRatio,
                PulleyPitchDiameterMm = tpl.PulleyPitchDiameterMm
            };
            CopySameProps(tpl, o, ExcludedHardwareFields);

            // 2) 补齐硬件（NodeId 模板阶段置 0，按轴时再映射）
            o = o with {
                Card = tpl.Card,
                Port = (ushort)tpl.Port,
                NodeId = 0,
                GearRatio = tpl.GearRatio,
                PulleyPitchDiameterMm = tpl.PulleyPitchDiameterMm <= 0 ? 79m : tpl.PulleyPitchDiameterMm,
            };

            // 3) 时间字段：模板里是“毫秒”的 int/decimal，DriverOptions 是 TimeSpan
            //    ——若模板没有这些字段，下面赋值不会编译；那就删掉对应三行即可。
            o = o with {
                MinWriteInterval = TimeSpan.FromMilliseconds(tpl.MinWriteInterval),
                HealthPingInterval = TimeSpan.FromMilliseconds(tpl.HealthPingInterval)
            };

            // 4) 兜底：仅对 rpm/rpm/s 和失败阈值做最小值兜底，防止 0 值导致底层拒绝
            if (o.MaxRpm <= 0) o = o with { MaxRpm = 1813m };
            if (o.MaxAccelRpmPerSec <= 0) o = o with { MaxAccelRpmPerSec = 1511m };
            if (o.MaxDecelRpmPerSec <= 0) o = o with { MaxDecelRpmPerSec = 1511m };
            if (o.ConsecutiveFailThreshold <= 0) o = o with { ConsecutiveFailThreshold = 5 };

            // 5) 机械参数：至少给出一种换算依据（丝杠螺距或皮带/辊筒直径）
            var hasLead = o.ScrewPitchMm > 0;
            var hasPulley = o.PulleyDiameterMm > 0 || o.PulleyPitchDiameterMm > 0;
            if (!hasLead && !hasPulley)

                throw new InvalidOperationException("模板缺少机械换算参数：请提供 ScrewPitchMm 或 PulleyDiameterMm / PulleyPitchDiameterMm。");

            if (o.GearRatio <= 0)
                throw new InvalidOperationException("模板缺少 GearRatio 或其值无效（必须 > 0）。");

            return o;
        }

        /// <summary>
        /// 供控制器在“按轴创建”时做 NodeId 映射：logic 1..N → 1001..100N。
        /// </summary>
        public static ushort DefaultNodeIdMap(int logicalAxisId) => (ushort)(1000 + logicalAxisId);

        // ===== 内部工具 =====

        private static readonly HashSet<string> ExcludedHardwareFields = new(StringComparer.Ordinal)
        {
            nameof(DriverOptions.Card),
            nameof(DriverOptions.Port),
            nameof(DriverOptions.NodeId),
            nameof(DriverOptions.IsReverse),
        };

        private static void CopySameProps(object src, object dest, ISet<string> exclude) {
            var sps = src.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

            var dps = dest.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
                .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            foreach (var sp in sps) {
                if (exclude.Contains(sp.Name)) continue;
                if (!dps.TryGetValue(sp.Name, out var dp)) continue;
                if (dp.PropertyType != sp.PropertyType) continue;
                dp.SetValue(dest, sp.GetValue(src));
            }
        }
    }
}