using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Infrastructure.Logging;

namespace ZakYip.Singulation.Infrastructure.Services {

    /// <summary>
    /// IO 联动服务：监听系统状态变化并自动控制配置的 IO 端口。
    /// </summary>
    public sealed class IoLinkageService {
        private readonly ILogger<IoLinkageService> _logger;
        private readonly IIoLinkageOptionsStore _store;
        private readonly IoStatusService _ioStatusService;
        private readonly ExceptionAggregationService? _exceptionAggregation;
        private SystemState _currentState = SystemState.Stopped;
        private readonly object _stateLock = new();

        public IoLinkageService(
            ILogger<IoLinkageService> logger,
            IIoLinkageOptionsStore store,
            IoStatusService ioStatusService,
            ExceptionAggregationService? exceptionAggregation = null) {
            _logger = logger;
            _store = store;
            _ioStatusService = ioStatusService;
            _exceptionAggregation = exceptionAggregation;
        }

        /// <summary>
        /// 当系统状态变化时调用此方法应用 IO 联动。
        /// </summary>
        /// <param name="newState">新的系统状态</param>
        /// <param name="ct">取消令牌</param>
        public async Task OnStateChangedAsync(SystemState newState, CancellationToken ct = default) {
            SystemState oldState;
            lock (_stateLock) {
                oldState = _currentState;
                _currentState = newState;
            }

            if (oldState == newState) {
                _logger.LogDebug("系统状态未变化，当前状态：{State}，跳过 IO 联动", newState);
                return;
            }

            _logger.LogInformation("系统状态变更：{OldState} → {NewState}，准备执行 IO 联动", oldState, newState);

            try {
                // 获取配置
                var options = await _store.GetAsync(ct);

                if (!options.Enabled) {
                    _logger.LogDebug("IO 联动功能已禁用，跳过联动操作");
                    return;
                }

                // 根据新状态决定要应用的 IO 联动列表
                var iosToApply = newState switch {
                    SystemState.Running => options.RunningStateIos,
                    SystemState.Stopped or SystemState.Ready => options.StoppedStateIos,
                    _ => null
                };

                if (iosToApply == null || iosToApply.Count == 0) {
                    _logger.LogDebug("当前状态 {State} 没有配置 IO 联动点", newState);
                    return;
                }

                // 使用结构化日志记录IO联动触发
                _logger.IoLinkageTriggered(oldState.ToString(), newState.ToString(), iosToApply.Count);

                // 应用每个 IO 联动点
                int successCount = 0;
                int failCount = 0;

                foreach (var ioPoint in iosToApply) {
                    try {
                        // 将 TriggerLevel 转换为 IoState
                        // ActiveHigh (0) -> 输出高电平 (High)
                        // ActiveLow (1) -> 输出低电平 (Low)
                        var ioState = ioPoint.Level == Core.Enums.TriggerLevel.ActiveHigh 
                            ? IoState.High 
                            : IoState.Low;

                        var (success, message) = await _ioStatusService.WriteOutputBitAsync(
                            ioPoint.BitNumber,
                            ioState,
                            ct);

                        if (success) {
                            successCount++;
                            _logger.IoLinkageSuccess(ioPoint.BitNumber, ioPoint.Level.ToString());
                        } else {
                            failCount++;
                            _logger.IoLinkageFailed(ioPoint.BitNumber, message ?? "Unknown");
                        }
                    }
                    catch (Exception ex) {
                        failCount++;
                        _logger.IoLinkageException(ex, ioPoint.BitNumber);
                        _exceptionAggregation?.RecordException(ex, $"IoLinkage:Bit{ioPoint.BitNumber}");
                    }
                }

                _logger.IoLinkageCompleted(newState.ToString(), successCount, failCount);
            }
            catch (OperationCanceledException) {
                // 取消操作，直接抛出，不记录为错误
                throw;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "执行 IO 联动时发生异常");
                _exceptionAggregation?.RecordException(ex, "IoLinkage:Execute");
            }
        }
    }
}
