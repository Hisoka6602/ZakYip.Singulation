using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Transport.Tcp {

    public sealed record class TcpServerOptions {

        /// <summary>监听地址；默认 0.0.0.0。</summary>
        public required IPAddress Address { get; init; } = IPAddress.Any;

        public int Port { get; init; } = 5000;

        /// <summary>
        /// 最大活动连接数。
        /// -1 表示无限制；
        /// 0 表示不允许任何连接；
        /// 其他负值（除 -1 外）为无效值（或将被视为错误）。
        /// </summary>
        public int MaxActiveConnections { get; init; } = 100;

        /// <summary>接收缓冲区大小（字节）。</summary>
        public int ReceiveBufferSize { get; init; } = 64 * 1024;

        /// <summary>Socket backlog。</summary>
        public int Backlog { get; init; } = 100;
    }
}