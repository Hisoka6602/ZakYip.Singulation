using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 速度规划器接口：
    /// 接收上游视觉给出的速度集合（<see cref="SpeedSet"/>），结合输送机拓扑（<see cref="ConveyorTopology"/>），
    /// 输出各轴的目标转速序列（单位：RPM）。
    ///
    /// 规划器的实现应负责：
    /// 1. 单位换算（例如 m/s → RPM）。
    /// 2. 限幅（最大/最小转速）。
    /// 3. 平滑处理（滑动窗口、滤波）。
    /// 4. 斜率限制（加速度 / jerk）。
    /// 5. 采样周期对齐（插值 / 缓存上一帧）。
    /// 6. 缺帧处理（保持、缓降、归零等策略）。
    /// </summary>
    public interface ISpeedPlanner {

        /// <summary>
        /// 当前规划器的运行状态（例如：正常、缺帧、过载、错误）。
        /// 可用于监控与诊断。
        /// </summary>
        PlannerStatus Status { get; }

        /// <summary>
        /// 在线更新规划器参数。
        /// 调用方可在运行时动态调整限幅、平滑窗口、缺帧策略等。
        /// 实现需保证线程安全和“原子切换”，避免半更新状态。
        /// </summary>
        /// <param name="params">新的运行参数。</param>
        void Configure(PlannerParams @params);

        /// <summary>
        /// 执行一次速度规划。
        /// 内部应根据配置完成限幅、平滑、斜率限制等步骤，
        /// 并将输入的 <see cref="SpeedSet"/> 转换为每个轴的目标 RPM。
        /// </summary>
        /// <param name="topology">
        /// 输送机拓扑，描述系统有哪些轴、顺序如何排列。
        /// 输出结果的顺序必须与此拓扑一致。
        /// </param>
        /// <param name="input">
        /// 上游视觉给出的速度集合，包括帧号、时间戳、各段速度。
        /// </param>
        /// <returns>
        /// 与 <paramref name="topology"/> 的轴数量一致的转速序列（单位：RPM）。
        /// </returns>
        ReadOnlyMemory<AxisRpm> Plan(ConveyorTopology topology, in SpeedSet input);
    }
}