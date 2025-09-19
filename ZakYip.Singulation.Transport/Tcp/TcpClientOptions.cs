using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Transport.Tcp {

    public sealed class TcpClientOptions {
        public string Host { get; init; } = "127.0.0.1";
        public int Port { get; init; } = 5000;

        /// <summary>启用 Nagle 禁用（减少延迟）。</summary>
        public bool NoDelay { get; init; } = true;

        /// <summary>接收缓冲区大小（字节）。</summary>
        public int ReceiveBufferSize { get; init; } = 64 * 1024;
    }
}