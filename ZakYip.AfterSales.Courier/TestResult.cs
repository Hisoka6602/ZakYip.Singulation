namespace ZakYip.AfterSales.Courier;

/// <summary>
/// 测试结果
/// </summary>
public class TestResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误码（如果有）
    /// </summary>
    public int? ErrorCode { get; set; }

    /// <summary>
    /// 返回数据
    /// </summary>
    public object? Data { get; set; }
}
