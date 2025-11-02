using System;
using csLTDMC;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Threading;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Utils;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Health;
using ZakYip.Singulation.Drivers.Resilience;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Leadshine
{
    /// <summary>
    /// 雷赛 LTDMC 实机轴驱动（EtherCAT）。
    /// - 通过 nmc_write_rxpdo / nmc_read_txpdo 直写/直读 PDO。
    /// - 通过 402 状态机使能。
    /// - 目标速度写 0x60FF（负载侧 pps，INT32），实际速度读 0x606C（负载侧 pps）。
    /// - 加减速度写 0x6083/0x6084（负载侧 pps²，UNSIGNED32）。
    /// - PPR（脉冲/转）来自 0x6092:01/02（Feed Constant），Enable 后必须就绪。
    /// </summary>

    /// <summary>
    /// 雷赛 LTDMC 实机轴驱动（EtherCAT）。
    /// - 0x60FF TargetVelocity / 0x606C ActualVelocity 的口径：**负载侧 pps**。
    /// - 线速度与 pps 的换算统一采用“几何直达口径”：以每转线位移 Lpr（丝杠导程或滚筒周长）为唯一真源。
    /// - Lpr 计算规则：若 ScrewPitchMm>0，则 Lpr=ScrewPitchMm；否则 Lpr=π·PulleyPitchDiameterMm。
    /// - PPR（每转脉冲数）来自 OD 0x6092 Feed Constant（:01/:02）。
    /// </summary>
    public sealed class LeadshineLtdmcAxisDrive : IAxisDrive
    {
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
        private static readonly Lazy<(bool ok, string reason)> SNative = new(() =>
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                    return (false, "非 Windows 平台：LTDMC.dll 仅支持 Windows");
                if (NativeLibrary.TryLoad("LTDMC.dll", out var _))
                    return (true, "");
                return (false, "找不到 LTDMC.dll，或位宽不匹配（x64/x86）/缺少依赖");
            }
            catch (Exception ex) { return (false, $"加载 LTDMC.dll 失败：{ex.Message}"); }
        }, isThreadSafe: true);

        // —— 每个实例仅通知一次 ——
        private bool _driverNotifiedOnce;

        private readonly ConsecutiveFailCounter _fails = new(3); // 实际应由 _opts.ConsecutiveFailThreshold 覆盖
        private readonly AxisHealthMonitor _health;
        private readonly Polly.ResiliencePipeline<short> _pdoPipe;
        private readonly Polly.ResiliencePipeline _retryPipe;

        // 内存池优化：使用 ArrayPool 减少 GC 压力
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

        public LeadshineLtdmcAxisDrive(DriverOptions opts)
        {
            _opts = opts;
            Axis = new AxisId(_opts.NodeId);

            // 健康监测：降级后轮询 PingAsync
            _health = new AxisHealthMonitor(PingAsync, _opts.HealthPingInterval);
            _health.Recovered += () =>
            {
                _fails.Reset();
                _status = DriverStatus.Connected;
            };

            // 断路器：降级与恢复回调
            _pdoPipe = AxisDegradePolicy.BuildPdoPipeline(
                _opts.ConsecutiveFailThreshold,
                onDegraded: () =>
                {
                    _status = DriverStatus.Degraded;
                    OnAxisDisconnected("Degraded by circuit-breaker (consecutive failures).");
                    if (_opts.EnableHealthMonitor)
                        _health.Start();
                },
                onRecovered: () =>
                {
                    _fails.Reset();
                    _status = DriverStatus.Connected;
                    _health.Stop();
                }
            );

            // 雷赛使能/失能重试管线：最多重试3次，无等待时间
            _retryPipe = LeadshineRetryPolicy.BuildEnableDisableRetryPipeline();
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

        public decimal? MaxLinearMmps
        {
            get
            {
                if (!HasValidMechanicsConfig())
                    return null;
                return new AxisRpm(_opts.MaxRpm).ToMmPerSec(_opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            }
        }

        public decimal? MaxAccelMmps2
        {
            get
            {
                if (!HasValidMechanicsConfig())
                    return null;
                return AxisRpm.RpmPerSecToMmPerSec2(_opts.MaxAccelRpmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            }
        }

        public decimal? MaxDecelMmps2
        {
            get
            {
                if (!HasValidMechanicsConfig())
                    return null;
                return AxisRpm.RpmPerSecToMmPerSec2(_opts.MaxDecelRpmPerSec, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            }
        }

        /// <summary>
        /// 轴类型，用于区分分离轴（Main）和疏散轴（Eject）。
        /// 默认值为 Main（分离轴）。
        /// </summary>
        public AxisType AxisType { get; set; } = AxisType.Main;

        // ---------------- 几何换算核心（本类私有） ----------------

        /// <summary>计算每转线位移 Lpr（mm/turn）。丝杠优先，其次滚筒。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private decimal GetLinearPerRevolutionMm()
        {
            // 若存在丝杠导程，优先采用；否则采用滚筒直径换算周长。
            if (_opts.ScrewPitchMm > 0m)
                return _opts.ScrewPitchMm;
            return (decimal)Math.PI * _opts.PulleyPitchDiameterMm;
        }

        /// <summary>线速度（mm/s）→ 负载侧 pps：pps = (mm/s ÷ Lpr) × PPR ÷ gearRatio。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal MmpsToLoadPps(decimal mmps, decimal lprMm, int ppr, decimal gearRatio)
        {
            if (mmps == 0m || lprMm <= 0m || ppr <= 0 || gearRatio <= 0m)
                return 0m;
            var revPerSecLoad = mmps / lprMm;
            var ppsLoad = revPerSecLoad * ppr / gearRatio;
            return ppsLoad;
        }

        /// <summary>线加速度（mm/s²）→ 负载侧 pps²：pps² = (mm/s² ÷ Lpr) × PPR ÷ gearRatio。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal Mmps2ToLoadPps2(decimal mmps2, decimal lprMm, int ppr, decimal gearRatio)
        {
            if (mmps2 <= 0m || lprMm <= 0m || ppr <= 0 || gearRatio <= 0m)
                return 0m;
            var revPerSec2Load = mmps2 / lprMm;
            var pps2Load = revPerSec2Load * ppr / gearRatio;
            return pps2Load;
        }

        /// <summary>负载侧 pps → 线速度（mm/s）：mm/s = (pps ÷ PPR) × Lpr。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal LoadPpsToMmps(int ppsLoad, int ppr, decimal lprMm)
        {
            if (ppr <= 0 || lprMm <= 0m)
                return 0m;
            var revPerSecLoad = (decimal)ppsLoad / ppr;
            var mmps = revPerSecLoad * lprMm;
            return mmps;
        }

        // ---------------- 核心接口：外部永远传 mm/s ----------------

        /// <summary>写入目标线速度（mm/s）→ 设备口径（负载侧 pps）。</summary>
        public async Task WriteSpeedAsync(decimal mmPerSec, CancellationToken ct = default)
        {
            await ThrottleAsync(ct);
            LastTargetMmps = mmPerSec;
            // PPR 必须就绪
            if (!Volatile.Read(ref _sPprReady) || Volatile.Read(ref _sPpr) <= 0)
            {
                OnAxisFaulted(new InvalidOperationException("写速度失败：PPR 未初始化，请先执行 Enable"));
                return;
            }

            var ppr = Volatile.Read(ref _sPpr);
            var lpr = GetLinearPerRevolutionMm(); // Lpr

            // —— 统一几何口径：直接计算负载侧 pps ——（显式包含 Math.PI 或 ScrewPitchMm）
            var loadPps = MmpsToLoadPps(mmPerSec, lpr, ppr, _opts.GearRatio);
            var deviceVal = (int)Math.Round(loadPps);

            // 方向
            if (_opts.IsReverse)
                deviceVal = -deviceVal;

            var ret = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, deviceVal, suppressLog: false);
            if (ret != 0)
            {
                SetErrorFromRet("write 0x60FF (TargetVelocity)", ret);
                OnAxisFaulted(new InvalidOperationException(LastErrorMessage!));
                return;
            }

            _status = DriverStatus.Connected;
        }

        /// <summary>
        /// 设置速度模式的加/减速度（外部 mm/s²）→ 内部负载侧 pps²（与 0x60FF 保持同一口径）。
        /// </summary>
        public async Task SetAccelDecelByLinearAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default)
        {
            await ThrottleAsync(ct);

            if (!HasValidMechanicsConfig())
            {
                OnAxisFaulted(new InvalidOperationException("机械参数必须提供 ScrewPitchMm 或 PulleyPitchDiameterMm"));
                return;
            }

            if (!Volatile.Read(ref _sPprReady) || Volatile.Read(ref _sPpr) <= 0)
            {
                OnAxisFaulted(new InvalidOperationException("写加减速度失败：PPR 未初始化，请先执行 Enable"));
                return;
            }

            var ppr = Volatile.Read(ref _sPpr);
            var lpr = GetLinearPerRevolutionMm();

            // —— 直接几何口径：mm/s² → pps²(load) ——
            var accPps2Load = Mmps2ToLoadPps2(accelMmPerSec2, lpr, ppr, _opts.GearRatio);
            var decPps2Load = Mmps2ToLoadPps2(decelMmPerSec2, lpr, ppr, _opts.GearRatio);

            var accDev = MathUtils.ClampToUInt32(accPps2Load);
            var decDev = MathUtils.ClampToUInt32(decPps2Load);

            // —— 写寄存器（0x6083 / 0x6084）——
            var r1 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileAcceleration, accDev);
            if (r1 != 0)
            { SetErrorFromRet("write 0x6083 (ProfileAcceleration)", r1); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return; }

            var r2 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileDeceleration, decDev);
            if (r2 != 0)
            { SetErrorFromRet("write 0x6084 (ProfileDeceleration)", r2); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return; }
        }

        /// <summary>
        /// 设置速度模式的加/减速度（外部 rpm/s，**电机侧**）→ 内部负载侧 pps²。
        /// 注：rpm/s 属转速量纲，无需 π；换算：motorPps² = (rpm/s ÷ 60) × PPR；loadPps² = motorPps² ÷ gearRatio。
        /// </summary>
        public async Task SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default)
        {
            await ThrottleAsync(ct);

            if (!Volatile.Read(ref _sPprReady) || Volatile.Read(ref _sPpr) <= 0)
            {
                OnAxisFaulted(new InvalidOperationException("写加减速度失败：PPR 未初始化，请先执行 Enable"));
                return;
            }
            var ppr = Volatile.Read(ref _sPpr);

            // 记录原始请求值（仅用于告警文案）
            var reqA = accelRpmPerSec;
            var reqD = decelRpmPerSec;

            // 1) 负值截断
            var intercepted = false;
            if (accelRpmPerSec < 0)
            { accelRpmPerSec = 0; intercepted = true; }
            if (decelRpmPerSec < 0)
            { decelRpmPerSec = 0; intercepted = true; }
            if (intercepted)
            {
                OnAxisFaulted(new InvalidOperationException($"[加减速度] 负值已截断: reqA={reqA} rpm/s, reqD={reqD} rpm/s → effA={accelRpmPerSec} rpm/s, effD={decelRpmPerSec} rpm/s"));
            }

            // 2) 限幅（rpm/s 口径）
            var effA = Math.Min(_opts.MaxAccelRpmPerSec, accelRpmPerSec);
            var effD = Math.Min(_opts.MaxDecelRpmPerSec, decelRpmPerSec);
            if (effA != accelRpmPerSec || effD != decelRpmPerSec)
            {
                OnAxisFaulted(new InvalidOperationException($"[加减速度] 超限值已截断: reqA={accelRpmPerSec} rpm/s, reqD={decelRpmPerSec} rpm/s → effA={effA} rpm/s, effD={effD} rpm/s (limits: A≤{_opts.MaxAccelRpmPerSec}, D≤{_opts.MaxDecelRpmPerSec})"));
            }

            // 3) rpm/s（电机侧）→ pps²（负载侧）
            var motorAccelPps2 = effA / 60m * ppr;
            var motorDecelPps2 = effD / 60m * ppr;

            var loadAccelPps2 = _opts.GearRatio > 0m ? motorAccelPps2 / _opts.GearRatio : motorAccelPps2;
            var loadDecelPps2 = _opts.GearRatio > 0m ? motorDecelPps2 / _opts.GearRatio : motorDecelPps2;

            var accDev = MathUtils.ClampToUInt32(loadAccelPps2);
            var decDev = MathUtils.ClampToUInt32(loadDecelPps2);

            // 4) 写寄存器（0x6083 / 0x6084）
            var r1 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileAcceleration, accDev);
            if (r1 != 0)
            { SetErrorFromRet("write 0x6083 (ProfileAcceleration)", r1); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return; }

            var r2 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileDeceleration, decDev);
            if (r2 != 0)
            { SetErrorFromRet("write 0x6084 (ProfileDeceleration)", r2); OnAxisFaulted(new InvalidOperationException(LastErrorMessage!)); return; }
        }

        /// <summary>停止轴运动（置零 + QuickStop）。</summary>
        public async ValueTask StopAsync(CancellationToken ct = default)
        {
            await ThrottleAsync(ct);

            // 策略A: 目标速度置零（负载侧 pps）
            _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0, suppressLog: true);

            // 策略B: QuickStop (ControlWord bit2=1)
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, (ushort)0x0002);
            if (ret != 0)
            { OnAxisFaulted(new InvalidOperationException($"QuickStop failed, ret={ret}")); return; }

            _status = DriverStatus.Connected;
        }

        /// <summary>心跳/反馈：读取 0x606C（负载侧 pps），换算为 mm/s 广播。</summary>
        public async Task<bool> PingAsync(CancellationToken ct = default)
        {
            if (!EnsureNativeLibLoaded())
            { _status = DriverStatus.Degraded; return false; }

            var ret = ReadTxPdo(LeadshineProtocolMap.Index.StatusWord, out ushort _, suppressLog: true);
            if (ret != 0)
            {
                SetErrorFromRet("read 0x6041 (StatusWord)", ret);
                _status = DriverStatus.Degraded;
                OnAxisDisconnected(LastErrorMessage!);
                return false;
            }

            _status = DriverStatus.Connected;

            ret = ReadTxPdo(LeadshineProtocolMap.Index.ActualVelocity, out int actualPps, suppressLog: true);
            if (ret != 0)
            { SetErrorFromRet("read 0x606C (ActualVelocity)", ret); return false; }

            var stamp = Stopwatch.GetTimestamp();

            // 方向一次性处理（负载侧 pps）
            var loadPpsVal = _opts.IsReverse ? -actualPps : actualPps;

            // —— 负载侧 pps → mm/s（几何直达口径）——
            var ppr = 0;
            try
            { ppr = await GetPprCachedAsync(ct).ConfigureAwait(false); }
            catch { /* 静默忽略 */ }

            var lpr = GetLinearPerRevolutionMm();
            var mmps = (ppr > 0) ? LoadPpsToMmps(loadPpsVal, ppr, lpr) : 0m;

            // —— 维持事件中 Rpm 的含义与旧版一致：电机侧 rpm ——
            var rpmVal = 0m;
            if (ppr > 0)
            {
                var motorPps = loadPpsVal * _opts.GearRatio;   // 负载侧 → 电机侧
                rpmVal = motorPps * 60m / ppr;                 // 电机侧 pps → rpm
            }

            if (ShouldPublishFeedbackMmps(mmps, stamp))
            {
                PublishSpeedFeedbackFromActualPps(actualPps, ppr); // 内部亦会做换算与事件发布
                CommitFeedbackMmps(mmps, stamp);
            }
            return true;
        }

        /// <summary>上电/使能：状态机 + 强制读取 PPR（未就绪则禁止写入）。使用 Polly 重试策略，最多重试3次。</summary>
        public async Task EnableAsync(CancellationToken ct = default)
        {
            await _retryPipe.ExecuteAsync(async (CancellationToken cancellationToken) =>
            {
                await ThrottleAsync(cancellationToken);

                // 简化封装：写 ControlWord、延时，然后验证
                async Task<bool> WriteAndVerifyCtrlAsync(ushort expectedValue, int delayMs)
                {
                    var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, expectedValue);
                    if (ret != 0)
                    {
                        SetErrorFromRet("write 0x6040 (ControlWord:<step>)", ret);
                        throw new InvalidOperationException(LastErrorMessage!);
                    }
                    if (delayMs > 0)
                        await Task.Delay(delayMs, cancellationToken);
                    
                    // 读回验证
                    var readRet = ReadTxPdo(LeadshineProtocolMap.Index.ControlWord, out ushort actualValue, suppressLog: true);
                    if (readRet == 0)
                    {
                        Debug.WriteLine($"[Enable] 写入 ControlWord: 0x{expectedValue:X4}, 读回: 0x{actualValue:X4}");
                        // 验证关键位是否设置正确（不要求完全相等，因为某些位可能由驱动器控制）
                        // 对于 EnableOperation (0x000F)，检查 bit0-3 是否都为1
                        if (expectedValue == LeadshineProtocolMap.ControlWord.EnableOperation)
                        {
                            if ((actualValue & 0x000F) != 0x000F)
                            {
                                throw new InvalidOperationException($"EnableOperation 验证失败: 期望 bit0-3=1, 实际 ControlWord=0x{actualValue:X4}");
                            }
                        }
                    }
                    return true;
                }

                // 1) 清除报警 → 清零
                await WriteAndVerifyCtrlAsync(LeadshineProtocolMap.ControlWord.FaultReset, LeadshineProtocolMap.DelayMs.AfterFaultReset);
                await WriteAndVerifyCtrlAsync(LeadshineProtocolMap.ControlWord.Clear, LeadshineProtocolMap.DelayMs.AfterClear);

                // 2) 设置模式：速度模式 (PV=3)
                var m = WriteRxPdo(LeadshineProtocolMap.Index.ModeOfOperation, LeadshineProtocolMap.Mode.ProfileVelocity);
                if (m != 0)
                { throw new InvalidOperationException("Set Mode=PV failed"); }
                await Task.Delay(LeadshineProtocolMap.DelayMs.AfterSetMode, cancellationToken);

                // 3) 402 状态机三步
                await WriteAndVerifyCtrlAsync(LeadshineProtocolMap.ControlWord.Shutdown, LeadshineProtocolMap.DelayMs.BetweenStateCmds);
                await WriteAndVerifyCtrlAsync(LeadshineProtocolMap.ControlWord.SwitchOn, LeadshineProtocolMap.DelayMs.BetweenStateCmds);
                await WriteAndVerifyCtrlAsync(LeadshineProtocolMap.ControlWord.EnableOperation, LeadshineProtocolMap.DelayMs.BetweenStateCmds);

                // 4) 强制读取 PPR（未取到禁止写速度）
                if (!Volatile.Read(ref _sPprReady))
                {
                    var ppr = await ReadAxisPulsesPerRevAsync(cancellationToken);
                    if (ppr > 0)
                    {
                        Volatile.Write(ref _sPpr, ppr);
                        Volatile.Write(ref _sPprReady, true);
                        Debug.WriteLine($"[PPR] 使能时初始化成功: {ppr}");
                    }
                    else
                    {
                        throw new InvalidOperationException("使能失败：未能读取到有效的 PPR（脉冲/转），禁止写入速度指令");
                    }
                }
            }, ct).ConfigureAwait(false);

            // 状态更新仅在成功后执行一次
            _status = DriverStatus.Connected;
            IsEnabled = true;
        }

        /// <summary>禁用（安全停机 + 状态回退 + 本地状态复位）。使用 Polly 重试策略，最多重试3次。</summary>
        public async ValueTask DisableAsync(CancellationToken ct = default)
        {
            await _retryPipe.ExecuteAsync(async (CancellationToken cancellationToken) =>
            {
                await ThrottleAsync(cancellationToken);

                // 1) 停止运动
                _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0, suppressLog: true);

                // 2) QuickStop (ControlWord bit2=1, bit1=1)
                var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, (ushort)0x0002);
                if (ret != 0)
                {
                    SetErrorFromRet("write 0x6040 (ControlWord:QuickStop)", ret);
                    throw new InvalidOperationException(LastErrorMessage!);
                }
                await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, cancellationToken);

                // 3) Shutdown 进入 Ready to Switch On
                var cw = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, LeadshineProtocolMap.ControlWord.Shutdown);
                if (cw != 0)
                {
                    SetErrorFromRet("write 0x6040 (ControlWord:Shutdown)", cw);
                    throw new InvalidOperationException($"Disable: write ControlWord=Shutdown(0x0006) failed, ret={cw}");
                }
                await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, cancellationToken);

                // 4) 读回验证 ControlWord，确保 Shutdown 成功（bit3 应该为0）
                var readRet = ReadTxPdo(LeadshineProtocolMap.Index.ControlWord, out ushort actualValue, suppressLog: true);
                if (readRet == 0)
                {
                    Debug.WriteLine($"[Disable] Shutdown 后读回 ControlWord: 0x{actualValue:X4}");
                    // 验证 bit3 (EnableOperation) 是否为0，表示已经禁用
                    // 注意：驱动器可能会修改某些位，所以不要求完全等于 Shutdown 值
                    if ((actualValue & 0x0008) != 0)
                    {
                        throw new InvalidOperationException($"Disable 验证失败: EnableOperation 位仍然为1, 实际 ControlWord=0x{actualValue:X4}");
                    }
                }
            }, ct).ConfigureAwait(false);

            // 状态清理仅在成功后执行一次
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
        public async ValueTask<int> ReadAxisPulsesPerRevAsync(CancellationToken ct = default)
        {
            await ThrottleAsync(ct);

            if (!EnsureNativeLibLoaded())
                return 0;

            var numerator = 0;
            var denominator = 0;
            try
            {
                const ushort idx = LeadshineProtocolMap.Index.FeedConstant;

                var retNum = LTDMC.nmc_get_node_od(
                    (ushort)_opts.Card, _opts.Port, _opts.NodeId,
                    idx,
                    LeadshineProtocolMap.SubIndex.Numerator,
                    LeadshineProtocolMap.BitLen.FeedConstant,
                    ref numerator
                );
                OnCommandIssued("nmc_get_node_od", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {idx} , {LeadshineProtocolMap.SubIndex.Numerator} , {LeadshineProtocolMap.BitLen.FeedConstant} , {numerator}", retNum);
                if (retNum != 0)
                {
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
                if (retDen != 0)
                {
                    OnAxisFaulted(new InvalidOperationException($"read 0x{idx:X4}:02 (Denominator) failed, ret={retDen}"));
                    return 0;
                }

                if (denominator <= 0)
                {
                    OnAxisFaulted(new InvalidOperationException($"read 0x{idx:X4}:02 returned invalid denominator={denominator}"));
                    return 0;
                }

                return numerator / denominator;
            }
            catch (DllNotFoundException ex)
            {
                if (!_driverNotifiedOnce)
                {
                    _driverNotifiedOnce = true;
                    _status = DriverStatus.Degraded;
                    OnDriverNotLoaded("LTDMC.dll", ex.Message);
                }
                return 0;
            }
            catch (EntryPointNotFoundException ex)
            {
                if (!_driverNotifiedOnce)
                {
                    _driverNotifiedOnce = true;
                    _status = DriverStatus.Degraded;
                    OnDriverNotLoaded("LTDMC.dll", $"入口缺失：{ex.Message}");
                }
                return 0;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await DisableAsync(CancellationToken.None);
                SetError(0, null);
            }
            catch { /* 静默收尾 */ }
        }

        /// <summary>
        /// 更新线速度/加减速度限幅（mm/s, mm/s²）。
        /// 仅更新 DriverOptions 内存缓存；不外抛异常。
        /// </summary>
        public Task UpdateLinearLimitsAsync(decimal maxLinearMmps, decimal maxAccelMmps2, decimal maxDecelMmps2,
            CancellationToken ct = default)
        {
            if (maxLinearMmps <= 0 || maxAccelMmps2 <= 0 || maxDecelMmps2 <= 0)
                return Task.FromResult(false);
            if (!HasValidMechanicsConfig())
                return Task.FromResult(false);

            var maxRpm = new AxisRpm(AxisRpm.FromMmPerSec(maxLinearMmps, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm).Value).Value;
            var maxAccelRpmPerSec = AxisRpm.MmPerSec2ToRpmPerSec(maxAccelMmps2, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);
            var maxDecelRpmPerSec = AxisRpm.MmPerSec2ToRpmPerSec(maxDecelMmps2, _opts.PulleyPitchDiameterMm, _opts.GearRatio, _opts.ScrewPitchMm);

            try
            {
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
            CancellationToken ct = default)
        {
            if (rollerDiameterMm <= 0 || gearRatio <= 0 || ppr <= 0)
                return Task.FromResult(false);

            try
            {
                _opts.PulleyPitchDiameterMm = rollerDiameterMm;
                _opts.GearRatio = gearRatio;

                // ★ 同步更新 PPR 缓存，让新换算立即生效
                Volatile.Write(ref _sPpr, ppr);
                Volatile.Write(ref _sPprReady, true);

                return Task.FromResult(true);
            }
            catch { return Task.FromResult(false); }
        }

        // ---------------- 内部工具 ----------------

        /// <summary>机械配置有效性（丝杠导程或滚筒直径至少其一有效）。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasValidMechanicsConfig() =>
            _opts.ScrewPitchMm > 0m || _opts.PulleyPitchDiameterMm > 0m;

        private async ValueTask ThrottleAsync(CancellationToken ct)
        {
            var now = Stopwatch.GetTimestamp();
            var min = (long)Math.Round(_opts.MinWriteInterval.TotalSeconds * Stopwatch.Frequency);
            var last = Interlocked.Read(ref _lastStamp);
            var delta = now - last;

            if (delta < min)
            {
                var waitTicks = min - delta;
                var waitMs = (int)Math.Ceiling(waitTicks * 1000.0 / Stopwatch.Frequency);
                if (waitMs > 0)
                    await Task.Delay(waitMs, ct);
                now = Stopwatch.GetTimestamp();
            }

            Interlocked.Exchange(ref _lastStamp, now);
        }

        /// <summary>统一写 RxPDO（按 Index/值自动选择位宽）。</summary>
        private short WriteRxPdo(ushort index, object value, byte subIndex = LeadshineProtocolMap.SubIndex.Root, bool suppressLog = false)
        {
            var ret = _pdoPipe.Execute(
                (CancellationToken _) => WriteRxPdoCore(index, value, subIndex, suppressLog),
                cancellationToken: CancellationToken.None);

            if (ret != 0)
            {
                _fails.Increment();
            }
            else
            {
                _fails.Reset();
                if (_status == DriverStatus.Degraded)
                {
                    _health.Stop();
                    _status = DriverStatus.Connected;
                }
            }
            return ret;
        }

        private short WriteRxPdoCore(ushort index, object value, byte subIndex = LeadshineProtocolMap.SubIndex.Root, bool suppressLog = false)
        {
            if (value is int i32)
            {
                var buf = BufferPool.Rent(4);
                try
                {
                    ByteUtils.WriteInt32LittleEndian(buf, i32);
                    var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 32, buf);
                    if (!suppressLog)
                        OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 32 , {i32}", ret);
                    return ret;
                }
                finally
                {
                    BufferPool.Return(buf, clearArray: false);
                }
            }
            if (value is uint u32)
            {
                var buf = BufferPool.Rent(4);
                try
                {
                    ByteUtils.WriteUInt32LittleEndian(buf, u32);
                    var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 32, buf);
                    if (!suppressLog)
                        OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 32 , {u32}", ret);
                    return ret;
                }
                finally
                {
                    BufferPool.Return(buf, clearArray: false);
                }
            }
            if (value is short i16)
            {
                var buf = BufferPool.Rent(2);
                try
                {
                    ByteUtils.WriteInt16LittleEndian(buf, i16);
                    var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 16, buf);
                    if (!suppressLog)
                        OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 16 , {i16}", ret);
                    return ret;
                }
                finally
                {
                    BufferPool.Return(buf, clearArray: false);
                }
            }
            if (value is ushort u16)
            {
                var buf = BufferPool.Rent(2);
                try
                {
                    ByteUtils.WriteUInt16LittleEndian(buf, u16);
                    var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 16, buf);
                    if (!suppressLog)
                        OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 16 , {u16}", ret);
                    return ret;
                }
                finally
                {
                    BufferPool.Return(buf, clearArray: false);
                }
            }
            if (value is byte b8)
            {
                var buf = BufferPool.Rent(1);
                try
                {
                    buf[0] = b8;
                    var ret = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, 8, buf);
                    if (!suppressLog)
                        OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , 8 , {b8}", ret);
                    return ret;
                }
                finally
                {
                    BufferPool.Return(buf, clearArray: false);
                }
            }

            var bytes = value switch
            {
                sbyte s8 => new[] { unchecked((byte)s8) },
                _ => throw new NotSupportedException($"不支持的写入类型：{value.GetType().Name}")
            };
            var bitLen = (ushort)(bytes.Length * 8);
            var retFallback = LTDMC.nmc_write_rxpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, bitLen, bytes);
            if (!suppressLog)
                OnCommandIssued("nmc_write_rxpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {value}", retFallback);
            return retFallback;
        }

        /// <summary>读 TxPDO 并解码（byte/sbyte/ushort/short/uint/int/byte[]），成功 0。</summary>
        private short ReadTxPdo<T>(ushort index, out T value, byte subIndex = LeadshineProtocolMap.SubIndex.Root, bool suppressLog = false)
        {
            value = default!;
            var bitLen = index switch
            {
                var i when i == LeadshineProtocolMap.Index.ControlWord => (ushort)LeadshineProtocolMap.BitLen.ControlWord,
                var i when i == LeadshineProtocolMap.Index.StatusWord => (ushort)LeadshineProtocolMap.BitLen.StatusWord,
                var i when i == LeadshineProtocolMap.Index.ModeOfOperation => (ushort)LeadshineProtocolMap.BitLen.ModeOfOperation,
                var i when i == LeadshineProtocolMap.Index.ActualVelocity => (ushort)LeadshineProtocolMap.BitLen.ActualVelocity,
                var i when i == LeadshineProtocolMap.Index.FeedConstant => 32,
                var i when i == LeadshineProtocolMap.Index.GearRatio => 32,
                _ => 0
            };
            if (bitLen == 0)
            {
                OnAxisFaulted(new InvalidOperationException($"Index 0x{index:X4} not mapped to BitLen."));
                return -1;
            }

            var byteLen = (bitLen + 7) / 8;
            var buf = BufferPool.Rent(byteLen);
            try
            {
                var ret = LTDMC.nmc_read_txpdo((ushort)_opts.Card, _opts.Port, _opts.NodeId, index, subIndex, (ushort)bitLen, buf);
                if (!suppressLog)
                    OnCommandIssued("nmc_read_txpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {byteLen}", ret);
                if (ret != 0)
                    return ret;

                object? boxed =
                    typeof(T) == typeof(byte) ? buf[0] :
                    typeof(T) == typeof(sbyte) ? unchecked((sbyte)buf[0]) :
                    typeof(T) == typeof(ushort) ? BitConverter.ToUInt16(buf, 0) :
                    typeof(T) == typeof(short) ? BitConverter.ToInt16(buf, 0) :
                    typeof(T) == typeof(uint) ? BitConverter.ToUInt32(buf, 0) :
                    typeof(T) == typeof(int) ? BitConverter.ToInt32(buf, 0) :
                    // When T is byte[], we return a new array copy for safety.
                    // Previous behavior may have returned the rented buffer directly, but this is unsafe with ArrayPool.
                    // Callers expecting the original buffer reference may be affected by this change.
                    typeof(T) == typeof(byte[]) ? buf.AsSpan(0, byteLen).ToArray() : null;

                if (boxed is null)
                {
                    OnAxisFaulted(new InvalidOperationException($"Unsupported target type {typeof(T).Name} for read 0x{index:X4}."));
                    return -2;
                }

                value = (T)boxed;

                if (!suppressLog)
                    OnCommandIssued("nmc_read_txpdo", $"{_opts.Card} , {_opts.Port} , {_opts.NodeId} , {index} , {subIndex} , {bitLen} , {byteLen}", 0,
                    note: $"decoded={boxed}");

                return 0;
            }
            finally
            {
                BufferPool.Return(buf, clearArray: false);
            }
        }

        // ---- 事件广播：逐订阅者、非阻塞、与调用方隔离 ----
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FireEachNonBlocking<T>(EventHandler<T>? multicast, object sender, T args)
        {
            if (multicast is null)
                return;
            foreach (var d in multicast.GetInvocationList())
            {
                var h = (EventHandler<T>)d;
                var state = new EvState<T>(sender, h, args);
                ThreadPool.UnsafeQueueUserWorkItem(static s =>
                {
                    var st = (EvState<T>)s!;
                    try
                    { st.Handler(st.Sender, st.Args); }
                    catch { /* 静默隔离 */ }
                }, state, preferLocal: true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<int> GetPprCachedAsync(CancellationToken ct)
        {
            var ready = Volatile.Read(ref _sPprReady);
            var v = ready ? Volatile.Read(ref _sPpr) : 0;
            return ValueTask.FromResult(v);
        }

        /// <summary>从负载侧 pps 发布速度反馈（事件载荷单位见注释）。</summary>
        private void PublishSpeedFeedbackFromActualPps(int actualPps, int pulsesPerRev)
        {
            var loadPpsVal = _opts.IsReverse ? -actualPps : actualPps;

            // 事件中 Rpm：维持“电机侧 rpm”的含义
            var rpmVal = 0m;
            if (pulsesPerRev > 0)
            {
                var motorPps = loadPpsVal * _opts.GearRatio;
                rpmVal = motorPps * 60m / pulsesPerRev;
            }

            // 事件中速度（mm/s）：采用几何直达口径（与 PingAsync 一致）
            var lpr = GetLinearPerRevolutionMm();
            var speedMmps = (pulsesPerRev > 0) ? LoadPpsToMmps(loadPpsVal, pulsesPerRev, lpr) : 0m;

            LastFeedbackMmps = speedMmps;

            var pps = (decimal)loadPpsVal; // 事件中直接使用负载侧 pps

            FireEachNonBlocking(SpeedFeedback, this,
                new AxisSpeedFeedbackEventArgs
                {
                    Axis = Axis,
                    Rpm = rpmVal,
                    SpeedMps = speedMmps,     // 注：该字段含义为 mm/s
                    PulsesPerSec = pps,
                    TimestampUtc = DateTime.UtcNow
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureNativeLibLoaded()
        {
            var probed = SNative.Value;
            if (probed.ok)
                return true;

            if (!_driverNotifiedOnce)
            {
                _driverNotifiedOnce = true;
                _status = DriverStatus.Disconnected;
                OnDriverNotLoaded("LTDMC.dll", probed.reason);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ToSwTicks(TimeSpan t) => (long)Math.Round(t.TotalSeconds * Stopwatch.Frequency);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldPublishFeedbackMmps(decimal mmps, long nowStamp)
        {
            var minGap = ToSwTicks(_feedbackMinInterval);
            var last = Volatile.Read(ref _lastFbStamp);
            if (nowStamp - last < minGap)
                return false;
            if (Math.Abs(mmps - _lastFbMmps) < FEEDBACK_DELTA_MMPS)
                return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CommitFeedbackMmps(decimal mmps, long nowStamp)
        {
            _lastFbMmps = mmps;
            Volatile.Write(ref _lastFbStamp, nowStamp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetError(int code, string? message)
        { LastErrorCode = code; LastErrorMessage = message; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetErrorFromRet(string action, int ret)
        { LastErrorCode = ret; LastErrorMessage = $"{action} failed, ret={ret}"; }

        // —— 便捷事件触发 ——
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnAxisFaulted(Exception ex) =>
            FireEachNonBlocking(AxisFaulted, this, new AxisErrorEventArgs { Axis = Axis, Exception = ex });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDriverNotLoaded(string lib, string msg) =>
            FireEachNonBlocking(DriverNotLoaded, this, new DriverNotLoadedEventArgs { LibraryName = lib, Message = msg });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnAxisDisconnected(string reason) =>
            FireEachNonBlocking(AxisDisconnected, this, new AxisDisconnectedEventArgs { Axis = Axis, Reason = reason });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnCommandIssued(AxisCommandIssuedEventArgs e) =>
            FireEachNonBlocking(CommandIssued, this, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnCommandIssued(string function, string argListWithSpaces, int result, string? note = null) =>
            FireEachNonBlocking(CommandIssued, this, new AxisCommandIssuedEventArgs
            {
                Axis = Axis,
                Invocation = $"{function}( {argListWithSpaces} )",
                Result = result,
                Timestamp = DateTimeOffset.Now,
                Note = note
            });
    }
}
