using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>
    /// 控制器/总线适配器（厂商相关）：负责硬件初始化、获取轴数、错误查询与复位等。
    /// </summary>
    public interface IBusAdapter {

        /// <summary>初始化控制器或通信总线（幂等）。</summary>
        Task InitializeAsync(CancellationToken ct = default);

        /// <summary>关闭/释放控制器资源（幂等）。</summary>
        Task CloseAsync(CancellationToken ct = default);

        /// <summary>获取总线发现到的轴数量（1-based 索引习惯由上层决定）。</summary>
        Task<int> GetAxisCountAsync(CancellationToken ct = default);

        /// <summary>读取当前错误码；0 表示正常。</summary>
        Task<int> GetErrorCodeAsync(CancellationToken ct = default);

        /// <summary>执行控制器冷复位（如需）；通常用于错误码非 0 的场景。</summary>
        Task ResetAsync(CancellationToken ct = default);

        /// <summary>
        /// 执行控制器热复位（软复位）。
        /// <para>热复位通常只会重置通信/状态机，不掉电，耗时短（1~2 秒）。</para>
        /// </summary>
        Task WarmResetAsync(CancellationToken ct = default);

        /// <summary>
        /// 根据厂商规则转换逻辑 NodeId → 物理 NodeId。
        /// <para>
        /// 例如：某些厂商的 NodeId 从 1 开始，而上层逻辑用 1001、1002 表示；
        /// 在创建驱动时需调用本方法做转换。
        /// </para>
        /// </summary>
        /// <param name="logicalNodeId">逻辑层的 NodeId（如 1001）。</param>
        /// <returns>物理层的 NodeId（如 1）。</returns>
        ushort TranslateNodeId(ushort logicalNodeId);

        /// <summary>
        /// 根据厂商/拓扑规则判断指定轴是否需要反转。
        /// <para>
        /// 例如：某些设备是奇数反转、偶数正转；
        /// 也可能完全不反转；或者有更复杂的映射表。
        /// </para>
        /// </summary>
        /// <param name="logicalNodeId">逻辑层的 NodeId（如 1001）。</param>
        /// <returns>true 表示需要反转；false 表示保持模板默认方向。</returns>
        bool ShouldReverse(ushort logicalNodeId);
    }
}