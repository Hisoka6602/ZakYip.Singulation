using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 日志级别。
    /// </summary>
    public enum LogKind {
        /// <summary>信息级别日志。</summary>
        [Description("信息")]
        Info,

        /// <summary>调试级别日志。</summary>
        [Description("调试")]
        Debug,

        /// <summary>警告级别日志。</summary>
        [Description("警告")]
        Warn
    }
}