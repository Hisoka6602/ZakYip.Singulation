namespace ZakYip.Singulation.Host.Dto {

    /// <summary>
    /// 统一 API 响应格式
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    public class ApiResponse<T> {
        public bool Result { get; set; }
        public string Msg { get; set; } = string.Empty;
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