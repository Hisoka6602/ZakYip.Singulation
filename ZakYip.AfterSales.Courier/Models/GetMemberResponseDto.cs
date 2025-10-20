namespace ZakYip.AfterSales.Courier.Models;

/// <summary>
/// 钉钉获取成员列表响应 DTO
/// </summary>
public class GetMemberResponseDto
{
    /// <summary>
    /// 错误码，0表示成功
    /// </summary>
    public int ErrCode { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrMsg { get; set; } = string.Empty;

    /// <summary>
    /// 成员列表
    /// </summary>
    public List<DingTalkMemberDto> UserList { get; set; } = new();

    /// <summary>
    /// 是否还有更多数据
    /// </summary>
    public bool HasMore { get; set; }
}
