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
            /// 单位: pps（脉冲/秒，负载侧），速度模式下写入。
            /// 注：与 0x606C (ActualVelocity) 单位保持一致。
            /// </summary>
            public const ushort TargetVelocity = 0x60FF;

            /// <summary>
            /// 0x6083 Profile Acceleration，加速度，UNSIGNED32。
            /// 单位：pps²（脉冲/秒²，负载侧），与 0x60FF (TargetVelocity) 的单位保持一致。
            /// 用于设定速度模式下的加速斜率。
            /// </summary>
            public const ushort ProfileAcceleration = 0x6083;

            /// <summary>
            /// 0x6084 Profile Deceleration，减速度，UNSIGNED32。
            /// 单位：pps²（脉冲/秒²，负载侧），与 0x60FF (TargetVelocity) 的单位保持一致。
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

            /// <summary>
            /// 0x606C 实际速度 (Actual Velocity)，INTEGER32。
            /// 单位：pps（脉冲/秒，负载侧）。
            /// 由驱动反馈当前实时速度。
            /// </summary>
            public const ushort ActualVelocity = 0x606C;
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
            /// <summary>控制字 (0x6040) 的位宽：16位</summary>
            public const int ControlWord = 16;

            /// <summary>状态字 (0x6041) 的位宽：16位</summary>
            public const int StatusWord = 16;

            /// <summary>操作模式 (0x6060) 的位宽：8位</summary>
            public const int ModeOfOperation = 8;

            /// <summary>目标速度 (0x60FF) 的位宽：32位</summary>
            public const int TargetVelocity = 32;

            /// <summary>Profile加速度 (0x6083) 的位宽：32位</summary>
            public const int ProfileAcceleration = 32;

            /// <summary>Profile减速度 (0x6084) 的位宽：32位</summary>
            public const int ProfileDeceleration = 32;

            /// <summary>实际速度 (0x606C) 的位宽：32位</summary>
            public const int ActualVelocity = 32;

            /// <summary>Feed Constant (0x6092) 的位宽：32位。用于读取分子/分母定义轴一圈对应的脉冲量</summary>
            public const int FeedConstant = 32;
        }

        /// <summary>
        /// 常用的 ControlWord 值（402 状态机命令）。
        /// </summary>
        internal static class ControlWord {
            /// <summary>清零控制字 (0x0000)：将控制字拉回0，用于初始化状态机</summary>
            public const ushort Clear = 0x0000;

            /// <summary>快速停止 (0x0002)：QuickStop命令，bit1=1</summary>
            public const ushort QuickStop = 0x0002;

            /// <summary>关机指令 (0x0006)：进入 Ready to Switch On 状态，bit1=1, bit2=1</summary>
            public const ushort Shutdown = 0x0006;

            /// <summary>开机指令 (0x0007)：进入 Switched On 状态，bit0=1, bit1=1, bit2=1</summary>
            public const ushort SwitchOn = 0x0007;

            /// <summary>故障复位 (0x0080)：清除驱动器故障状态，bit7=1</summary>
            public const ushort FaultReset = 0x0080;

            /// <summary>使能运行 (0x000F)：进入 Operation Enabled 状态，bit0-3=1，允许轴运动</summary>
            public const ushort EnableOperation = 0x000F;
        }

        /// <summary>
        /// ControlWord 位掩码（用于验证状态）。
        /// </summary>
        internal static class ControlWordMask {
            /// <summary>EnableOperation 位掩码 (0x000F)：bit0-3，用于验证是否使能运行。与 ControlWord.EnableOperation 相同。</summary>
            public const ushort EnableOperationMask = ControlWord.EnableOperation;

            /// <summary>EnableOperation 位 (0x0008)：bit3，用于验证运行使能位</summary>
            public const ushort EnableOperationBit = 0x0008;
        }

        /// <summary>
        /// StatusWord 位掩码（用于读取驱动器状态）。
        /// 基于 CiA 402 (DS402) 规范。
        /// </summary>
        internal static class StatusWordMask {
            /// <summary>Fault 位 (0x0008)：bit3 (0-indexed，即第4位)，1 表示驱动器处于故障状态</summary>
            public const ushort FaultBit = 0x0008;
        }

        /// <summary>
        /// 常用 ModeOfOperation 值（0x6060）。
        /// </summary>
        internal static class Mode {
            /// <summary>速度模式 (Profile Velocity, PV)：值为3，基于目标速度控制</summary>
            public const byte ProfileVelocity = 3;

            /// <summary>位置模式 (Profile Position, PP)：值为1，基于目标位置控制</summary>
            public const byte ProfilePosition = 1;

            /// <summary>回零模式 (Homing, HM)：值为6，执行回零动作</summary>
            public const byte Homing = 6;
        }

        /// <summary>
        /// 推荐的延时参数（毫秒）。
        /// 用于执行状态机步骤之间的最小等待，避免命令过快。
        /// 实际值可按现场调优。
        /// </summary>
        internal static class DelayMs {
            /// <summary>故障复位后的延时（毫秒）：10ms，等待驱动器清除故障状态</summary>
            public const int AfterFaultReset = 10;

            /// <summary>清零控制字后的延时（毫秒）：5ms，等待控制字生效</summary>
            public const int AfterClear = 5;

            /// <summary>设置操作模式后的延时（毫秒）：5ms，等待模式切换完成</summary>
            public const int AfterSetMode = 5;

            /// <summary>状态机命令之间的延时（毫秒）：10ms，避免状态转换过快</summary>
            public const int BetweenStateCmds = 10;
        }
    }
}