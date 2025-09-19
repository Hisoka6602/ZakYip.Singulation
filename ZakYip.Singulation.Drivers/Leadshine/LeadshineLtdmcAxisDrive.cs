using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions;
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
        private const ushort IDX_CTRLWORD = 0x6040;
        private const ushort IDX_STATUS = 0x6041;
        private const ushort IDX_TGT_VEL = 0x60FF; // 标准 DS402 目标速度（若未映射，请在 Motion 里映射到 RxPDO）
        private const ushort IDX_CTRL = 0x6040;  // 控制字
        private const ushort IDX_MODE = 0x6060;  // 模式设定（int8，PV=3）
        private const ushort IDX_TGTVEL = 0x60FF;  // 目标速度（int32）
        private const ushort IDX_PROF_ACCEL = 0x6083; // Profile Acceleration
        private const ushort IDX_PROF_DECEL = 0x6084; // Profile Deceleration

        public LeadshineLtdmcAxisDrive(ushort cardNo, ushort axisNo, ushort nodeIndex, AxisId axisId, DriverOptions opts, double rpmTo60ff = 1.0) {
            _card = cardNo;
            _axis = axisNo;
            _nodeId = (ushort)(1000 + nodeIndex);     // 1000+i 规则 :contentReference[oaicite:7]{index=7}
            Axis = axisId;
            _opts = opts ?? new DriverOptions();
            _rpmTo60FF = rpmTo60ff;
        }

        public AxisId Axis { get; }
        public DriverStatus Status => _status;

        public async ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 1) 限幅
            var max = Math.Abs(_opts.MaxRpm);
            var target = Math.Max(-max, Math.Min(max, rpm.Value));
            // 2) 单位换算 -> INT32（0x60FF）
            var vel = (int)Math.Round(target * _rpmTo60FF);
            var bytes = BitConverter.GetBytes(vel); // 小端

            // 3) 写 0x60FF (32bit)
            var ret = LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, IDX_TGT_VEL, 0, 32, bytes);
            if (ret != 0) throw new InvalidOperationException($"write 0x60FF failed, ret={ret}");

            _status = DriverStatus.Connected;
        }

        public async ValueTask SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 量化到无符号32位（常见：Profile Accel/Decel 为 U32）
            static uint ToU32(decimal v) => v <= 0 ? 0u : (uint)Math.Min(uint.MaxValue, Math.Round(v));

            var acc = BitConverter.GetBytes(ToU32(accelRpmPerSec * _accelTo6083));
            var dec = BitConverter.GetBytes(ToU32(decelRpmPerSec * _accelTo6083));

            var r1 = LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, IDX_PROF_ACCEL, 0, 32, acc);
            if (r1 != 0) throw new InvalidOperationException($"write 0x6083 failed, ret={r1}");

            var r2 = LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, IDX_PROF_DECEL, 0, 32, dec);
            if (r2 != 0) throw new InvalidOperationException($"write 0x6084 failed, ret={r2}");
        }

        public async ValueTask StopAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // 两种策略：A. 将目标速度置 0；B. 控制字 QuickStop（不同驱动 QuickStop 行为可能需要参数）
            // 先 A 再 B 更稳妥
            var zero = BitConverter.GetBytes(0);
            _ = LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, IDX_TGT_VEL, 0, 32, zero);

            // Quick stop：bit2=1（0x0002）
            var cw = BitConverter.GetBytes((ushort)0x0002); // 0000 0010b：请求 Quick Stop
            var ret = LTDMC.nmc_write_rxpdo(_card, Port /*=2*/, _nodeId, 0x6040, 0, 16, cw);
            if (ret != 0) throw new InvalidOperationException($"QuickStop 0x0002 failed, ret={ret}");

            _status = DriverStatus.Connected;
        }

        public async ValueTask<bool> PingAsync(CancellationToken ct = default) {
            // 读 0x6041 状态字来判断在线（TxPDO；16bit）
            var buf = new byte[2];
            var ret = LTDMC.nmc_read_txpdo(_card, Port, _nodeId, IDX_STATUS, 0, 16, buf);
            if (ret == 0) {
                _status = DriverStatus.Connected;
                return true;
            }
            _status = DriverStatus.Degraded;
            return false;
        }

        /// <summary>上电/使能（可选：调用后再写速度）</summary>
        public async ValueTask EnableAsync(CancellationToken ct = default) {
            // 402 状态机：先设模式→再上电/切态
            await ThrottleAsync(ct);
            short WriteU16(ushort index, ushort value)
                => LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, index, 0, 16, BitConverter.GetBytes(value));
            short WriteU8(ushort index, byte value)
                => LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, index, 0, 8, [value]);

            // 0)清除报警
            var r = WriteU16(IDX_CTRL, 0x0080);              // Fault Reset
            if (r != 0) throw new InvalidOperationException($"FaultReset(0x0080) failed, ret={r}");
            await Task.Delay(10, ct);
            r = WriteU16(IDX_CTRL, 0x0000);                  // 拉回 0
            if (r != 0) throw new InvalidOperationException($"CtrlWord=0x0000 failed, ret={r}");
            await Task.Delay(5, ct);

            // 1) 速度模式：0x6060 = 3 (PV / Profile Velocity)，8 bit
            r = WriteU8(IDX_MODE, 3);
            if (r != 0) throw new InvalidOperationException($"Set 0x6060=3 failed, ret={r}");
            await Task.Delay(5, ct);

            // 2) 402 控制字序列（全用 0x6040 写 16bit）
            r = WriteU16(IDX_CTRL, 0x0006);                  // Shutdown
            if (r != 0) throw new InvalidOperationException($"Shutdown(0x0006) failed, ret={r}");
            await Task.Delay(10, ct);

            r = WriteU16(IDX_CTRL, 0x0007);                  // Switch On
            if (r != 0) throw new InvalidOperationException($"SwitchOn(0x0007) failed, ret={r}");
            await Task.Delay(10, ct);

            r = WriteU16(IDX_CTRL, 0x000F);                  // Enable Operation
            if (r != 0) throw new InvalidOperationException($"EnableOp(0x000F) failed, ret={r}");

            _status = DriverStatus.Connected;              // 或 Online，按你的枚举定义
        }

        public async ValueTask DisableAsync(CancellationToken ct = default) {
            await ThrottleAsync(ct);

            // —— 1) 目标速度清零（防止残余速度命令在再次上电时拉起）——
            // 若现场把 0x60FF 映射为 16 位，请用 bitlength=16（2字节）；若是 32 位把下面两行改成 32 位。
            var zeroVel16 = BitConverter.GetBytes((short)0);
            _ = LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, 0x60FF, 0, 16, zeroVel16);
            // 如果现场是 32 位：_ = LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, 0x60FF, 0, 32, BitConverter.GetBytes(0));

            // —— 2) 写控制字为 0（0x6040 = 0x0000）→ 关闭电压/退出运行态 ——
            var cw0 = BitConverter.GetBytes((ushort)0x0000);   // 16bit，小端 2 字节
            var ret = LTDMC.nmc_write_rxpdo(_card, Port, _nodeId, 0x6040, 0, 16, cw0);

            if (ret != 0) throw new InvalidOperationException($"Disable failed: write 0x6040=0x0000, ret={ret}");
            await Task.Delay(10, ct);

            _status = DriverStatus.Disconnected;
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
    }

    // LTDMC extern 函数（如果你已有 LTDMC.cs，这段可以省略）
    internal static class LTDMC {

        [System.Runtime.InteropServices.DllImport("LTDMC.dll")]
        public static extern short nmc_write_rxpdo(ushort cardno, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);

        [System.Runtime.InteropServices.DllImport("LTDMC.dll")]
        public static extern short nmc_read_txpdo(ushort CardNo, ushort portnum, ushort slave_station_addr, ushort index, ushort subindex, ushort bitlength, byte[] data);
    }
}