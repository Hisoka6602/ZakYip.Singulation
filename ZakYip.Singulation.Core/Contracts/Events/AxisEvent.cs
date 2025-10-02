using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Core.Contracts.Events {
    /// <summary>
    /// 轴侧事件：不以字节为中心，而是以域模型为中心的简洁承载。
    /// 不引入装箱/反射，保持零分配（除异常对象本身）。
    /// </summary>
    public readonly record struct AxisEvent(
        string Source,            // 建议格式 "axis:<id>" 或 "driver:<lib>" 或 "controller"
        AxisEventType Type,       // 枚举见下
        AxisId? AxisId,           // 哪根轴（可空：驱动库级/控制器级事件时为空）
        string? Reason,           // 文本原因（断线/未加载/自检失败等）
        Exception? Exception      // 异常对象（faulted等），可空
    );
}