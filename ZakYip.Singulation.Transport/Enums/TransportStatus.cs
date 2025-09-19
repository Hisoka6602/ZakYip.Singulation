using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Transport.Enums {

    public enum TransportStatus {

        /// <summary>
        /// 已停止：尚未启动，或调用 Stop() 后完全退出。
        /// </summary>
        [Description("已停止")]
        Stopped = 0,

        /// <summary>
        /// 启动中：正在建立连接（Client 模式）或正在绑定端口监听（Server 模式）。
        /// </summary>
        [Description("启动中")]
        Starting = 1,

        /// <summary>
        /// 运行中：连接已建立或监听已就绪，可以正常收发字节流。
        /// </summary>
        [Description("运行中")]
        Running = 2,

        /// <summary>
        /// 故障：发生错误导致不可用，需要 Stop 后再重启。
        /// </summary>
        [Description("故障")]
        Faulted = 3
    }
}