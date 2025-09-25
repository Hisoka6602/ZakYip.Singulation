using System;
using csLTDMC;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Health;
using ZakYip.Singulation.Drivers.Resilience;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Leadshine {

    /// <summary>
    /// 雷赛 LTDMC 实机轴驱动（EtherCAT）。
    /// - 通过 nmc_write_rxpdo / nmc_read_txpdo 直写/直读 PDO。
    /// - 通过 nmc_set_axis_contrlword 走 402 状态机使能。
    /// - 目标速度写 0x60FF（INT32），状态字读 0x6041。
    /// - 端口固定 2；从站 NodeID = 1000 + nodeIndex。:contentReference[oaicite:4]{index=4} :contentReference[oaicite:5]{index=5}
    /// </summary>
    public sealed class LeadshineLtdmcAxisDrive : IAxisDrive {
        private readonly DriverOptions _opts;
        private volatile DriverStatus _status = DriverStatus.Disconnected;

        // —— 全局 PPR（只在 Enable 时读取一次）—— ★ 新增
        private static volatile int _sPpr;        // 0 表示未知

        private static volatile bool _sPprReady;  // 读取成功的一次性门闩

        // —— 速度反馈节流控制 ——
        // 最小反馈间隔：避免淹没上层（可做成 DriverOptions 配置）
        private readonly TimeSpan _feedbackMinInterval = TimeSpan.FromMilliseconds(20);

        private const decimal FEEDBACK_DELTA_MMPS = 1.0m;
        private long _lastFbStamp;
        private decimal _lastFbMmps;
        private long _lastStamp;

        // —— 原生库可用性（仅探测一次）——
        private static readonly Lazy<(bool ok, string reason)> SNative = new(() => {
            try {
                if (!OperatingSystem.IsWindows())
                    return (false, "非 Windows 平台：LTDMC.dll 仅支持 Windows");

                // 尝试按默认搜索路径加载（工作目录 / PATH）
                if (NativeLibrary.TryLoad("LTDMC.dll", out var handle)) {
                    // 可选：不释放，直到进程结束；也可记录 handle 自行管理
                    return (true, "");
                }
                return (false, "找不到 LTDMC.dll，或位宽不匹配（x64/x86）/缺少依赖");
            }
            catch (Exception ex) {
                return (false, $"加载 LTDMC.dll 失败：{ex.Message}");
            }
        }, isThreadSafe: true);

        // —— 每个实例仅通知一次 ——
        private bool _driverNotifiedOnce;

        private readonly ConsecutiveFailCounter _fails = new(3); // 会用 _opts.ConsecutiveFailThreshold 替换
        private readonly AxisHealthMonitor _health;
        private readonly Polly.ResiliencePipeline<short> _pdoPipe;

        [ThreadStatic] private static byte[]? _tlsTxBuf8;

        public LeadshineLtdmcAxisDrive(DriverOptions opts) {
            _opts = opts;
            Axis = new AxisId(_opts.NodeId);
            // 健康监测：降级后轮询 PingAsync
            _health = new AxisHealthMonitor(PingAsync, _opts.HealthPingInterval);
            _health.Recovered += () => {
                _fails.Reset();
                _status = DriverStatus.Connected;
            };

            // 断路器：降级与恢复回调
            _pdoPipe = AxisDegradePolicy.BuildPdoPipeline(
                _opts.ConsecutiveFailThreshold,
                onDegraded: () => {
                    _status = DriverStatus.Degraded;
                    OnAxisDisconnected("Degraded by circuit-breaker (consecutive failures).");
                    if (_opts.EnableHealthMonitor) _health.Start();
                },
                onRecovered: () => {
                    _fails.Reset();
                    _status = DriverStatus.Connected;
                    _health.Stop();
                }
            );
        }

        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        public AxisId Axis { get; }
        public DriverStatus Status => _status;
        public decimal? LastTargetMmps { get; private set; }
        public decimal? LastFeedbackMmps { get; private set; }
        public bool IsEnabled { get; private set; }
        public int LastErrorCode { get; private set; }
        public string? LastErrorMessage { get; private set; }

        [System.Obsolete("默认单位为 mm/s；建议改用 WriteSpeedAsync(mmPerSec)。此方法仅为兼容入口。")]
        public async ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default) {
            await ThrottleAsync(ct);
            var rpmAsync = await WriteTargetVelocityFromRpmAsync(rpm.Value, ct);
            if (rpmAsync) {
                LastTargetMmps = rpm.ToMmPerSec(_opts.PulleyPitchDiameterMm, _opts.GearRatio);
            }
        }

        public async Task WriteSpeedAsync(decimal mmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 取 PPR（带缓存）
            var ppr = await GetPprCachedAsync(ct);
            int deviceVal;
            if (ppr > 0) {
                var pps = AxisRpm.MmPerSecToPps(mmPerSec, _opts.PulleyPitchDiameterMm, ppr, _opts.GearRatio);
                Debug.WriteLine($"pps:{pps}");
                deviceVal = (int)Math.Round(pps);
            }
            else {
                // 退化（保持旧比例）
                var rpmVo = AxisRpm.FromMmPerSec(mmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio);
                deviceVal = (int)Math.Round(rpmVo.Value);
            }

            if (_opts.IsReverse) deviceVal = -deviceVal;
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, deviceVal);
            if (ret != 0) { SetErrorFromRet("write 0x60FF (TargetVelocity)", ret); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return; }

            _status = DriverStatus.Connected;
            LastTargetMmps = mmPerSec;
        }

        /// <summary>
        /// 设置速度模式下的加速度/减速度（单位：RPM/s）。
        /// <para>
        /// 典型实现：将 <c>0x6083</c>（Profile Acceleration）与 <c>0x6084</c>（Profile Deceleration）
        /// 写为设备单位（优先 <b>counts/s²</b>，即 pps²），失败时退回现场比例系数。
        /// </para>
        /// <remarks>
        /// 数学：pps² = ( rpm/s ÷ 60 ) × PPR；若 PPR 未知，则使用 _accelTo6083 比例（兼容旧逻辑）。<br/>
        /// 写入类型通常为 U32（4字节，非负）；这里对负值一律量化为 0。
        /// </remarks>
        /// </summary>
        public async Task SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec,
            CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 记录原始请求值（用于事件说明）
            var reqA = accelRpmPerSec;
            var reqD = decelRpmPerSec;

            // 1) 入口清理：负值截断（一次性上报）
            var intercepted = false;
            if (accelRpmPerSec < 0) { accelRpmPerSec = 0; intercepted = true; }
            if (decelRpmPerSec < 0) { decelRpmPerSec = 0; intercepted = true; }
            if (intercepted) {
                OnAxisFaulted(new InvalidOperationException(
                    $"[AccelDecel] negative clamped: reqA={reqA} rpm/s, reqD={reqD} rpm/s → effA={accelRpmPerSec} rpm/s, effD={decelRpmPerSec} rpm/s"));
            }

            // 2) 限幅（以 rpm/s 为口径），超限也上报一次
            var effA = Math.Min(_opts.MaxAccelRpmPerSec, accelRpmPerSec);
            var effD = Math.Min(_opts.MaxDecelRpmPerSec, decelRpmPerSec);
            if (effA != accelRpmPerSec || effD != decelRpmPerSec) {
                OnAxisFaulted(new InvalidOperationException(
                    $"[AccelDecel] over-limit clamped: reqA={accelRpmPerSec} rpm/s, reqD={decelRpmPerSec} rpm/s → effA={effA} rpm/s, effD={effD} rpm/s (limits: A≤{_opts.MaxAccelRpmPerSec}, D≤{_opts.MaxDecelRpmPerSec})"));
            }

            // 3) 量化到设备单位（优先 PPR → counts/s²；否则退化到 rpm/s 原样）
            static uint ClampU32(decimal v) => v <= 0 ? 0u : (uint)Math.Min(uint.MaxValue, Math.Round(v));

            var ppr = 0;
            try { ppr = await GetPprCachedAsync(ct).ConfigureAwait(false); } catch { /* 忽略，退化路径 */ }

            uint accDev, decDev;
            if (ppr > 0) {
                // pps² = ( rpm/s ÷ 60 ) × PPR
                var accPps2 = (effA / 60.0m) * ppr;
                var decPps2 = (effD / 60.0m) * ppr;
                accDev = ClampU32(accPps2);
                decDev = ClampU32(decPps2);
            }
            else {
                // 退化：保持现场比例（rpm/s → 设备单位）
                accDev = ClampU32(effA);
                decDev = ClampU32(effD);
                OnAxisFaulted(new InvalidOperationException(
                    "[AccelDecel] PPR unavailable, degrading to raw rpm/s quantization."));
            }

            // 4) 写寄存器（0x6083 / 0x6084），失败仅事件，不抛异常
            var r1 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileAcceleration, accDev);
            if (r1 != 0) {
                SetErrorFromRet("write 0x6083 (ProfileAcceleration)", r1);
                OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return;
            }

            var r2 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileDeceleration, decDev);
            if (r2 != 0) {
                SetErrorFromRet("write 0x6084 (ProfileDeceleration)", r2);
                OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return;
            }
        }

        /// <summary>
        /// 设置速度模式下的加速度/减速度（单位：mm/s²）。
        /// 内部换算为 RPM/s，再调用主实现写入 0x6083/0x6084。
        /// 数学公式：
        ///   rpm/s = ( a(m/s²) / (π·D(m)) ) × 60
        /// 其中 a(m/s²) = a(mm/s²) / 1000。
        /// </summary>
        public async ValueTask SetAccelDecelByLinearAsync(decimal accelMmPerSec, decimal decelMmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 滚筒直径（米）
            var dm = _opts.PulleyPitchDiameterMm / 1000.0m;
            if (dm <= 0) {
                OnAxisFaulted(new InvalidOperationException("drum diameter must be > 0"));
                return;
            }

            // mm/s² → m/s²
            var accMps2 = accelMmPerSec / (decimal)1000.0;
            var decMps2 = decelMmPerSec / (decimal)1000.0;

            // m/s² → rpm/s ： rpm/s = (a / (π·D)) × 60
            var accRpmPerSec = accMps2 / ((decimal)Math.PI * dm) * (decimal)60.0;
            var decRpmPerSec = decMps2 / ((decimal)Math.PI * dm) * (decimal)60.0;

            // 复用 RPM/s 版本
            await SetAccelDecelAsync(accRpmPerSec, decRpmPerSec, ct);
        }

        public async ValueTask StopAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 策略A: 目标速度置零
            _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0);

            // 策略B: QuickStop (ControlWord bit2=1)
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, (ushort)0x0002);
            if (ret != 0) { OnAxisFaulted(new InvalidOperationException($"QuickStop failed, ret={ret}")); return; }

            _status = DriverStatus.Connected;
        }

        public async Task<bool> PingAsync(CancellationToken ct = default) {
            if (!EnsureNativeLibLoaded()) { _status = DriverStatus.Degraded; return false; }
            var ret = ReadTxPdo(LeadshineProtocolMap.Index.StatusWord, out ushort _);
            if (ret != 0) {
                SetErrorFromRet("read 0x6041 (StatusWord)", ret);
                _status = DriverStatus.Degraded;
                OnAxisDisconnected(LastErrorMessage!);
                return false;
            }

            _status = DriverStatus.Connected;

            ret = ReadTxPdo(LeadshineProtocolMap.Index.ActualVelocity, out int actualRpm);
            if (ret != 0) {
                SetErrorFromRet("read 0x606C (ActualVelocity)", ret);
                return false;
            }

            // 读实际速度（RPM）并发布反馈
            var stamp = Stopwatch.GetTimestamp();

            // 方向在阈值判断中只应用一次
            var rpmVal = _opts.IsReverse ? -actualRpm : actualRpm;

            // 用 mm/s 做一等公民的节流/阈值判断
            var mmps = new AxisRpm(rpmVal).ToMmPerSec(_opts.PulleyPitchDiameterMm, _opts.GearRatio);

            if (ShouldPublishFeedbackMmps(mmps, stamp)) {
                var ppr = 0;
                try { ppr = await GetPprCachedAsync(ct).ConfigureAwait(false); } catch { /* 忽略：无 PPR 仍可发布反馈 */ }

                // 发布时交给 PublishSpeedFeedbackFromActualRpm 统一做方向/单位换算
                PublishSpeedFeedbackFromActualRpm(actualRpm, ppr);

                CommitFeedbackMmps(mmps, stamp);
            }
            return true;
        }

        /// <summary>上电/使能（可选：调用后再写速度）</summary>
        public async Task EnableAsync(CancellationToken ct = default) {
            // 节流，避免过快写总线
            await ThrottleAsync(ct);

            // 简化封装：写 ControlWord 并可选延时
            async Task<bool> WriteCtrlAsync(ushort value, int delayMs) {
                var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, value);
                if (ret != 0) {
                    SetErrorFromRet("write 0x6040 (ControlWord:<step>)", ret);
                    OnAxisFaulted(new InvalidOperationException(LastErrorMessage!));
                    return false;
                }
                if (delayMs > 0) await Task.Delay(delayMs, ct);
                return true;
            }

            // 0) 清除报警 → 拉回 0
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.FaultReset, LeadshineProtocolMap.DelayMs.AfterFaultReset)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.Clear, LeadshineProtocolMap.DelayMs.AfterClear)) return;

            // 1) 设置模式：速度模式 (PV=3)
            var m = WriteRxPdo(
                LeadshineProtocolMap.Index.ModeOfOperation,
                LeadshineProtocolMap.Mode.ProfileVelocity  // 单个 byte
            );
            if (m != 0) { OnAxisFaulted(new InvalidOperationException("Set Mode=PV failed")); return; }
            await Task.Delay(LeadshineProtocolMap.DelayMs.AfterSetMode, ct);

            // 2) 402 状态机三步
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.Shutdown, LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.SwitchOn, LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.EnableOperation, 0)) return;
            if (!_sPprReady) {
                try {
                    var ppr = await ReadAxisPulsesPerRevAsync(ct);
                    if (ppr > 0) {
                        Volatile.Write(ref _sPpr, ppr);
                        Volatile.Write(ref _sPprReady, true);
                        Debug.WriteLine($"[PPR] initialized once at enable: {ppr}");
                    }
                    else {
                        OnAxisFaulted(new InvalidOperationException("Read PPR at enable returned 0."));
                    }
                }
                catch (Exception ex) {
                    OnAxisFaulted(new InvalidOperationException($"Read PPR at enable failed: {ex.Message}", ex));
                }
            }
            _status = DriverStatus.Connected;
            IsEnabled = true;
        }

        public async ValueTask DisableAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 1) 先做一次安全停机：目标速度=0 + QuickStop（bit2）
            _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0);
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, (ushort)0x0002);
            if (ret != 0) {
                SetErrorFromRet("write 0x6040 (ControlWord:<step>)", ret);
                OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return;
            }
            await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, ct);

            // 2) 退回到“Switch On Disabled”：Shutdown（0x0006）
            var cw = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, LeadshineProtocolMap.ControlWord.Shutdown);
            if (cw != 0) {
                SetErrorFromRet("write 0x6040 (ControlWord:<step>)", cw);
                OnAxisFaulted(new InvalidOperationException($"Disable: write ControlWord=Shutdown(0x0006) failed, ret={cw}"));
            }
            await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, ct);

            // 3) 健康监测停止 + 断路器计数复位
            _health.Stop();
            _fails.Reset();                      // 避免下次启用继承旧的失败窗口

            // 4) 本地状态与反馈记账清理
            _status = DriverStatus.Disconnected;
            Volatile.Write(ref _lastFbStamp, 0);
            _lastFbMmps = 0;
            IsEnabled = false;
        }

        /// <summary>
        /// 读取“轴的一圈脉冲量(PPR)”。
        /// 使用 OD 对象 0x6092（Feed Constant）：
        ///   :01 Numerator = 电气轴每转所需指令脉冲数
        ///   :02 Denominator = 物理轴相数
        /// 数学：PPR = Numerator / Denominator
        /// 成功返回 PPR；失败返回 0（不抛异常，事件通知）。
        /// </summary>
        public async ValueTask<int> ReadAxisPulsesPerRevAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            if (!EnsureNativeLibLoaded())
                return 0;

            int numerator = 0, denominator = 0;

            try {
                const ushort idx = LeadshineProtocolMap.Index.FeedConstant;

                // 读取 Numerator (0x6092:01)
                var retNum = LTDMC.nmc_get_node_od(
                    (ushort)_opts.Card, _opts.Port, _opts.NodeId,
                    idx,
                    LeadshineProtocolMap.SubIndex.Numerator,
                    LeadshineProtocolMap.BitLen.FeedConstant,
                    ref numerator
                );
                OnCommandIssued("nmc_get_node_od", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {idx} , {LeadshineProtocolMap.SubIndex.Numerator} , {LeadshineProtocolMap.BitLen.FeedConstant} , {numerator}", retNum);
                if (retNum != 0) {
                    OnAxisFaulted(new InvalidOperationException($"read 0x{idx:X4}:01 (Numerator) failed, ret={retNum}"));
                    return 0;
                }

                // 读取 Denominator (0x6092:02)
                var retDen = LTDMC.nmc_get_node_od(
                    (ushort)_opts.Card, _opts.Port, _opts.NodeId,
                    idx,
                    LeadshineProtocolMap.SubIndex.Denominator,
                    LeadshineProtocolMap.BitLen.FeedConstant,
                    ref denominator
                );
                OnCommandIssued("nmc_get_node_od", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {idx} , {LeadshineProtocolMap.SubIndex.Denominator} , {LeadshineProtocolMap.BitLen.FeedConstant} , {denominator}", retDen);
                if (retDen != 0) {
                    OnAxisFaulted(new InvalidOperationException($"read 0x{idx:X4}:02 (Denominator) failed, ret={retDen}"));
                    return 0;
                }

                if (denominator <= 0) {
                    OnAxisFaulted(new InvalidOperationException(
                        $"read 0x{idx:X4}:02 returned invalid denominator={denominator}"));
                    return 0;
                }

                return numerator / denominator;
            }
            catch (DllNotFoundException ex) {
                if (!_driverNotifiedOnce) {
                    _driverNotifiedOnce = true;
                    _status = DriverStatus.Degraded;
                    OnDriverNotLoaded("LTDMC.dll", ex.Message);
                }
                return 0;
            }
            catch (EntryPointNotFoundException ex) {
                if (!_driverNotifiedOnce) {
                    _driverNotifiedOnce = true;
                    _status = DriverStatus.Degraded;
                    OnDriverNotLoaded("LTDMC.dll", $"入口缺失：{ex.Message}");
                }
                return 0;
            }
        }

        public async ValueTask DisposeAsync() {
            try {
                await DisableAsync(CancellationToken.None);
                SetError(0, null);
            }
            catch { /* 忽略收尾失败 */ }
        }

        /// <summary>
        /// 更新线速度/加减速度限幅（mm/s, mm/s²）。
        /// 仅更新 DriverOptions 中的对应字段，并立即对“后续命令”的限幅生效。
        /// 如需立即收敛当前目标，可在成功后对当前目标做一次钳制重写。
        /// </summary>
        public Task UpdateLinearLimitsAsync(decimal maxLinearMmps, decimal maxAccelMmps2, decimal maxDecelMmps2,
            CancellationToken ct = default) {
            if (maxLinearMmps <= 0 || maxAccelMmps2 <= 0 || maxDecelMmps2 <= 0) return Task.FromResult(false);

            try {
                _opts.MaxRpm = maxLinearMmps;
                _opts.MaxAccelRpmPerSec = maxAccelMmps2;
                _opts.MaxDecelRpmPerSec = maxDecelMmps2;

                return Task.FromResult(true);
            }
            catch {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 更新机械参数（滚筒直径/齿轮比/PPR），并重算“量化系数”（如 _rpmTo60FF、_accelTo6083）。
        /// 不外抛异常；失败返回 false。单位：mm, (motor:roller), PPR。
        /// </summary>
        public Task UpdateMechanicsAsync(decimal rollerDiameterMm, decimal gearRatio, int ppr,
            CancellationToken ct = default) {
            if (rollerDiameterMm <= 0 || gearRatio <= 0 || ppr <= 0) return Task.FromResult(false);

            try {
                // 1) 更新 Mechanics——这应是驱动内的“唯一真源”
                _opts.PulleyPitchDiameterMm = rollerDiameterMm;
                _opts.GearRatio = gearRatio;

                return Task.FromResult(true);
            }
            catch {
                return Task.FromResult(false);
            }
        }

        private async ValueTask ThrottleAsync(CancellationToken ct) {
            var now = Stopwatch.GetTimestamp();
            var min = (long)Math.Round(_opts.MinWriteInterval.TotalSeconds * Stopwatch.Frequency);
            var last = Interlocked.Read(ref _lastStamp);
            var delta = now - last;

            if (delta < min) {
                var waitTicks = min - delta;
                var waitMs = (int)Math.Ceiling(waitTicks * 1000.0 / Stopwatch.Frequency);
                if (waitMs > 0) await Task.Delay(waitMs, ct);
                now = Stopwatch.GetTimestamp();
            }

            Interlocked.Exchange(ref _lastStamp, now);
        }

        /// <summary>
        /// 按 Index 自动选择 BitLen，统一写 RxPDO。
        /// 这样上层调用只需要传 Index+值，避免重复定义多个 WriteXxx。
        /// </summary>
        private short WriteRxPdo(ushort index, object value, byte subIndex = LeadshineProtocolMap.SubIndex.Root) {
            var ret = _pdoPipe.Execute(
                (CancellationToken _) => WriteRxPdoCore(index, value, subIndex),
                cancellationToken: CancellationToken.None);

            if (ret != 0) {
                // 连续失败计数：达到阈值后 AxisDegradePolicy 会打开断路器（onDegraded 已订阅）
                _fails.Increment();
                // 降级状态由断路器回调统一设置；这里不重复设置
            }
            else {
                // 成功立刻复位；如果健康监测在跑，尝试停止
                _fails.Reset();
                if (_status == DriverStatus.Degraded) {
                    // 让断路器在下一次成功/窗口统计后关闭；我们也主动停一下监测
                    _health.Stop();
                    _status = DriverStatus.Connected;
                }
            }

            return ret;
        }

        private short WriteRxPdoCore(ushort index, object value, byte subIndex = LeadshineProtocolMap.SubIndex.Root) {
            if (value is int i32) {
                var buf = GetTxBuffer(4);
                BinaryPrimitives.WriteInt32LittleEndian(buf, i32);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 32, buf);
                OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 32 , {i32}", ret);
                return ret;
            }
            if (value is uint u32) {
                var buf = GetTxBuffer(4);
                BinaryPrimitives.WriteUInt32LittleEndian(buf, u32);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 32, buf);
                OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 32 , {u32}", ret);
                return ret;
            }
            if (value is short i16) {
                var buf = GetTxBuffer(2);
                BinaryPrimitives.WriteInt16LittleEndian(buf, i16);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 16, buf);
                OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 16 , {i16}", ret);
                return ret;
            }
            if (value is ushort u16) {
                var buf = GetTxBuffer(2);
                BinaryPrimitives.WriteUInt16LittleEndian(buf, u16);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 16, buf);
                OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 16 , {u16}", ret);
                return ret;
            }
            if (value is byte b8) {
                var buf = GetTxBuffer(1);
                buf[0] = b8;
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 8, buf);
                OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 8 , {b8}", ret);
                return ret;
            }

            var bytes = value switch {
                sbyte s8 => new[] { unchecked((byte)s8) },
                _ => throw new NotSupportedException($"不支持的写入类型：{value.GetType().Name}")
            };
            var bitLen = (ushort)(bytes.Length * 8);
            var retFallback = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, bitLen, bytes);
            OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {value}", retFallback);
            return retFallback;
        }

        /// <summary>
        /// 读 TxPDO 并解码到指定类型；成功返回 0，失败返回 ret（不抛异常）。
        /// 支持 byte/sbyte/ushort/short/uint/int/byte[]。
        /// </summary>
        private short ReadTxPdo<T>(ushort index, out T value, byte subIndex = LeadshineProtocolMap.SubIndex.Root) {
            value = default!;
            ushort bitLen;
            switch (index) {
                case LeadshineProtocolMap.Index.StatusWord: bitLen = (ushort)LeadshineProtocolMap.BitLen.StatusWord; break;
                case LeadshineProtocolMap.Index.ModeOfOperation: bitLen = (ushort)LeadshineProtocolMap.BitLen.ModeOfOperation; break;
                case LeadshineProtocolMap.Index.ActualVelocity: bitLen = (ushort)LeadshineProtocolMap.BitLen.ActualVelocity; break;
                case LeadshineProtocolMap.Index.FeedConstant: bitLen = 32; break;
                case LeadshineProtocolMap.Index.GearRatio: bitLen = 32; break;
                default:
                    OnAxisFaulted(new InvalidOperationException($"Index 0x{index:X4} not mapped to BitLen."));
                    return -1;
            }

            var byteLen = (bitLen + 7) / 8;
            var buf = new byte[byteLen];
            var ret = LTDMC.nmc_read_txpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, bitLen, buf);
            OnCommandIssued("nmc_read_txpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {byteLen}", ret);
            if (ret != 0) return ret;

            object? boxed =
                typeof(T) == typeof(byte) ? buf[0] :
                typeof(T) == typeof(sbyte) ? unchecked((sbyte)buf[0]) :
                typeof(T) == typeof(ushort) ? BitConverter.ToUInt16(buf, 0) :
                typeof(T) == typeof(short) ? BitConverter.ToInt16(buf, 0) :
                typeof(T) == typeof(uint) ? BitConverter.ToUInt32(buf, 0) :
                typeof(T) == typeof(int) ? BitConverter.ToInt32(buf, 0) :
                typeof(T) == typeof(byte[]) ? buf :
                null;

            if (boxed is null) {
                OnAxisFaulted(new InvalidOperationException($"Unsupported target type {typeof(T).Name} for read 0x{index:X4}."));
                return -2;
            }

            value = (T)boxed;

            // 可选：再发一条带解码值的备注（result=0）
            OnCommandIssued("nmc_read_txpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {byteLen}", 0,
                note: $"decoded={boxed}");

            return 0;
        }

        // 通用：逐订阅者非阻塞广播
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FireEachNonBlocking<T>(EventHandler<T>? multicast, object sender, T args) {
            if (multicast is null) return;

            // 逐个订阅者，隔离执行
            foreach (var @delegate in multicast.GetInvocationList()) {
                var h = (EventHandler<T>)@delegate;
                var state = new EvState<T>(sender, h, args);
                ThreadPool.UnsafeQueueUserWorkItem(static s => {
                    var st = (EvState<T>)s!;
                    try { st.Handler(st.Sender, st.Args); }
                    catch (Exception ex) {
                        // 这里没 logger，只能静默；外层可包 SafeLog
                    }
                }, state, preferLocal: true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<int> GetPprCachedAsync(CancellationToken ct) {
            // ★ 修改：仅返回静态值，不再主动发起读取
            var v = Volatile.Read(ref _sPprReady) ? Volatile.Read(ref _sPpr) : 0;
            return ValueTask.FromResult(v);
        }

        private void PublishSpeedFeedbackFromActualRpm(int actualRpm, int pulsesPerRev) {
            // 1) 方向一致性：仅在入口处做一次符号
            var rpmVal = _opts.IsReverse ? -actualRpm : actualRpm;

            // 2) 用 AxisRpm 统一做换算
            var rpm = new AxisRpm(rpmVal);
            var speedMmps = rpm.ToMmPerSec(_opts.PulleyPitchDiameterMm, _opts.GearRatio);
            LastFeedbackMmps = speedMmps;
            // pps：考虑齿比（电机轴:负载轴）
            decimal pps = 0;
            if (pulsesPerRev > 0) {
                // 如果你的 AxisRpm.ToPulsePerSec 有 gearRatio 参数，直接用它；
                // 否则按公式算：pps = (rpm/60) * PPR / gearRatio
                pps = (rpm.Value / 60m) * pulsesPerRev / _opts.GearRatio;
            }

            FireEachNonBlocking(SpeedFeedback, this,
                new AxisSpeedFeedbackEventArgs(Axis, rpmVal, speedMmps, pps, DateTime.Now));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureNativeLibLoaded() {
            var probed = SNative.Value;     // 只在第一次初始化时做昂贵工作
            if (probed.ok) return true;

            if (!_driverNotifiedOnce) {
                _driverNotifiedOnce = true;
                _status = DriverStatus.Disconnected;
                OnDriverNotLoaded("LTDMC.dll", probed.reason);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ToSwTicks(TimeSpan t) => (long)Math.Round(t.TotalSeconds * Stopwatch.Frequency);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldPublishFeedbackMmps(decimal mmps, long nowStamp) {
            var minGap = ToSwTicks(_feedbackMinInterval);
            var last = Volatile.Read(ref _lastFbStamp);
            if (nowStamp - last < minGap) return false;
            if (Math.Abs(mmps - _lastFbMmps) < FEEDBACK_DELTA_MMPS) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CommitFeedbackMmps(decimal mmps, long nowStamp) {
            _lastFbMmps = mmps;
            Volatile.Write(ref _lastFbStamp, nowStamp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetError(int code, string? message) { LastErrorCode = code; LastErrorMessage = message; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetErrorFromRet(string action, int ret) { LastErrorCode = ret; LastErrorMessage = $"{action} failed, ret={ret}"; }

        private async ValueTask<bool> WriteTargetVelocityFromRpmAsync(decimal rpm, CancellationToken ct) {
            var max = Math.Abs(_opts.MaxRpm);
            var targetRpm = Math.Max(-max, Math.Min(max, rpm));

            int deviceVal;
            int ppr = 0;
            try { ppr = await GetPprCachedAsync(ct); } catch { /* 忽略 */ }

            if (ppr > 0) {
                //  改用 AxisRpm 的方法
                var rpmVo = new AxisRpm(targetRpm);
                deviceVal = (int)Math.Round(rpmVo.ToPulsePerSec(ppr));   // pps = (rpm/60)*PPR
            }
            else {
                // 退化路径：保留旧比例
                deviceVal = (int)Math.Round(targetRpm);
            }

            if (_opts.IsReverse) deviceVal = -deviceVal;

            var ret = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, deviceVal);
            if (ret != 0) { SetErrorFromRet("write 0x60FF (TargetVelocity)", ret); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return false; }

            _status = DriverStatus.Connected;
            return true;
        }

        /// <summary>获取至少 len 字节的线程本地缓冲。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] GetTxBuffer(int len) {
            var buf = _tlsTxBuf8;
            if (buf == null || buf.Length < len) {
                buf = new byte[Math.Max(8, len)];
                _tlsTxBuf8 = buf;
            }
            return buf;
        }

        //用便捷封装
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnAxisFaulted(Exception ex) =>
            FireEachNonBlocking(AxisFaulted, this, new AxisErrorEventArgs(Axis, ex));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDriverNotLoaded(string lib, string msg) =>
            FireEachNonBlocking(DriverNotLoaded, this, new DriverNotLoadedEventArgs(lib, msg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnAxisDisconnected(string reason) =>
            FireEachNonBlocking(AxisDisconnected, this, new AxisDisconnectedEventArgs(Axis, reason));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnCommandIssued(AxisCommandIssuedEventArgs e) =>
            FireEachNonBlocking(CommandIssued, this, e);

        // 便捷重载：函数名 + 参数串 + 结果码 → 直接构造 Invocation 并广播
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnCommandIssued(string function, string argListWithSpaces, int result, string? note = null) =>
            FireEachNonBlocking(CommandIssued, this, new AxisCommandIssuedEventArgs {
                Axis = Axis,
                Invocation = $"{function}( {argListWithSpaces} )",
                Result = result,
                Timestamp = DateTimeOffset.Now,
                Note = note
            });
    }
}