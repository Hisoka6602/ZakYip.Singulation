using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Enums;
using csLTDMC;

namespace ZakYip.Singulation.Host.Safety {

    /// <summary>
    /// 雷赛硬件安全 IO 模块：通过控制器 IO 端口读取物理按键状态。
    /// 支持急停、启动、停止、复位四个物理按键。
    /// </summary>
    public sealed class LeadshineSafetyIoModule : ISafetyIoModule, IDisposable {
        private readonly ILogger<LeadshineSafetyIoModule> _logger;
        private readonly ushort _cardNo;
        private LeadshineSafetyIoOptions _options;
        private readonly object _optionsLock = new();
        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private bool _disposed;

        // 按键状态缓存（用于边沿检测）
        private bool _lastEmergencyStopState;
        private bool _lastStopState;
        private bool _lastStartState;
        private bool _lastResetState;
        private bool _lastRemoteLocalModeState;

        public string Name => "leadshine-hardware-io";

        public event EventHandler<SafetyTriggerEventArgs>? EmergencyStop;
        public event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        public event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        public event EventHandler<SafetyTriggerEventArgs>? ResetRequested;
        public event EventHandler<RemoteLocalModeChangedEventArgs>? RemoteLocalModeChanged;

        public LeadshineSafetyIoModule(
            ILogger<LeadshineSafetyIoModule> logger,
            ushort cardNo,
            LeadshineSafetyIoOptions options) {
            _logger = logger;
            _cardNo = cardNo;
            _options = options;
        }

        public Task StartAsync(CancellationToken ct) {
            if (_pollingTask is not null) {
                _logger.LogWarning("安全 IO 模块已在运行中");
                return Task.CompletedTask;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _pollingTask = Task.Run(() => PollingLoopAsync(_cts.Token), _cts.Token);

            _logger.LogInformation(
                "雷赛安全 IO 模块已启动：急停={EmergencyStopBit}, 停止={StopBit}, 启动={StartBit}, 复位={ResetBit}, 轮询间隔={PollingMs}ms",
                _options.EmergencyStopBit, _options.StopBit, _options.StartBit, _options.ResetBit, _options.PollingIntervalMs);

            return Task.CompletedTask;
        }

        private async Task PollingLoopAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                try {
                    LeadshineSafetyIoOptions currentOptions;
                    lock (_optionsLock) {
                        currentOptions = _options;
                    }

                    // 读取急停按键（如果端口号大于99或小于0则不检测）
                    // 急停是开关类型IO，检测电平变化即可
                    if (currentOptions.EmergencyStopBit >= 0 && currentOptions.EmergencyStopBit <= 99) {
                        bool currentState = ReadInputBit(currentOptions.EmergencyStopBit, currentOptions.InvertEmergencyStopLogic ?? currentOptions.InvertLogic);
                        if (currentState && !_lastEmergencyStopState) {
                            _logger.LogWarning("【IO端点调用】检测到急停按键按下 - IO端口：IN{Port}", currentOptions.EmergencyStopBit);
                            EmergencyStop?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.EmergencyStop, "物理急停按键"));
                        }
                        _lastEmergencyStopState = currentState;
                    }

                    // 读取停止按键（如果端口号大于99或小于0则不检测）
                    // 停止按钮是瞬时触发型，使用电平检测+边沿触发（检测到触发电平时触发一次）
                    if (currentOptions.StopBit >= 0 && currentOptions.StopBit <= 99) {
                        bool currentState = ReadInputBit(currentOptions.StopBit, currentOptions.InvertStopLogic ?? currentOptions.InvertLogic);
                        // 检测到触发电平（从非触发状态到触发状态的转换）时触发一次
                        // ReadInputBit 已经应用了反转逻辑，所以 currentState=true 表示按键处于触发状态
                        if (currentState && !_lastStopState) {
                            _logger.LogInformation("【IO端点调用】检测到停止按键按下 - IO端口：IN{Port}", currentOptions.StopBit);
                            StopRequested?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.StopButton, "物理停止按键"));
                        }
                        _lastStopState = currentState;
                    }

                    // 读取启动按键（如果端口号大于99或小于0则不检测）
                    // 启动按钮是瞬时触发型，使用电平检测+边沿触发（检测到触发电平时触发一次）
                    if (currentOptions.StartBit >= 0 && currentOptions.StartBit <= 99) {
                        bool currentState = ReadInputBit(currentOptions.StartBit, currentOptions.InvertStartLogic ?? currentOptions.InvertLogic);
                        // 检测到触发电平（从非触发状态到触发状态的转换）时触发一次
                        // ReadInputBit 已经应用了反转逻辑，所以 currentState=true 表示按键处于触发状态
                        if (currentState && !_lastStartState) {
                            _logger.LogInformation("【IO端点调用】检测到启动按键按下 - IO端口：IN{Port}", currentOptions.StartBit);
                            StartRequested?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.StartButton, "物理启动按键"));
                        }
                        _lastStartState = currentState;
                    }

                    // 读取复位按键（如果端口号大于99或小于0则不检测）
                    // 复位按钮是瞬时触发型，使用电平检测+边沿触发（检测到触发电平时触发一次）
                    if (currentOptions.ResetBit >= 0 && currentOptions.ResetBit <= 99) {
                        bool currentState = ReadInputBit(currentOptions.ResetBit, currentOptions.InvertResetLogic ?? currentOptions.InvertLogic);
                        // 检测到触发电平（从非触发状态到触发状态的转换）时触发一次
                        // ReadInputBit 已经应用了反转逻辑，所以 currentState=true 表示按键处于触发状态
                        if (currentState && !_lastResetState) {
                            _logger.LogInformation("【IO端点调用】检测到复位按键按下 - IO端口：IN{Port}", currentOptions.ResetBit);
                            ResetRequested?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.ResetButton, "物理复位按键"));
                        }
                        _lastResetState = currentState;
                    }

                    // 读取远程/本地模式（如果端口号大于99或小于0则不检测）
                    // 远程/本地是开关类型IO，检测电平变化即可
                    if (currentOptions.RemoteLocalModeBit >= 0 && currentOptions.RemoteLocalModeBit <= 99) {
                        bool rawState = ReadInputBit(currentOptions.RemoteLocalModeBit, currentOptions.InvertRemoteLocalLogic ?? currentOptions.InvertLogic);
                        // 根据 RemoteLocalActiveHigh 配置决定高电平对应的模式
                        bool isRemoteMode = currentOptions.RemoteLocalActiveHigh ? rawState : !rawState;
                        
                        if (isRemoteMode != _lastRemoteLocalModeState) {
                            var modeText = isRemoteMode ? "远程模式" : "本地模式";
                            _logger.LogInformation("检测到远程/本地模式切换：{Mode}", modeText);
                            RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs(isRemoteMode, $"切换到{modeText}"));
                            _lastRemoteLocalModeState = isRemoteMode;
                        }
                    }

                    await Task.Delay(currentOptions.PollingIntervalMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                    break;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "读取安全 IO 时发生异常");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("雷赛安全 IO 模块已停止");
        }

        private bool ReadInputBit(int bitNo, bool invertLogic) {
            try {
                // 调用雷赛 API 读取输入位
                // 返回值：0=低电平，1=高电平，<0=错误
                short result = LTDMC.dmc_read_inbit(_cardNo, (ushort)bitNo);
                
                if (result < 0) {
                    _logger.LogWarning("读取输入位 {BitNo} 失败，错误码：{ErrorCode}", bitNo, result);
                    return false;
                }

                // 根据 invertLogic 配置应用反转逻辑：
                // invertLogic=false: 高电平(1)时返回true（常开按键，按下时为高电平）
                // invertLogic=true:  低电平(0)时返回true（常闭按键，按下时为低电平）
                // 返回值 true 表示按键处于"触发状态"（按下），false 表示"非触发状态"（未按下）
                bool state = result == 1;
                return invertLogic ? !state : state;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "读取输入位 {BitNo} 时发生异常", bitNo);
                return false;
            }
        }

        /// <summary>
        /// 更新配置选项（用于热更新）。
        /// </summary>
        /// <param name="newOptions">新的配置选项。</param>
        public void UpdateOptions(LeadshineSafetyIoOptions newOptions) {
            lock (_optionsLock) {
                _options = newOptions;
                _logger.LogInformation(
                    "安全 IO 配置已更新：急停={EmergencyStopBit}, 停止={StopBit}, 启动={StartBit}, 复位={ResetBit}, 轮询间隔={PollingMs}ms",
                    _options.EmergencyStopBit, _options.StopBit, _options.StartBit, _options.ResetBit, _options.PollingIntervalMs);
            }
        }

        public void Dispose() {
            if (_disposed) return;

            _cts?.Cancel();
            try {
                _pollingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException) {
                // 忽略取消异常
            }

            _cts?.Dispose();
            _disposed = true;
        }
    }
}
