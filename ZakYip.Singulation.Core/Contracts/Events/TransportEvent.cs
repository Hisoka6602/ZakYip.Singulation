using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Events {
    public readonly record struct TransportEvent(
        string Source,                     // 哪个传输发来的（名称/端口）
        TransportEventType Type,
        ReadOnlyMemory<byte> Payload,      // Data 时有效（紧凑副本）
        int Count,                         // BytesReceived 时有效
        TransportConnectionState Conn,     // StateChanged 时有效
        Exception? Exception               // Error 时有效
    );
}