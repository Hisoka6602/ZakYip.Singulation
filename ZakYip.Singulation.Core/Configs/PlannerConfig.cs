using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs {

    /// <summary>
    /// 单件分离系统的规划参数配置。
    /// 用于描述皮带/电机的物理特性与控制算法的限制条件。
    /// </summary>
    public sealed class PlannerConfig {
        // ------------------ 机械参数 ------------------

        /// <summary>
        /// 轴/小皮带数量。
        /// 对应一条单件分离线上电机/皮带的总数。
        /// </summary>
        public int AxisCount { get; init; }

        /// <summary>
        /// 每条皮带的直径（单位：米）。
        /// 用于把线速度（m/s）换算为电机转速。
        /// </summary>
        public double[] BeltDiameter { get; init; } = [];

        /// <summary>
        /// 每条皮带的齿轮传动比。
        /// 如果电机与皮带直接相连则为 1.0。
        /// 大于 1 表示电机转动一圈，皮带转动不足一圈。
        /// </summary>
        public double[] GearRatio { get; init; } = [];

        /// <summary>
        /// 每条电机的脉冲数（PPR，Pulse Per Revolution）。
        /// 驱动器参数：电机转一圈需要的脉冲数量。
        /// </summary>
        public int[] PulsesPerRev { get; init; } = [];

        // ------------------ 限制参数 ------------------

        /// <summary>
        /// 允许的最大皮带线速度（单位：米/秒）。
        /// 超过此值的输入速度会被限幅。
        /// </summary>
        public double MaxBeltSpeed { get; init; } = 3.0;

        /// <summary>
        /// 允许的最小皮带线速度（单位：米/秒）。
        /// 一般不小于 0。用于防止速度倒转或异常。
        /// </summary>
        public double MinBeltSpeed { get; init; } = 0.0;

        /// <summary>
        /// 最大加速度限制（单位：米/秒²）。
        /// 用于生成速度斜坡，避免电机过冲或机械冲击。
        /// </summary>
        public double MaxAccel { get; init; } = 2.0;

        /// <summary>
        /// 最大加加速度 Jerk（单位：米/秒³）。
        /// 控制加速度的变化率，可用于实现 S 曲线平滑。
        /// </summary>
        public double MaxJerk { get; init; } = 10.0;

        // ------------------ 控制周期 ------------------

        /// <summary>
        /// 控制/规划的时间周期（单位：秒）。
        /// 决定算法每次迭代的时间步长，例如 0.01 表示 10ms。
        /// </summary>
        public double ControlPeriodSec { get; init; } = 0.01;

        // ------------------ 高级控制 ------------------

        /// <summary>
        /// 是否启用平滑控制（如速度斜坡/S 曲线）。
        /// True 则对输入速度进行平滑处理，False 则直接输出。
        /// </summary>
        public bool EnableSmoothing { get; init; } = true;

        /// <summary>
        /// 希望维持的最小包裹间距（单位：米，可选）。
        /// 若配置，将用于算法中控制速度，防止包裹追尾。
        /// </summary>
        public double? SafeGapDistance { get; init; }
    }
}