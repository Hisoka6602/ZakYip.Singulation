using System;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Utils;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Protocol.Vendors.Guiwei {

    /// <summary>
    /// 归位编解码器：速度帧无控制码/长度/XOR，仅由起止符包裹，载荷为 N*int32 小端的速度（mm/s）。
    /// </summary>
    public sealed class GuiweiCodec : IUpstreamCodec {
        private int _mainCount;
        private int _ejectCount;

        /// <summary>
        /// 使用指定的分离段/扩散段数量创建编解码器。
        /// </summary>
        /// <param name="mainCount">分离段电机数量（例如 28）。</param>
        /// <param name="ejectCount">扩散电机数量（例如 1）。</param>
        public GuiweiCodec(int mainCount, int ejectCount) {
            _mainCount = mainCount;
            _ejectCount = ejectCount;
        }

        /// <inheritdoc />
        public bool TryDecodeSpeed(ReadOnlySpan<byte> frame, DateTime timestamp, out SpeedSet set) {
            set = default;
            if (frame.Length < 2) return false;
            if (frame[0] != GuiweiControl.Start || frame[^1] != GuiweiControl.End) return false;

            var payload = frame.Slice(1, frame.Length - 2);
            if (payload.Length % 4 != 0) return false;

            int n = payload.Length / 4;
            
            // 使用 ArrayPool 减少临时数组的 GC 压力
            var all = ArrayPool<int>.Shared.Rent(n);
            try {
                for (int i = 0, off = 0; i < n; i++, off += 4)
                    all[i] = ByteUtils.ReadInt32LittleEndian(payload.Slice(off, 4)); // mm/s

                var main = new int[Math.Min(_mainCount, n)];
                var eject = new int[Math.Min(_ejectCount, Math.Max(0, n - _mainCount))];

                Array.Copy(all, 0, main, 0, main.Length);
                if (eject.Length > 0) Array.Copy(all, _mainCount, eject, 0, eject.Length);

                set = new SpeedSet(timestamp, 0, main, eject);
                return true;
            }
            finally {
                ArrayPool<int>.Shared.Return(all);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// 归位协议不支持位置帧，始终返回 false。
        /// 如需对接位置信息，可扩展协议或使用华雷协议。
        /// </remarks>
        public bool TryDecodePositions(ReadOnlySpan<byte> frame, out IReadOnlyList<ParcelPose> poses) {
            poses = [];
            return false;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 归位协议不支持状态帧，始终返回 false。
        /// </remarks>
        public bool TryDecodeStatus(ReadOnlySpan<byte> frame, out StatusSnapshot status) {
            status = new StatusSnapshot(false, 0, [], VisionAlarm.None);
            return false;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 归位协议不支持参数查询帧，始终返回 false。
        /// </remarks>
        public bool TryDecodeParams(ReadOnlySpan<byte> frame, out VisionParams param) {
            param = new VisionParams();
            return false;
        }

        /// <inheritdoc />
        /// <remarks>
        /// 归位协议的控制报文为简约格式，当前实现未定义具体格式。
        /// 如需启动/停止控制，建议通过其他通道或扩展协议。
        /// </remarks>
        public int EncodeStartStop(IBufferWriter<byte> writer, bool start) {
            
            return 0;
        }

        /// <summary>
        /// 编码模式和速度设置命令（当前未实现）。
        /// </summary>
        /// <param name="writer">目标缓冲区写入器。</param>
        /// <param name="mode">分离模式。</param>
        /// <param name="maxMmps">最大速度（mm/s）。</param>
        /// <param name="minMmps">最小速度（mm/s）。</param>
        /// <returns>写入的字节数（当前返回 0）。</returns>
        public int EncodeModeAndSpeed(IBufferWriter<byte> writer, byte mode, ushort maxMmps, ushort minMmps) => 0;

        /// <summary>
        /// 编码分离间距设置命令（当前未实现）。
        /// </summary>
        /// <param name="writer">目标缓冲区写入器。</param>
        /// <param name="spacingMm">分离间距（mm）。</param>
        /// <returns>写入的字节数（当前返回 0）。</returns>
        public int EncodeSpacing(IBufferWriter<byte> writer, ushort spacingMm) => 0;

        /// <summary>
        /// 编码暂停命令（当前未实现）。
        /// </summary>
        /// <param name="writer">目标缓冲区写入器。</param>
        /// <param name="pause">true 表示暂停，false 表示恢复。</param>
        /// <returns>写入的字节数（当前返回 0）。</returns>
        public int EncodePause(IBufferWriter<byte> writer, bool pause) => 0;

        /// <summary>
        /// 编码参数设置命令（当前未实现）。
        /// </summary>
        /// <param name="writer">目标缓冲区写入器。</param>
        /// <param name="ejectCount">扩散段数量。</param>
        /// <param name="ejectMmps">扩散段速度（mm/s）。</param>
        /// <param name="autoStartDelaySec">自动启动延迟（秒）。</param>
        /// <returns>写入的字节数（当前返回 0）。</returns>
        public int EncodeSetParams(IBufferWriter<byte> writer, byte ejectCount, ushort ejectMmps, byte autoStartDelaySec) => 0;

        /// <summary>
        /// 编码状态查询命令（当前未实现）。
        /// </summary>
        /// <param name="writer">目标缓冲区写入器。</param>
        /// <returns>写入的字节数（当前返回 0）。</returns>
        public int EncodeQueryStatus(IBufferWriter<byte> writer) => 0;

        /// <summary>
        /// 编码参数查询命令（当前未实现）。
        /// </summary>
        /// <param name="writer">目标缓冲区写入器。</param>
        /// <returns>写入的字节数（当前返回 0）。</returns>
        public int EncodeGetParams(IBufferWriter<byte> writer) => 0;

        /// <summary>
        /// 动态更新编解码器的轴布局配置。
        /// </summary>
        /// <param name="mainCount">分离段电机数量，必须 >= 0。</param>
        /// <param name="ejectCount">扩散段电机数量，必须 >= 0。</param>
        /// <exception cref="ArgumentOutOfRangeException">当参数为负数时抛出。</exception>
        /// <remarks>
        /// 此方法是线程安全的，使用 Volatile.Write 确保内存可见性。
        /// 更新后的配置将在下次解码时生效。
        /// </remarks>
        public void SetAxisLayout(int mainCount, int ejectCount) {
            if (mainCount < 0 || ejectCount < 0)
                throw new ArgumentOutOfRangeException(nameof(mainCount), "Counts must be >= 0.");

            Volatile.Write(ref _mainCount, mainCount);
            Volatile.Write(ref _ejectCount, ejectCount);
        }

        /// <summary>
        /// 设置网格布局（当前实现为透传，不做转换）。
        /// </summary>
        /// <param name="source">源速度列表。</param>
        /// <param name="xCount">X 方向数量（未使用）。</param>
        /// <param name="enabled">是否启用（未使用）。</param>
        /// <returns>原样返回源速度列表。</returns>
        /// <remarks>
        /// 归位协议使用简单的线性布局，不需要网格转换。
        /// 此方法保留用于接口兼容性。
        /// </remarks>
        public IReadOnlyList<int> SetGridLayout(IReadOnlyList<int> source, int xCount, bool enabled = true) {
            return source;
        }

        public SpeedSet SetGridLayout(SpeedSet source, int xCount, bool enabled = true) {
            return source;
        }
    }
}