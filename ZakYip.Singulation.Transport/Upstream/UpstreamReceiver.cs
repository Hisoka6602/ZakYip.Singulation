using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Protocol.Abstractions;

namespace ZakYip.Singulation.Transport.Upstream {

    /// <summary>
    /// 上游接收器：对接某厂商的分帧器与编解码器，从 TCP/UDP 字节流中
    /// 解析出 SpeedSet/位姿/状态/参数，并以事件的方式抛出给上层。
    /// </summary>
    public sealed class UpstreamReceiver {
        private readonly IUpstreamFramer _framer;
        private readonly IUpstreamCodec _codec;

        /// <summary>
        /// 当成功解析到一帧速度集合时触发。
        /// </summary>
        public event EventHandler<SpeedSet>? SpeedReceived;

        /// <summary>
        /// 使用指定的分帧与编解码实现创建接收器。
        /// </summary>
        public UpstreamReceiver(IUpstreamFramer framer, IUpstreamCodec codec) {
            _framer = framer;
            _codec = codec;
        }

        /// <summary>
        /// 将上游收到的一段连续字节提交给接收器；内部会处理粘包/半包并触发相应事件。
        /// </summary>
        /// <param name="bytes">上游收取的字节切片。</param>
        public void OnBytes(ReadOnlySpan<byte> bytes) {
            var buf = bytes;
            while (_framer.TryReadFrame(ref buf, out var frame)) {
                if (_codec.TryDecodeSpeed(frame, out var set))
                    SpeedReceived?.Invoke(this, set);
                // 同理：TryDecodePositions/TryDecodeStatus/TryDecodeParams 可根据需要扩展分发事件。
            }
        }
    }
}