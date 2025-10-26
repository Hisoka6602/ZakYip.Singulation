using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    public sealed class UpstreamOptionsDoc {
        [BsonId] public string Id { get; set; } = "upstream_options_singleton";

        /// <summary>目标主机地址。</summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>速度端口（客户端或服务端都复用此语义）。</summary>
        public int SpeedPort { get; set; } = 5001;

        /// <summary>位置端口。</summary>
        public int PositionPort { get; set; } = 0;

        /// <summary>心跳端口。</summary>
        public int HeartbeatPort { get; set; } = 0;

        /// <summary>是否校验 CRC。</summary>
        public bool ValidateCrc { get; set; } = true;

        /// <summary>
        /// 传输角色：Client（主动连对方）或 Server（本地监听，等对方推送）。
        /// </summary>
        public TransportRole Role { get; set; } = TransportRole.Client;
    }
}