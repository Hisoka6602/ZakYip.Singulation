using Polly;
using System;
using csLTDMC;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Core.Abstractions;
using ZakYip.Singulation.Drivers.Abstractions;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using NLog;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// 雷赛 LTDMC 总线适配器：封装 dmc_board_init_eth / nmc_get_total_slaves / nmc_get_errcode / dmc_cool_reset / dmc_board_close。
    /// </summary>
    /// <remarks>
    /// 该适配器实现了 <see cref="IBusAdapter"/> 接口，提供对雷赛 LTD-MC 系列控制器的通信封装。
    /// 所有操作均进行安全隔离：底层 SDK 调用异常不会向上传播，而是通过 <see cref="LastErrorMessage"/> 和 <see cref="ErrorOccurred"/> 事件通知上层。
    /// </remarks>
    public sealed class LeadshineLtdmcBusAdapter : IBusAdapter
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly ISystemClock _clock;
        private readonly ushort _cardNo;
        private readonly ushort _portNo;
        private readonly string? _controllerIp;
        private volatile string? _lastErrorMessage;
        private readonly object _errorLock = new object();
        private readonly IEmcResourceLock _resourceLock;
        private readonly EmcResetCoordinator _resetCoordinator;
        
        /// <summary>
        /// 用于保护操作的信号量，确保在重新连接期间阻止其他操作。
        /// </summary>
        private readonly SemaphoreSlim _operationSemaphore = new SemaphoreSlim(1, 1);
        
        /// <summary>
        /// 指示当前是否正在执行重新连接操作。
        /// </summary>
        private volatile bool _isReconnecting;
        
        /// <summary>
        /// 当开始重新连接时触发（在调用 Close 之前）。
        /// </summary>
        public event EventHandler? ReconnectionStarting;
        
        /// <summary>
        /// 当重新连接完成时触发（在重新初始化之后）。
        /// </summary>
        public event EventHandler? ReconnectionCompleted;

        /// <summary>
        /// 获取控制器IP地址（以太网模式），若为本地PCI模式则返回 null
        /// </summary>
        public string? ControllerIp => _controllerIp;

        /// <summary>
        /// 获取最后一次操作的错误信息（线程安全）。如果最近一次操作成功，此值为 null。
        /// </summary>
        public string? LastErrorMessage
        {
            get
            {
                lock (_errorLock)
                    return _lastErrorMessage;
            }
            private set
            {
                lock (_errorLock)
                    _lastErrorMessage = value;
            }
        }

        /// <summary>
        /// 当发生错误时触发的事件（线程安全）。
        /// 可用于上层监控总线状态。
        /// </summary>
        public event Action<IBusAdapter, string>? ErrorOccurred;

        /// <summary>
        /// 当接收到其他进程的 EMC 复位通知时触发。
        /// <para>
        /// 应用程序应订阅此事件，以便在其他实例执行复位时采取适当的应对措施（如暂停操作、保存状态等）。
        /// </para>
        /// </summary>
        public event EventHandler<EmcResetEventArgs>? EmcResetNotificationReceived;

        /// <summary>
        /// 初始化一个新的雷赛 LTD-MC 总线适配器实例。
        /// </summary>
        /// <param name="cardNo">控制器卡号（通常为 0）。</param>
        /// <param name="portNo">端口号（CAN/EtherCAT 端口编号）。</param>
        /// <param name="controllerIp">控制器 IP 地址（仅以太网模式需要）；若为空或 null，则使用本地 PCI 模式。</param>
        /// <param name="clock">系统时钟（用于时间戳和时间计算）。</param>
        public LeadshineLtdmcBusAdapter(ushort cardNo, ushort portNo, string? controllerIp, ISystemClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _cardNo = cardNo;
            _portNo = portNo;
            _controllerIp = string.IsNullOrWhiteSpace(controllerIp) ? null : controllerIp;
            
            // 初始化分布式锁（基于卡号）
            _resourceLock = new EmcNamedMutexLock($"CardNo_{cardNo}");
            
            // 初始化复位协调器
            _resetCoordinator = new EmcResetCoordinator(cardNo, clock, enablePolling: true);
            _resetCoordinator.ResetNotificationReceived += OnResetNotificationReceived;
            
            _logger.Info($"[LeadshineBusAdapter] 已初始化分布式锁和复位协调器，卡号: {cardNo}");
        }

        /// <summary>
        /// 处理来自其他进程的复位通知。
        /// </summary>
        private void OnResetNotificationReceived(object? sender, EmcResetEventArgs e)
        {
            _logger.Warn($"[LeadshineBusAdapter] 收到 EMC 复位通知 - 卡号: {e.Notification.CardNo}, " +
                        $"类型: {e.Notification.ResetType}, 来源: {e.Notification.ProcessName}({e.Notification.ProcessId}), " +
                        $"预计恢复时间: {e.Notification.EstimatedRecoverySeconds}秒");
            
            // 向上层传播事件
            EmcResetNotificationReceived?.Invoke(this, e);
            
            // 触发重新连接流程（异步执行，不阻塞事件处理）
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleReconnectionAsync(e.Notification, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[LeadshineBusAdapter] 处理重新连接失败");
                }
            });
        }
        
        /// <summary>
        /// 停止所有轴的速度（设置目标速度为 0）并失能所有轴。
        /// <para>
        /// 直接通过 LTDMC API 操作所有从站节点，无需通过 IAxisDrive 实例。
        /// 这是在接收到复位通知时的关键步骤，确保所有轴安全停止。
        /// </para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async Task StopAndDisableAllAxesAsync(CancellationToken ct)
        {
            try
            {
                // 获取总线上的轴数量
                ushort totalSlaves = 0;
                var ret = LTDMC.nmc_get_total_slaves(_cardNo, _portNo, ref totalSlaves);
                if (ret != 0 || totalSlaves == 0)
                {
                    _logger.Warn($"[LeadshineBusAdapter] 无法获取轴数量或没有轴，跳过停止和失能操作。ret={ret}, totalSlaves={totalSlaves}");
                    return;
                }

                _logger.Info($"[LeadshineBusAdapter] 开始停止并失能 {totalSlaves} 个轴");

                // 遍历所有从站节点（节点地址通常从 1 开始）
                for (ushort nodeId = 1; nodeId <= totalSlaves; nodeId++)
                {
                    try
                    {
                        // 步骤 1：设置目标速度为 0 (0x60FF - Target Velocity)
                        var zeroVelocity = new byte[4]; // INT32，值为 0
                        WritePdoQuietly(nodeId, 0x60FF, 0, 32, zeroVelocity, "设置速度为0");

                        // 步骤 2：执行 QuickStop (ControlWord = 0x0002)
                        WritePdoQuietly(nodeId, 0x6040, 0, 16, BitConverter.GetBytes((ushort)0x0002), "QuickStop");

                        await Task.Delay(50, ct).ConfigureAwait(false); // 给予短暂延时确保命令生效

                        // 步骤 3：执行 Shutdown (ControlWord = 0x0006) 进入 Ready to Switch On 状态
                        WritePdoQuietly(nodeId, 0x6040, 0, 16, BitConverter.GetBytes((ushort)0x0006), "Shutdown");

                        _logger.Debug($"[LeadshineBusAdapter] 轴 {nodeId} 已停止并失能");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"[LeadshineBusAdapter] 处理轴 {nodeId} 时发生异常");
                    }
                }

                _logger.Info($"[LeadshineBusAdapter] 所有轴已停止并失能，状态：[停止]");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[LeadshineBusAdapter] StopAndDisableAllAxesAsync 执行失败");
            }
        }

        /// <summary>
        /// 写入 PDO 数据的辅助方法，失败时仅记录警告而不抛出异常。
        /// </summary>
        private void WritePdoQuietly(ushort nodeId, ushort index, ushort subIndex, ushort bitLength, byte[] data, string operation)
        {
            var ret = LTDMC.nmc_write_rxpdo(_cardNo, _portNo, nodeId, index, subIndex, bitLength, data);
            if (ret != 0)
            {
                _logger.Warn($"[LeadshineBusAdapter] 轴 {nodeId} {operation} 失败，ret={ret}");
            }
        }

        /// <summary>
        /// 直接使用 LTDMC API 重新连接，不触发完整的 InitializeAsync 流程。
        /// <para>
        /// 这避免了在 InitializeAsync 中可能触发的额外复位操作。
        /// </para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>连接是否成功。</returns>
        private async Task<bool> ReconnectDirectAsync(CancellationToken ct)
        {
            try
            {
                var isEthernet = _controllerIp is not null;
                var methodName = isEthernet ? "dmc_board_init_eth" : "dmc_board_init";
                
                _logger.Info($"[LeadshineBusAdapter] 开始直接重新连接，卡号: {_cardNo}，方法: {methodName}");

                // 使用 Task.Run 避免阻塞
                var initRet = await Task.Run(() =>
                    isEthernet ? LTDMC.dmc_board_init_eth(_cardNo, _controllerIp!) : LTDMC.dmc_board_init(),
                    ct).ConfigureAwait(false);

                if (initRet != 0)
                {
                    _logger.Error($"[LeadshineBusAdapter] 直接重新连接失败，方法：{methodName}，返回值：{initRet}（预期：0），卡号：{_cardNo}");
                    SetError($"直接重新连接失败: ret={initRet}");
                    return false;
                }

                _logger.Info($"[LeadshineBusAdapter] 直接重新连接成功，方法：{methodName}，返回值：{initRet}，卡号：{_cardNo}");
                IsInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[LeadshineBusAdapter] 直接重新连接异常，卡号: {_cardNo}");
                SetError($"直接重新连接异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理重新连接流程：停止所有轴 → 失能所有轴 → 关闭连接 → 等待恢复 → 重新连接。
        /// <para>
        /// 在此过程中，所有其他操作将被阻塞，直到重新连接完成。
        /// </para>
        /// </summary>
        /// <param name="notification">复位通知信息。</param>
        /// <param name="ct">取消令牌。</param>
        private async Task HandleReconnectionAsync(EmcResetNotification notification, CancellationToken ct)
        {
            // 获取操作信号量，阻止所有其他操作
            await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            
            try
            {
                _isReconnecting = true;
                _logger.Info($"[LeadshineBusAdapter] 开始重新连接流程，卡号: {_cardNo}");
                
                // 触发重新连接开始事件
                ReconnectionStarting?.Invoke(this, EventArgs.Empty);
                
                // 步骤 1：停止所有轴速度并失能所有轴，设置状态为 [停止]
                _logger.Info($"[LeadshineBusAdapter] 步骤 1/4：停止所有轴速度并失能所有轴");
                await StopAndDisableAllAxesAsync(ct).ConfigureAwait(false);
                
                // 步骤 2：关闭当前连接
                _logger.Info($"[LeadshineBusAdapter] 步骤 2/4：关闭当前连接");
                await CloseAsync(ct).ConfigureAwait(false);
                
                // 步骤 3：等待预计的恢复时间（在其他实例完成复位/重置操作之前不能操作轴和IO）
                var waitTime = TimeSpan.FromSeconds(notification.EstimatedRecoverySeconds + 2); // 额外 2 秒缓冲
                _logger.Info($"[LeadshineBusAdapter] 步骤 3/4：等待 {waitTime.TotalSeconds} 秒以确保其他实例完成复位");
                await Task.Delay(waitTime, ct).ConfigureAwait(false);
                
                // 步骤 4：直接重新连接（使用 dmc_board_init/dmc_board_init_eth，而不是 InitializeAsync）
                _logger.Info($"[LeadshineBusAdapter] 步骤 4/4：直接重新连接（不调用 InitializeAsync）");
                var reconnectSuccess = await ReconnectDirectAsync(ct).ConfigureAwait(false);
                
                if (reconnectSuccess)
                {
                    _logger.Info($"[LeadshineBusAdapter] 重新连接成功，卡号: {_cardNo}");
                }
                else
                {
                    _logger.Error($"[LeadshineBusAdapter] 重新连接失败，卡号: {_cardNo}");
                    SetError("重新连接失败");
                }
                
                // 触发重新连接完成事件
                ReconnectionCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[LeadshineBusAdapter] 重新连接过程异常，卡号: {_cardNo}");
                SetError($"重新连接过程异常: {ex.Message}");
            }
            finally
            {
                _isReconnecting = false;
                _operationSemaphore.Release();
                _logger.Info($"[LeadshineBusAdapter] 重新连接流程结束，卡号: {_cardNo}");
            }
        }

        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// 获取一个值，指示当前是否正在执行重新连接操作。
        /// <para>
        /// 在重新连接期间，所有雷赛方法调用和 IO 监控应被阻止。
        /// </para>
        /// </summary>
        public bool IsReconnecting => _isReconnecting;

        public async Task<KeyValuePair<bool, string>> InitializeAsync(CancellationToken ct = default)
        {
            // 获取操作信号量（除非已经在重新连接流程中）
            if (!_isReconnecting)
            {
                await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            }
            
            try
            {
                return await InitializeInternalAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                if (!_isReconnecting)
                {
                    _operationSemaphore.Release();
                }
            }
        }
        
        /// <summary>
        /// 内部初始化方法，假定调用者已持有操作信号量。
        /// </summary>
        private async Task<KeyValuePair<bool, string>> InitializeInternalAsync(CancellationToken ct = default)
        {
            return await Safe<KeyValuePair<bool, string>>(async () =>
            {
                if (IsInitialized)
                    return new(true, "Already initialized."); // 幂等

                var ok = await EnsureBootGapAsync(TimeSpan.FromSeconds(15), ct);
                if (!ok.Key)
                {
                    SetError(ok.Value);
                }
                // 重试节奏：0ms → 300ms → 1s → 2s（可调）
                var delays = new[]
                {
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
        };

                int attempt = 0;

                var policy = Policy
                    .HandleResult<KeyValuePair<bool, string>>(r => r.Key == false)
                    .Or<Exception>()
                    .WaitAndRetryAsync(
                        delays,
                        onRetryAsync: (outcome, delay, retryAttempt, _) =>
                        {
                            var reason = outcome.Exception?.Message ?? outcome.Result.Value;
                            SetError($"Initialize retry #{retryAttempt} after {delay.TotalMilliseconds}ms, reason={reason}");
                            return Task.CompletedTask;
                        });

                return await policy.ExecuteAsync(async () =>
                {
                    attempt++;

                    // === 1) 可选 Ping 预检（仅当配置了 IP） ===
                    if (_controllerIp != null)
                    {
                        try
                        {
                            using var ping = new Ping();
                            var reply = await ping.SendPingAsync(_controllerIp, TimeSpan.FromMilliseconds(1000), cancellationToken: ct)
                                                  .ConfigureAwait(false);
                            if (reply.Status != IPStatus.Success)
                            {
                                const string msg = "Ping controller failed.";
                                SetError(msg);
                                return new(false, msg);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return new(false, "Canceled");
                        }
                        catch
                        {
                            const string msg = "Ping 异常";
                            SetError(msg);
                            return new(false, msg);
                        }
                    }

                    // === 2) LTDMC 初始化（WhenAny 严格超时 + 返回码判定） ===
                    try
                    {
                        var initTask = Task.Run(() => _controllerIp is null
                            ? LTDMC.dmc_board_init()
                            : LTDMC.dmc_board_init_eth(_cardNo, _controllerIp), ct);

                        var timeoutTask = Task.Delay(5000, ct);
                        var winner = await Task.WhenAny(initTask, timeoutTask).ConfigureAwait(false);
                        if (winner != initTask)
                        {
                            const string msg = "LTDMC init 超时";
                            SetError(msg);
                            return new(false, msg);
                        }

                        var ret = await initTask.ConfigureAwait(false);
                        if (ret != 0)
                        {
                            var msg = $"LTDMC init 返回: {ret}";
                            _logger.Error($"【面板控制器初始化失败】方法：{(_controllerIp is null ? "dmc_board_init" : "dmc_board_init_eth")}，返回值：{ret}（预期：0），卡号：{_cardNo}");
                            SetError(msg);
                            return new(false, msg);
                        }
                        _logger.Info($"【面板控制器初始化成功】方法：{(_controllerIp is null ? "dmc_board_init" : "dmc_board_init_eth")}，返回值：{ret}，卡号：{_cardNo}");
                    }
                    catch (OperationCanceledException)
                    {
                        const string msg = "LTDMC init 取消";
                        SetError(msg);
                        return new(false, msg);
                    }
                    catch (Exception ex)
                    {
                        var msg = $"LTDMC init 异常: {ex.Message}";
                        SetError(msg);
                        return new(false, msg);
                    }

                    // === 3) 总线健康检查（在初始化之后：唯一判据：errcode） ===
                    try
                    {
                        ushort errcode = 0;
                        LTDMC.nmc_get_errcode(_cardNo, _portNo, ref errcode);
                        if (errcode != 0)
                        {
                            _logger.Warn($"【面板控制器总线异常检测】方法：nmc_get_errcode，错误码：{errcode}（预期：0），卡号：{_cardNo}，端口：{_portNo}，尝试次数：{attempt}");
                            // 本次尝试仅做一次复位：偶数尝试→软复位；奇数尝试→冷复位
                            var useSoftReset = (attempt % 2 == 0);

                            try
                            {
                                if (useSoftReset)
                                {
                                    var rc = await Task.Run(() =>
                                        {
                                            LTDMC.dmc_soft_reset(_cardNo);
                                            return LTDMC.dmc_board_close();
                                        }, ct);
                                    if (rc != 0)
                                    {
                                        var msg = $"软复位失败: rc={rc}";
                                        _logger.Error($"【面板控制器软复位失败】方法：dmc_board_close，返回值：{rc}（预期：0），卡号：{_cardNo}");
                                        SetError(msg);
                                        return new(false, msg);
                                    }
                                    _logger.Info($"【面板控制器软复位成功】方法：dmc_soft_reset + dmc_board_close，返回值：{rc}，卡号：{_cardNo}");
                                    await Task.Delay(TimeSpan.FromSeconds(10), ct); // 恢复时间
                                }
                                else
                                {
                                    await ResetAsync(ct);      // 冷复位（无返回值）
                                    await Task.Delay(200, ct); // 恢复时间
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                return new(false, "Canceled");
                            }
                            catch (Exception ex)
                            {
                                var msg = $"复位过程异常: {ex.Message}";
                                SetError(msg);
                                return new(false, msg);
                            }

                            // 复位后再读一次 errcode；若仍异常则失败，交给 Polly
                            LTDMC.nmc_get_errcode(_cardNo, _portNo, ref errcode);
                            if (errcode != 0)
                            {
                                var msg = $"总线异常未恢复: err={errcode}";
                                _logger.Error($"【面板控制器总线异常未恢复】方法：nmc_get_errcode，错误码：{errcode}（预期：0），卡号：{_cardNo}，端口：{_portNo}");
                                SetError(msg);
                                return new(false, msg);
                            }
                            _logger.Info($"【面板控制器总线异常已恢复】错误码：{errcode}，卡号：{_cardNo}，端口：{_portNo}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return new(false, "Canceled");
                    }
                    catch (Exception ex)
                    {
                        var msg = $"总线检查异常: {ex.Message}";
                        SetError(msg);
                        return new(false, msg);
                    }

                    IsInitialized = true;
                    return new(true, "Bus initialized.");
                });
            }, "InitializeAsync");
        }

        /// <summary>
        /// 关闭/释放控制器资源（幂等）。
        /// 若未初始化，则直接返回。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>任务。</returns>
        public async Task CloseAsync(CancellationToken ct = default)
        {
            // 如果正在重新连接，则内部调用，不需要获取信号量
            if (!_isReconnecting)
            {
                await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            }
            
            try
            {
                await Safe(() =>
                {
                    if (!IsInitialized)
                        return Task.CompletedTask;

                    LTDMC.dmc_board_close();
                    IsInitialized = false;
                    
                    // 仅在非重新连接流程中释放分布式资源
                    if (!_isReconnecting)
                    {
                        _resourceLock?.Dispose();
                        _resetCoordinator?.Dispose();
                    }
                    
                    return Task.CompletedTask;
                }, "CloseAsync").ConfigureAwait(false);
            }
            finally
            {
                if (!_isReconnecting)
                {
                    _operationSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// 获取总线发现到的轴数量（1-based 索引习惯由上层决定）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>轴数量；若通信失败，返回 0。</returns>
        public async Task<int> GetAxisCountAsync(CancellationToken ct = default)
        {
            if (_isReconnecting)
            {
                _logger.Warn($"[LeadshineBusAdapter] GetAxisCountAsync 被阻止：当前正在重新连接");
                return 0;
            }
            
            await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await Safe(() =>
                {
                    ushort total = 0;
                    var ret = LTDMC.nmc_get_total_slaves(_cardNo, _portNo, ref total);
                    if (ret != 0)
                    {
                        _logger.Error($"【面板控制器获取轴数失败】方法：nmc_get_total_slaves，返回值：{ret}（预期：0），卡号：{_cardNo}，端口：{_portNo}");
                        throw new InvalidOperationException($"nmc_get_total_slaves failed, ret={ret}");
                    }

                    return Task.FromResult((int)total);
                }, "GetAxisCountAsync", defaultValue: 0).ConfigureAwait(false);
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 读取当前错误码；0 表示正常。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>错误码；若通信失败，返回 -999。</returns>
        public async Task<int> GetErrorCodeAsync(CancellationToken ct = default)
        {
            if (_isReconnecting)
            {
                _logger.Warn($"[LeadshineBusAdapter] GetErrorCodeAsync 被阻止：当前正在重新连接");
                return -999;
            }
            
            await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await Safe(() =>
                {
                    ushort err = 0;
                    var ret = LTDMC.nmc_get_errcode(_cardNo, _portNo, ref err);
                    if (ret != 0)
                    {
                        _logger.Error($"【面板控制器获取错误码失败】方法：nmc_get_errcode，返回值：{ret}（预期：0），卡号：{_cardNo}，端口：{_portNo}");
                        throw new InvalidOperationException($"nmc_get_errcode failed, ret={ret}");
                    }

                    return Task.FromResult((int)err);
                }, "GetErrorCodeAsync", defaultValue: -999).ConfigureAwait(false);
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 执行控制器冷复位（如需）；通常用于错误码非 0 的场景。
        /// 冷复位会断电重启控制器，耗时约 15 秒。
        /// <para>
        /// 此方法会：
        /// 1. 获取分布式锁以确保独占访问
        /// 2. 广播复位通知到其他进程
        /// 3. 执行冷复位操作
        /// 4. 释放分布式锁
        /// </para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>任务。</returns>
        public async Task ResetAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            if (_isReconnecting)
            {
                _logger.Warn($"[LeadshineBusAdapter] ResetAsync 被阻止：当前正在重新连接");
                throw new InvalidOperationException("Cannot perform reset while reconnecting");
            }
            
            // 获取操作信号量
            await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);

            _logger.Info($"【面板控制器冷复位开始】卡号：{_cardNo}");
            
            try
            {
                // 尝试获取分布式锁（30 秒超时）
                var lockAcquired = await _resourceLock.TryAcquireAsync(TimeSpan.FromSeconds(30), ct);
                if (!lockAcquired)
                {
                    var errorMsg = "无法获取 EMC 资源锁，可能有其他实例正在执行复位操作";
                    _logger.Error($"【面板控制器冷复位失败】{errorMsg}");
                    SetError(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                try
                {
                // 广播复位通知到其他进程
                _logger.Info($"【面板控制器冷复位】广播通知到其他进程，卡号：{_cardNo}");
                await _resetCoordinator.BroadcastResetNotificationAsync(EmcResetType.Cold, ct);

                // 给其他进程一些时间接收通知并准备
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);

                var success = await Safe(async () =>
                {
                    // 执行冷复位并检查返回值
                    LTDMC.dmc_cool_reset(_cardNo);
                    var rc = LTDMC.dmc_board_close();
                    if (rc != 0)
                    {
                        _logger.Error($"【面板控制器冷复位失败】方法：dmc_board_close，返回值：{rc}（预期：0），卡号：{_cardNo}");
                        throw new InvalidOperationException($"Cold reset failed for card {_cardNo} with error code {rc}. Verify hardware connection and card status.");
                    }
                    _logger.Info($"【面板控制器冷复位成功】方法：dmc_cool_reset + dmc_board_close，返回值：{rc}，卡号：{_cardNo}");

                    await CloseAsync(ct).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                    var initResult = await InitializeAsync(ct).ConfigureAwait(false);
                    if (!initResult.Key)
                    {
                        _logger.Error($"【面板控制器冷复位后初始化失败】原因：{initResult.Value}，卡号：{_cardNo}");
                        throw new InvalidOperationException($"Initialization after reset failed: {initResult.Value}");
                    }
                }, "ResetAsync");

                if (!success)
                {
                    _logger.Error($"【面板控制器冷复位流程失败】卡号：{_cardNo}");
                    SetError("ResetAsync: 冷复位流程执行失败。");
                }
                }
                finally
                {
                    // 释放分布式锁
                    _resourceLock.Release();
                    _logger.Info($"【面板控制器冷复位】已释放资源锁，卡号：{_cardNo}");
                }
            }
            finally
            {
                // 释放操作信号量
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 执行控制器热复位（软复位）。
        /// <para>
        /// 热复位通常只会重置通信/状态机，不掉电，耗时短（1~2 秒）。
        /// 若未初始化，将尝试直接初始化。
        /// </para>
        /// <para>
        /// 此方法会：
        /// 1. 获取分布式锁以确保独占访问
        /// 2. 广播复位通知到其他进程
        /// 3. 执行热复位操作
        /// 4. 释放分布式锁
        /// </para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>任务。</returns>
        public async Task WarmResetAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            if (_isReconnecting)
            {
                _logger.Warn($"[LeadshineBusAdapter] WarmResetAsync 被阻止：当前正在重新连接");
                throw new InvalidOperationException("Cannot perform warm reset while reconnecting");
            }
            
            // 获取操作信号量
            await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);

            _logger.Info($"【面板控制器热复位开始】卡号：{_cardNo}");
            
            try
            {
                // 尝试获取分布式锁（30 秒超时）
                var lockAcquired = await _resourceLock.TryAcquireAsync(TimeSpan.FromSeconds(30), ct);
                if (!lockAcquired)
                {
                    var errorMsg = "无法获取 EMC 资源锁，可能有其他实例正在执行复位操作";
                    _logger.Error($"【面板控制器热复位失败】{errorMsg}");
                    SetError(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                try
                {
                // 广播复位通知到其他进程
                _logger.Info($"【面板控制器热复位】广播通知到其他进程，卡号：{_cardNo}");
                await _resetCoordinator.BroadcastResetNotificationAsync(EmcResetType.Warm, ct);

                // 给其他进程一些时间接收通知并准备
                await Task.Delay(TimeSpan.FromMilliseconds(300), ct);

                var success = await Safe(async () =>
                {
                    // 若未初始化，直接初始化即可
                    if (!IsInitialized)
                    {
                        _logger.Info($"【面板控制器未初始化，执行初始化】卡号：{_cardNo}");
                        var (key, value) = await InitializeAsync(ct).ConfigureAwait(false);
                        IsInitialized = key;
                        return;
                    }

                    // 1) 软复位控制器
                    var retSoft = LTDMC.dmc_soft_reset(_cardNo);
                    if (retSoft != 0)
                    {
                        _logger.Error($"【面板控制器热复位失败】方法：dmc_soft_reset，返回值：{retSoft}（预期：0），卡号：{_cardNo}");
                        throw new InvalidOperationException($"dmc_soft_reset failed, ret={retSoft}");
                    }
                    _logger.Info($"【面板控制器热复位成功】方法：dmc_soft_reset，返回值：{retSoft}，卡号：{_cardNo}");

                    // 2) 关闭当前连接
                    LTDMC.dmc_board_close();
                    IsInitialized = false;

                    // 3) 等待控制器复位（官方建议 300~1500ms）
                    await Task.Delay(TimeSpan.FromMilliseconds(800), ct).ConfigureAwait(false);

                    // 4) 重新初始化（仅支持以太网）
                    if (string.IsNullOrWhiteSpace(_controllerIp))
                    {
                        _logger.Error($"【面板控制器热复位失败】原因：热复位需要配置控制器IP地址，卡号：{_cardNo}");
                        throw new InvalidOperationException("WarmReset requires controller IP for dmc_board_init_eth.");
                    }

                    var retInit = LTDMC.dmc_board_init_eth(_cardNo, _controllerIp);
                    if (retInit != 0)
                    {
                        _logger.Error($"【面板控制器热复位后初始化失败】方法：dmc_board_init_eth，返回值：{retInit}（预期：0），卡号：{_cardNo}");
                        throw new InvalidOperationException($"dmc_board_init_eth failed, ret={retInit}");
                    }
                    _logger.Info($"【面板控制器热复位后初始化成功】方法：dmc_board_init_eth，返回值：{retInit}，卡号：{_cardNo}");

                    IsInitialized = true;
                }, "WarmResetAsync");

                if (!success)
                {
                    _logger.Error($"【面板控制器热复位流程失败】卡号：{_cardNo}");
                    SetError("WarmResetAsync: 热复位流程执行失败。");
                }
                }
                finally
                {
                    // 释放分布式锁
                    _resourceLock.Release();
                    _logger.Info($"【面板控制器热复位】已释放资源锁，卡号：{_cardNo}");
                }
            }
            finally
            {
                // 释放操作信号量
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 根据厂商规则转换逻辑 NodeId → 物理 NodeId。
        /// <para>
        /// 例如：某些厂商的 NodeId 从 1 开始，而上层逻辑用 1001、1002 表示；
        /// 在创建驱动时需调用本方法做转换。
        /// </para>
        /// </summary>
        /// <param name="logicalNodeId">逻辑层的 NodeId（如 1001）。</param>
        /// <returns>物理层的 NodeId（如 1）。</returns>
        public ushort TranslateNodeId(ushort logicalNodeId)
        {
            // 传入：物理 1,2,3…；输出：逻辑 1001,1002,1003…
            // 若传入已是 1000+，保持不变（防止重复映射）
            return logicalNodeId >= 1000 ? logicalNodeId : (ushort)(1000 + logicalNodeId);
        }

        /// <summary>
        /// 根据厂商/拓扑规则判断指定轴是否需要反转。
        /// <para>
        /// 例如：某些设备是奇数反转、偶数正转；
        /// 也可能完全不反转；或者有更复杂的映射表。
        /// </para>
        /// </summary>
        /// <param name="logicalNodeId">逻辑层的 NodeId（如 1001）。</param>
        /// <returns>true 表示需要反转；false 表示保持模板默认方向。</returns>
        public bool ShouldReverse(ushort logicalNodeId)
        {
            // 奇数 NodeId 反转
            return logicalNodeId % 2 == 1;
        }

        /// <summary>
        /// 计算“系统开机至当前进程启动”的时间差（boot→appStart gap，使用本地时间）。
        /// 若 gap 小于给定阈值，则等待 gap；否则不等待。
        /// 返回 (true, msg) 表示可以继续初始化；(false, msg) 表示在等待期间被取消。
        /// </summary>
        private static async Task<KeyValuePair<bool, string>> EnsureBootGapAsync(
            TimeSpan threshold,
            CancellationToken ct = default)
        {
            var log = NLog.LogManager.GetCurrentClassLogger();

            // 1) 系统已开机时长（毫秒计时器，不受本地时间影响）
            var systemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64);

            // 2) 当前进程已运行时长（使用本地时间）
            TimeSpan processUptime;
            try
            {
                var ps = System.Diagnostics.Process.GetCurrentProcess();
                // 注意：StartTime 与 DateTime.Now 同为本地时间
                processUptime = DateTime.Now - ps.StartTime;
            }
            catch (Exception ex)
            {
                log.Warn(ex, "[BootGap] 读取进程启动时间失败，跳过等待。");
                return new(true, "Skipped: cannot read process start time.");
            }

            // 3) gap = 系统开机至进程启动的间隔
            var gap = systemUptime - processUptime;
            if (gap < TimeSpan.Zero)
                gap = TimeSpan.Zero;

            // 4) 若 gap < 阈值，则等待 gap（可取消）
            if (gap > TimeSpan.Zero && gap < threshold)
            {
                log.Info($"[BootGap] gap={gap.TotalMilliseconds:N0}ms < {threshold.TotalMilliseconds:N0}ms, delay {gap.TotalMilliseconds:N0}ms.");
                try
                {
                    await Task.Delay(gap, ct).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    log.Warn("[BootGap] 等待被取消。");
                    return new(false, "Canceled during boot-gap delay.");
                }
            }
            else
            {
                log.Info($"[BootGap] gap={gap.TotalMilliseconds:N0}ms ≥ {threshold.TotalMilliseconds:N0}ms，无需等待。");
            }

            return new(true, "Boot-gap ensured.");
        }

        #region 安全执行工具方法

        /// <summary>
        /// 安全执行某个异步动作：吞掉异常并返回 false；具体异常信息由驱动层事件和 <see cref="LastErrorMessage"/> 负责记录。
        /// </summary>
        /// <param name="act">要执行的异步操作。</param>
        /// <param name="operationName">操作名称，用于错误日志。</param>
        /// <returns>任务，成功返回 true，失败返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<bool> Safe(Func<Task> act, string operationName)
        {
            try
            {
                await act().ConfigureAwait(false);
                ClearError();
                return true;
            }
            catch (Exception ex)
            {
                SetError($"{operationName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全执行有返回值的异步函数，失败时返回默认值。
        /// </summary>
        /// <typeparam name="T">返回值类型。</typeparam>
        /// <param name="func">要执行的异步函数。</param>
        /// <param name="operationName">操作名称，用于错误日志。</param>
        /// <param name="defaultValue">失败时返回的默认值。</param>
        /// <returns>成功返回函数结果，失败返回默认值。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<T> Safe<T>(Func<Task<T>> func, string operationName, T defaultValue = default!)
        {
            try
            {
                var result = await func().ConfigureAwait(false);
                ClearError();
                return result;
            }
            catch (Exception ex)
            {
                SetError($"{operationName}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 设置错误信息并触发事件。
        /// </summary>
        /// <param name="message">错误消息。</param>
        private void SetError(string message)
        {
            LastErrorMessage = message;
            ErrorOccurred?.Invoke(this, message);
        }

        /// <summary>
        /// 清除错误信息。
        /// </summary>
        private void ClearError()
        {
            LastErrorMessage = null;
        }

        #endregion 安全执行工具方法

        #region 批量操作优化方法

        /// <summary>
        /// 批量写入多个轴的 RxPDO，用于提升批量控制性能。
        /// </summary>
        /// <param name="nodeIds">节点 ID 列表</param>
        /// <param name="requests">每个节点的批量写入请求</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量操作结果字典（键为节点 ID）</returns>
        public async Task<Dictionary<ushort, LeadshineBatchPdoOperations.BatchWriteResult[]>> BatchWriteMultipleAxesAsync(
            IReadOnlyList<ushort> nodeIds,
            IReadOnlyList<IReadOnlyList<LeadshineBatchPdoOperations.BatchWriteRequest>> requests,
            CancellationToken ct = default)
        {
            if (nodeIds == null)
                throw new ArgumentNullException(nameof(nodeIds));
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));
            if (nodeIds.Count != requests.Count)
                throw new ArgumentException("节点 ID 列表和请求列表长度必须一致", nameof(nodeIds));
            
            if (_isReconnecting)
            {
                _logger.Warn($"[LeadshineBusAdapter] BatchWriteMultipleAxesAsync 被阻止：当前正在重新连接");
                return new Dictionary<ushort, LeadshineBatchPdoOperations.BatchWriteResult[]>();
            }
            
            await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            
            try
            {
                var results = new Dictionary<ushort, LeadshineBatchPdoOperations.BatchWriteResult[]>();

                // 并行处理多个轴的批量写入
                var tasks = new Task<(ushort nodeId, LeadshineBatchPdoOperations.BatchWriteResult[] result)>[nodeIds.Count];

                for (int i = 0; i < nodeIds.Count; i++)
                {
                    var nodeId = nodeIds[i];
                    var request = requests[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        var result = await LeadshineBatchPdoOperations.BatchWriteRxPdoAsync(
                            _cardNo, _portNo, nodeId, request, ct).ConfigureAwait(false);
                        return (nodeId, result);
                    }, ct);
                }

                var allResults = await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var (nodeId, result) in allResults)
                {
                    results[nodeId] = result;
                }

                return results;
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 批量读取多个轴的 TxPDO，用于提升批量状态查询性能。
        /// </summary>
        /// <param name="nodeIds">节点 ID 列表</param>
        /// <param name="requests">每个节点的批量读取请求</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>批量操作结果字典（键为节点 ID）</returns>
        public async Task<Dictionary<ushort, LeadshineBatchPdoOperations.BatchReadResult[]>> BatchReadMultipleAxesAsync(
            IReadOnlyList<ushort> nodeIds,
            IReadOnlyList<IReadOnlyList<LeadshineBatchPdoOperations.BatchReadRequest>> requests,
            CancellationToken ct = default)
        {
            if (nodeIds == null || requests == null || nodeIds.Count != requests.Count)
            {
                throw new ArgumentException("节点 ID 列表和请求列表长度必须一致");
            }
            
            if (_isReconnecting)
            {
                _logger.Warn($"[LeadshineBusAdapter] BatchReadMultipleAxesAsync 被阻止：当前正在重新连接");
                return new Dictionary<ushort, LeadshineBatchPdoOperations.BatchReadResult[]>();
            }
            
            await _operationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            
            try
            {
                var results = new Dictionary<ushort, LeadshineBatchPdoOperations.BatchReadResult[]>();

                // 并行处理多个轴的批量读取
                var tasks = new Task<(ushort nodeId, LeadshineBatchPdoOperations.BatchReadResult[] result)>[nodeIds.Count];

                for (int i = 0; i < nodeIds.Count; i++)
                {
                    var nodeId = nodeIds[i];
                    var request = requests[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        var result = await LeadshineBatchPdoOperations.BatchReadTxPdoAsync(
                            _cardNo, _portNo, nodeId, request, ct).ConfigureAwait(false);
                        return (nodeId, result);
                    }, ct);
                }

                var allResults = await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var (nodeId, result) in allResults)
                {
                    results[nodeId] = result;
                }

                return results;
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        #endregion 批量操作优化方法
    }
}
