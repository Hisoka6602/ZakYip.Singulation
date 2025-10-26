using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 传输事件类型。
    /// </summary>
    public enum TransportEventType {
        /// <summary>接收到数据。</summary>
        [Description("数据接收")]
        Data,

        /// <summary>字节流接收事件。</summary>
        [Description("字节流接收")]
        BytesReceived,

        /// <summary>状态变更事件。</summary>
        [Description("状态变更")]
        StateChanged,

        /// <summary>错误事件。</summary>
        [Description("错误")]
        Error
    }
}