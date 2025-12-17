using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Abstractions.Cabinet;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Infrastructure.Services {

    /// <summary>
    /// 速度联动服务统计接口
    /// </summary>
    public interface ISpeedLinkageService {
        SpeedLinkageStatistics GetStatistics();
    }

    /// <summary>
    /// 速度联动服务：监听轴速度变化并自动控制配置的 IO 端口。
    /// 当指定轴组的所有轴速度从非0降到0时，设置指定IO为指定电平；
    /// 当所有轴速度从0提升到非0时，设置相反电平。
    /// 注意：仅在远程模式下生效，本地模式下不触发速度联动。
    /// </summary>
    public sealed class SpeedLinkageService : BackgroundService, ISpeedLinkageService {
        private readonly ILogger<SpeedLinkageService> _logger;
        private readonly ISpeedLinkageOptionsStore _store;
        private readonly IoStatusService _ioStatusService;
        private readonly IAxisController _axisController;
        private readonly ICabinetPipeline _cabinetPipeline;
        private readonly ISystemClock _clock;
        
        // 用于跟踪每个组的状态：true表示组内所有轴都已停止
        private readonly Dictionary<int, bool> _groupStoppedStates = new();
        private readonly object _stateLock = new();

        // 性能优化：缓存轴ID到驱动器的映射，避免每次都使用LINQ查询
        private Dictionary<int, IAxisDrive>? _axisIdToDriveCache;

        // 健康状态跟踪
        private long _totalChecks = 0;
        private long _totalStateChanges = 0;
        private long _totalIoWrites = 0;
        private long _failedIoWrites = 0;
        private long _totalErrors = 0;
        private DateTime _lastCheckTime = DateTime.MinValue;
        private DateTime _lastErrorTime = DateTime.MinValue;
        private Exception? _lastError = null;
        private bool _isRunning = false;

        public SpeedLinkageService(
            ILogger<SpeedLinkageService> logger,
            ISpeedLinkageOptionsStore store,
            IoStatusService ioStatusService,
            IAxisController axisController,
            ICabinetPipeline cabinetPipeline,
            ISystemClock clock) {
            _logger = logger;
            _store = store;
            _ioStatusService = ioStatusService;
            _axisController = axisController;
            _cabinetPipeline = cabinetPipeline;
            _clock = clock;
        }

        /// <summary>
        /// 获取服务统计信息（用于健康检查）
        /// </summary>
        public SpeedLinkageStatistics GetStatistics() {
            lock (_stateLock) {
                return new SpeedLinkageStatistics {
                    TotalChecks = _totalChecks,
                    TotalStateChanges = _totalStateChanges,
                    TotalIoWrites = _totalIoWrites,
                    FailedIoWrites = _failedIoWrites,
                    TotalErrors = _totalErrors,
                    LastCheckTime = _lastCheckTime,
                    LastErrorTime = _lastErrorTime,
                    LastError = _lastError?.Message,
                    IsRunning = _isRunning,
                    ActiveGroupsCount = _groupStoppedStates.Count
                };
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("速度联动服务启动");
            _isRunning = true;

            try {
                while (!stoppingToken.IsCancellationRequested) {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                    await CheckAndApplySpeedLinkageAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) {
                _logger.LogInformation("速度联动服务已取消");
            }
            catch (Exception ex) // Intentional: Background service errors should be logged and rethrown for host to handle
            {
                _logger.LogError(ex, "速度联动服务异常");
                throw;
            }
            finally {
                _isRunning = false;
            }

            _logger.LogInformation("速度联动服务已停止");
        }

        /// <summary>
        /// 检查轴速度并应用 IO 联动。
        /// </summary>
        private async Task CheckAndApplySpeedLinkageAsync(CancellationToken ct) {
            try {
                // 更新统计信息
                lock (_stateLock) {
                    _totalChecks++;
                    _lastCheckTime = _clock.UtcNow;
                }

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

                // 性能优化：构建轴ID到驱动器的映射缓存（如果尚未构建或驱动器列表已更改）
                if (_axisIdToDriveCache == null) {
                    BuildAxisIdToDriveCache();
                }

                // 收集所有需要执行的 IO 写入操作
                var ioWrites = new List<(int BitNumber, IoState State)>();

                // 处理每个联动组
                for (int groupIndex = 0; groupIndex < options.LinkageGroups.Count; groupIndex++) {
                    var group = options.LinkageGroups[groupIndex];
                    var writes = ProcessGroup(groupIndex, group);
                    if (writes != null) {
                        ioWrites.AddRange(writes);
                    }
                }

                // 批量并行执行所有 IO 写入操作，提高性能
                if (ioWrites.Count > 0) {
                    await ApplyIoWritesBatchAsync(ioWrites, ct);
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex) // Intentional: Speed linkage check errors should update statistics and rethrow
            {
                lock (_stateLock) {
                    _totalErrors++;
                    _lastErrorTime = _clock.UtcNow;
                    _lastError = ex;
                }
                _logger.LogError(ex, "检查速度联动时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 构建轴ID到驱动器的映射缓存，提升查找性能。
        /// </summary>
        private void BuildAxisIdToDriveCache() {
            var drives = _axisController.Drives;
            var cache = new Dictionary<int, IAxisDrive>(drives.Count);
            
            for (int i = 0; i < drives.Count; i++) {
                var drive = drives[i];
                cache[drive.Axis.Value] = drive;
            }
            
            _axisIdToDriveCache = cache;
        }

        /// <summary>
        /// 处理单个联动组，返回需要执行的 IO 写入操作。
        /// </summary>
        private List<(int BitNumber, IoState State)>? ProcessGroup(
            int groupIndex,
            SpeedLinkageGroup group) {

            // 检查组内所有轴是否都已停止
            // 使用缓存的字典查找，避免每次都使用LINQ查询，提升性能
            bool allStopped = true;
            foreach (var axisId in group.AxisIds) {
                // 从缓存中查找对应的轴ID
                if (!_axisIdToDriveCache!.TryGetValue(axisId, out var drive)) {
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

            // 如果状态发生变化，准备 IO 写入操作
            if (allStopped != previouslyStopped) {
                lock (_stateLock) {
                    _groupStoppedStates[groupIndex] = allStopped;
                    _totalStateChanges++;
                }

                // 仅在状态变化时记录日志
                _logger.LogInformation(
                    "速度联动组 {GroupIndex} 状态变更：{OldState} → {NewState}",
                    groupIndex,
                    previouslyStopped ? "已停止" : "运动中",
                    allStopped ? "已停止" : "运动中");

                // 收集该组的 IO 写入操作
                var writes = new List<(int BitNumber, IoState State)>();
                foreach (var ioPoint in group.IoPoints) {
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

                    writes.Add((ioPoint.BitNumber, ioState));
                }
                return writes;
            }

            return null;
        }

        /// <summary>
        /// 批量执行 IO 写入操作。
        /// 注意：由于硬件API可能不支持并发访问，顺序执行确保稳定性。
        /// 优化点：批量收集减少了服务调用和状态检查的次数。
        /// </summary>
        private async Task ApplyIoWritesBatchAsync(
            List<(int BitNumber, IoState State)> writes,
            CancellationToken ct) {

            int successCount = 0;
            int failCount = 0;

            // 顺序执行 IO 写入操作（硬件API可能不支持并发访问）
            foreach (var write in writes) {
                try {
                    var (success, message) = await _ioStatusService.WriteOutputBitAsync(
                        write.BitNumber,
                        write.State,
                        ct);

                    if (success) {
                        successCount++;
                    } else {
                        failCount++;
                        _logger.LogWarning(
                            "批量 IO 写入：IO {BitNumber} 设置失败，原因：{Message}",
                            write.BitNumber,
                            message);
                    }
                }
                catch (Exception ex) // Intentional: Single IO write failure should not stop batch processing
                {
                    failCount++;
                    _logger.LogError(
                        ex,
                        "批量 IO 写入：IO {BitNumber} 设置异常",
                        write.BitNumber);
                }
            }

            // 更新统计信息
            lock (_stateLock) {
                _totalIoWrites += successCount + failCount;
                _failedIoWrites += failCount;
            }

            if (failCount > 0) {
                _logger.LogWarning(
                    "批量 IO 写入完成：成功={Success}，失败={Fail}",
                    successCount,
                    failCount);
            }
        }
    }
}
