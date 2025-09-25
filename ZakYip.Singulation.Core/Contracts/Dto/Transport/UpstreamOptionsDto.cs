using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Dto.Transport {

    /// <summary>
    /// 上游 TCP 连接参数（LiteDB 单文档存储）。
    /// </summary>
    public sealed class UpstreamOptionsDto {

        /// <summary>
        /// 固定主键，单文档模式使用常量 Id 避免多条记录。
        /// </summary>
        public string Id { get; set; } = "upstream_options_singleton";

        /// <summary>目标主机地址。</summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>速度端口（客户端或服务端都复用此语义）。</summary>
        public int SpeedPort { get; set; } = 5001;

        /// <summary>位置端口。</summary>
        public int PositionPort { get; set; } = 5002;

        /// <summary>心跳端口。</summary>
        public int HeartbeatPort { get; set; } = 5003;

        /// <summary>是否校验 CRC。</summary>
        public bool ValidateCrc { get; set; } = true;

        /// <summary>
        /// 传输角色：Client（主动连对方）或 Server（本地监听，等对方推送）。
        /// </summary>
        public TransportRole Role { get; set; } = TransportRole.Client;
    }
}