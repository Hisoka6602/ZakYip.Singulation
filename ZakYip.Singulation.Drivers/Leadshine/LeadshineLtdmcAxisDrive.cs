using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Drivers.Abstractions.Events;

namespace ZakYip.Singulation.Drivers.Leadshine {

    /// <summary>
    /// 雷赛 LTDMC 实机轴驱动（EtherCAT）。
    /// - 通过 nmc_write_rxpdo / nmc_read_txpdo 直写/直读 PDO。
    /// - 通过 nmc_set_axis_contrlword 走 402 状态机使能。
    /// - 目标速度写 0x60FF（INT32），状态字读 0x6041。
    /// - 端口固定 2；从站 NodeID = 1000 + nodeIndex。:contentReference[oaicite:4]{index=4} :contentReference[oaicite:5]{index=5}
    /// </summary>
    public sealed class LeadshineLtdmcAxisDrive : IAxisDrive {
        private readonly ushort _card;
        private readonly ushort _axis;        // LTDMC 的“总线轴号”（从 1 开始）
        private readonly ushort _nodeId;      // EtherCAT NodeID = 1000 + nodeIndex
        private readonly DriverOptions _opts;
        private readonly double _rpmTo60FF;   // 单位换算：rpm -> 0x60FF 单位（常见=1，按现场配置调整）
        private readonly bool _isReverse;
        private readonly double _drumDiameterMm; // 单位：毫米(mm)
        private readonly double _gearRatio;//齿轮比
        private volatile DriverStatus _status = DriverStatus.Disconnected;
        private long _lastTicks;

        // —— 全局 PPR（只在 Enable 时读取一次）—— ★ 新增
        private static volatile int s_ppr;        // 0 表示未知

        private static volatile bool s_pprReady;  // 读取成功的一次性门闩

        // 可选：加/减速换算比例（RPM/s -> 设备单位，很多现场=1.0）
        private readonly decimal _accelTo6083 = 1.0m;

        private const ushort Port = 2;        // EtherCAT 端口固定为2 :contentReference[oaicite:6]{index=6}

        // —— 读缓存 & 复用缓冲区 ——
        // 读状态(0x6041)与实际速度(0x606C)的复用缓冲，避免重复分配
        private readonly byte[] _bufStatus = new byte[2];

        private readonly byte[] _bufVelocity = new byte[4];

        // —— 速度反馈节流控制 ——
        // 最小反馈间隔：避免淹没上层（可做成 DriverOptions 配置）
        private readonly TimeSpan _feedbackMinInterval = TimeSpan.FromMilliseconds(20);

        // 显著变化阈值：rpm 变化小于该阈值则静默（可做成 DriverOptions 配置）
        private const int FEEDBACK_DELTA_RPM = 5;

        private long _lastFeedbackTicks;   // 上次发出反馈的 UTC ticks
        private int _lastFeedbackRpm;      // 上次反馈的 rpm

        // —— PPR（脉冲/圈）缓存 ——
        // TTL：读取失败多时场景，减少反复访问；成功后 N 秒内复用
        private readonly TimeSpan _pprTtl = TimeSpan.FromSeconds(5);

        private int _cachedPpr;            // 0 表示未知/未取到
        private long _pprExpireTicks;      // 到期时间（UTC ticks）

        // —— 原生库可用性（仅探测一次）——
        private static readonly Lazy<(bool ok, string reason)> s_native = new(() => {
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

        public LeadshineLtdmcAxisDrive(ushort cardNo, ushort axisNo,
            ushort nodeIndex, AxisId axisId, DriverOptions opts,
            double rpmTo60ff = 1.0, bool isReverse = false,
            double gearRatio = 1.0,
            double drumDiameterMm = 76.0) {
            _card = cardNo;
            _axis = axisNo;
            _nodeId = (ushort)(1000 + nodeIndex);     // 1000+i 规则 :contentReference[oaicite:7]{index=7}
            Axis = axisId;
            _opts = opts ?? new DriverOptions();
            _rpmTo60FF = rpmTo60ff;
            _isReverse = isReverse;
            _gearRatio = gearRatio;
            _drumDiameterMm = drumDiameterMm;
        }

        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        public AxisId Axis { get; }
        public DriverStatus Status => _status;

        public async ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default) {
            await ThrottleAsync(ct);
            await WriteTargetVelocityFromRpmAsync(rpm.Value, ct);
        }

        public async ValueTask WriteSpeedAsync(double mmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 取 PPR（带缓存）
            var ppr = await GetPprCachedAsync(ct);
            int deviceVal;
            if (ppr > 0) {
                var pps = AxisRpm.MmPerSecToPps(mmPerSec, _drumDiameterMm, ppr, _gearRatio);
                Debug.WriteLine($"pps:{pps}");
                deviceVal = (int)Math.Round(pps);
            }
            else {
                // 退化（保持旧比例）
                var rpmVo = AxisRpm.FromMetersPerSecondMm(mmPerSec / 1000.0, _drumDiameterMm, _gearRatio);
                deviceVal = (int)Math.Round(rpmVo.Value * _rpmTo60FF);
            }

            if (_isReverse) deviceVal = -deviceVal;
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, deviceVal);
            if (ret != 0) { OnAxisFaulted(new InvalidOperationException($"write TargetVelocity failed, ret={ret}")); return; }

            _status = DriverStatus.Connected;
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
        public async ValueTask SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            static uint ClampU32(double v) => v <= 0 ? 0u : (uint)Math.Min(uint.MaxValue, Math.Round(v));

            // —— 优先：用 PPR 做物理换算 → counts/s²（pps²） ——
            int ppr = 0;
            try { ppr = await GetPprCachedAsync(ct); } catch { /* 忽略：不可得则走退化路径 */ }

            uint accDev, decDev;
            if (ppr > 0) {
                // pps² = ( rpm/s ÷ 60 ) × PPR
                var accPps2 = ((double)accelRpmPerSec / 60.0) * ppr;
                var decPps2 = ((double)decelRpmPerSec / 60.0) * ppr;
                accDev = ClampU32(accPps2);
                decDev = ClampU32(decPps2);
            }
            else {
                // 退化：保持现场比例（rpm/s → 设备单位）
                accDev = ClampU32((double)(accelRpmPerSec * _accelTo6083));
                decDev = ClampU32((double)(decelRpmPerSec * _accelTo6083));
            }

            var r1 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileAcceleration, accDev);
            if (r1 != 0) { OnAxisFaulted(new InvalidOperationException($"write ProfileAcceleration failed, ret={r1}")); return; }

            var r2 = WriteRxPdo(LeadshineProtocolMap.Index.ProfileDeceleration, decDev);
            if (r2 != 0) { OnAxisFaulted(new InvalidOperationException($"write ProfileDeceleration failed, ret={r2}")); return; }
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
            var dm = _drumDiameterMm / 1000.0;
            if (dm <= 0) {
                OnAxisFaulted(new InvalidOperationException("drum diameter must be > 0"));
                return;
            }

            // mm/s² → m/s²
            var accMps2 = accelMmPerSec / (decimal)1000.0;
            var decMps2 = decelMmPerSec / (decimal)1000.0;

            // m/s² → rpm/s ： rpm/s = (a / (π·D)) × 60
            var accRpmPerSec = accMps2 / (decimal)(Math.PI * dm) * (decimal)60.0;
            var decRpmPerSec = decMps2 / (decimal)(Math.PI * dm) * (decimal)60.0;

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

        public async ValueTask<bool> PingAsync(CancellationToken ct = default) {
            if (!EnsureNativeLibLoaded()) { _status = DriverStatus.Degraded; return false; }
            if (ReadTxPdo(LeadshineProtocolMap.Index.StatusWord, out ushort _) != 0) {
                _status = DriverStatus.Degraded;
                OnAxisDisconnected("Ping failed or device not responding");
                return false;
            }

            _status = DriverStatus.Connected;

            // 读实际速度（RPM）并发布反馈
            if (ReadTxPdo(LeadshineProtocolMap.Index.ActualVelocity, out int actualRpm) == 0) {
                if (_isReverse) actualRpm = -actualRpm;
                var now = DateTime.Now.Ticks;
                if (ShouldPublishFeedback(actualRpm, now)) {
                    int ppr = 0;
                    try { ppr = await GetPprCachedAsync(ct); } catch { /* 忽略 */ }
                    PublishSpeedFeedbackFromActualRpm(actualRpm, ppr);
                    CommitFeedbackBookkeeping(_isReverse ? -actualRpm : actualRpm, now);
                }
            }
            return true;
        }

        /// <summary>上电/使能（可选：调用后再写速度）</summary>
        public async ValueTask EnableAsync(CancellationToken ct = default) {
            // 节流，避免过快写总线
            await ThrottleAsync(ct);

            // 简化封装：写 ControlWord 并可选延时
            async Task<bool> WriteCtrlAsync(ushort value, int delayMs) {
                var r = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, BitConverter.GetBytes(value));
                if (r != 0) { OnAxisFaulted(new InvalidOperationException($"CtrlWord=0x{value:X4} failed, ret={r}")); return false; }
                if (delayMs > 0) await Task.Delay(delayMs, ct);
                return true;
            }

            // 0) 清除报警 → 拉回 0
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.FaultReset, LeadshineProtocolMap.DelayMs.AfterFaultReset)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.Clear, LeadshineProtocolMap.DelayMs.AfterClear)) return;

            // 1) 设置模式：速度模式 (PV=3)
            var m = WriteRxPdo(
                LeadshineProtocolMap.Index.ModeOfOperation,
                new[] { LeadshineProtocolMap.Mode.ProfileVelocity }
            );
            if (m != 0) { OnAxisFaulted(new InvalidOperationException("Set Mode=PV failed")); return; }
            await Task.Delay(LeadshineProtocolMap.DelayMs.AfterSetMode, ct);

            // 2) 402 状态机三步
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.Shutdown, LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.SwitchOn, LeadshineProtocolMap.DelayMs.BetweenStateCmds)) return;
            if (!await WriteCtrlAsync(LeadshineProtocolMap.ControlWord.EnableOperation, 0)) return;
            if (!s_pprReady) {
                try {
                    var ppr = await ReadAxisPulsesPerRevAsync(ct);
                    if (ppr > 0) {
                        Volatile.Write(ref s_ppr, ppr);
                        Volatile.Write(ref s_pprReady, true);
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
        }

        public async ValueTask DisableAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 1) 目标速度清零
            _ = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, 0);

            // 2) 写控制字 → 0x0007 (Switch On but disable operation)
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.ControlWord, LeadshineProtocolMap.ControlWord.SwitchOn);
            if (ret != 0) { OnAxisFaulted(new InvalidOperationException($"Disable failed: write ControlWord=0x0007, ret={ret}")); return; }

            await Task.Delay(LeadshineProtocolMap.DelayMs.BetweenStateCmds, ct);

            _status = DriverStatus.Disconnected;
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
                    _card,
                    Port,
                    _nodeId,
                    idx,
                    LeadshineProtocolMap.SubIndex.Numerator,
                    LeadshineProtocolMap.BitLen.FeedConstant,
                    ref numerator
                );
                if (retNum != 0) {
                    OnAxisFaulted(new InvalidOperationException(
                        $"read 0x{idx:X4}:01 (Numerator) failed, ret={retNum}"));
                    return 0;
                }

                // 读取 Denominator (0x6092:02)
                var retDen = LTDMC.nmc_get_node_od(
                    _card,
                    Port,
                    _nodeId,
                    idx,
                    LeadshineProtocolMap.SubIndex.Denominator,
                    LeadshineProtocolMap.BitLen.FeedConstant,
                    ref denominator
                );
                if (retDen != 0) {
                    OnAxisFaulted(new InvalidOperationException(
                        $"read 0x{idx:X4}:02 (Denominator) failed, ret={retDen}"));
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
            try { await DisableAsync(CancellationToken.None); }
            catch { /* 忽略收尾失败 */ }
        }

        private async ValueTask ThrottleAsync(CancellationToken ct) {
            var now = DateTime.Now.Ticks;
            var last = Interlocked.Read(ref _lastTicks);
            var gap = _opts.CommandMinInterval.Ticks;
            if (now - last < gap) {
                var wait = new TimeSpan(gap - (now - last));
                if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            }
            Interlocked.Exchange(ref _lastTicks, DateTime.Now.Ticks);
        }

        /// <summary>
        /// 按 Index 自动选择 BitLen，统一写 RxPDO。
        /// 这样上层调用只需要传 Index+值，避免重复定义多个 WriteXxx。
        /// </summary>
        private short WriteRxPdo(ushort index, object value, byte subIndex = LeadshineProtocolMap.SubIndex.Root) {
            ushort bitLen;
            switch (index) {
                case LeadshineProtocolMap.Index.ControlWord: bitLen = LeadshineProtocolMap.BitLen.ControlWord; break;
                case LeadshineProtocolMap.Index.StatusWord: bitLen = LeadshineProtocolMap.BitLen.StatusWord; break;
                case LeadshineProtocolMap.Index.ModeOfOperation: bitLen = LeadshineProtocolMap.BitLen.ModeOfOperation; break;
                case LeadshineProtocolMap.Index.TargetVelocity: bitLen = LeadshineProtocolMap.BitLen.TargetVelocity; break;
                case LeadshineProtocolMap.Index.ProfileAcceleration: bitLen = LeadshineProtocolMap.BitLen.ProfileAcceleration; break;
                case LeadshineProtocolMap.Index.ProfileDeceleration: bitLen = LeadshineProtocolMap.BitLen.ProfileDeceleration; break;
                default:
                    OnAxisFaulted(new InvalidOperationException($"Index 0x{index:X4} not mapped to BitLen."));
                    return -1;
            }

            ReadOnlySpan<byte> payload = value switch {
                ushort v => BitConverter.GetBytes(v),
                short v => BitConverter.GetBytes(v),
                int v => BitConverter.GetBytes(v),
                uint v => BitConverter.GetBytes(v),
                byte v => [v],
                sbyte v => [unchecked((byte)v)],
                byte[] v => v,
                _ => null
            };

            if (payload == default) {
                OnAxisFaulted(new InvalidOperationException($"Unsupported type {value.GetType().Name} for index 0x{index:X4}"));
                return -2;
            }

            try {
                return LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, index, subIndex, bitLen, payload.ToArray());
            }
            catch (DllNotFoundException ex) {
                if (!_driverNotifiedOnce) { _driverNotifiedOnce = true; OnDriverNotLoaded("LTDMC.dll", ex.Message); }
                _status = DriverStatus.Degraded;
                return short.MinValue; // 统一异常返回码
            }
            catch (EntryPointNotFoundException ex) {
                if (!_driverNotifiedOnce) { _driverNotifiedOnce = true; OnDriverNotLoaded("LTDMC.dll", $"入口缺失：{ex.Message}"); }
                _status = DriverStatus.Degraded;
                return short.MinValue;
            }
        }

        /// <summary>
        /// 读 TxPDO 并解码到指定类型；成功返回 0，失败返回 ret（不抛异常）。
        /// 支持 byte/sbyte/ushort/short/uint/int/byte[]。
        /// </summary>
        private short ReadTxPdo<T>(ushort index, out T value, byte subIndex = LeadshineProtocolMap.SubIndex.Root) {
            value = default!;

            // 1) 映射位宽
            ushort bitLen;
            switch (index) {
                case LeadshineProtocolMap.Index.StatusWord: bitLen = (ushort)LeadshineProtocolMap.BitLen.StatusWord; break;
                case LeadshineProtocolMap.Index.ModeOfOperation: bitLen = (ushort)LeadshineProtocolMap.BitLen.ModeOfOperation; break;
                case LeadshineProtocolMap.Index.ActualVelocity: bitLen = (ushort)LeadshineProtocolMap.BitLen.ActualVelocity; break;
                case LeadshineProtocolMap.Index.FeedConstant: bitLen = 32; break; // :01/:02 一般 32bit
                case LeadshineProtocolMap.Index.GearRatio: bitLen = 32; break;
                default:
                    OnAxisFaulted(new InvalidOperationException($"Index 0x{index:X4} not mapped to BitLen."));
                    return -1;
            }

            // 2) 调用底层读取
            var byteLen = (bitLen + 7) / 8;
            var buf = new byte[byteLen];
            var ret = LTDMC.nmc_read_txpdo(_card, Port, _nodeId, index, subIndex, bitLen, buf);
            if (ret != 0) return ret;

            // 3) 小端解码
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
            var v = Volatile.Read(ref s_pprReady) ? Volatile.Read(ref s_ppr) : 0;
            return ValueTask.FromResult(v);
        }

        private void PublishSpeedFeedbackFromActualRpm(int actualRpm, int pulsesPerRev) {
            // 1) 方向一致性：仅在入口处做一次符号
            var rpmVal = _isReverse ? -actualRpm : actualRpm;

            // 2) 用 AxisRpm 统一做换算
            var rpm = new AxisRpm(rpmVal);

            // m/s（直径用 mm 版本；若有传动比，传 gearRatio）
            var speedMps = rpm.ToMetersPerSecByMm(_drumDiameterMm /*, gearRatio: 1.0 */);

            // pps（需 PPR；未知时返回 0）
            var pps = pulsesPerRev > 0 ? rpm.ToPulsePerSec(pulsesPerRev) : 0;

            // 3) 非阻塞广播
            FireEachNonBlocking(SpeedFeedback, this,
                new AxisSpeedFeedbackEventArgs(Axis, rpmVal, speedMps, pps, DateTime.Now));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EnoughInterval(long lastTicks, long nowTicks, long minGapTicks)
            => nowTicks - lastTicks >= minGapTicks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldPublishFeedback(int rpm, long nowTicks) {
            // 1) 时间节流
            var lastTicks = Volatile.Read(ref _lastFeedbackTicks);
            if (!EnoughInterval(lastTicks, nowTicks, _feedbackMinInterval.Ticks))
                return false;

            // 2) 显著变化阈值
            if (Math.Abs(rpm - _lastFeedbackRpm) < FEEDBACK_DELTA_RPM)
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CommitFeedbackBookkeeping(int rpm, long nowTicks) {
            _lastFeedbackRpm = rpm;
            Volatile.Write(ref _lastFeedbackTicks, nowTicks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureNativeLibLoaded() {
            var probed = s_native.Value;     // 只在第一次初始化时做昂贵工作
            if (probed.ok) return true;

            if (!_driverNotifiedOnce) {
                _driverNotifiedOnce = true;
                _status = DriverStatus.Disconnected;
                OnDriverNotLoaded("LTDMC.dll", probed.reason);
            }
            return false;
        }

        private async ValueTask<bool> WriteTargetVelocityFromRpmAsync(double rpm, CancellationToken ct) {
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
                deviceVal = (int)Math.Round(targetRpm * _rpmTo60FF);
            }

            if (_isReverse) deviceVal = -deviceVal;

            var ret = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, deviceVal);
            if (ret != 0) { OnAxisFaulted(new InvalidOperationException($"write TargetVelocity failed, ret={ret}")); return false; }

            _status = DriverStatus.Connected;
            return true;
        }

        //用便捷封装
        private void OnAxisFaulted(Exception ex) =>
            FireEachNonBlocking(AxisFaulted, this, new AxisErrorEventArgs(Axis, ex));

        private void OnDriverNotLoaded(string lib, string msg) =>
            FireEachNonBlocking(DriverNotLoaded, this, new DriverNotLoadedEventArgs(lib, msg));

        private void OnAxisDisconnected(string reason) =>
            FireEachNonBlocking(AxisDisconnected, this, new AxisDisconnectedEventArgs(Axis, reason));
    }

    // LTDMC extern 函数（如果你已有 LTDMC.cs，这段可以省略）
    internal static class LTDMC {

        [System.Runtime.InteropServices.DllImport("LTDMC.dll")]
        public static extern short nmc_write_rxpdo(ushort cardno, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);

        [System.Runtime.InteropServices.DllImport("LTDMC.dll")]
        public static extern short nmc_read_txpdo(ushort cardNo, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);

        [System.Runtime.InteropServices.DllImport("LTDMC.dll")]
        public static extern short nmc_get_node_od(ushort cardNo, ushort portNum, ushort nodenum, ushort index, ushort subindex, ushort valuelength, ref int value);
    }
}