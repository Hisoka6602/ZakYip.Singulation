using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using csLTDMC;

namespace ZakYip.Singulation.Infrastructure.Services {

    /// <summary>
    /// 指示灯服务：根据系统状态控制三色灯和按钮灯，以及远程连接指示灯。
    /// </summary>
    public sealed class IndicatorLightService {
        private readonly ILogger<IndicatorLightService> _logger;
        private readonly ushort _cardNo;
        private LeadshineCabinetIoOptions _options;
        private SystemState _currentState = SystemState.Stopped;
        private bool _isRemoteConnected = false;
        private readonly object _stateLock = new();
        private readonly IoLinkageService? _ioLinkageService;

        public IndicatorLightService(
            ILogger<IndicatorLightService> logger,
            ushort cardNo,
            LeadshineCabinetIoOptions options,
            IoLinkageService? ioLinkageService = null) {
            _logger = logger;
            _cardNo = cardNo;
            _options = options;
            _ioLinkageService = ioLinkageService;
        }

        /// <summary>
        /// 获取当前系统状态。
        /// </summary>
        public SystemState CurrentState {
            get {
                lock (_stateLock) {
                    return _currentState;
                }
            }
        }

        /// <summary>
        /// 更新系统状态并控制对应的指示灯。
        /// </summary>
        /// <param name="newState">新的系统状态</param>
        /// <param name="ct">取消令牌</param>
        public async Task UpdateStateAsync(SystemState newState, CancellationToken ct = default) {
            SystemState oldState;
            lock (_stateLock) {
                oldState = _currentState;
                _currentState = newState;
            }

            if (oldState == newState) {
                _logger.LogDebug("系统状态未变化，当前状态：{State}", newState);
                return;
            }

            _logger.LogInformation("系统状态变更：{OldState} → {NewState}", oldState, newState);

            // 控制三色灯
            await UpdateTriColorLightsAsync(newState, ct).ConfigureAwait(false);

            // 控制按钮灯
            await UpdateButtonLightsAsync(newState, ct).ConfigureAwait(false);

            // 调用 IO 联动服务
            if (_ioLinkageService != null) {
                try {
                    await _ioLinkageService.OnStateChangedAsync(newState, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException ocex) {
                    _logger.LogWarning(ocex, "IO 联动服务操作被取消");
                }
                // 如果有其他已知异常类型，可以在此添加额外的 catch 块
            }
        }

        /// <summary>
        /// 根据系统状态更新三色灯。
        /// 重要规则：红灯和其他颜色灯禁止同时亮，红灯亮时只能红灯亮。
        /// </summary>
        private async Task UpdateTriColorLightsAsync(SystemState state, CancellationToken ct) {
            bool redOn = false, yellowOn = false, greenOn = false;

            switch (state) {
                case SystemState.Running:
                    // 运行中 → 绿色
                    greenOn = true;
                    break;
                case SystemState.Stopped:
                    // 已停止 → 黄色
                    yellowOn = true;
                    break;
                case SystemState.Ready:
                    // 准备中 → 黄色 + 绿色
                    yellowOn = true;
                    greenOn = true;
                    break;
                case SystemState.Alarm:
                    // 报警 → 红色（此时黄灯和绿灯必须关闭）
                    redOn = true;
                    yellowOn = false;  // 强制关闭黄灯
                    greenOn = false;   // 强制关闭绿灯
                    _logger.LogInformation("三色灯控制：红灯独占模式 - 红灯亮，黄灯和绿灯强制关闭");
                    break;
            }

            // 安全检查：确保红灯亮时，黄灯和绿灯必须关闭

            await Task.WhenAll(
                SetLightAsync("红灯", _options.CabinetIndicatorPoint.RedLight, redOn, _options.CabinetIndicatorPoint.RedLightTriggerLevel, ct),
                SetLightAsync("黄灯", _options.CabinetIndicatorPoint.YellowLight, yellowOn, _options.CabinetIndicatorPoint.YellowLightTriggerLevel, ct),
                SetLightAsync("绿灯", _options.CabinetIndicatorPoint.GreenLight, greenOn, _options.CabinetIndicatorPoint.GreenLightTriggerLevel, ct)
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// 根据系统状态更新按钮灯。
        /// </summary>
        private async Task UpdateButtonLightsAsync(SystemState state, CancellationToken ct) {
            // 状态 = 运行中 → 启动按钮灯亮
            // 状态 != 运行中 → 停止按钮灯亮
            bool startLightOn = (state == SystemState.Running);
            bool stopLightOn = (state != SystemState.Running);

            await Task.WhenAll(
                SetLightAsync("启动按钮灯", _options.CabinetIndicatorPoint.StartButtonLight, startLightOn, _options.CabinetIndicatorPoint.StartButtonLightTriggerLevel, ct),
                SetLightAsync("停止按钮灯", _options.CabinetIndicatorPoint.StopButtonLight, stopLightOn, _options.CabinetIndicatorPoint.StopButtonLightTriggerLevel, ct)
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// 设置单个灯的电平状态。
        /// </summary>
        /// <param name="name">灯的名称</param>
        /// <param name="bitNo">输出位编号</param>
        /// <param name="on">是否亮灯</param>
        /// <param name="triggerLevel">触发电平配置（ActiveHigh=高电平亮灯，ActiveLow=低电平亮灯）</param>
        /// <param name="ct">取消令牌</param>
        private Task SetLightAsync(string name, int bitNo, bool on, Core.Enums.TriggerLevel triggerLevel, CancellationToken ct) {
            if (bitNo < 0) {
                // 该灯未配置，跳过
                return Task.CompletedTask;
            }

            try {
                // 根据 triggerLevel 决定电平逻辑
                // XOR logic: state = on ^ (triggerLevel == ActiveLow)
                // Truth table:
                //   on | triggerLevel | state
                //  ----+--------------+------
                //   T  | ActiveHigh   |  1 (高电平亮灯)
                //   F  | ActiveHigh   |  0 (高电平灭灯)
                //   T  | ActiveLow    |  0 (低电平亮灯)
                //   F  | ActiveLow    |  1 (低电平灭灯)
                bool isActiveLow = triggerLevel == Core.Enums.TriggerLevel.ActiveLow;
                ushort state = (on ^ isActiveLow) ? (ushort)1 : (ushort)0;
                short result = LTDMC.dmc_write_outbit(_cardNo, (ushort)bitNo, state);

                if (result < 0) {
                    _logger.LogWarning("设置{Name}（位{BitNo}）失败，错误码：{ErrorCode}", name, bitNo, result);
                } else {
                    _logger.LogDebug("设置{Name}（位{BitNo}）= {State}（电平配置：{Level}）", 
                        name, bitNo, on ? "亮" : "灭", triggerLevel == Core.Enums.TriggerLevel.ActiveLow ? "低电平有效" : "高电平有效");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "设置{Name}（位{BitNo}）时发生异常", name, bitNo);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新配置选项（用于热更新）。
        /// </summary>
        public void UpdateOptions(LeadshineCabinetIoOptions newOptions) {
            _options = newOptions;
            _logger.LogInformation("指示灯服务配置已更新");
        }

        /// <summary>
        /// 更新远程 TCP 连接状态并控制远程连接指示灯。
        /// </summary>
        /// <param name="isConnected">是否已连接</param>
        /// <param name="ct">取消令牌</param>
        public async Task UpdateRemoteConnectionStateAsync(bool isConnected, CancellationToken ct = default) {
            bool oldState;
            lock (_stateLock) {
                oldState = _isRemoteConnected;
                if (oldState == isConnected) {
                    return;
                }
                _isRemoteConnected = isConnected;
            }
            _logger.LogInformation("远程连接状态变更：{OldState} → {NewState}", oldState ? "已连接" : "未连接", isConnected ? "已连接" : "未连接");

            // 控制远程连接指示灯
            await SetLightAsync("远程连接指示灯", _options.CabinetIndicatorPoint.RemoteConnectionLight, isConnected, _options.CabinetIndicatorPoint.RemoteConnectionLightTriggerLevel, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// 显示运行预警灯（红灯）指定秒数后执行回调。
        /// 用于本地模式下按下启动按钮时，先亮红灯持续指定秒数，再执行开启逻辑。
        /// </summary>
        /// <param name="warningSeconds">预警持续秒数</param>
        /// <param name="callback">预警结束后执行的回调</param>
        /// <param name="ct">取消令牌</param>
        public async Task ShowRunningWarningAsync(int warningSeconds, Func<Task> callback, CancellationToken ct = default) {
            if (warningSeconds <= 0) {
                // 无预警，直接执行回调
                await callback().ConfigureAwait(false);
                return;
            }

            _logger.LogInformation("【运行预警】开始预警，持续 {Seconds} 秒，三色灯亮红灯", warningSeconds);
            
            // 暂时设置为报警状态（红灯）
            await UpdateTriColorLightsAsync(SystemState.Alarm, ct).ConfigureAwait(false);
            
            // 等待指定秒数
            try {
                await Task.Delay(TimeSpan.FromSeconds(warningSeconds), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                _logger.LogWarning("【运行预警】预警过程被取消");
                throw;
            }
            
            _logger.LogInformation("【运行预警】预警结束，开始执行启动逻辑");
            
            // 执行回调
            await callback().ConfigureAwait(false);
        }
    }
}
