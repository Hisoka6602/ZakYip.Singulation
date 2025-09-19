using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Common {

    /// <summary>
    /// 厂商协议映射抽象：把上层的语义化动作（Enable/Stop/SetRpm…）
    /// 映射为底层总线/SDK 的字节帧，并从响应中解析原始状态/报警码。
    /// <para>
    /// 约定：实现应是“纯函数式、无状态、线程安全”的；校验（如 CRC/校验和）、
    /// 寄存器地址、功能码、字节序（大小端）等均在此类内完成。
    /// </para>
    /// </summary>
    public abstract class ProtocolMap {

        /// <summary>
        /// 构建“使能/上电就绪”命令帧。
        /// </summary>
        /// <returns>可直接下发的只读字节序列。</returns>
        public abstract ReadOnlyMemory<byte> BuildEnable();

        /// <summary>
        /// 构建“禁用/失能”命令帧（非故障停机，关闭力矩/输出）。
        /// </summary>
        public abstract ReadOnlyMemory<byte> BuildDisable();

        /// <summary>
        /// 构建“停止”命令帧（急停或软停由具体协议/参数决定）。
        /// </summary>
        public abstract ReadOnlyMemory<byte> BuildStop();

        /// <summary>
        /// 构建“回零/回参考点”命令帧。
        /// </summary>
        public abstract ReadOnlyMemory<byte> BuildHome();

        /// <summary>
        /// 构建“设置目标转速（RPM）”命令帧。
        /// <para>
        /// 实现需完成单位换算与量化（如 rpm→寄存器值）、符号/方向处理、
        /// 小数到整数的舍入策略以及范围裁剪（限幅）。
        /// </para>
        /// </summary>
        /// <param name="rpm">目标转速（RPM）。</param>
        public abstract ReadOnlyMemory<byte> BuildSetRpm(double rpm);

        /// <summary>
        /// 构建“读取状态/诊断”命令帧（通常为无副作用的查询）。
        /// </summary>
        public abstract ReadOnlyMemory<byte> BuildReadStatus();

        /// <summary>
        /// 解析“读取状态/诊断”的响应字节。
        /// </summary>
        /// <param name="rsp">有效载荷的连续只读切片（零拷贝）。</param>
        /// <returns>
        /// <para>
        /// <c>state</c>：厂商<span>原始</span>状态码（未标准化，供上层自行映射到统一状态枚举）；<br/>
        /// <c>alarm</c>：厂商<span>原始</span>报警/故障码（未标准化，原样上抛便于记录与告警）。
        /// </para>
        /// </returns>
        public abstract (int state, int alarm) ParseStatus(ReadOnlySpan<byte> rsp);
    }
}