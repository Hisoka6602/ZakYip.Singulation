using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Common;
using ZakYip.Singulation.Drivers.Abstractions.Ports;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>轴 → 驱动实例映射与拓扑绑定。</summary>
    public interface IDriveRegistry {

        /// <summary>
        /// 为指定厂商注册驱动工厂。
        /// </summary>
        /// <param name="vendor">厂商标识（如 "Leadshine"、"Shengpai"、"Inovance"）。建议大小写不敏感。</param>
        /// <param name="factory">
        /// 构造函数委托：<c>(axisId, port, opts) =&gt; IAxisDrive</c>。
        /// 其中 <paramref name="port"/> 通常为“每轴一个”的通信端口实例。
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="vendor"/> 或 <paramref name="factory"/> 为空。</exception>
        /// <remarks>
        /// 重复注册的处理策略由实现定义：常见做法是后注册覆盖先注册，便于热更新驱动实现。
        /// </remarks>
        void Register(string vendor, Func<AxisId, IAxisPort, DriverOptions, IAxisDrive> factory);

        /// <summary>
        /// 根据厂商标识创建一个轴驱动实例。
        /// </summary>
        /// <param name="vendor">厂商标识（必须已通过 <see cref="Register"/> 注册）。</param>
        /// <param name="axisId">目标轴ID。</param>
        /// <param name="port">用于该轴的通信端口（TCP/SDK 的抽象）。</param>
        /// <param name="opts">驱动运行参数（限幅、退避、节流等）。</param>
        /// <returns>新建的 <see cref="IAxisDrive"/> 实例。</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="vendor"/>、<paramref name="port"/> 或 <paramref name="opts"/> 为空。
        /// </exception>
        /// <exception cref="KeyNotFoundException">未找到对应 <paramref name="vendor"/> 的注册工厂。</exception>
        /// <remarks>
        /// 返回实例的生命周期由调用方管理：使用完成后需调用 <see cref="IAsyncDisposable.DisposeAsync"/> 释放底层资源。
        /// </remarks>
        IAxisDrive Create(string vendor, AxisId axisId, IAxisPort port, DriverOptions opts);
    }
}