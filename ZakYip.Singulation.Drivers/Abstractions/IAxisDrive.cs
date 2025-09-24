using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Enums;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Drivers.Abstractions.Events;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>
    /// 单轴驱动的最小控制接口。
    /// <para>实现方应保证：幂等、低延迟、低分配，并对异常/掉线具备可恢复能力。</para>
    /// </summary>
    public interface IAxisDrive : IAsyncDisposable {
        // ---------------- 新增事件 ----------------

        /// <summary>
        /// 当轴运行过程中出现异常时触发。
        /// <para>典型如速度命令失败、写寄存器返回非 0 等。</para>
        /// </summary>
        event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        /// <summary>
        /// 当底层驱动库函数未正确加载（如 LTDMC.dll 缺失）时触发。
        /// <para>实现应在构造或调用前检查并在此事件中通知。</para>
        /// </summary>
        event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;

        /// <summary>
        /// 当轴与控制器断线时触发。
        /// <para>实现方应在心跳（Ping）或写命令失败时触发。</para>
        /// </summary>
        event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        /// <summary>
        /// 实时速度反馈（只播报观测到的实际速度，不保证每次命令后都立刻到达目标）。
        /// <para>实现应确保非阻塞广播；订阅方的异常不得影响驱动主流程。</para>
        /// <para>单位齐备：rpm / m/s / pps；方向为数值正负，受反转配置影响。</para>
        /// </summary>
        event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        /// <summary>
        /// 函数调用事件(可用于输出/日志记录)
        /// </summary>
        event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

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
        /// 最近一次下发的目标线速度 (mm/s)。
        /// <para>值为 null 表示尚未下发过速度命令。</para>
        /// </summary>
        decimal? LastTargetMmps { get; }

        /// <summary>
        /// 最近一次驱动反馈的线速度 (mm/s)。
        /// <para>值为 null 表示尚未接收到反馈。</para>
        /// </summary>
        decimal? LastFeedbackMmps { get; }

        /// <summary>
        /// 当前驱动是否处于“已使能”状态。
        /// <para>true 表示已使能，可以响应速度命令；false 表示未使能或被禁用。</para>
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 最近错误码（由驱动器/总线提供）。
        /// <para>0 表示正常；非 0 表示存在错误。</para>
        /// </summary>
        int LastErrorCode { get; }

        /// <summary>
        /// 最近错误消息（人类可读描述）。
        /// <para>可能为 null 或空字符串，表示当前无错误或驱动未提供详细信息。</para>
        /// </summary>
        string? LastErrorMessage { get; }

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
        /// 写入目标转速(mm/s)
        /// </summary>
        /// <param name="mmPerSec"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task WriteSpeedAsync(decimal mmPerSec, CancellationToken ct = default);

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
        /// <param name="ct"></param>
        Task SetAccelDecelAsync(decimal accelRpmPerSec, decimal decelRpmPerSec, CancellationToken ct = default);

        /// <summary>
        /// 设置速度模式下的加速度/减速度（单位：mm/s²）。
        /// <para>内部会自动换算成 RPM/s，再写入 0x6083/0x6084。</para>
        /// </summary>
        /// <param name="accelMmPerSec">加速度 (mm/s²)</param>
        /// <param name="decelMmPerSec">减速度 (mm/s²)</param>
        ValueTask SetAccelDecelByLinearAsync(decimal accelMmPerSec, decimal decelMmPerSec, CancellationToken ct = default);

        /// <summary>
        /// 停止轴运动（急停或减速停由实现/设备配置决定）。
        /// <para>建议可重入且快速返回；设备支持时优先软停，异常时回退为急停。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <returns>异步任务。</returns>
        ValueTask StopAsync(CancellationToken ct = default);

        /// <summary>
        /// 轴使能
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task EnableAsync(CancellationToken ct = default);

        /// <summary>
        /// 更新线速度/加减速度的限幅（单位均为 mm/s 或 mm/s²）。
        /// <para>实现要求：仅更新内存缓存与后续命令的限幅逻辑，不得外抛异常；若需要持久化由上层仓储处理。</para>
        /// </summary>
        /// <param name="maxLinearMmps">最大线速度 (mm/s)。传入非正值应当被拒绝（返回 false），或钳制到最小合理值。</param>
        /// <param name="maxAccelMmps2">最大线加速度 (mm/s²)。同上。</param>
        /// <param name="maxDecelMmps2">最大线减速度 (mm/s²)。同上。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>操作是否接受并已生效（不抛异常，失败返回 false）。</returns>
        Task UpdateLinearLimitsAsync(decimal maxLinearMmps, decimal maxAccelMmps2, decimal maxDecelMmps2,
            CancellationToken ct = default);

        /// <summary>
        /// 更新机械参数（滚筒直径/齿轮比/PPR）。仅更新内存缓存与换算系数，后续命令按新系数生效。
        /// <para>齿轮比约定：motor:roller（电机轴:滚筒轴）。</para>
        /// </summary>
        /// <param name="rollerDiameterMm">滚筒直径 (mm)，必须 &gt; 0。</param>
        /// <param name="gearRatio">齿轮比 (motor:roller)，必须 &gt; 0。</param>
        /// <param name="ppr">每转脉冲数 PPR，必须 &gt; 0。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>操作是否接受并已生效（不抛异常，失败返回 false）。</returns>
        Task UpdateMechanicsAsync(decimal rollerDiameterMm, decimal gearRatio, int ppr, CancellationToken ct = default);
    }
}