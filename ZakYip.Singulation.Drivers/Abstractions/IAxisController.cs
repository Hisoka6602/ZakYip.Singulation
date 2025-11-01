using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>
    /// 轴群编排器：
    /// 负责创建/持有多根 <see cref="IAxisDrive"/>，并提供“全体/批量”的常用操作。
    /// <para>
    /// 约束：
    /// - 不应外抛异常，所有错误均通过 <see cref="ControllerFaulted"/> 事件通知；
    /// - 方法需具备幂等性与高性能；
    /// - 异步操作应支持 <see cref="CancellationToken"/> 取消。
    /// </para>
    /// </summary>
    public interface IAxisController {

        /// <summary>
        /// 当控制器内部发生错误（如总线不可用、驱动创建失败、参数非法等）时触发。
        /// <para>事件不能被外部阻塞，应快速返回。</para>
        /// </summary>
        event EventHandler<string>? ControllerFaulted;

        /// <summary>
        /// 底层总线适配器。
        /// <para>用于驱动节点发现、通信初始化与关闭。</para>
        /// </summary>
        IBusAdapter Bus { get; }

        /// <summary>
        /// 已创建/托管的驱动集合。
        /// <para>顺序通常与总线上 NodeId 顺序一致。</para>
        /// </summary>
        IReadOnlyList<IAxisDrive> Drives { get; }

        /// <summary>
        /// 所有轴的实时速度反馈（mm/s）。
        /// <para>索引与 <see cref="Drives"/> 一致，值对应各轴的 <see cref="IAxisDrive.LastFeedbackMmps"/>。</para>
        /// <para>值为 null 表示该轴尚未收到反馈。</para>
        /// </summary>
        IReadOnlyList<decimal?> RealtimeSpeedsMmps { get; }

        /// <summary>
        /// 初始化总线并按规则创建 N 根驱动（通过 <see cref="IDriveRegistry"/> 工厂）。
        /// <para>优先使用 <paramref name="overrideAxisCount"/> 指定数量；
        /// 否则尝试从总线查询；再不行则根据模板 NodeId 范围推算。</para>
        /// <para>若失败则通过 <see cref="ControllerFaulted"/> 通知，而不会抛异常。</para>
        /// </summary>
        /// <param name="vendor">驱动厂商标识，用于注册表选择具体实现。</param>
        /// <param name="template">驱动初始化模板参数。</param>
        /// <param name="overrideAxisCount">可选，强制覆盖的轴数量。</param>
        /// <param name="ct">取消标记。</param>
        Task<KeyValuePair<bool, string>> InitializeAsync(
            string vendor,
            DriverOptions template,
            int? overrideAxisCount = null,
            CancellationToken ct = default);

        /// <summary>
        /// 启用所有已创建的轴驱动。
        /// <para>通常对应 CANopen 的 "Enable Operation"。</para>
        /// </summary>
        Task EnableAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 禁用所有已创建的轴驱动（关闭使能）。
        /// <para>通常对应 CANopen 的 "Disable Operation" 或 "Switch On Disabled"。</para>
        /// </summary>
        Task DisableAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 为所有轴统一设置加速度/减速度（线加速度，单位：mm/s²）。
        /// <para>内部会自动转换为对应的电机参数格式。</para>
        /// </summary>
        Task SetAccelDecelAllAsync(decimal accelMmPerSec2, decimal decelMmPerSec2, CancellationToken ct = default);

        /// <summary>
        /// 为所有轴统一写入目标速度（单位：mm/s）。
        /// <para>内部会限幅并换算为协议支持的单位。</para>
        /// </summary>
        Task WriteSpeedAllAsync(decimal mmPerSec, CancellationToken ct = default);

        /// <summary>
        /// 停止所有轴运动（通常对应“立即减速到零速”）。
        /// </summary>
        Task StopAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 释放所有驱动资源，解除事件绑定，并关闭总线。
        /// <para>执行后 <see cref="Drives"/> 集合会被清空。</para>
        /// </summary>
        Task DisposeAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 将视觉上游解析出的速度集一次性下发到各轴（单位：mm/s）。
        /// 不抛异常；长度不匹配与单轴失败通过 ControllerFaulted 事件与日志呈现。
        /// 约定：Main 段在前，Eject 段在后，按拓扑顺序依次贴到轴 0..N-1。
        /// </summary>
        Task ApplySpeedSetAsync(SpeedSet set, CancellationToken ct = default);

        /// <summary>
        /// 重置所有轴的上次速度缓存。
        /// <para>用于模式切换（如本地/远程模式）时清除缓存，确保下次速度设置能够正确写入。</para>
        /// </summary>
        void ResetLastSpeeds();
    }
}