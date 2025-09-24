using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Abstractions.Events;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>
    /// 聚合多个轴的驱动事件，并对外统一转发。
    /// <para>
    /// 使用场景：当系统同时控制多根轴时，单个驱动产生的事件会被集中汇总，
    /// 再通过聚合器统一对外抛出，方便上层订阅与处理。
    /// </para>
    /// <para>
    /// 约束：
    /// - 聚合器仅负责事件转发，不应包含业务逻辑；
    /// - 事件处理必须高效、不可阻塞；
    /// - 不抛出异常，异常需通过事件参数或上层策略处理。
    /// </para>
    /// </summary>
    public interface IAxisEventAggregator {

        /// <summary>
        /// 绑定一个驱动实例，使其事件被聚合器捕获并统一转发。
        /// <para>通常在驱动初始化或加入控制器时调用。</para>
        /// </summary>
        /// <param name="drive">要绑定的轴驱动实例。</param>
        void Attach(IAxisDrive drive);

        /// <summary>
        /// 解绑一个驱动实例，停止转发其事件。
        /// <para>通常在驱动释放或控制器销毁时调用。</para>
        /// </summary>
        /// <param name="drive">要解绑的轴驱动实例。</param>
        void Detach(IAxisDrive drive);

        /// <summary>
        /// 当驱动上报速度反馈时触发。
        /// <para>事件参数包含当前速度、时间戳等信息。</para>
        /// </summary>
        event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        /// <summary>
        /// 当驱动发出控制命令时触发。
        /// <para>可用于记录日志、监控或命令下发追踪。</para>
        /// </summary>
        event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        /// <summary>
        /// 当驱动检测到错误（如过载、通信故障）时触发。
        /// <para>取代异常抛出，用于上层感知错误状态。</para>
        /// </summary>
        event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        /// <summary>
        /// 当驱动与总线或上层控制器断开时触发。
        /// <para>用于通知上层进行重连或容错处理。</para>
        /// </summary>
        event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        /// <summary>
        /// 当驱动库或底层依赖未正确加载时触发。
        /// <para>典型场景：缺少 DLL、驱动未初始化。</para>
        /// </summary>
        event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;
    }
}