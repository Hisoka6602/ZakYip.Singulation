using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    /// <summary>
    /// 运行时状态提供者接口，用于收集和报告系统运行状态。
    /// </summary>
    public interface IRuntimeStatusProvider {

        /// <summary>
        /// 获取系统运行时状态的快照。
        /// </summary>
        /// <returns>包含当前系统运行时状态的快照对象。</returns>
        SystemRuntimeStatus Snapshot();

        /// <summary>
        /// 通知传输层状态变化。
        /// </summary>
        /// <param name="name">传输层名称。</param>
        /// <param name="role">传输层角色（如 Client、Server）。</param>
        /// <param name="status">传输层状态（如 Connected、Disconnected）。</param>
        /// <param name="remote">远程地址（可选）。</param>
        void OnTransportState(string name, string role, string status, string? remote);

        /// <summary>
        /// 通知传输层接收到的字节数。
        /// </summary>
        /// <param name="name">传输层名称。</param>
        /// <param name="bytes">接收到的字节数。</param>
        void OnTransportBytes(string name, int bytes);

        /// <summary>
        /// 通知上游心跳事件。
        /// </summary>
        /// <param name="utc">心跳时间（UTC）。</param>
        /// <param name="fps">帧率（可选，单位：帧/秒）。</param>
        void OnUpstreamHeartbeat(DateTime utc, double? fps = null);

        /// <summary>
        /// 通知控制器信息更新。
        /// </summary>
        /// <param name="online">控制器是否在线。</param>
        /// <param name="vendor">控制器厂商名称（可选）。</param>
        /// <param name="ip">控制器IP地址（可选）。</param>
        /// <param name="axisCount">轴数量。</param>
        void OnControllerInfo(bool online, string? vendor, string? ip, int axisCount);
    }
}