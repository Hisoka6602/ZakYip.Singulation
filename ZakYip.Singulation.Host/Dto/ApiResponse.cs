using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 统一 API 响应格式
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    [SwaggerSchema(Description = "统一的 API 响应格式，包含执行结果、消息和数据")]
    public record class ApiResponse<T> {
        /// <summary>操作是否成功</summary>
        [SwaggerSchema(Description = "操作是否成功，true 表示成功，false 表示失败")]
        [Required]
        public bool Result { get; set; }
        
        /// <summary>响应消息</summary>
        [SwaggerSchema(Description = "响应消息，用于向客户端提供操作结果的文字说明")]
        [Required]
        public string Msg { get; set; } = string.Empty;
        
        /// <summary>返回数据</summary>
        [SwaggerSchema(Description = "返回的数据对象，类型根据具体接口而定", Nullable = true)]
        public T? Data { get; set; }

        /// <summary>
        /// 成功响应
        /// </summary>
        public static ApiResponse<T> Success(T? data = default, string msg = "操作成功") {
            return new ApiResponse<T> { Result = true, Msg = msg, Data = data };
        }

        /// <summary>
        /// 失败响应
        /// </summary>
        public static ApiResponse<T> Fail(string msg, T? data = default) {
            return new ApiResponse<T> { Result = false, Msg = msg, Data = data };
        }

        /// <summary>
        /// 创建未找到响应
        /// </summary>
        public static ApiResponse<T> NotFound(string msg = "未找到资源", T? data = default) {
            return new ApiResponse<T> { Result = false, Msg = msg, Data = data };
        }

        /// <summary>
        /// 创建验证失败响应
        /// </summary>
        public static ApiResponse<T> Invalid(string msg, T? data = default) {
            return new ApiResponse<T> { Result = false, Msg = msg, Data = data };
        }
    }
}