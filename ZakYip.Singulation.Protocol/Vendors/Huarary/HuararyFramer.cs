using System;
using System.Linq;
using System.Text;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Protocol.Vendors.Huarary {

    /// <summary>
    /// 华睿分帧器：按 起始+控制码+长度(LE)+...+XOR+结束 的格式切帧并校验边界。
    /// </summary>
    public sealed class HuararyFramer : IUpstreamFramer {

        /// <inheritdoc />
        public bool TryReadFrame(ref ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> frame) {
            frame = default;
            var span = buffer;

            // 寻找起始符
            int s = span.IndexOf(HuararyControl.Start);
            if (s < 0 || span.Length - s < 6)
                return false;

            // 确认起始
            if (span[s] != HuararyControl.Start)
                return false;

            // 长度字段（小端，含首尾与校验）
            if (span.Length < s + 4)
                return false;
            ushort len = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(s + 2, 2));

            // 数据不足一帧
            if (span.Length - s < len)
                return false;

            // 结束符检查
            if (span[s + len - 1] != HuararyControl.End) {
                // 轻度容错：跳过该起点继续搜
                buffer = span[(s + 1)..];
                return false;
            }

            // 输出帧，并前移缓冲
            frame = span.Slice(s, len);
            buffer = span[(s + len)..];
            return true;
        }
    }
}