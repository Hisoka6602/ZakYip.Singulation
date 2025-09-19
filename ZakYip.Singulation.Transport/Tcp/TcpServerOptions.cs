using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Transport.Tcp {

    public sealed class TcpServerOptions {

        /// <summary>监听地址；默认 0.0.0.0。</summary>
        public IPAddress Address { get; init; } = IPAddress.Any;

        public int Port { get; init; } = 5000;

        /// <summary>最多只保留 1 个活动连接（视觉一般只连一个）。</summary>
        public int MaxActiveConnections { get; init; } = 1;

        /// <summary>接收缓冲区大小（字节）。</summary>
        public int ReceiveBufferSize { get; init; } = 64 * 1024;

        /// <summary>Socket backlog。</summary>
        public int Backlog { get; init; } = 100;
    }
}