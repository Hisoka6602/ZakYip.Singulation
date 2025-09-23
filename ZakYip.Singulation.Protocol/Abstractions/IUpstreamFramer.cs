using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Protocol.Abstractions {

    /// <summary>
    /// 上游字节流的“分帧器”，负责从粘包/半包的字节缓冲中切出一帧完整报文。
    /// 不同厂商的帧定界方式不同（如华睿含长度/校验，归位为简约起止符）。
    /// </summary>
    public interface IUpstreamFramer {

        /// <summary>
        /// 尝试从字节缓冲中读取一帧完整报文。
        /// </summary>
        /// <param name="buffer">
        /// 输入/输出的只读字节切片。方法成功返回后，会前移该切片到帧尾之后的位置。
        /// </param>
        /// <param name="frame">
        /// 成功时输出一帧完整数据（通常包含起始与结束符）；失败时为 <c>default</c>。
        /// </param>
        /// <returns>
        /// 读取成功返回 <c>true</c>；若当前数据不足构成一帧返回 <c>false</c>。
        /// </returns>
        bool TryReadFrame(ref ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> frame);
    }
}