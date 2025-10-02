using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Dto {

    public sealed class DecodeRequest {

        /// <summary>HEX，如 "2A 81 82 00 E8 03 ..."（允许空格/破折号）</summary>
        public string? Hex { get; init; }

        /// <summary>Base64 字符串</summary>
        public string? Base64 { get; init; }

        /// <summary>原始字节（JSON 数组）</summary>
        public byte[]? Bytes { get; init; }
    }
}