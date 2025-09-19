using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Protocol.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Protocol.Abstractions {

    /// <summary>私有协议解码：零分配优先。</summary>
    public interface IUpstreamCodec {

        CodecResult TryDecode(ReadOnlySpan<byte> buffer, out SpeedSet set, out CodecFlags flags);
    }
}