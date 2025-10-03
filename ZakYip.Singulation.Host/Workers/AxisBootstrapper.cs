using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Host.Workers {

    public sealed class AxisBootstrapper : BackgroundService {
        private readonly ILogger<AxisBootstrapper> _log;
        private readonly IControllerOptionsStore _ctrlOpts;
        private readonly IAxisController _controller;
        private readonly IHostApplicationLifetime _lifetime;

        public AxisBootstrapper(
            ILogger<AxisBootstrapper> log,
            IControllerOptionsStore ctrlOpts,
            IAxisController controller,
            IHostApplicationLifetime lifetime) {
            _log = log;
            _ctrlOpts = ctrlOpts;
            _controller = controller;
            _lifetime = lifetime;
            _lifetime.ApplicationStopping.Register(OnStopping);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                // 1) 从 LiteDB 读取控制器模板；若无则写入默认并继续使用默认
                var opt = await _ctrlOpts.GetAsync(stoppingToken);
                // 2) 按模板初始化轴（这一步会调用 Bus.Initialize、GetAxisCount、并批量创建驱动）
                var driverTemplate = opt.Template.ToDriverOptionsTemplate();

                await _controller.InitializeAsync(
                    vendor: opt.Vendor,
                    template: driverTemplate,           // ← 用集中映射后的模板
                    overrideAxisCount: opt.OverrideAxisCount,
                    ct: stoppingToken);

                // 3) 如果需要，统一上电、设加减速、设目标速度
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

        private void OnStopping() {
            try {
                _log.LogInformation("AxisBootstrapper stopping: releasing all axes...");
                _controller.DisposeAllAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
                _log.LogInformation("AxisBootstrapper stopping: all axes released.");
            }
            catch (Exception ex) {
                _log.LogError(ex, "AxisBootstrapper stopping: release failed");
            }
        }
    }
}