using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private volatile DriverStatus _status = DriverStatus.Disconnected;
        private long _lastTicks;

        // 可选：加/减速换算比例（RPM/s -> 设备单位，很多现场=1.0）
        private readonly decimal _accelTo6083 = 1.0m;

        private const ushort Port = 2;        // EtherCAT 端口固定为2 :contentReference[oaicite:6]{index=6}

        public LeadshineLtdmcAxisDrive(ushort cardNo, ushort axisNo, ushort nodeIndex, AxisId axisId, DriverOptions opts, double rpmTo60ff = 1.0) {
            _card = cardNo;
            _axis = axisNo;
            _nodeId = (ushort)(1000 + nodeIndex);     // 1000+i 规则 :contentReference[oaicite:7]{index=7}
            Axis = axisId;
            _opts = opts ?? new DriverOptions();
            _rpmTo60FF = rpmTo60ff;
        }

        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        public AxisId Axis { get; }
        public DriverStatus Status => _status;

        public async ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 1) 限幅
            var max = Math.Abs(_opts.MaxRpm);
            var target = Math.Max(-max, Math.Min(max, rpm.Value));
            // 2) 单位换算 -> INT32（0x60FF）
            var vel = (int)Math.Round(target * _rpmTo60FF);

            // 3) 写 0x60FF
            var ret = WriteRxPdo(LeadshineProtocolMap.Index.TargetVelocity, vel);
            if (ret != 0) { OnAxisFaulted(new InvalidOperationException($"write TargetVelocity failed, ret={ret}")); return; }

            _status = DriverStatus.Connected;
        }

        public async ValueTask SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            static uint ToU32(decimal v) => v <= 0 ? 0u : (uint)Math.Min(uint.MaxValue, Math.Round(v));

            var r1 = WriteRxPdo(
                LeadshineProtocolMap.Index.ProfileAcceleration,
                ToU32(accelRpmPerSec * _accelTo6083));
            if (r1 != 0) { OnAxisFaulted(new InvalidOperationException($"write ProfileAcceleration failed, ret={r1}")); return; }

            var r2 = WriteRxPdo(
                LeadshineProtocolMap.Index.ProfileDeceleration,
                ToU32(decelRpmPerSec * _accelTo6083));
            if (r2 != 0) { OnAxisFaulted(new InvalidOperationException($"write ProfileDeceleration failed, ret={r2}")); return; }
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
            // 读 0x6041 状态字来判断在线（TxPDO；16bit）
            var buf = new byte[2];
            var ret = LTDMC.nmc_read_txpdo(_card, Port, _nodeId,
                LeadshineProtocolMap.Index.StatusWord, 0,
                LeadshineProtocolMap.BitLen.StatusWord, buf);
            if (ret == 0) {
                _status = DriverStatus.Connected;
                return true;
            }
            _status = DriverStatus.Degraded;
            OnAxisDisconnected("Ping failed or device not responding");
            return false;
        }

        /// <summary>上电/使能（可选：调用后再写速度）</summary>
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
        /// 读取“轴的一圈脉冲量”
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask<int> ReadAxisPulsesPerRevAsync(CancellationToken ct = default) {
            return 0;
        }

        public async ValueTask DisposeAsync() {
            try { await DisableAsync(CancellationToken.None); }
            catch { /* 忽略收尾失败 */ }
        }

        private async ValueTask ThrottleAsync(CancellationToken ct) {
            var now = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read(ref _lastTicks);
            var gap = _opts.CommandMinInterval.Ticks;
            if (now - last < gap) {
                var wait = new TimeSpan(gap - (now - last));
                if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            }
            Interlocked.Exchange(ref _lastTicks, DateTime.UtcNow.Ticks);
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

            return LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, index, subIndex, bitLen, payload.ToArray());
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

        // 你的三种事件：用便捷封装
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
        public static extern short nmc_read_txpdo(ushort CardNo, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);
    }
}