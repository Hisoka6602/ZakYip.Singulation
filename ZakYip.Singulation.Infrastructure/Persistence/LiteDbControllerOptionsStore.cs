using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>
    /// 基于 LiteDB 的控制器模板存储：单文档集合，主键固定为 "default"。
    /// - 存储字段与 DriverOptionsTemplateDto 完全对齐（去掉 Card/Port/NodeId/IsReverse）。
    /// - TimeSpan 以 ticks 存取，避免不同序列化器差异。
    /// - 兼容读取旧版本中“线速度制”的字段并自动换算。
    /// </summary>
    public sealed class LiteDbControllerOptionsStore : IControllerOptionsStore {
        private const string Key = "default";
        private readonly ILiteCollection<ControllerOptionsBson> _coll;

        public LiteDbControllerOptionsStore(ILiteDatabase db) {
            _coll = db.GetCollection<ControllerOptionsBson>("controller_options");
            _coll.EnsureIndex(x => x.Id, unique: true);
        }

        public Task<ControllerOptionsDto?> GetAsync(CancellationToken ct = default) {
            var doc = _coll.FindById(Key);
            return Task.FromResult(doc?.ToDto());
        }

        public Task UpsertAsync(ControllerOptionsDto dto, CancellationToken ct = default) {
            _coll.Upsert(ControllerOptionsBson.FromDto(Key, dto));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken ct = default) {
            _coll.Delete(Key);
            return Task.CompletedTask;
        }

        // ==================== Bson 持久化模型 ====================

        private sealed class ControllerOptionsBson {
            public string Id { get; set; } = default!;
            public string Vendor { get; set; } = default!;
            public int? OverrideAxisCount { get; set; }

            /// <summary>控制器 IP（如 192.168.5.11）。</summary>
            public string ControllerIp { get; set; } = "192.168.5.11";

            public DriverOptionsTemplateBson Template { get; set; } = default!;

            public static ControllerOptionsBson FromDto(string id, ControllerOptionsDto dto) => new() {
                Id = id,
                Vendor = dto.Vendor,
                OverrideAxisCount = dto.OverrideAxisCount,
                ControllerIp = dto.ControllerIp,
                Template = DriverOptionsTemplateBson.FromDto(dto.Template)
            };

            public ControllerOptionsDto ToDto() => new() {
                Vendor = Vendor,
                OverrideAxisCount = OverrideAxisCount,
                ControllerIp = ControllerIp,
                Template = Template.ToDto()
            };
        }

        /// <summary>
        /// 新版模板字段（镜像 DriverOptions 的子集），并内置“旧版线速度制字段”的兼容读取。
        /// </summary>
        private sealed class DriverOptionsTemplateBson {

            // ======= 新版字段：与 DriverOptionsTemplateDto 一一对应 =======
            public decimal GearRatio { get; set; } = 1m;

            public decimal ScrewPitchMm { get; set; }
            public decimal PulleyDiameterMm { get; set; }
            public decimal PulleyPitchDiameterMm { get; set; }

            public decimal MaxRpm { get; set; } = 1813m;
            public decimal MaxAccelRpmPerSec { get; set; } = 1511m;
            public decimal MaxDecelRpmPerSec { get; set; } = 1511m;

            public long MinWriteIntervalTicks { get; set; } = TimeSpan.FromMilliseconds(5).Ticks;
            public int ConsecutiveFailThreshold { get; set; } = 5;
            public bool EnableHealthMonitor { get; set; } = true;
            public long HealthPingIntervalTicks { get; set; } = TimeSpan.FromMilliseconds(500).Ticks;

            // ======= 旧版字段：用于兼容读取（可空） =======
            public decimal? MaxLinearMmps { get; set; }         // 线速度上限(mm/s)

            public double? MaxAccelLinearMmps2 { get; set; }   // 线加速度(mm/s²)
            public double? MaxDecelLinearMmps2 { get; set; }   // 线减速度(mm/s²)
            public double? RollerDiameterMm { get; set; }      // 旧直径命名
            public double? GearRatioLegacy { get; set; }       // 旧齿比为 double
            public int? Ppr { get; set; }                   // 旧模板里可能出现
            public bool? IsReverseDefault { get; set; }      // 旧模板里可能出现

            public static DriverOptionsTemplateBson FromDto(DriverOptionsTemplateDto d) => new() {
                GearRatio = d.GearRatio,
                ScrewPitchMm = d.ScrewPitchMm,
                PulleyDiameterMm = d.PulleyDiameterMm,
                PulleyPitchDiameterMm = d.PulleyPitchDiameterMm,

                MaxRpm = d.MaxRpm,
                MaxAccelRpmPerSec = d.MaxAccelRpmPerSec,
                MaxDecelRpmPerSec = d.MaxDecelRpmPerSec,

                MinWriteIntervalTicks = d.MinWriteInterval.Ticks,
                ConsecutiveFailThreshold = d.ConsecutiveFailThreshold,
                EnableHealthMonitor = d.EnableHealthMonitor,
                HealthPingIntervalTicks = d.HealthPingInterval.Ticks
            };

            public DriverOptionsTemplateDto ToDto() {
                // 情况 A：新字段已存在 → 直接回 DTO
                if (MaxRpm > 0 && MaxAccelRpmPerSec > 0 && MaxDecelRpmPerSec > 0) {
                    return new DriverOptionsTemplateDto {
                        GearRatio = GearRatio,
                        ScrewPitchMm = ScrewPitchMm,
                        PulleyDiameterMm = PulleyDiameterMm,
                        PulleyPitchDiameterMm = PulleyPitchDiameterMm,

                        MaxRpm = MaxRpm,
                        MaxAccelRpmPerSec = MaxAccelRpmPerSec,
                        MaxDecelRpmPerSec = MaxDecelRpmPerSec,

                        MinWriteInterval = new TimeSpan(MinWriteIntervalTicks),
                        ConsecutiveFailThreshold = ConsecutiveFailThreshold,
                        EnableHealthMonitor = EnableHealthMonitor,
                        HealthPingInterval = new TimeSpan(HealthPingIntervalTicks)
                    };
                }

                // 情况 B：旧版本仅存了“线速度制” → 尝试换算到 rpm/rpm/s
                // 需要：旧直径 + 旧齿比；优先使用 PulleyPitchDiameterMm 与 GearRatio（若为空则回落到旧命名）
                var dia = (double)(PulleyPitchDiameterMm > 0 ? PulleyPitchDiameterMm : (decimal)(RollerDiameterMm ?? 0d));
                var gr = (double)(GearRatio > 0 ? GearRatio : (decimal)(GearRatioLegacy ?? 1d));

                // rpm = mm/s * (60 * gearRatio) / (π * D)
                double ToRpmFromMmps(double mmps) =>
                    (dia > 0 && gr > 0) ? mmps * (60.0 * gr) / (Math.PI * dia) : 0.0;

                var rpm = MaxLinearMmps.HasValue ? (decimal)ToRpmFromMmps((double)MaxLinearMmps.Value) : 0m;
                var arps = MaxAccelLinearMmps2.HasValue ? (decimal)ToRpmFromMmps(MaxAccelLinearMmps2.Value) : 0m;
                var drps = MaxDecelLinearMmps2.HasValue ? (decimal)ToRpmFromMmps(MaxDecelLinearMmps2.Value) : 0m;

                // 回落到默认值，保证可用
                if (rpm <= 0) rpm = 1813m;
                if (arps <= 0) arps = 1511m;
                if (drps <= 0) drps = 1511m;

                var minInterval = MinWriteIntervalTicks != 0 ? new TimeSpan(MinWriteIntervalTicks) : TimeSpan.FromMilliseconds(5);
                var healthPing = HealthPingIntervalTicks != 0 ? new TimeSpan(HealthPingIntervalTicks) : TimeSpan.FromMilliseconds(500);

                return new DriverOptionsTemplateDto {
                    GearRatio = (decimal)gr,
                    ScrewPitchMm = ScrewPitchMm,
                    PulleyDiameterMm = PulleyDiameterMm,
                    PulleyPitchDiameterMm = (decimal)dia,

                    MaxRpm = rpm,
                    MaxAccelRpmPerSec = arps,
                    MaxDecelRpmPerSec = drps,

                    MinWriteInterval = minInterval,
                    ConsecutiveFailThreshold = ConsecutiveFailThreshold == 0 ? 5 : ConsecutiveFailThreshold,
                    EnableHealthMonitor = EnableHealthMonitor,
                    HealthPingInterval = healthPing
                };
            }
        }
    }
}