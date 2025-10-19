using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 视觉软件状态快照（心跳/状态查询响应）。
    /// </summary>
    public sealed record class StatusSnapshot {
        /// <summary>是否处于运行状态。</summary>
        public bool Running { get; init; }

        /// <summary>相机帧率（fps）。</summary>
        public byte CameraFps { get; init; }

        /// <summary>相机列表：包含序列号与状态。</summary>
        public IReadOnlyList<(string Sn, byte State)> Cameras { get; init; }

        /// <summary>报警标志位集合。</summary>
        public VisionAlarm AlarmFlags { get; init; }

        /// <summary>
        /// 通过参数创建状态快照。
        /// </summary>
        public StatusSnapshot(bool running, byte cameraFps, IReadOnlyList<(string Sn, byte State)> cameras, VisionAlarm alarmFlags) {
            Running = running;
            CameraFps = cameraFps;
            Cameras = cameras;
            AlarmFlags = alarmFlags;
        }
    }
}
