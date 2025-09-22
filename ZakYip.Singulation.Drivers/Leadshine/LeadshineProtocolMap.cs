using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Leadshine {

    /// <summary>
    /// 雷赛驱动协议对象字典映射表。
    /// 集中管理索引(Index)、子索引(SubIndex)、位宽(BitLen)以及常用的控制字/模式值，
    /// 用于避免魔法数字，提升代码可读性与可维护性。
    ///
    /// 基于 CiA-402（DS402）规范的常见对象：
    /// - 0x6040 控制字 (Control Word)
    /// - 0x6041 状态字 (Status Word)
    /// - 0x6060 操作模式 (Mode of Operation)
    /// - 0x60FF 目标速度 (Target Velocity)
    /// - 0x6083 Profile Acceleration
    /// - 0x6084 Profile Deceleration
    /// - 0x6091 Gear Ratio
    /// - 0x6092 Feed Constant
    /// </summary>
    public static class LeadshineProtocolMap {

        /// <summary>
        /// 对象字典索引（Index）。
        /// 每个 ushort 常量对应 CiA-402 的一个对象。
        /// </summary>
        internal static class Index {

            /// <summary>
            /// 0x6040 控制字 (Control Word)，UNSIGNED16。
            /// 用于控制状态机，如上电、使能、清故障。
            /// </summary>
            public const ushort ControlWord = 0x6040;

            /// <summary>
            /// 0x6041 状态字 (Status Word)，UNSIGNED16。
            /// 反映驱动当前状态（Ready, Switched On, Fault 等）。
            /// </summary>
            public const ushort StatusWord = 0x6041;

            /// <summary>
            /// 0x6060 操作模式 (Mode of Operation)，INTEGER8。
            /// 设定工作模式：1=PP(位置)，3=PV(速度)，6=HM(回零)。
            /// </summary>
            public const ushort ModeOfOperation = 0x6060;

            /// <summary>
            /// 0x60FF 目标速度 (Target Velocity)，INTEGER32。
            /// 单位: counts/s（脉冲/秒），速度模式下写入。
            /// </summary>
            public const ushort TargetVelocity = 0x60FF;

            /// <summary>
            /// 0x6083 Profile Acceleration，加速度，UNSIGNED32。
            /// 用于设定速度模式下的加速斜率。
            /// </summary>
            public const ushort ProfileAcceleration = 0x6083;

            /// <summary>
            /// 0x6084 Profile Deceleration，减速度，UNSIGNED32。
            /// 用于设定速度模式下的减速斜率。
            /// </summary>
            public const ushort ProfileDeceleration = 0x6084;

            /// <summary>
            /// 0x6092 Feed Constant，整数比，INTEGER32。
            /// 分子/分母定义轴一圈对应的脉冲量。
            /// </summary>
            public const ushort FeedConstant = 0x6092;

            /// <summary>
            /// 0x6091 Gear Ratio，整数比，INTEGER32。
            /// 分子/分母定义电机/轴的传动比。
            /// </summary>
            public const ushort GearRatio = 0x6091;
        }

        /// <summary>
        /// 子索引 (SubIndex)。
        /// 常见的 :00 主值，以及 :01 Numerator, :02 Denominator。
        /// </summary>
        internal static class SubIndex {

            /// <summary>主值 (:00)</summary>
            public const byte Root = 0x00;

            /// <summary>分子 (:01)</summary>
            public const byte Numerator = 0x01;

            /// <summary>分母 (:02)</summary>
            public const byte Denominator = 0x02;
        }

        /// <summary>
        /// 对应 Index 的位宽（BitLen）。
        /// 用于 nmc_write_rxpdo/nmc_read_txpdo 调用时的 bitLen 参数。
        /// </summary>
        internal static class BitLen {
            public const int ControlWord = 16; // 0x6040
            public const int StatusWord = 16; // 0x6041
            public const int ModeOfOperation = 8;  // 0x6060
            public const int TargetVelocity = 32; // 0x60FF
            public const int ProfileAcceleration = 32; // 0x6083
            public const int ProfileDeceleration = 32; // 0x6084
                                                       // 0x6091, 0x6092 分子/分母一般也是 32bit
        }

        /// <summary>
        /// 常用的 ControlWord 值（402 状态机命令）。
        /// </summary>
        internal static class ControlWord {
            public const ushort Clear = 0x0000; // 拉回0
            public const ushort FaultReset = 0x0080; // 清故障
            public const ushort Shutdown = 0x0006; // 进入 Ready to Switch On
            public const ushort SwitchOn = 0x0007; // 进入 Switched On
            public const ushort EnableOperation = 0x000F; // 进入 Operation Enabled
        }

        /// <summary>
        /// 常用 ModeOfOperation 值（0x6060）。
        /// </summary>
        internal static class Mode {
            public const byte ProfileVelocity = 3; // PV 模式 (速度)
            public const byte ProfilePosition = 1; // PP 模式 (位置)
            public const byte Homing = 6; // HM 模式 (回零)
        }

        /// <summary>
        /// 推荐的延时参数（毫秒）。
        /// 用于执行状态机步骤之间的最小等待，避免命令过快。
        /// 实际值可按现场调优。
        /// </summary>
        internal static class DelayMs {
            public const int AfterFaultReset = 10;
            public const int AfterClear = 5;
            public const int AfterSetMode = 5;
            public const int BetweenStateCmds = 10;
        }
    }
}