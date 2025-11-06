using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// IO 类型枚举。
    /// </summary>
    [Description("IO 类型")]
    public enum IoType {
        /// <summary>输入 IO</summary>
        [Description("输入")]
        Input = 0,

        /// <summary>输出 IO</summary>
        [Description("输出")]
        Output = 1
    }
}
