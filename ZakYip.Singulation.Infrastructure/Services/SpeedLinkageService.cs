using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Infrastructure.Services {

    /// <summary>
    /// 速度联动服务：监听轴速度变化并自动控制配置的 IO 端口。
    /// 当指定轴组的所有轴速度从非0降到0时，设置指定IO为指定电平；
    /// 当所有轴速度从0提升到非0时，设置相反电平。
    /// 注意：仅在远程模式下生效，本地模式下不触发速度联动。
    /// </summary>
    public sealed class SpeedLinkageService : BackgroundService {
        private readonly ILogger<SpeedLinkageService> _logger;
        private readonly ISpeedLinkageOptionsStore _store;
        private readonly IoStatusService _ioStatusService;
        private readonly IAxisController _axisController;
        private readonly ICabinetPipeline _cabinetPipeline;
        
        // 用于跟踪每个组的状态：true表示组内所有轴都已停止
        private readonly Dictionary<int, bool> _groupStoppedStates = new();
        private readonly object _stateLock = new();

        public SpeedLinkageService(
            ILogger<SpeedLinkageService> logger,
            ISpeedLinkageOptionsStore store,
            IoStatusService ioStatusService,
            IAxisController axisController,
            ICabinetPipeline cabinetPipeline) {
            _logger = logger;
            _store = store;
            _ioStatusService = ioStatusService;
            _axisController = axisController;
            _cabinetPipeline = cabinetPipeline;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("速度联动服务启动");

            try {
                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                    await CheckAndApplySpeedLinkageAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) {
                _logger.LogInformation("速度联动服务已取消");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "速度联动服务异常");
                throw;
            }

            _logger.LogInformation("速度联动服务已停止");
        }

        /// <summary>
        /// 检查轴速度并应用 IO 联动。
        /// </summary>
        private async Task CheckAndApplySpeedLinkageAsync(CancellationToken ct) {
            try {
                // 仅在远程模式下执行速度联动
                if (!_cabinetPipeline.IsRemoteMode) {
                    return;
                }

                // 获取配置
                var options = await _store.GetAsync(ct);

                if (!options.Enabled) {
                    return;
                }

                if (options.LinkageGroups.Count == 0) {
                    return;
                }

                // 处理每个联动组
                for (int groupIndex = 0; groupIndex < options.LinkageGroups.Count; groupIndex++) {
                    var group = options.LinkageGroups[groupIndex];
                    await ProcessGroupAsync(groupIndex, group, ct);
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "检查速度联动时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 处理单个联动组。
        /// </summary>
        private async Task ProcessGroupAsync(
            int groupIndex,
            SpeedLinkageGroup group,
            CancellationToken ct) {

            // 检查组内所有轴是否都已停止
            // 使用轴ID（如1001, 1002）而非索引来查找对应的驱动器
            bool allStopped = true;
            foreach (var axisId in group.AxisIds) {
                // 从驱动器列表中查找对应的轴ID
                var drive = _axisController.Drives.FirstOrDefault(d => d.Axis.Value == axisId);
                if (drive == null) {
                    _logger.LogWarning("速度联动组 {GroupIndex} 中的轴ID {AxisId} 未找到对应的驱动器", groupIndex, axisId);
                    continue;
                }

                // 获取该轴的目标速度
                var speed = drive.LastTargetMmps;
                
                // 如果速度为null或非0，则认为轴未停止
                if (!speed.HasValue || Math.Abs(speed.Value) > 0.001m) {
                    allStopped = false;
                    break;
                }
            }

            // 检查状态是否发生变化
            bool previouslyStopped;
            lock (_stateLock) {
                _groupStoppedStates.TryGetValue(groupIndex, out previouslyStopped);
            }

            // 如果状态发生变化，应用 IO 联动
            if (allStopped != previouslyStopped) {
                lock (_stateLock) {
                    _groupStoppedStates[groupIndex] = allStopped;
                }

                // 仅在状态变化时记录日志
                _logger.LogInformation(
                    "速度联动组 {GroupIndex} 状态变更：{OldState} → {NewState}",
                    groupIndex,
                    previouslyStopped ? "已停止" : "运动中",
                    allStopped ? "已停止" : "运动中");

                // 应用 IO 联动
                await ApplyGroupIoAsync(groupIndex, group, allStopped, ct);
            }
        }

        /// <summary>
        /// 应用组的 IO 联动。
        /// </summary>
        private async Task ApplyGroupIoAsync(
            int groupIndex,
            SpeedLinkageGroup group,
            bool allStopped,
            CancellationToken ct) {

            int successCount = 0;
            int failCount = 0;

            foreach (var ioPoint in group.IoPoints) {
                try {
                    // 确定目标电平：
                    // - allStopped=true：使用 LevelWhenStopped
                    // - allStopped=false：使用相反电平
                    var targetLevel = allStopped 
                        ? ioPoint.LevelWhenStopped 
                        : (ioPoint.LevelWhenStopped == TriggerLevel.ActiveHigh 
                            ? TriggerLevel.ActiveLow 
                            : TriggerLevel.ActiveHigh);

                    // 转换为 IoState
                    var ioState = targetLevel == TriggerLevel.ActiveHigh 
                        ? IoState.High 
                        : IoState.Low;

                    var (success, message) = await _ioStatusService.WriteOutputBitAsync(
                        ioPoint.BitNumber,
                        ioState,
                        ct);

                    if (success) {
                        successCount++;
                        // 移除成功时的调试日志，避免日志过多
                    } else {
                        failCount++;
                        _logger.LogWarning(
                            "速度联动组 {GroupIndex}：IO {BitNumber} 设置失败，原因：{Message}",
                            groupIndex,
                            ioPoint.BitNumber,
                            message);
                    }
                }
                catch (Exception ex) {
                    failCount++;
                    _logger.LogError(
                        ex,
                        "速度联动组 {GroupIndex}：IO {BitNumber} 设置异常",
                        groupIndex,
                        ioPoint.BitNumber);
                }
            }

            // 仅在有失败时记录详细信息，成功时使用调试级别
            if (failCount > 0) {
                _logger.LogWarning(
                    "速度联动组 {GroupIndex} IO设置完成：成功={Success}，失败={Fail}",
                    groupIndex,
                    successCount,
                    failCount);
            }
        }
    }
}
