using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Protocol.Enums {

    public enum UpstreamCtrl : byte {
        Speed = 0x81,   // 速度端
        Pos = 0x82,   // 位置端

        // 心跳/指令端发出：
        StartStop = 0x89,

        SetModeSpeed = 0x84,
        SetSpacing = 0x86,
        PauseResume = 0x85,
        SetParams = 0x83,
        QueryStatus = 0x87, // 返回 0x5B
        GetParams = 0x88, // 返回 0x5C
        StatusResp = 0x5B,
        ParamsResp = 0x5C,
    }
}