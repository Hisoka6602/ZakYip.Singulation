using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 视觉软件状态快照（心跳/状态查询响应）。
    /// </summary>
    /// <param name="Running">是否处于运行状态。</param>
    /// <param name="CameraFps">相机帧率（fps）。</param>
    /// <param name="Cameras">相机列表：包含序列号与状态。</param>
    /// <param name="AlarmFlags">报警标志位集合。</param>
    public sealed record StatusSnapshot(
        bool Running,
        byte CameraFps,
        IReadOnlyList<(string Sn, byte State)> Cameras,
        VisionAlarm AlarmFlags);
}