using System;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Protocol.Vendors.Guiwei {

    /// <summary>
    /// 归位编解码器：速度帧无控制码/长度/XOR，仅由起止符包裹，载荷为 N*int32 小端的速度（mm/s）。
    /// </summary>
    public sealed class GuiweiCodec : IUpstreamCodec {
        private readonly int _mainCount;
        private readonly int _ejectCount;

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
        public bool TryDecodeSpeed(ReadOnlySpan<byte> frame, out SpeedSet set) {
            set = default;
            if (frame.Length < 2) return false;
            if (frame[0] != GuiweiControl.Start || frame[^1] != GuiweiControl.End) return false;

            var payload = frame.Slice(1, frame.Length - 2);
            if (payload.Length % 4 != 0) return false;

            int n = payload.Length / 4;
            var all = new int[n];
            for (int i = 0, off = 0; i < n; i++, off += 4)
                all[i] = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4)); // mm/s

            var main = new int[Math.Min(_mainCount, n)];
            var eject = new int[Math.Min(_ejectCount, Math.Max(0, n - _mainCount))];

            Array.Copy(all, 0, main, 0, main.Length);
            if (eject.Length > 0) Array.Copy(all, _mainCount, eject, 0, eject.Length);

            set = new SpeedSet(DateTime.UtcNow, 0, main, eject);
            return true;
        }

        /// <inheritdoc />
        public bool TryDecodePositions(ReadOnlySpan<byte> frame, out IReadOnlyList<ParcelPose> poses) {
            poses = Array.Empty<ParcelPose>();
            return false; // 归位速度优先，位置/状态如需对接可扩展。
        }

        /// <inheritdoc />
        public bool TryDecodeStatus(ReadOnlySpan<byte> frame, out StatusSnapshot status) {
            status = new StatusSnapshot(false, 0, Array.Empty<(string, byte)>(), VisionAlarm.None);
            return false;
        }

        /// <inheritdoc />
        public bool TryDecodeParams(ReadOnlySpan<byte> frame, out VisionParams param) {
            param = new VisionParams();
            return false;
        }

        /// <inheritdoc />
        public int EncodeStartStop(IBufferWriter<byte> writer, bool start) {
            // 归位的控制报文为简约格式，按文档约定写入；此处留空壳，按需完善。:contentReference[oaicite:14]{index=14}
            return 0;
        }

        public int EncodeModeAndSpeed(IBufferWriter<byte> writer, byte mode, ushort maxMmps, ushort minMmps) => 0;

        public int EncodeSpacing(IBufferWriter<byte> writer, ushort spacingMm) => 0;

        public int EncodePause(IBufferWriter<byte> writer, bool pause) => 0;

        public int EncodeSetParams(IBufferWriter<byte> writer, byte ejectCount, ushort ejectMmps, byte autoStartDelaySec) => 0;

        public int EncodeQueryStatus(IBufferWriter<byte> writer) => 0;

        public int EncodeGetParams(IBufferWriter<byte> writer) => 0;
    }
}