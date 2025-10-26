using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 解码结果数据传输对象
    /// </summary>
    [SwaggerSchema(Description = "在线解码的结果对象，包含解码成功标志、帧类型和解析出的速度数据")]
    public sealed class DecodeResult {
        /// <summary>解码是否成功</summary>
        [SwaggerSchema(Description = "解码是否成功，true 表示成功解析，false 表示无法识别")]
        [Required]
        public bool Ok { get; init; }
        
        /// <summary>帧类型</summary>
        [SwaggerSchema(Description = "识别出的帧类型，如 \"speed\"（速度帧）或 \"unknown\"（未知）")]
        [Required]
        public string Kind { get; init; } = "unknown";
        
        /// <summary>原始数据长度</summary>
        [SwaggerSchema(Description = "原始输入数据的字节长度")]
        [Required]
        public int RawLength { get; init; }
        
        /// <summary>速度数据</summary>
        [SwaggerSchema(Description = "解析出的速度数据对象，包含主线速度和弹射速度信息", Nullable = true)]
        public SpeedSet? Speed { get; init; }
    }
}