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
        private LeadshineSafetyIoOptions _options;
        private SystemState _currentState = SystemState.Stopped;
        private bool _isRemoteConnected = false;
        private readonly object _stateLock = new();

        public IndicatorLightService(
            ILogger<IndicatorLightService> logger,
            ushort cardNo,
            LeadshineSafetyIoOptions options) {
            _logger = logger;
            _cardNo = cardNo;
            _options = options;
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
                SetLightAsync("红灯", _options.RedLightBit, redOn, _options.InvertRedLightLogic ?? _options.InvertLightLogic, ct),
                SetLightAsync("黄灯", _options.YellowLightBit, yellowOn, _options.InvertYellowLightLogic ?? _options.InvertLightLogic, ct),
                SetLightAsync("绿灯", _options.GreenLightBit, greenOn, _options.InvertGreenLightLogic ?? _options.InvertLightLogic, ct)
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
                SetLightAsync("启动按钮灯", _options.StartButtonLightBit, startLightOn, _options.InvertStartButtonLightLogic ?? _options.InvertLightLogic, ct),
                SetLightAsync("停止按钮灯", _options.StopButtonLightBit, stopLightOn, _options.InvertStopButtonLightLogic ?? _options.InvertLightLogic, ct)
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// 设置单个灯的电平状态。
        /// </summary>
        /// <param name="name">灯的名称</param>
        /// <param name="bitNo">输出位编号</param>
        /// <param name="on">是否亮灯</param>
        /// <param name="invertLogic">是否反转输出逻辑（true=低电平亮灯，false=高电平亮灯）</param>
        /// <param name="ct">取消令牌</param>
        private Task SetLightAsync(string name, int bitNo, bool on, bool invertLogic, CancellationToken ct) {
            if (bitNo < 0) {
                // 该灯未配置，跳过
                return Task.CompletedTask;
            }

            try {
                // 根据 invertLogic 决定电平逻辑
                // XOR logic: state = on ^ invertLogic
                // Truth table:
                //   on | invertLogic | state
                //  ----+-------------+------
                //   T  |     F       |  1 (高电平亮灯)
                //   F  |     F       |  0 (高电平灭灯)
                //   T  |     T       |  0 (低电平亮灯)
                //   F  |     T       |  1 (低电平灭灯)
                ushort state = (on ^ invertLogic) ? (ushort)1 : (ushort)0;
                short result = LTDMC.dmc_write_outbit(_cardNo, (ushort)bitNo, state);

                if (result < 0) {
                    _logger.LogWarning("设置{Name}（位{BitNo}）失败，错误码：{ErrorCode}", name, bitNo, result);
                } else {
                    _logger.LogDebug("设置{Name}（位{BitNo}）= {State}（逻辑：{Logic}）", 
                        name, bitNo, on ? "亮" : "灭", invertLogic ? "低电平亮灯" : "高电平亮灯");
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
        public void UpdateOptions(LeadshineSafetyIoOptions newOptions) {
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
                _isRemoteConnected = isConnected;
            }

            if (oldState == isConnected) {
                return;
            }

            _logger.LogInformation("远程连接状态变更：{OldState} → {NewState}", oldState ? "已连接" : "未连接", isConnected ? "已连接" : "未连接");

            // 控制远程连接指示灯
            await SetLightAsync("远程连接指示灯", _options.RemoteConnectionLightBit, isConnected, 
                _options.InvertRemoteConnectionLightLogic ?? _options.InvertLightLogic, ct).ConfigureAwait(false);
        }
    }
}
