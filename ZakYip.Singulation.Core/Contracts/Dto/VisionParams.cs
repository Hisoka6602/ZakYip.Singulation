using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Contracts.Dto {
    /// <summary>
    /// 视觉软件公开的参数视图（通过“获取参数”接口或厂商自定义上行获得）。
    /// </summary>
    public sealed record VisionParams {
        /// <summary>速度端口号（TCP）。</summary>
        public int SpeedPort { get; set; }

        /// <summary>位置端口号（TCP）。</summary>
        public int PositionPort { get; set; }

        /// <summary>心跳/指令端口号（TCP）。</summary>
        public int HeartbeatPort { get; set; }

        /// <summary>疏散/扩散单元数量。</summary>
        public int EjectUnitCount { get; set; }

        /// <summary>疏散/扩散单元默认速度（mm/s）。</summary>
        public int EjectDefaultMmps { get; set; }

        /// <summary>自动开始触发延时（秒）。</summary>
        public int AutoStartDelaySec { get; set; }

        /// <summary>视觉软件版本（可空）。</summary>
        public string? Version { get; set; }
    }
}