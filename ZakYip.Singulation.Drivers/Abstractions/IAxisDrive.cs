using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>
    /// 单轴驱动的最小控制接口。
    /// <para>实现方应保证：幂等、低延迟、低分配，并对异常/掉线具备可恢复能力。</para>
    /// </summary>
    public interface IAxisDrive : IAsyncDisposable {

        /// <summary>
        /// 轴标识（不可变）。
        /// </summary>
        AxisId Axis { get; }

        /// <summary>
        /// 当前驱动状态（离线/恢复中/在线/降级/已释放等）。
        /// <para>用于对外观测连接与健康状况，便于上层调度与告警。</para>
        /// </summary>
        DriverStatus Status { get; }

        /// <summary>
        /// 写入目标转速（RPM）。
        /// <list type="bullet">
        /// <item>实现可在内部做单位换算、限幅、同值去重与命令节流。</item>
        /// <item>遇到短暂异常应按策略重试并退避；超过阈值再上抛。</item>
        /// </list>
        /// </summary>
        /// <param name="rpm">目标转速（<see cref="AxisRpm"/>）。</param>
        /// <param name="ct">取消令牌；取消时应尽快停止发送，通常抛出 <see cref="OperationCanceledException"/>。</param>
        /// <returns>异步任务。</returns>
        ValueTask WriteSpeedAsync(AxisRpm rpm, CancellationToken ct = default);

        /// <summary>
        /// 设置速度模式下的加速度/减速度（单位：RPM/s）。
        /// <para>
        /// 典型实现：将 <c>0x6083</c>（Profile Acceleration）与 <c>0x6084</c>（Profile Deceleration）
        /// 通过 PDO 写入（<c>nmc_write_rxpdo</c>），常为 U32（4 字节）。
        /// </para>
        /// <remarks>
        /// 通常在清故障与设模式（0x6060=3）之后、进入 <c>Enable Operation</c> 之前调用一次；
        /// 如现场映射为 16 位，请相应调整 <c>bitlength</c> 与打包长度。
        /// </remarks>
        /// </summary>
        /// <param name="accelRpmPerSec">加速度（RPM/s）。</param>
        /// <param name="decelRpmPerSec">减速度（RPM/s）。</param>
        ValueTask SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default);

        /// <summary>
        /// 停止轴运动（急停或减速停由实现/设备配置决定）。
        /// <para>建议可重入且快速返回；设备支持时优先软停，异常时回退为急停。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>异步任务。</returns>
        ValueTask StopAsync(CancellationToken ct = default);

        /// <summary>
        /// 连通性/活跃性探测（轻量心跳）。
        /// <para>实现应使用无副作用的读操作，内部可含重试与退避。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>
        /// <c>true</c> 表示请求-应答成功且校验通过；<c>false</c> 表示通信异常（为便于心跳循环，不抛异常）。
        /// </returns>
        ValueTask<bool> PingAsync(CancellationToken ct = default);
    }
}