using Polly;
using System;
using csLTDMC;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Abstractions;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ZakYip.Singulation.Drivers.Leadshine {

    /// <summary>
    /// 雷赛 LTDMC 总线适配器：封装 dmc_board_init_eth / nmc_get_total_slaves / nmc_get_errcode / dmc_cool_reset / dmc_board_close。
    /// </summary>
    /// <remarks>
    /// 该适配器实现了 <see cref="IBusAdapter"/> 接口，提供对雷赛 LTD-MC 系列控制器的通信封装。
    /// 所有操作均进行安全隔离：底层 SDK 调用异常不会向上传播，而是通过 <see cref="LastErrorMessage"/> 和 <see cref="ErrorOccurred"/> 事件通知上层。
    /// </remarks>
    public sealed class LeadshineLtdmcBusAdapter : IBusAdapter {
        private readonly ushort _cardNo;
        private readonly ushort _portNo;
        private readonly string? _controllerIp;
        private volatile string? _lastErrorMessage;
        private readonly object _errorLock = new object();

        /// <summary>
        /// 获取最后一次操作的错误信息（线程安全）。如果最近一次操作成功，此值为 null。
        /// </summary>
        public string? LastErrorMessage {
            get {
                lock (_errorLock) return _lastErrorMessage;
            }
            private set {
                lock (_errorLock) _lastErrorMessage = value;
            }
        }

        /// <summary>
        /// 当发生错误时触发的事件（线程安全）。
        /// 可用于上层监控总线状态。
        /// </summary>
        public event Action<IBusAdapter, string>? ErrorOccurred;

        /// <summary>
        /// 初始化一个新的雷赛 LTD-MC 总线适配器实例。
        /// </summary>
        /// <param name="cardNo">控制器卡号（通常为 0）。</param>
        /// <param name="portNo">端口号（CAN/EtherCAT 端口编号）。</param>
        /// <param name="controllerIp">控制器 IP 地址（仅以太网模式需要）；若为空或 null，则使用本地 PCI 模式。</param>
        public LeadshineLtdmcBusAdapter(ushort cardNo, ushort portNo, string? controllerIp) {
            _cardNo = cardNo;
            _portNo = portNo;
            _controllerIp = string.IsNullOrWhiteSpace(controllerIp) ? null : controllerIp;
        }

        public bool IsInitialized { get; private set; }

        public async Task<KeyValuePair<bool, string>> InitializeAsync(CancellationToken ct = default) {
            return await Safe<KeyValuePair<bool, string>>(async () => {
                if (IsInitialized) return new(true, "Already initialized."); // 幂等

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
                        onRetryAsync: (outcome, delay, retryAttempt, _) => {
                            var reason = outcome.Exception?.Message ?? outcome.Result.Value;
                            SetError($"Initialize retry #{retryAttempt} after {delay.TotalMilliseconds}ms, reason={reason}");
                            return Task.CompletedTask;
                        });

                return await policy.ExecuteAsync(async () => {
                    attempt++;

                    // === 0) 总线健康检查（唯一判据：errcode） ===
                    try {
                        ushort errcode = 0;
                        LTDMC.nmc_get_errcode(_cardNo, _portNo, ref errcode);
                        if (errcode != 0) {
                            // 本次尝试仅做一次复位：偶数尝试→软复位；奇数尝试→冷复位
                            var useSoftReset = (attempt % 2 == 0);

                            try {
                                if (useSoftReset) {
                                    var rc = await Task.Run(() => LTDMC.dmc_soft_reset(_cardNo), ct);
                                    if (rc != 0) {
                                        var msg = $"软复位失败: rc={rc}";
                                        SetError(msg);
                                        return new(false, msg);
                                    }
                                    await Task.Delay(500, ct); // 恢复时间
                                }
                                else {
                                    await ResetAsync(ct);      // 冷复位（无返回值）
                                    await Task.Delay(200, ct); // 恢复时间
                                }
                            }
                            catch (OperationCanceledException) {
                                return new(false, "Canceled");
                            }
                            catch (Exception ex) {
                                var msg = $"复位过程异常: {ex.Message}";
                                SetError(msg);
                                return new(false, msg);
                            }

                            // 复位后再读一次 errcode；若仍异常则失败，交给 Polly
                            LTDMC.nmc_get_errcode(_cardNo, _portNo, ref errcode);
                            if (errcode != 0) {
                                var msg = $"总线异常未恢复: err={errcode}";
                                SetError(msg);
                                return new(false, msg);
                            }
                        }
                    }
                    catch (OperationCanceledException) {
                        return new(false, "Canceled");
                    }
                    catch (Exception ex) {
                        var msg = $"总线检查异常: {ex.Message}";
                        SetError(msg);
                        return new(false, msg);
                    }

                    // === 1) 可选 Ping 预检（仅当配置了 IP） ===
                    if (_controllerIp != null) {
                        try {
                            using var ping = new Ping();
                            var reply = await ping.SendPingAsync(_controllerIp, TimeSpan.FromMilliseconds(1000), cancellationToken: ct)
                                                  .ConfigureAwait(false);
                            if (reply.Status != IPStatus.Success) {
                                const string msg = "Ping controller failed.";
                                SetError(msg);
                                return new(false, msg);
                            }
                        }
                        catch (OperationCanceledException) {
                            return new(false, "Canceled");
                        }
                        catch {
                            const string msg = "Ping 异常";
                            SetError(msg);
                            return new(false, msg);
                        }
                    }

                    // === 2) LTDMC 初始化（WhenAny 严格超时 + 返回码判定） ===
                    try {
                        var initTask = Task.Run(() => {
                            return _controllerIp is null
                                ? LTDMC.dmc_board_init()
                                : LTDMC.dmc_board_init_eth(_cardNo, _controllerIp);
                        });

                        var timeoutTask = Task.Delay(5000, ct);
                        var winner = await Task.WhenAny(initTask, timeoutTask).ConfigureAwait(false);
                        if (winner != initTask) {
                            const string msg = "LTDMC init 超时";
                            SetError(msg);
                            return new(false, msg);
                        }

                        var ret = await initTask.ConfigureAwait(false);
                        if (ret != 0) {
                            var msg = $"LTDMC init 返回: {ret}";
                            SetError(msg);
                            return new(false, msg);
                        }
                    }
                    catch (OperationCanceledException) {
                        const string msg = "LTDMC init 取消";
                        SetError(msg);
                        return new(false, msg);
                    }
                    catch (Exception ex) {
                        var msg = $"LTDMC init 异常: {ex.Message}";
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
        public Task CloseAsync(CancellationToken ct = default) {
            return Safe(() => {
                if (!IsInitialized) return Task.CompletedTask;

                LTDMC.dmc_board_close();
                IsInitialized = false;
                return Task.CompletedTask;
            }, "CloseAsync");
        }

        /// <summary>
        /// 获取总线发现到的轴数量（1-based 索引习惯由上层决定）。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>轴数量；若通信失败，返回 0。</returns>
        public Task<int> GetAxisCountAsync(CancellationToken ct = default) {
            return Safe(async () => {
                ushort total = 0;
                var ret = LTDMC.nmc_get_total_slaves(_cardNo, _portNo, ref total);
                if (ret != 0) {
                    throw new InvalidOperationException($"nmc_get_total_slaves failed, ret={ret}");
                }

                return (int)total;
            }, "GetAxisCountAsync", defaultValue: 0);
        }

        /// <summary>
        /// 读取当前错误码；0 表示正常。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>错误码；若通信失败，返回 -999。</returns>
        public Task<int> GetErrorCodeAsync(CancellationToken ct = default) {
            return Safe(async () => {
                ushort err = 0;
                var ret = LTDMC.nmc_get_errcode(_cardNo, _portNo, ref err);
                if (ret != 0) {
                    throw new InvalidOperationException($"nmc_get_errcode failed, ret={ret}");
                }

                return (int)err;
            }, "GetErrorCodeAsync", defaultValue: -999);
        }

        /// <summary>
        /// 执行控制器冷复位（如需）；通常用于错误码非 0 的场景。
        /// 冷复位会断电重启控制器，耗时约 15 秒。
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>任务。</returns>
        public async Task ResetAsync(CancellationToken ct = default) {
            ct.ThrowIfCancellationRequested();

            var success = await Safe(async () => {
                LTDMC.dmc_cool_reset(_cardNo);
                await CloseAsync(ct).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                await InitializeAsync(ct).ConfigureAwait(false);
            }, "ResetAsync");

            if (!success) {
                SetError("ResetAsync: 冷复位流程执行失败。");
            }
        }

        /// <summary>
        /// 执行控制器热复位（软复位）。
        /// <para>
        /// 热复位通常只会重置通信/状态机，不掉电，耗时短（1~2 秒）。
        /// 若未初始化，将尝试直接初始化。
        /// </para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>任务。</returns>
        public async Task WarmResetAsync(CancellationToken ct = default) {
            ct.ThrowIfCancellationRequested();

            var success = await Safe(async () => {
                // 若未初始化，直接初始化即可
                if (!IsInitialized) {
                    var (key, value) = await InitializeAsync(ct).ConfigureAwait(false);
                    IsInitialized = key;
                    return;
                }

                // 1) 软复位控制器
                var retSoft = LTDMC.dmc_soft_reset(_cardNo);
                if (retSoft != 0) {
                    throw new InvalidOperationException($"dmc_soft_reset failed, ret={retSoft}");
                }

                // 2) 关闭当前连接
                LTDMC.dmc_board_close();
                IsInitialized = false;

                // 3) 等待控制器复位（官方建议 300~1500ms）
                await Task.Delay(TimeSpan.FromMilliseconds(800), ct).ConfigureAwait(false);

                // 4) 重新初始化（仅支持以太网）
                if (string.IsNullOrWhiteSpace(_controllerIp)) {
                    throw new InvalidOperationException("WarmReset requires controller IP for dmc_board_init_eth.");
                }

                var retInit = LTDMC.dmc_board_init_eth(_cardNo, _controllerIp);
                if (retInit != 0) {
                    throw new InvalidOperationException($"dmc_board_init_eth failed, ret={retInit}");
                }

                IsInitialized = true;
            }, "WarmResetAsync");

            if (!success) {
                SetError("WarmResetAsync: 热复位流程执行失败。");
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
        public ushort TranslateNodeId(ushort logicalNodeId) {
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
        public bool ShouldReverse(ushort logicalNodeId) {
            // 奇数 NodeId 反转
            return logicalNodeId % 2 == 1;
        }

        #region 安全执行工具方法

        /// <summary>
        /// 安全执行某个异步动作：吞掉异常并返回 false；具体异常信息由驱动层事件和 <see cref="LastErrorMessage"/> 负责记录。
        /// </summary>
        /// <param name="act">要执行的异步操作。</param>
        /// <param name="operationName">操作名称，用于错误日志。</param>
        /// <returns>任务，成功返回 true，失败返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<bool> Safe(Func<Task> act, string operationName) {
            try {
                await act();
                ClearError();
                return true;
            }
            catch (Exception ex) {
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
        private async Task<T> Safe<T>(Func<Task<T>> func, string operationName, T defaultValue = default!) {
            try {
                var result = await func();
                ClearError();
                return result;
            }
            catch (Exception ex) {
                SetError($"{operationName}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 设置错误信息并触发事件。
        /// </summary>
        /// <param name="message">错误消息。</param>
        private void SetError(string message) {
            LastErrorMessage = message;
            ErrorOccurred?.Invoke(this, message);
        }

        /// <summary>
        /// 清除错误信息。
        /// </summary>
        private void ClearError() {
            LastErrorMessage = null;
        }

        #endregion 安全执行工具方法
    }
}