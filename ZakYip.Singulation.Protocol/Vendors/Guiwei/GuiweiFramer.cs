using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Protocol.Vendors.Guiwei {

    /// <summary>
    /// 归位分帧器：仅依赖起止符，按4字节对齐切出一帧。
    /// </summary>
    public sealed class GuiweiFramer : IUpstreamFramer {

        /// <inheritdoc />
        public bool TryReadFrame(ref ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> frame) {
            frame = default;
            var span = buffer;

            int s = span.IndexOf(GuiweiControl.Start);
            if (s < 0 || span.Length - s < 2) return false;

            int e = span.Slice(s + 1).IndexOf(GuiweiControl.End);
            if (e < 0) return false;

            e += s + 1; // 绝对下标
            frame = span.Slice(s, e - s + 1);
            buffer = span[(e + 1)..];
            return true;
        }
    }
}