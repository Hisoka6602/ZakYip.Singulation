using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// IO 状态枚举。
    /// </summary>
    [Description("IO 状态")]
    public enum IoState {
        /// <summary>低电平</summary>
        [Description("低电平")]
        Low = 0,

        /// <summary>高电平</summary>
        [Description("高电平")]
        High = 1
    }
}
