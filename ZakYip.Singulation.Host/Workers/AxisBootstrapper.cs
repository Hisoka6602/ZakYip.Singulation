using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Host.Workers {

    public sealed class AxisBootstrapper : BackgroundService {
        private readonly ILogger<AxisBootstrapper> _log;
        private readonly IControllerOptionsStore _ctrlOpts;
        private readonly IAxisController _controller;

        public AxisBootstrapper(
            ILogger<AxisBootstrapper> log,
            IControllerOptionsStore ctrlOpts,
            IAxisController controller) {
            _log = log;
            _ctrlOpts = ctrlOpts;
            _controller = controller;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                // 1) 从 LiteDB 读取控制器模板；若无则写入默认并继续使用默认
                var opt = await _ctrlOpts.GetAsync(stoppingToken);
                if (opt is null) {
                    opt = new ControllerOptions {
                        Vendor = "leadshine",
                        ControllerIp = "192.168.5.11",
                        Template = new DriverOptionsTemplateOptions {
                            Card = 8,
                            Port = 2,
                            GearRatio = 0.4m,
                            PulleyPitchDiameterMm = 79
                        }
                    };
                    await _ctrlOpts.UpsertAsync(opt, stoppingToken);
                    _log.LogWarning("LiteDB 中不存在控制器模板，已写入默认模板并使用之。");
                }

                // 2) 按模板初始化轴（这一步会调用 Bus.Initialize、GetAxisCount、并批量创建驱动）
                await _controller.InitializeAsync(
                    vendor: opt.Vendor,
                    template: MapToDriverOptions(opt.Template),
                    overrideAxisCount: opt.OverrideAxisCount,
                    ct: stoppingToken);

                // 3) 如果需要，统一上电、设加减速、设目标速度（来自模板的线性参数，按你现有控制逻辑决定是否在这里做）
                await _controller.EnableAllAsync(stoppingToken);
                await _controller.SetAccelDecelAllAsync(
                    accelMmPerSec2: opt.Template.MaxAccelRpmPerSec,
                    decelMmPerSec2: opt.Template.MaxDecelRpmPerSec,
                    ct: stoppingToken);

                _log.LogInformation("Axis bootstrap done. Vendor={Vendor}", opt.Vendor);
            }
            catch (Exception ex) {
                _log.LogError(ex, "Axis bootstrap failed.");
            }
        }

        private static DriverOptions MapToDriverOptions(DriverOptionsTemplateOptions t) => new DriverOptions {
            //—— 将模板映射到实际驱动选项（字段名以你项目中 Drivers.Common/DriverOptions 为准）——
            Card = (ushort)t.Card,
            Port = (ushort)t.Port,
            GearRatio = t.GearRatio,
            PulleyPitchDiameterMm = t.PulleyPitchDiameterMm,
            MinWriteInterval = TimeSpan.FromSeconds(t.MinWriteInterval),
            ConsecutiveFailThreshold = t.ConsecutiveFailThreshold,
            EnableHealthMonitor = t.EnableHealthMonitor,
            HealthPingInterval = TimeSpan.FromMilliseconds(t.HealthPingInterval),
            // 线性限幅（你 Core 的模板里已经是 mm/s 与 mm/s²）
            MaxAccelRpmPerSec = t.MaxAccelRpmPerSec,
            MaxDecelRpmPerSec = t.MaxDecelRpmPerSec,
            NodeId = 0
        };
    }
}