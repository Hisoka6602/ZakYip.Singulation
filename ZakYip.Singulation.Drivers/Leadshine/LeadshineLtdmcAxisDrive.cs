using System;
using csLTDMC;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Utils;
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
    /// - 通过 402 状态机使能。
    /// - 目标速度写 0x60FF（负载侧 pps，INT32），实际速度读 0x606C（负载侧 pps）。
    /// - 加减速度写 0x6083/0x6084（负载侧 pps²，UNSIGNED32）。
    /// - PPR（脉冲/转）来自 0x6092:01/02（Feed Constant），Enable 后必须就绪。
    /// </summary>
    public sealed class LeadshineLtdmcAxisDrive : IAxisDrive {
        private readonly DriverOptions _opts;
        private volatile DriverStatus _status = DriverStatus.Disconnected;

        // —— 全局 PPR（只在 Enable 或 UpdateMechanics 时设置）——
        // 设备单位换算的唯一真源；未就绪禁止写速度/加减速度。
        private static int _sPpr;              // 0 表示未知

        private static bool _sPprReady;        // 一次性门闩

        // —— 速度反馈节流控制 ——（避免淹没上层）
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
                if (NativeLibrary.TryLoad("LTDMC.dll", out var _)) return (true, "");
                return (false, "找不到 LTDMC.dll，或位宽不匹配（x64/x86）/缺少依赖");
            }
            catch (Exception ex) { return (false, $"加载 LTDMC.dll 失败：{ex.Message}"); }
        }, isThreadSafe: true);

        // —— 每个实例仅通知一次 ——
        private bool _driverNotifiedOnce;

        private readonly ConsecutiveFailCounter _fails = new(3); // 实际应由 _opts.ConsecutiveFailThreshold 覆盖
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

        // ---------------- 事件 ----------------
        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        // ---------------- 只读属性 ----------------
        public AxisId Axis { get; }

        public DriverStatus Status => _status;
        public decimal? LastTargetMmps { get; private set; }
        public decimal? LastFeedbackMmps { get; private set; }
        public bool IsEnabled { get; private set; }
        public int LastErrorCode { get; private set; }
        public string? LastErrorMessage { get; private set; }

        public decimal? MaxLinearMmps {
            get {
                if (!HasValidMechanicsConfig()) return null;
                return new AxisRpm(_opts.MaxRpm).ToMmPerSec(_opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            }
        }

        public decimal? MaxAccelMmps2 {
            get {
                if (!HasValidMechanicsConfig()) return null;
                return AxisRpm.RpmPerSecToMmPerSec2(_opts.MaxAccelRpmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            }
        }

        public decimal? MaxDecelMmps2 {
            get {
                if (!HasValidMechanicsConfig()) return null;
                return AxisRpm.RpmPerSecToMmPerSec2(_opts.MaxDecelRpmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            }
        }

        // ---------------- 核心接口：外部永远传 mm/s ----------------

        /// <summary>写入目标线速度（mm/s）→ 设备口径（负载侧 pps）</summary>
        public async Task WriteSpeedAsync(decimal mmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // PPR 必须就绪
            if (!Volatile.Read(ref _sPprReady) || Volatile.Read(ref _sPpr) <= 0) {
                OnAxisFaulted(new InvalidOperationException("写速度失败：PPR 未初始化，请先执行 Enable"));
                return;
            }

            var ppr = Volatile.Read(ref _sPpr);

            // mm/s → rpm（考虑滚筒/丝杠、齿比）
            var rpm = AxisRpm.FromMmPerSec(mmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);

            // rpm → 电机侧 pps → 负载侧 pps（设备口径）
            var motorPps = rpm.Value / 60m * ppr;
            var loadPps = _opts.GearRatio > 0m ? motorPps / _opts.GearRatio : motorPps;

            var deviceVal = (int)Math.Round(loadPps);
            if (_opts.IsReverse) deviceVal = -deviceVal;

            var ret = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, deviceVal, suppressLog: false);
            if (ret != 0) {
                SetErrorFromRet("write 0x60FF (TargetVelocity)", ret);
                OnAxisFaulted(new InvalidOperationException(LastErrorMessage!));
                return;
            }

            _status = DriverStatus.Connected;
            LastTargetMmps = mmPerSec;
        }

        /// <summary>
        /// 设置速度模式的加/减速度（外部 mm/s²）→ 内部负载侧 pps²（与 0x60FF 保持同一口径）
        /// </summary>
        public async Task SetAccelDecelByLinearAsync(decimal accelMmPerSec, decimal decelMmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            if (!HasValidMechanicsConfig()) {
                OnAxisFaulted(new InvalidOperationException("机械参数必须提供 ScrewPitchMm 或 PulleyPitchDiameterMm"));
                return;
            }

            // mm/s² → rpm/s
            var accRpmPerSec = AxisRpm.MmPerSec2ToRpmPerSec(accelMmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            var decRpmPerSec = AxisRpm.MmPerSec2ToRpmPerSec(decelMmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);

            await SetAccelDecelAsync(accRpmPerSec, decRpmPerSec, ct);
        }

        /// <summary>
        /// 设置速度模式的加/减速度（外部 rpm/s）→ 内部负载侧 pps²
        /// </summary>
        public async Task SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            if (!Volatile.Read(ref _sPprReady) || Volatile.Read(ref _sPpr) <= 0) {
                OnAxisFaulted(new InvalidOperationException("写加减速度失败：PPR 未初始化，请先执行 Enable"));
                return;
            }
            var ppr = Volatile.Read(ref _sPpr);

            // 记录原始请求值（仅用于告警文案）
            var reqA = accelRpmPerSec; var reqD = decelRpmPerSec;

            // 1) 负值截断
            var intercepted = false;
            if (accelRpmPerSec < 0) { accelRpmPerSec = 0; intercepted = true; }
            if (decelRpmPerSec < 0) { decelRpmPerSec = 0; intercepted = true; }
            if (intercepted) {
                OnAxisFaulted(new InvalidOperationException($"[加减速度] 负值已截断: reqA={reqA} rpm/s, reqD={reqD} rpm/s → effA={accelRpmPerSec} rpm/s, effD={decelRpmPerSec} rpm/s"));
            }

            // 2) 限幅（rpm/s 口径）
            var effA = Math.Min(_opts.MaxAccelRpmPerSec, accelRpmPerSec);
            var effD = Math.Min(_opts.MaxDecelRpmPerSec, decelRpmPerSec);
            if (effA != accelRpmPerSec || effD != decelRpmPerSec) {
                OnAxisFaulted(new InvalidOperationException($"[加减速度] 超限值已截断: reqA={accelRpmPerSec} rpm/s, reqD={decelRpmPerSec} rpm/s → effA={effA} rpm/s, effD={effD} rpm/s (limits: A≤{_opts.MaxAccelRpmPerSec}, D≤{_opts.MaxDecelRpmPerSec})"));
            }

            // 3) rpm/s → 电机侧 pps² → 负载侧 pps²（设备口径）
            var motorAccelPps2 = effA / 60m * ppr;
            var motorDecelPps2 = effD / 60m * ppr;

            var loadAccelPps2 = _opts.GearRatio > 0m ? motorAccelPps2 / _opts.GearRatio : motorAccelPps2;
            var loadDecelPps2 = _opts.GearRatio > 0m ? motorDecelPps2 / _opts.GearRatio : motorDecelPps2;

            static uint ClampU32(decimal v) => v <= 0 ? 0u : (uint)Math.Min(uint.MaxValue, Math.Round(v));
            var accDev = ClampU32(loadAccelPps2);
            var decDev = ClampU32(loadDecelPps2);

            // 4) 写寄存器（0x6083 / 0x6084）
            var r1 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileAcceleration, accDev);
            if (r1 != 0) { SetErrorFromRet("write 0x6083 (ProfileAcceleration)", r1); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return; }

            var r2 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileDeceleration, decDev);
            if (r2 != 0) { SetErrorFromRet("write 0x6084 (ProfileDeceleration)", r2); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return; }
        }

        /// <summary>停止轴运动（置零 + QuickStop）</summary>
        public async ValueTask StopAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 策略A: 目标速度置零（负载侧 pps）
            _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0, suppressLog: true);

            // 策略B: QuickStop (ControlWord bit2=1)
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, (ushort)0x0002);
            if (ret != 0) { OnAxisFaulted(new InvalidOperationException($"QuickStop failed, ret={ret}")); return; }

            _status = DriverStatus.Connected;
        }

        /// <summary>心跳/反馈：读取 0x606C（负载侧 pps），换算为 mm/s 广播</summary>
        public async Task<bool> PingAsync(CancellationToken ct = default) {
            if (!EnsureNativeLibLoaded()) { _status = DriverStatus.Degraded; return false; }

            var ret = ReadTxPdo(LeadshineProtocolMap.Index.StatusWord, out ushort _, suppressLog: true);
            if (ret != 0) {
                SetErrorFromRet("read 0x6041 (StatusWord)", ret);
                _status = DriverStatus.Degraded;
                OnAxisDisconnected(LastErrorMessage!);
                return false;
            }

            _status = DriverStatus.Connected;

            ret = ReadTxPdo(LeadshineProtocolMap.Index.ActualVelocity, out int actualPps, suppressLog: true);
            if (ret != 0) { SetErrorFromRet("read 0x606C (ActualVelocity)", ret); return false; }

            var stamp = Stopwatch.GetTimestamp();

            // 方向一次性处理（负载侧 pps）
            var loadPpsVal = _opts.IsReverse ? -actualPps : actualPps;

            // 负载侧 pps → 电机侧 pps → rpm → mm/s
            var ppr = 0;
            try { ppr = await GetPprCachedAsync(ct).ConfigureAwait(false); } catch { /* 忽略 */ }

            var rpmVal = 0m;
            if (ppr > 0) {
                var motorPps = loadPpsVal * _opts.GearRatio;   // 负载侧 → 电机侧
                rpmVal = motorPps * 60m / ppr;                 // 电机侧 pps → rpm
            }

            var mmps = new AxisRpm(rpmVal).ToMmPerSec(_opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);

            if (ShouldPublishFeedbackMmps(mmps, stamp)) {
                PublishSpeedFeedbackFromActualPps(actualPps, ppr); // 内部做一致性换算与事件发布
                CommitFeedbackMmps(mmps, stamp);
            }
            return true;
        }

        /// <summary>上电/使能：状态机 + 强制读取 PPR（未就绪则禁止写入）</summary>
        public async Task EnableAsync(CancellationToken ct = default) {
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

            // 0) 清除报警 → 清零
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.FaultReset, LeadshineProtocolMap.DelayMs.AfterFaultReset)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.Clear, LeadshineProtocolMap.DelayMs.AfterClear)) return;

            // 1) 设置模式：速度模式 (PV=3)
            var m = WriteRxPdo(LeadshineProtocolMap.Index.ModeOfOperation, LeadshineProtocolMap.Mode.ProfileVelocity);
            if (m != 0) { OnAxisFaulted(new InvalidOperationException("Set Mode=PV failed")); return; }
            await Task.Delay(LeadshineProtocolMap.DelayMs.AfterSetMode, ct);

            // 2) 402 状态机三步
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.Shutdown, LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.SwitchOn, LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.EnableOperation, LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;

            // 3) 强制读取 PPR（未取到禁止写速度）
            if (!Volatile.Read(ref _sPprReady)) {
                try {
                    var ppr = await ReadAxisPulsesPerRevAsync(ct);
                    if (ppr > 0) {
                        Volatile.Write(ref _sPpr, ppr);
                        Volatile.Write(ref _sPprReady, true);
                        Debug.WriteLine($"[PPR] 使能时初始化成功: {ppr}");
                    }
                    else {
                        OnAxisFaulted(new InvalidOperationException("使能失败：未能读取到有效的 PPR（脉冲/转），禁止写入速度指令"));
                        return;
                    }
                }
                catch (Exception ex) {
                    OnAxisFaulted(new InvalidOperationException($"使能失败：读取 PPR 时发生错误（{ex.Message}），禁止写入速度指令", ex));
                    return;
                }
            }

            _status = DriverStatus.Connected;
            IsEnabled = true;
        }

        /// <summary>禁用（安全停机 + 状态回退 + 本地状态复位）</summary>
        public async ValueTask DisableAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0, suppressLog: true);
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, (ushort)0x0002);
            if (ret != 0) {
                SetErrorFromRet("write 0x6040 (ControlWord:<step>)", ret);
                OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return;
            }
            await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, ct);

            var cw = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, LeadshineProtocolMap.ControlWord.Shutdown);
            if (cw != 0) {
                SetErrorFromRet("write 0x6040 (ControlWord:<step>)", cw);
                OnAxisFaulted(new InvalidOperationException($"Disable: write ControlWord=Shutdown(0x0006) failed, ret={cw}"));
            }
            await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, ct);

            _health.Stop();
            _fails.Reset();

            _status = DriverStatus.Disconnected;
            Volatile.Write(ref _lastFbStamp, 0);
            _lastFbMmps = 0;
            IsEnabled = false;
        }

        /// <summary>
        /// 读取“轴的一圈脉冲量(PPR)”。
        /// 使用 OD 0x6092（Feed Constant）: :01 Numerator / :02 Denominator，PPR = Numerator / Denominator。
        /// 成功返回 PPR；失败返回 0（不抛异常，事件通知）。
        /// </summary>
        public async ValueTask<int> ReadAxisPulsesPerRevAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            if (!EnsureNativeLibLoaded())
                return 0;

            int numerator = 0, denominator = 0;
            try {
                const ushort idx = LeadshineProtocolMap.Index.FeedConstant;

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
                    OnAxisFaulted(new InvalidOperationException($"read 0x{idx:X4}:02 returned invalid denominator={denominator}"));
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
        /// 仅更新 DriverOptions 内存缓存；不外抛异常。
        /// </summary>
        public Task UpdateLinearLimitsAsync(decimal maxLinearMmps, decimal maxAccelMmps2, decimal maxDecelMmps2,
            CancellationToken ct = default) {
            if (maxLinearMmps <= 0 || maxAccelMmps2 <= 0 || maxDecelMmps2 <= 0) return Task.FromResult(false);
            if (!HasValidMechanicsConfig()) return Task.FromResult(false);

            var maxRpm = AxisRpm.FromMmPerSec(maxLinearMmps, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm).Value;
            var maxAccelRpmPerSec = AxisRpm.MmPerSec2ToRpmPerSec(maxAccelMmps2, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            var maxDecelRpmPerSec = AxisRpm.MmPerSec2ToRpmPerSec(maxDecelMmps2, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);

            try {
                _opts.MaxRpm = maxRpm;
                _opts.MaxAccelRpmPerSec = maxAccelRpmPerSec;
                _opts.MaxDecelRpmPerSec = maxDecelRpmPerSec;
                return Task.FromResult(true);
            }
            catch { return Task.FromResult(false); }
        }

        /// <summary>
        /// 更新机械参数（滚筒直径/齿轮比/PPR）。仅更新内存缓存与换算系数，后续命令按新系数生效。
        /// </summary>
        public Task UpdateMechanicsAsync(decimal rollerDiameterMm, decimal gearRatio, int ppr,
            CancellationToken ct = default) {
            if (rollerDiameterMm <= 0 || gearRatio <= 0 || ppr <= 0) return Task.FromResult(false);

            try {
                _opts.PulleyPitchDiameterMm = rollerDiameterMm;
                _opts.GearRatio = gearRatio;

                // ★ 关键：同步更新 PPR 缓存，让新换算立即生效
                Volatile.Write(ref _sPpr, ppr);
                Volatile.Write(ref _sPprReady, true);

                return Task.FromResult(true);
            }
            catch { return Task.FromResult(false); }
        }

        // ---------------- 内部工具 ----------------

        /// <summary>机械配置有效性（丝杠导程或滚筒直径至少其一有效）</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasValidMechanicsConfig() =>
            _opts.ScrewPitchMm > 0m || _opts.PulleyPitchDiameterMm > 0m;

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

        /// <summary>统一写 RxPDO（按 Index/值自动选择位宽）</summary>
        private short WriteRxPdo(ushort index, object value, byte subIndex = LeadshineProtocolMap.SubIndex.Root, bool suppressLog = false) {
            var ret = _pdoPipe.Execute(
                (CancellationToken _) => WriteRxPdoCore(index, value, subIndex, suppressLog),
                cancellationToken: CancellationToken.None);

            if (ret != 0) {
                _fails.Increment();
            }
            else {
                _fails.Reset();
                if (_status == DriverStatus.Degraded) {
                    _health.Stop();
                    _status = DriverStatus.Connected;
                }
            }
            return ret;
        }

        private short WriteRxPdoCore(ushort index, object value, byte subIndex = LeadshineProtocolMap.SubIndex.Root, bool suppressLog = false) {
            if (value is int i32) {
                var buf = GetTxBuffer(4);
                BinaryPrimitives.WriteInt32LittleEndian(buf, i32);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 32, buf);
                if (!suppressLog) OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 32 , {i32}", ret);
                return ret;
            }
            if (value is uint u32) {
                var buf = GetTxBuffer(4);
                BinaryPrimitives.WriteUInt32LittleEndian(buf, u32);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 32, buf);
                if (!suppressLog) OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 32 , {u32}", ret);
                return ret;
            }
            if (value is short i16) {
                var buf = GetTxBuffer(2);
                BinaryPrimitives.WriteInt16LittleEndian(buf, i16);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 16, buf);
                if (!suppressLog) OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 16 , {i16}", ret);
                return ret;
            }
            if (value is ushort u16) {
                var buf = GetTxBuffer(2);
                BinaryPrimitives.WriteUInt16LittleEndian(buf, u16);
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 16, buf);
                if (!suppressLog) OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 16 , {u16}", ret);
                return ret;
            }
            if (value is byte b8) {
                var buf = GetTxBuffer(1);
                buf[0] = b8;
                var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 8, buf);
                if (!suppressLog) OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 8 , {b8}", ret);
                return ret;
            }

            var bytes = value switch {
                sbyte s8 => new[] { unchecked((byte)s8) },
                _ => throw new NotSupportedException($"不支持的写入类型：{value.GetType().Name}")
            };
            var bitLen = (ushort)(bytes.Length * 8);
            var retFallback = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, bitLen, bytes);
            if (!suppressLog) OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {value}", retFallback);
            return retFallback;
        }

        /// <summary>读 TxPDO 并解码（byte/sbyte/ushort/short/uint/int/byte[]），成功 0</summary>
        private short ReadTxPdo<T>(ushort index, out T value, byte subIndex = LeadshineProtocolMap.SubIndex.Root, bool suppressLog = false) {
            value = default!;
            ushort bitLen = index switch {
                var i when i == LeadshineProtocolMap.Index.StatusWord => (ushort)LeadshineProtocolMap.BitLen.StatusWord,
                var i when i == LeadshineProtocolMap.Index.ModeOfOperation => (ushort)LeadshineProtocolMap.BitLen.ModeOfOperation,
                var i when i == LeadshineProtocolMap.Index.ActualVelocity => (ushort)LeadshineProtocolMap.BitLen.ActualVelocity,
                var i when i == LeadshineProtocolMap.Index.FeedConstant => 32,
                var i when i == LeadshineProtocolMap.Index.GearRatio => 32,
                _ => 0
            };
            if (bitLen == 0) {
                OnAxisFaulted(new InvalidOperationException($"Index 0x{index:X4} not mapped to BitLen."));
                return -1;
            }

            var byteLen = (bitLen + 7) / 8;
            var buf = new byte[byteLen];
            var ret = LTDMC.nmc_read_txpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, bitLen, buf);
            if (!suppressLog) OnCommandIssued("nmc_read_txpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {byteLen}", ret);
            if (ret != 0) return ret;

            object? boxed =
                typeof(T) == typeof(byte) ? buf[0] :
                typeof(T) == typeof(sbyte) ? unchecked((sbyte)buf[0]) :
                typeof(T) == typeof(ushort) ? BitConverter.ToUInt16(buf, 0) :
                typeof(T) == typeof(short) ? BitConverter.ToInt16(buf, 0) :
                typeof(T) == typeof(uint) ? BitConverter.ToUInt32(buf, 0) :
                typeof(T) == typeof(int) ? BitConverter.ToInt32(buf, 0) :
                typeof(T) == typeof(byte[]) ? buf : null;

            if (boxed is null) {
                OnAxisFaulted(new InvalidOperationException($"Unsupported target type {typeof(T).Name} for read 0x{index:X4}."));
                return -2;
            }

            value = (T)boxed;

            if (!suppressLog) OnCommandIssued("nmc_read_txpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {byteLen}", 0,
                note: $"decoded={boxed}");

            return 0;
        }

        // ---- 事件广播：逐订阅者、非阻塞、与调用方隔离 ----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FireEachNonBlocking<T>(EventHandler<T>? multicast, object sender, T args) {
            if (multicast is null) return;
            foreach (var d in multicast.GetInvocationList()) {
                var h = (EventHandler<T>)d;
                var state = new EvState<T>(sender, h, args);
                ThreadPool.UnsafeQueueUserWorkItem(static s => {
                    var st = (EvState<T>)s!;
                    try { st.Handler(st.Sender, st.Args); } catch { /* 静默隔离 */ }
                }, state, preferLocal: true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<int> GetPprCachedAsync(CancellationToken ct) {
            var ready = Volatile.Read(ref _sPprReady);
            var v = ready ? Volatile.Read(ref _sPpr) : 0;
            return ValueTask.FromResult(v);
        }

        /// <summary>从负载侧 pps 发布速度反馈（事件载荷单位见注释）</summary>
        private void PublishSpeedFeedbackFromActualPps(int actualPps, int pulsesPerRev) {
            var loadPpsVal = _opts.IsReverse ? -actualPps : actualPps;

            // 负载侧 pps → 电机侧 pps → rpm
            var rpmVal = 0m;
            if (pulsesPerRev > 0) {
                var motorPps = loadPpsVal * _opts.GearRatio;
                rpmVal = motorPps * 60m / pulsesPerRev;
            }

            var rpm = new AxisRpm(rpmVal);
            var speedMmps = rpm.ToMmPerSec(_opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            LastFeedbackMmps = speedMmps;

            var pps = (decimal)loadPpsVal; // 事件中直接使用负载侧 pps

            FireEachNonBlocking(SpeedFeedback, this,
                new AxisSpeedFeedbackEventArgs {
                    Axis = Axis,
                    Rpm = rpmVal,
                    SpeedMps = speedMmps,     // 注：该字段含义为 mm/s
                    PulsesPerSec = pps,
                    TimestampUtc = DateTime.UtcNow
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureNativeLibLoaded() {
            var probed = SNative.Value;
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

        // 线程本地发送缓冲
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] GetTxBuffer(int len) {
            var buf = _tlsTxBuf8;
            if (buf == null || buf.Length < len) {
                buf = new byte[Math.Max(8, len)];
                _tlsTxBuf8 = buf;
            }
            return buf;
        }

        // —— 便捷事件触发 ——
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