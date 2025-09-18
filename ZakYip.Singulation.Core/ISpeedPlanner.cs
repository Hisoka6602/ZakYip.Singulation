using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Contracts.Struct;

namespace ZakYip.Singulation.Core {

    /// <summary>速度规划：输入拓扑 + SpeedSet，输出每轴 RPM。</summary>
    public interface ISpeedPlanner {
        PlannerStatus Status { get; }

        void Configure(PlannerParams @params); // 在线调参（原子切换由实现保证）

        /// <summary>
        /// 规划一次：实现可内部做平滑/限幅/斜率/采样对齐。
        /// </summary>
        /// <returns>与拓扑 Axes 同长度的 RPM 序列。</returns>
        ReadOnlyMemory<AxisRpm> Plan(ConveyorTopology topology, in SpeedSet input);
    }
}