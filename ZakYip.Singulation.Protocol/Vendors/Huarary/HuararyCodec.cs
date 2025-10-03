using System;
using System.Linq;
using System.Text;
using System.Buffers;
using Newtonsoft.Json;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Protocol.Vendors.Huarary {

    /// <summary>
    /// 华睿编解码器：按控制码分派不同语义的解析；速度值为 int32 小端（mm/s）。
    /// 同时提供各命令的编码（固定长度帧含 XOR 校验）。
    /// </summary>
    public sealed class HuararyCodec : IUpstreamCodec {
        private int _mainCount;
        private int _ejectCount;
        private int _xCount;

        /// <summary>
        /// 使用指定的分离段/疏散段数量创建编解码器。
        /// </summary>
        /// <param name="mainCount">分离段电机数量（例如 28）。</param>
        /// <param name="ejectCount">疏散/扩散电机数量（例如 3）。</param>
        public HuararyCodec(int mainCount, int ejectCount) {
            _mainCount = mainCount;
            _ejectCount = ejectCount;
        }

        /// <inheritdoc />
        public bool TryDecodeSpeed(ReadOnlySpan<byte> frame, out SpeedSet set) {
            set = default;
            if (frame.Length < 1 + 1 + 2 + 1 + 1) return false;
            if (frame[0] != HuararyControl.Start || frame[^1] != HuararyControl.End) return false;

            var ctrl = frame[1];
            if (ctrl != HuararyControl.CtrlSpeed) return false;

            // 长度一致性
            var len = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(2, 2));
            if (len != frame.Length) return false;

            // XOR 校验（从起始到 payload 末尾）
            var checksumIndex = frame.Length - 2;
            byte xor = 0;
            for (int i = 0; i < checksumIndex; i++) xor ^= frame[i];
            if (xor != frame[checksumIndex]) return false;

            // 载荷 = [起始(1) 控制(1) 长度(2)] ... [XOR(1) 结束(1)]
            var payload = frame.Slice(4, frame.Length - 6);
            if (payload.Length % 4 != 0) return false;

            var n = payload.Length / 4;
            var all = new int[n];
            for (int i = 0, off = 0; i < n; i++, off += 4)
                all[i] = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4)); // mm/s

            var main = new int[Math.Min(_mainCount, n)];
            var eject = new int[Math.Min(_ejectCount, Math.Max(0, n - _mainCount))];

            Array.Copy(all, 0, main, 0, main.Length);
            if (eject.Length > 0) Array.Copy(all, _mainCount, eject, 0, eject.Length);

            set = new SpeedSet(DateTime.Now, 0, main, eject);
            return true;
        }

        /// <inheritdoc />
        public bool TryDecodePositions(ReadOnlySpan<byte> frame, out IReadOnlyList<ParcelPose> poses) {
            poses = [];
            if (frame.Length < 6) return false;
            if (frame[0] != HuararyControl.Start || frame[^1] != HuararyControl.End) return false;
            if (frame[1] != HuararyControl.CtrlPos) return false;

            var len = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(2, 2));
            if (len != frame.Length) return false;

            var checksumIndex = frame.Length - 2;
            byte xor = 0;
            for (var i = 0; i < checksumIndex; i++) xor ^= frame[i];
            if (xor != frame[checksumIndex]) return false;

            var payload = frame.Slice(4, frame.Length - 6);
            if (payload.Length < 4) return false;

            var off = 0;
            var count = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4)); off += 4;

            var list = new List<ParcelPose>(count);
            for (var i = 0; i < count; i++) {
                if (off + 5 * 4 > payload.Length) break;
                var x = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))); off += 4;
                var y = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))); off += 4;
                var l = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))); off += 4;
                var w = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))); off += 4;
                var a = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off, 4))); off += 4;
                list.Add(new ParcelPose(x, y, l, w, a));
            }

            poses = list;
            return true;
        }

        /// <inheritdoc />
        public bool TryDecodeStatus(ReadOnlySpan<byte> frame, out StatusSnapshot status) {
            // 状态响应 Ctrl=0x5B；包含运行态、报警、相机 FPS/列表等。:contentReference[oaicite:4]{index=4}
            status = new StatusSnapshot(false, 0, Array.Empty<(string, byte)>(), VisionAlarm.None);
            // 这里给出骨架，细字段按需要完整展开。
            return false;
        }

        /// <inheritdoc />
        public bool TryDecodeParams(ReadOnlySpan<byte> frame, out VisionParams param) {
            // 参数响应 Ctrl=0x5C；端口/疏散数量/速度/延时/版本号等。:contentReference[oaicite:5]{index=5}
            param = new VisionParams();
            // 这里给出骨架，字段解析可按需要扩展。
            return false;
        }

        /// <inheritdoc />
        public int EncodeStartStop(IBufferWriter<byte> writer, bool start) {
            // 固定长度10字节：2A 89 0A 00 00 00 00 [01/00] XOR 3B。:contentReference[oaicite:6]{index=6}
            return EncodeFixed10(writer, HuararyControl.CtrlStartStop, start ? (byte)0x01 : (byte)0x00);
        }

        /// <inheritdoc />
        public int EncodeModeAndSpeed(IBufferWriter<byte> writer, byte mode, ushort maxMmps, ushort minMmps) {
            // 固定长度11字节：2A 84 0B 00 [mode] [maxLE2] [minLE2] XOR 3B。:contentReference[oaicite:7]{index=7}
            var span = writer.GetSpan(11);
            span[0] = HuararyControl.Start;
            span[1] = HuararyControl.CtrlModeSpeed;
            span[2] = 0x0B; span[3] = 0x00;
            span[4] = mode;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(5, 2), maxMmps);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(7, 2), minMmps);
            byte xor = 0;
            for (var i = 0; i < 9; i++) xor ^= span[i];
            span[9] = xor;
            span[10] = HuararyControl.End;
            writer.Advance(11);
            return 11;
        }

        /// <inheritdoc />
        public int EncodeSpacing(IBufferWriter<byte> writer, ushort spacingMm) {
            // 固定长度10字节，分离模式字节固定0x02：2A 86 0A 00 [mmLE2] 02 00 XOR 3B。:contentReference[oaicite:8]{index=8}
            var span = writer.GetSpan(10);
            span[0] = HuararyControl.Start;
            span[1] = HuararyControl.CtrlSpacing;
            span[2] = 0x0A; span[3] = 0x00;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4, 2), spacingMm);
            span[6] = 0x02; // 分离模式
            span[7] = 0x00; // 备用
            byte xor = 0; for (var i = 0; i < 8; i++) xor ^= span[i];
            span[8] = xor;
            span[9] = HuararyControl.End;
            writer.Advance(10);
            return 10;
        }

        /// <inheritdoc />
        public int EncodePause(IBufferWriter<byte> writer, bool pause) {
            // 固定长度10字节：2A 85 0A 00 00 00 00 [01/00] XOR 3B。:contentReference[oaicite:9]{index=9}
            return EncodeFixed10(writer, HuararyControl.CtrlPause, pause ? (byte)0x01 : (byte)0x00);
        }

        /// <inheritdoc />
        public int EncodeSetParams(IBufferWriter<byte> writer, byte ejectCount, ushort ejectMmps, byte autoStartDelaySec) {
            // 固定长度10字节：2A 83 0A 00 [count] [mmLE2] [delay] XOR 3B。:contentReference[oaicite:10]{index=10}
            var span = writer.GetSpan(10);
            span[0] = HuararyControl.Start;
            span[1] = HuararyControl.CtrlSetParams;
            span[2] = 0x0A; span[3] = 0x00;
            span[4] = ejectCount;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(5, 2), ejectMmps);
            span[7] = autoStartDelaySec;
            byte xor = 0; for (var i = 0; i < 8; i++) xor ^= span[i];
            span[8] = xor;
            span[9] = HuararyControl.End;
            writer.Advance(10);
            return 10;
        }

        /// <inheritdoc />
        public int EncodeQueryStatus(IBufferWriter<byte> writer) {
            // 固定长度10字节：2A 87 0A 00 00 00 00 00 XOR 3B（响应为 0x5B）。:contentReference[oaicite:11]{index=11}
            return EncodeFixed10(writer, HuararyControl.CtrlQueryStatus, 0x00);
        }

        /// <inheritdoc />
        public int EncodeGetParams(IBufferWriter<byte> writer) {
            // 固定长度10字节：2A 88 0A 00 00 00 00 00 XOR 3B（响应为 0x5C）。:contentReference[oaicite:12]{index=12}
            return EncodeFixed10(writer, HuararyControl.CtrlGetParams, 0x00);
        }

        public void SetAxisLayout(int mainCount, int ejectCount) {
            if (mainCount < 0 || ejectCount < 0)
                throw new ArgumentOutOfRangeException(nameof(mainCount), "Counts must be >= 0.");

            Volatile.Write(ref _mainCount, mainCount);
            Volatile.Write(ref _ejectCount, ejectCount);
        }

        public IReadOnlyList<int> SetGridLayout(IReadOnlyList<int> source, int xCount, bool enabled = true) {
            _xCount = xCount;
            if (!enabled || xCount <= 0) return source.ToArray();
            var n = source.Count;
            if (n == 0) return [];

            // 行数按“向上取整”计算，保证不满一行的尾部也能被遍历到
            var rows = (n + xCount - 1) / xCount;

            var result = new int[n];
            var write = 0;

            // 关键：按列优先输出。对每一列 x，依次读取每一行 y 的元素 y*xCount + x
            // 当索引超界时跳过，这样不满一行的数据也会在正确的位置被写入。
            for (var x = 0; x < xCount; x++) {
                for (var y = 0; y < rows; y++) {
                    var srcIndex = y * xCount + x;
                    if (srcIndex < n) {
                        result[write++] = source[srcIndex];
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 设置二维网格布局与“横纵转换”选项（仅对 Main 段做列优先重排；Eject 保持不变）。
        /// </summary>
        public SpeedSet SetGridLayout(SpeedSet source, int xCount, bool enabled = true) {
            _xCount = xCount;
            // 未启用或参数非法：直接返回原 SpeedSet
            if (!enabled || xCount <= 0)
                return source;

            var main = source.MainMmps;
            if (main.Count == 0)
                return source;
            var transposedMain = SetGridLayout(main, xCount, enabled: true);

            return new SpeedSet(
                source.TimestampUtc,
                source.Sequence,
                transposedMain,
                source.EjectMmps
            );
        }

        /// <summary>
        /// 编码华睿固定长度10字节命令的通用帮助函数。
        /// </summary>
        private static int EncodeFixed10(IBufferWriter<byte> writer, byte ctrl, byte arg) {
            var span = writer.GetSpan(10);
            span[0] = HuararyControl.Start;
            span[1] = ctrl;
            span[2] = 0x0A; span[3] = 0x00; // 长度=10
            span[4] = 0x00; span[5] = 0x00; span[6] = 0x00; span[7] = arg;
            byte xor = 0; for (var i = 0; i < 8; i++) xor ^= span[i];
            span[8] = xor;
            span[9] = HuararyControl.End;
            writer.Advance(10);
            return 10;
        }
    }
}