using System.ComponentModel;

namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// 编解码结果。
    /// </summary>
    public enum CodecResult {
        /// <summary>解码成功。</summary>
        [Description("解码成功")]
        Ok,

        /// <summary>需要更多数据才能完成解码。</summary>
        [Description("需要更多数据")]
        NeedMoreData,

        /// <summary>数据格式错误或损坏。</summary>
        [Description("数据格式错误")]
        Malformed,

        /// <summary>不支持的协议版本。</summary>
        [Description("不支持的协议版本")]
        UnsupportedVersion
    }
}