using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 规划器当前的运行状态。
    /// 用于对外监控与诊断。
    /// </summary>
    public enum PlannerStatus {

        /// <summary>
        /// 空闲状态：尚未收到任何输入帧，或者没有在运行。
        /// </summary>
        [Description("Idle - 未运行/等待输入")]
        Idle,

        /// <summary>
        /// 正常运行中：正在接收上游输入并输出规划结果。
        /// </summary>
        [Description("Running - 正常运行")]
        Running,

        /// <summary>
        /// 降级状态：检测到缺帧、乱序、延迟等异常，
        /// 规划器仍能产出结果，但精度或稳定性下降。
        /// </summary>
        [Description("Degraded - 功能降级")]
        Degraded,

        /// <summary>
        /// 故障状态：出现严重错误（如配置无效、内部异常、无法输出），
        /// 规划器无法继续正常工作。
        /// </summary>
        [Description("Faulted - 故障停止")]
        Faulted
    }
}