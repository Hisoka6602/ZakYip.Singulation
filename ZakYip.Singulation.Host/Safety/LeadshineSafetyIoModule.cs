using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private readonly LeadshineSafetyIoOptions _options;
        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private bool _disposed;

        // 按键状态缓存（用于边沿检测）
        private bool _lastEmergencyStopState;
        private bool _lastStopState;
        private bool _lastStartState;
        private bool _lastResetState;

        public string Name => "leadshine-hardware-io";

        public event EventHandler<SafetyTriggerEventArgs>? EmergencyStop;
        public event EventHandler<SafetyTriggerEventArgs>? StopRequested;
        public event EventHandler<SafetyTriggerEventArgs>? StartRequested;
        public event EventHandler<SafetyTriggerEventArgs>? ResetRequested;

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
                    // 读取急停按键
                    if (_options.EmergencyStopBit >= 0) {
                        bool currentState = ReadInputBit(_options.EmergencyStopBit, _options.InvertEmergencyStopLogic ?? _options.InvertLogic);
                        if (currentState && !_lastEmergencyStopState) {
                            _logger.LogWarning("检测到急停按键按下");
                            EmergencyStop?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.EmergencyStop, "物理急停按键"));
                        }
                        _lastEmergencyStopState = currentState;
                    }

                    // 读取停止按键
                    if (_options.StopBit >= 0) {
                        bool currentState = ReadInputBit(_options.StopBit, _options.InvertStopLogic ?? _options.InvertLogic);
                        if (currentState && !_lastStopState) {
                            _logger.LogInformation("检测到停止按键按下");
                            StopRequested?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.StopButton, "物理停止按键"));
                        }
                        _lastStopState = currentState;
                    }

                    // 读取启动按键
                    if (_options.StartBit >= 0) {
                        bool currentState = ReadInputBit(_options.StartBit, _options.InvertStartLogic ?? _options.InvertLogic);
                        if (currentState && !_lastStartState) {
                            _logger.LogInformation("检测到启动按键按下");
                            StartRequested?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.StartButton, "物理启动按键"));
                        }
                        _lastStartState = currentState;
                    }

                    // 读取复位按键
                    if (_options.ResetBit >= 0) {
                        bool currentState = ReadInputBit(_options.ResetBit, _options.InvertResetLogic ?? _options.InvertLogic);
                        if (currentState && !_lastResetState) {
                            _logger.LogInformation("检测到复位按键按下");
                            ResetRequested?.Invoke(this, new SafetyTriggerEventArgs(SafetyTriggerKind.ResetButton, "物理复位按键"));
                        }
                        _lastResetState = currentState;
                    }

                    await Task.Delay(_options.PollingIntervalMs, ct).ConfigureAwait(false);
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
                // 返回值：0=低电平/未按下，1=高电平/按下，<0=错误
                short result = LTDMC.dmc_read_inbit(_cardNo, (ushort)bitNo);
                
                if (result < 0) {
                    _logger.LogWarning("读取输入位 {BitNo} 失败，错误码：{ErrorCode}", bitNo, result);
                    return false;
                }

                // 根据配置决定是否反转逻辑（常开/常闭）
                bool state = result == 1;
                return invertLogic ? !state : state;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "读取输入位 {BitNo} 时发生异常", bitNo);
                return false;
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

    /// <summary>
    /// 雷赛安全 IO 模块配置选项。
    /// </summary>
    public sealed class LeadshineSafetyIoOptions {
        /// <summary>急停按键输入位编号，-1 表示禁用。</summary>
        public int EmergencyStopBit { get; set; } = -1;

        /// <summary>停止按键输入位编号，-1 表示禁用。</summary>
        public int StopBit { get; set; } = -1;

        /// <summary>启动按键输入位编号，-1 表示禁用。</summary>
        public int StartBit { get; set; } = -1;

        /// <summary>复位按键输入位编号，-1 表示禁用。</summary>
        public int ResetBit { get; set; } = -1;

        /// <summary>轮询间隔（毫秒），默认 50ms。</summary>
        public int PollingIntervalMs { get; set; } = 50;

        /// <summary>是否反转输入逻辑（用于常闭按键），默认 false。此属性作为默认值，可被各按键独立配置覆盖。</summary>
        public bool InvertLogic { get; set; } = false;

        /// <summary>急停按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertEmergencyStopLogic { get; set; } = null;

        /// <summary>停止按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertStopLogic { get; set; } = null;

        /// <summary>启动按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertStartLogic { get; set; } = null;

        /// <summary>复位按键是否反转输入逻辑（用于常闭按键），null 时使用 InvertLogic 的值。</summary>
        public bool? InvertResetLogic { get; set; } = null;
    }
}
