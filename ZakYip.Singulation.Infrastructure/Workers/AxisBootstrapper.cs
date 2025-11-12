using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Core.Utils;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Infrastructure.Configs.Mappings;

namespace ZakYip.Singulation.Infrastructure.Workers {

    /// <summary>
    /// 轴控制器引导启动服务，负责在应用程序启动时初始化运动控制轴。
    /// </summary>
    /// <remarks>
    /// 此服务在后台执行以下操作：
    /// 1. 从配置存储中读取控制器选项
    /// 2. 初始化总线适配器和运动轴驱动
    /// 3. 使能所有轴并设置加速度/减速度参数
    /// 4. 在应用程序停止时释放所有轴资源
    /// </remarks>
    public sealed class AxisBootstrapper : BackgroundService {
        private readonly ILogger<AxisBootstrapper> _log;
        private readonly IControllerOptionsStore _ctrlOpts;
        private readonly IAxisController _controller;
        private readonly IHostApplicationLifetime _lifetime;

        /// <summary>
        /// 初始化 <see cref="AxisBootstrapper"/> 类的新实例。
        /// </summary>
        /// <param name="log">日志记录器实例。</param>
        /// <param name="ctrlOpts">控制器配置选项存储。</param>
        /// <param name="controller">轴控制器接口实例。</param>
        /// <param name="lifetime">应用程序生命周期管理器。</param>
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

        /// <summary>
        /// 执行后台服务的主方法，在独立任务中初始化轴控制器。
        /// </summary>
        /// <param name="stoppingToken">用于通知任务应停止的取消令牌。</param>
        /// <returns>立即完成的任务，实际初始化在后台异步执行。</returns>
        /// <remarks>
        /// 此方法立即返回以避免阻塞应用程序启动（包括 Kestrel 服务器）。
        /// 实际的初始化工作在 <see cref="InitializeInBackgroundAsync"/> 方法中异步执行。
        /// </remarks>
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            // 立即返回，让应用继续启动（包括 Kestrel）
            // 在后台任务中执行初始化，不阻塞应用启动
            // 使用 Task.Run 确保异常被正确隔离和处理
            _ = Task.Run(() => InitializeInBackgroundAsync(stoppingToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 在后台异步初始化轴控制器系统。
        /// </summary>
        /// <param name="stoppingToken">用于取消初始化操作的令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 执行步骤：
        /// 1. 从 LiteDB 读取控制器模板配置，如果不存在则使用默认值
        /// 2. 根据配置初始化总线、探测轴数量并创建驱动实例
        /// 3. 使能所有轴
        /// 4. 根据机械参数计算并设置加速度和减速度
        /// 如果初始化失败，会记录错误但不会终止应用程序。
        /// </remarks>
        private async Task InitializeInBackgroundAsync(CancellationToken stoppingToken) {
            try {
                // 1) 从 LiteDB 读取控制器模板；若无则写入默认并继续使用默认
                var opt = await _ctrlOpts.GetAsync(stoppingToken).ConfigureAwait(false);
                // 2) 按模板初始化轴（这一步会调用 Bus.Initialize、GetAxisCount、并批量创建驱动）
                var driverTemplate = opt.Template.ToDriverOptionsTemplate();

                await _controller.InitializeAsync(
                    vendor: opt.Vendor,
                    template: driverTemplate,           // ← 用集中映射后的模板
                    overrideAxisCount: opt.OverrideAxisCount,
                    ct: stoppingToken).ConfigureAwait(false);

                // 3) 如果需要，统一上电、设加减速、设目标速度
                await _controller.EnableAllAsync(stoppingToken).ConfigureAwait(false);

                var tpl = opt.Template;
                var accelMmps2 = AxisRpm.RpmPerSecToMmPerSec2(tpl.MaxAccelRpmPerSec, tpl.PulleyPitchDiameterMm, tpl.GearRatio, tpl.ScrewPitchMm);
                var decelMmps2 = AxisRpm.RpmPerSecToMmPerSec2(tpl.MaxDecelRpmPerSec, tpl.PulleyPitchDiameterMm, tpl.GearRatio, tpl.ScrewPitchMm);

                if (accelMmps2 > 0m && decelMmps2 > 0m) {
                    await _controller.SetAccelDecelAllAsync(
                        accelMmPerSec2: accelMmps2,
                        decelMmPerSec2: decelMmps2,
                        ct: stoppingToken).ConfigureAwait(false);
                }
                else {
                    _log.LogWarning("Axis bootstrap: skip accel/decel bootstrap due to invalid mechanical parameters or template limits.");
                }

                _log.LogInformation("Axis bootstrap done. Vendor={Vendor}", opt.Vendor);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                _log.LogInformation("Axis bootstrap canceled during shutdown.");
            }
            catch (Exception ex) {
                _log.LogError(ex, "Axis bootstrap failed.");
            }
        }

        /// <summary>
        /// 在应用程序停止时释放所有轴资源的回调方法。
        /// </summary>
        /// <remarks>
        /// 此方法在应用程序生命周期的停止阶段被调用，
        /// 负责安全地释放所有已初始化的轴驱动和总线连接。
        /// </remarks>
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