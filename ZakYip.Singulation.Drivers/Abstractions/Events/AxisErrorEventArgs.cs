using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Abstractions.Events {

    /// <summary>
    /// 轴运行异常事件参数。
    /// </summary>
    public sealed class AxisErrorEventArgs : EventArgs {

        public AxisErrorEventArgs(AxisId axis, Exception ex) {
            Axis = axis;
            Exception = ex;
        }

        /// <summary>发生异常的轴标识。</summary>
        public AxisId Axis { get; }

        /// <summary>具体异常对象。</summary>
        public Exception Exception { get; }
    }
}