using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Abstractions.Events {
    /// <summary>
    /// 函数发送事件参数：用于记录一次底层驱动库函数调用（如 nmc_write_rxpdo / nmc_get_node_od）。
    /// </summary>
    public sealed record AxisCommandIssuedEventArgs {
        /// <summary>轴标识。</summary>
        public required AxisId Axis { get; init; }

        /// <summary>
        /// 调用快照，例如：
        /// <c>nmc_get_node_od(8,2,1001,24722,1,32,10000)</c>
        /// </summary>
        public required string Invocation { get; init; }

        /// <summary>返回结果码（通常 0=成功）。</summary>
        public required int Result { get; init; }

        /// <summary>事件时间戳。</summary>
        public required DateTimeOffset Timestamp { get; init; }

        /// <summary>附加备注（如限幅/换算说明）。</summary>
        public string? Note { get; init; }

        public override string ToString()
            => $"{Invocation} = {Result}";
    }
}