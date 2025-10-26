using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 解码请求数据传输对象
    /// </summary>
    [SwaggerSchema(Description = "在线解码请求对象，支持 HEX、Base64 和原始字节数组三种格式")]
    public sealed class DecodeRequest {

        /// <summary>HEX，如 "2A 81 82 00 E8 03 ..."（允许空格/破折号）</summary>
        [SwaggerSchema(Description = "十六进制字符串格式的数据，如 \"2A 81 82 00 E8 03\"，允许使用空格或破折号分隔", Nullable = true)]
        public string? Hex { get; init; }

        /// <summary>Base64 字符串</summary>
        [SwaggerSchema(Description = "Base64 编码的字符串格式数据", Nullable = true)]
        public string? Base64 { get; init; }

        /// <summary>原始字节（JSON 数组）</summary>
        [SwaggerSchema(Description = "原始字节数组，以 JSON 数组形式传递", Nullable = true)]
        public byte[]? Bytes { get; init; }
    }
}