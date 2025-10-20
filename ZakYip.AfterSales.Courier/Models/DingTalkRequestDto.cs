namespace ZakYip.AfterSales.Courier.Models;

/// <summary>
/// 钉钉 API 请求基础 DTO
/// </summary>
public class DingTalkRequestDto
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
}
