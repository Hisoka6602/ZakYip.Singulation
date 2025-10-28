namespace ZakYip.Singulation.Host.Configuration;

/// <summary>
/// 主机性能和资源限制常量
/// Host performance and resource limit constants
/// </summary>
public static class HostConstants
{
    /// <summary>
    /// 线程池最小工作线程数
    /// Minimum worker threads in thread pool
    /// </summary>
    public const int MinWorkerThreads = 128;

    /// <summary>
    /// 线程池最小 I/O 完成线程数
    /// Minimum I/O completion threads in thread pool
    /// </summary>
    public const int MinCompletionPortThreads = 128;

    /// <summary>
    /// 请求体最大大小（字节）
    /// Maximum request body size in bytes
    /// </summary>
    /// <remarks>
    /// 默认设置为 30GB，用于支持大文件上传
    /// Default is 30GB to support large file uploads
    /// </remarks>
    public const long MaxRequestBodySizeBytes = 30L * 1024 * 1024 * 1024; // 30GB

    /// <summary>
    /// 表单上传最大大小（字节）
    /// Maximum form upload size in bytes
    /// </summary>
    public const long MaxFormUploadSizeBytes = long.MaxValue;
}
