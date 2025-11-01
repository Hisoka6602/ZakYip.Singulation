using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Contracts.Events.Safety;
using ZakYip.Singulation.Core.Enums;
using csLTDMC;

namespace ZakYip.Singulation.Infrastructure.Safety {

    /// <summary>
    /// 雷赛硬件控制面板 IO 模块：通过控制器 IO 端口读取物理按键状态。
    /// 支持急停、启动、停止、复位四个物理按键，以及远程/本地模式切换开关。
    /// </summary>
    public sealed class LeadshineCabinetIoModule : ISafetyIoModule, IDisposable {
        /// <summary>日志记录器。</summary>
        private readonly ILogger<LeadshineCabinetIoModule> _logger;
        
        /// <summary>雷赛控制器卡号。</summary>
        private readonly ushort _cardNo;
        
        /// <summary>控制面板 IO 配置选项。</summary>
        private LeadshineCabinetIoOptions _options;
        
        /// <summary>配置选项的线程锁。</summary>
        private readonly object _optionsLock = new();
        
        /// <summary>取消令牌源。</summary>
        private CancellationTokenSource? _cts;
        
        /// <summary>轮询任务。</summary>
        private Task? _pollingTask;
        
        /// <summary>是否已释放资源。</summary>
        private bool _disposed;

        // 按键状态缓存（用于边沿检测）
        /// <summary>上次急停按键状态。</summary>
        private bool _lastEmergencyStopState;
        
        /// <summary>上次停止按键状态。</summary>
        private bool _lastStopState;
        
        /// <summary>上次启动按键状态。</summary>
        private bool _lastStartState;
        
        /// <summary>上次复位按键状态。</summary>
        private bool _lastResetState;
        
        /// <summary>上次远程/本地模式状态。</summary>
        private bool _lastRemoteLocalModeState;

        /// <summary>
        /// 获取模块名称。
        /// </summary>
        public string Name => "leadshine-hardware-io";

        /// <summary>
        /// 当检测到急停按键按下时触发的事件。
        /// </summary>
        public event EventHandler<SafetyTriggerEventArgs>? EmergencyStop;
        
        /// <summary>
        /// 当检测到停止按键按下时触发的事件。
        /// </summary>
        public event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        
        /// <summary>
        /// 当检测到启动按键按下时触发的事件。
        /// </summary>
        public event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        
        /// <summary>
        /// 当检测到复位按键按下时触发的事件。
        /// </summary>
        public event EventHandler<SafetyTriggerEventArgs>? ResetRequested;
        
        /// <summary>
        /// 当检测到远程/本地模式切换时触发的事件。
        /// </summary>
        public event EventHandler<RemoteLocalModeChangedEventArgs>? RemoteLocalModeChanged;

        /// <summary>
        /// 初始化 <see cref="LeadshineCabinetIoModule"/> 类的新实例。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="cardNo">雷赛控制器卡号。</param>
        /// <param name="options">控制面板 IO 配置选项。</param>
        public LeadshineCabinetIoModule(
            ILogger<LeadshineCabinetIoModule> logger,
            ushort cardNo,
            LeadshineCabinetIoOptions options) {
            _logger = logger;
            _cardNo = cardNo;
            _options = options;
        }

        /// <summary>
        /// 启动控制面板 IO 模块的轮询循环。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task StartAsync(CancellationToken ct) {
            if (_pollingTask is not null) {
                _logger.LogWarning("控制面板 IO 模块已在运行中");
                return;
            }

            // 启动时读取远程/本地模式 IO 状态并触发初始事件
            // 使用超时保护，避免阻塞应用启动
            var remoteLocalModeBit = _options.CabinetInputPoint.RemoteLocalMode;
            if (remoteLocalModeBit >= 0 && remoteLocalModeBit <= 99) {
                try {
                    // 使用 Task.Run 将同步 IO 调用放到线程池，并设置 2 秒超时
                    var invertLogic = _options.CabinetInputPoint.InvertRemoteLocalLogic ?? _options.CabinetInputPoint.InvertLogic;
                    var readTask = Task.Run(() => ReadInputBit(remoteLocalModeBit, invertLogic), ct);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), ct);
                    var completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);
                    
                    if (completedTask == readTask) {
                        // ReadInputBit 返回 true 表示触发状态（考虑了 InvertRemoteLocalLogic）
                        bool rawState = await readTask.ConfigureAwait(false);
                        // RemoteLocalActiveHigh 决定高电平对应哪个模式：true=高电平为远程，false=高电平为本地
                        bool isRemoteMode = _options.CabinetInputPoint.RemoteLocalActiveHigh ? rawState : !rawState;
                        _lastRemoteLocalModeState = isRemoteMode;
                        
                        var modeText = isRemoteMode ? "远程模式" : "本地模式";
                        _logger.LogInformation("启动时读取远程/本地模式 IO 状态：{Mode}", modeText);
                        
                        // 触发初始模式事件，让 SafetyPipeline 知道当前模式
                        RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = isRemoteMode, Description = $"启动时检测到{modeText}" });
                    }
                    else {
                        // 超时，使用默认值
                        _logger.LogWarning("启动时读取远程/本地模式 IO 超时（控制器可能未初始化），默认为本地模式");
                        _lastRemoteLocalModeState = false; // 默认本地模式
                        RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = false, Description = "启动时读取超时，默认为本地模式" });
                    }
                }
                catch (TaskCanceledException ex) {
                    _logger.LogWarning(ex, "启动时读取远程/本地模式 IO 任务被取消，默认为本地模式");
                    _lastRemoteLocalModeState = false; // 默认本地模式
                    RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = false, Description = "启动时读取任务被取消，默认为本地模式" });
                }
                catch (TimeoutException ex) {
                    _logger.LogWarning(ex, "启动时读取远程/本地模式 IO 超时异常，默认为本地模式");
                    _lastRemoteLocalModeState = false; // 默认本地模式
                    RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = false, Description = "启动时读取超时异常，默认为本地模式" });
                }
                // If you know the hardware library throws only Exception, you may keep this, but document why:
                // catch (Exception ex) {
                //     _logger.LogWarning(ex, "启动时读取远程/本地模式 IO 失败，默认为本地模式");
                //     _lastRemoteLocalModeState = false; // 默认本地模式
                //     RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = false, Description = "启动时读取失败，默认为本地模式" });
                // }
            }
            else {
                _logger.LogInformation("远程/本地模式 IO 未配置，默认为本地模式");
                _lastRemoteLocalModeState = false; // 默认本地模式
                RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = false, Description = "未配置 IO，默认为本地模式" });
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _pollingTask = Task.Run(() => PollingLoopAsync(_cts.Token), _cts.Token);

            _logger.LogInformation(
                "雷赛控制面板 IO 模块已启动：急停={EmergencyStopBit}, 停止={StopBit}, 启动={StartBit}, 复位={ResetBit}, 轮询间隔={PollingMs}ms",
                _options.CabinetInputPoint.EmergencyStop, _options.CabinetInputPoint.Stop, _options.CabinetInputPoint.Start, _options.CabinetInputPoint.Reset, _options.PollingIntervalMs);
        }

        /// <summary>
        /// 轮询循环，持续读取 IO 端口状态并触发相应事件。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        private async Task PollingLoopAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                try {
                    LeadshineCabinetIoOptions currentOptions;
                    lock (_optionsLock) {
                        currentOptions = _options;
                    }

                    var inputPoint = currentOptions.CabinetInputPoint;

                    // 读取急停按键（如果端口号大于99或小于0则不检测）
                    // 急停是开关类型IO，检测电平变化即可
                    if (inputPoint.EmergencyStop >= 0 && inputPoint.EmergencyStop <= 99) {
                        bool currentState = ReadInputBit(inputPoint.EmergencyStop, inputPoint.InvertEmergencyStopLogic ?? inputPoint.InvertLogic);
                        if (currentState && !_lastEmergencyStopState) {
                            _logger.LogWarning("【IO端点调用】检测到急停按键按下 - IO端口：IN{Port}", inputPoint.EmergencyStop);
                            EmergencyStop?.Invoke(this, new SafetyTriggerEventArgs { Kind = SafetyTriggerKind.EmergencyStop, Description = "物理急停按键" });
                        }
                        _lastEmergencyStopState = currentState;
                    }

                    // 读取停止按键（如果端口号大于99或小于0则不检测）
                    // 停止按钮是瞬时触发型，使用电平检测+边沿触发（检测到触发电平时触发一次）
                    if (inputPoint.Stop >= 0 && inputPoint.Stop <= 99) {
                        bool currentState = ReadInputBit(inputPoint.Stop, inputPoint.InvertStopLogic ?? inputPoint.InvertLogic);
                        // 检测到触发电平（从非触发状态到触发状态的转换）时触发一次
                        // ReadInputBit 已经应用了反转逻辑，所以 currentState=true 表示按键处于触发状态
                        if (currentState && !_lastStopState) {
                            _logger.LogInformation("【IO端点调用】检测到停止按键按下 - IO端口：IN{Port}", inputPoint.Stop);
                            StopRequested?.Invoke(this, new SafetyTriggerEventArgs { Kind = SafetyTriggerKind.StopButton, Description = "物理停止按键" });
                        }
                        _lastStopState = currentState;
                    }

                    // 读取启动按键（如果端口号大于99或小于0则不检测）
                    // 启动按钮是瞬时触发型，使用电平检测+边沿触发（检测到触发电平时触发一次）
                    if (inputPoint.Start >= 0 && inputPoint.Start <= 99) {
                        bool currentState = ReadInputBit(inputPoint.Start, inputPoint.InvertStartLogic ?? inputPoint.InvertLogic);
                        // 检测到触发电平（从非触发状态到触发状态的转换）时触发一次
                        // ReadInputBit 已经应用了反转逻辑，所以 currentState=true 表示按键处于触发状态
                        if (currentState && !_lastStartState) {
                            _logger.LogInformation("【IO端点调用】检测到启动按键按下 - IO端口：IN{Port}", inputPoint.Start);
                            StartRequested?.Invoke(this, new SafetyTriggerEventArgs { Kind = SafetyTriggerKind.StartButton, Description = "物理启动按键" });
                        }
                        _lastStartState = currentState;
                    }

                    // 读取复位按键（如果端口号大于99或小于0则不检测）
                    // 复位按钮是瞬时触发型，使用电平检测+边沿触发（检测到触发电平时触发一次）
                    if (inputPoint.Reset >= 0 && inputPoint.Reset <= 99) {
                        bool currentState = ReadInputBit(inputPoint.Reset, inputPoint.InvertResetLogic ?? inputPoint.InvertLogic);
                        // 检测到触发电平（从非触发状态到触发状态的转换）时触发一次
                        // ReadInputBit 已经应用了反转逻辑，所以 currentState=true 表示按键处于触发状态
                        if (currentState && !_lastResetState) {
                            _logger.LogInformation("【IO端点调用】检测到复位按键按下 - IO端口：IN{Port}", inputPoint.Reset);
                            ResetRequested?.Invoke(this, new SafetyTriggerEventArgs { Kind = SafetyTriggerKind.ResetButton, Description = "物理复位按键" });
                        }
                        _lastResetState = currentState;
                    }

                    // 读取远程/本地模式（如果端口号大于99或小于0则不检测）
                    // 远程/本地是开关类型IO，检测电平变化即可
                    if (inputPoint.RemoteLocalMode >= 0 && inputPoint.RemoteLocalMode <= 99) {
                        bool rawState = ReadInputBit(inputPoint.RemoteLocalMode, inputPoint.InvertRemoteLocalLogic ?? inputPoint.InvertLogic);
                        // 根据 RemoteLocalActiveHigh 配置决定高电平对应的模式
                        bool isRemoteMode = inputPoint.RemoteLocalActiveHigh ? rawState : !rawState;
                        
                        if (isRemoteMode != _lastRemoteLocalModeState) {
                            var modeText = isRemoteMode ? "远程模式" : "本地模式";
                            _logger.LogInformation("检测到远程/本地模式切换：{Mode}", modeText);
                            RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs { IsRemoteMode = isRemoteMode, Description = $"切换到{modeText}" });
                            _lastRemoteLocalModeState = isRemoteMode;
                        }
                    }

                    await Task.Delay(currentOptions.PollingIntervalMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                    break;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "读取控制面板 IO 时发生异常");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("雷赛控制面板 IO 模块已停止");
        }

        /// <summary>
        /// 读取指定输入位的状态。
        /// </summary>
        /// <param name="bitNo">输入位编号。</param>
        /// <param name="invertLogic">是否反转逻辑（true=低电平触发，false=高电平触发）。</param>
        /// <returns>按键是否处于触发状态。true=按下，false=未按下。</returns>
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
        public void UpdateOptions(LeadshineCabinetIoOptions newOptions) {
            lock (_optionsLock) {
                _options = newOptions;
                _logger.LogInformation(
                    "控制面板 IO 配置已更新：急停={EmergencyStopBit}, 停止={StopBit}, 启动={StartBit}, 复位={ResetBit}, 轮询间隔={PollingMs}ms",
                    _options.CabinetInputPoint.EmergencyStop, _options.CabinetInputPoint.Stop, _options.CabinetInputPoint.Start, _options.CabinetInputPoint.Reset, _options.PollingIntervalMs);
            }
        }

        /// <summary>
        /// 释放模块占用的资源。
        /// </summary>
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
