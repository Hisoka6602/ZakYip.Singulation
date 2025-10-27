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
    /// 接收上游视觉给出的速度集合（<see cref="SpeedSet"/>），
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
        void Configure(LinearPlannerParams @params);
    }
}